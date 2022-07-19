using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lexxys.States;

public class StateConfig
{
	public int? Id { get; }
	public string Name { get; }
	public string? Description { get; }
	public string? Guard { get; }
	public string? StateEnter { get; }
	public string? StatePassthrough { get; }
	public string? StateEntered { get; }
	public string? StateExit { get; }
	public IReadOnlyCollection<string>? Roles { get; }
	public IReadOnlyCollection<StatechartConfig>? Charts { get; }

	public StateConfig(string name, int? id, string? description, string? guard, string? stateEnter, string? statePassthrough, string? stateEntered, string? stateExit, IReadOnlyCollection<string>? roles, IReadOnlyCollection<StatechartConfig>? charts)
	{
		if (name == null || (name = name.Trim()).Length == 0)
			throw new ArgumentNullException(nameof(name));
		(Id, Name) = FixName(id, name);
		Description = description;
		Guard = guard;
		StateEnter = stateEnter;
		StatePassthrough = statePassthrough;
		StateEntered = stateEntered;
		StateExit = stateExit;
		Roles = roles;
		Charts = charts;
	}

	internal State<T> Create<T>(ITokenScope scope, Func<string, IStateAction<T>?> actionBuilder, Func<string, IStateCondition<T>?> conditionBuilder, Func<string, StatechartConfig>? referenceResolver)
	{
		if (scope == null)
			throw new ArgumentNullException(nameof(scope));
		if (actionBuilder == null)
			throw new ArgumentNullException(nameof(actionBuilder));
		if (conditionBuilder == null)
			throw new ArgumentNullException(nameof(conditionBuilder));

		var token = CreateToken(scope, Id, Name, Description);
		var state = new State<T>(
			token: token,
			guard: String.IsNullOrEmpty(Guard) ? null: conditionBuilder(Guard!),
			roles: Roles,
			charts: Charts?.Select(o => o.Create<T>(scope.WithDomain(token), actionBuilder, conditionBuilder, referenceResolver)).ToList());

		if (!String.IsNullOrEmpty(StateEnter))
			state.StateEnter += actionBuilder(StateEnter!);
		if (!String.IsNullOrEmpty(StatePassthrough))
			state.StatePassthrough += actionBuilder(StatePassthrough!);
		if (!String.IsNullOrEmpty(StateEntered))
			state.StateEntered += actionBuilder(StateEntered!);
		if (!String.IsNullOrEmpty(StateExit))
			state.StateExit += actionBuilder(StateExit!);
		return state;
	}

	static Token CreateToken(ITokenScope scope, int? id, string name, string? description)
	{
		return id == null ? scope.Token(name, description): scope.Token(id.GetValueOrDefault(), name, description);
	}

	internal static (int? Id, string Name) FixName(int? id, string name)
	{
		if (name == null)
			throw new ArgumentNullException(nameof(name));

		if (id == null)
		{
			int i = name.IndexOf('.');
			if (i >= 0)
			{
				if (Int32.TryParse(name.Substring(i + 1), out var x))
				{
					id = x;
					name = name.Substring(0, i).Trim();
				}
			}
			else if (name.Length > 0 && name[name.Length - 1] == ')' && (i = name.IndexOf('(')) > 0)
			{
				if (Int32.TryParse(name.Substring(i + 1, name.Length - i - 2), out var x))
				{
					id = x;
					name = name.Substring(0, i).Trim();
				}
			}
		}
		return (id, name);
	}

	internal void GenerateCode(TextWriter writer, string objName, string varName, string scope, string temp, Dictionary<StatechartConfig, string> chartMethods, string indent)
	{
		writer.WriteLine(TrimEnd(
			$"{indent}var {varName} = new State<{objName}>({T(scope, Id, Name, Description, Charts?.Count > 0 ? temp: null)}, {CC(Charts, chartMethods, $"s.WithDomain({temp})")}, {P(Guard, objName)}, {RR(Roles)}",
			", null, null, null") + ");");
		if (!String.IsNullOrEmpty(StateEnter))
			writer.WriteLine($"{indent}{varName}.StateEnter += {A(StateEnter, objName)};");
		if (!String.IsNullOrEmpty(StatePassthrough))
			writer.WriteLine($"{indent}{varName}.StatePassthrough += {A(StatePassthrough, objName)};");
		if (!String.IsNullOrEmpty(StateEntered))
			writer.WriteLine($"{indent}{varName}.StateEntered += {A(StateEntered, objName)};");
		if (!String.IsNullOrEmpty(StateExit))
			writer.WriteLine($"{indent}{varName}.StateExit += {A(StateExit, objName)};");
	}

	internal static string T(string scope, int? id, string? name, string? description, string? temp = null)
	{
		if (id == null && name == null)
			return "null";
		var text = new StringBuilder();
		if (!String.IsNullOrEmpty(temp))
			text.Append(temp).Append(" = ");
		text.Append(scope).Append('.').Append("Token(");
		if (id == null)
			text.Append(S(name));
		else if (name == null)
			text.Append(id);
		else
			text.Append(id).Append(", ").Append(S(name));
		if (description == null)
			text.Append(')');
		else
			text.Append(", ").Append(S(description)).Append(')');
		return text.ToString();
	}

	// TODO: Handle special characters \n \x01 etc.
	internal static string S(string? value)
		=> value == null ? "null" : Strings.EscapeCsString(value);

	internal static string P(string? value, string objName)
		=> String.IsNullOrEmpty(value) ? "null" : $"StateCondition.Create<{objName}>((obj, chart, state, transition) => " + (value.Contains('\n') ? "{ " + value + " }" : value!.StartsWith("return ", StringComparison.Ordinal) ? value.Substring(7).TrimEnd(';'): value.TrimEnd(';')) + ")";

	internal static string A(string? value, string objName)
		=> String.IsNullOrEmpty(value) ? "null" : $"StateAction.Create<{objName}>((obj, chart, state, transition) => " + (value.Contains('\n') ? "{ " + value + " }" : value!.TrimEnd(';')) + ")";

	internal static string RR(IReadOnlyCollection<string>? roles)
		=> roles?.Count > 0 ? "new[] { " + String.Join(", ", roles.Select(o => S(o))) + " }" : "null";

	internal static string CC(IReadOnlyCollection<StatechartConfig>? charts, Dictionary<StatechartConfig, string> chartMethods, string scope)
		=> charts?.Count > 0 ? "new[] { " + String.Join(", ", charts.Select(o => $"{chartMethods[o]}({scope})")) + " }" : "null";

	internal static string TrimEnd(string value, string defaults)
	{
		for (int i = value.Length - 1, j = defaults.Length - 1; i >= 0 && j >= 0; --i, --j)
		{
			if (value[i] != defaults[j])
			{
				int k = defaults.IndexOf(',', j);
				if (k < 0)
					return value;
				return value.Substring(0, value.Length - (defaults.Length - k));
			}
		}
		return value.Substring(0, value.Length - defaults.Length);
	}
}
