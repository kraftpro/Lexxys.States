using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lexxys.States;

#pragma warning disable CA1819
#pragma warning disable CA1307 // Specify StringComparison for clarity

public class StateConfig
{
	public int? Id { get; init; }
	public string Name { get; init; }
	public string? Description { get; init; }
	public string? Guard { get; init; }
	public string? StateEnter { get; init; }
	public string? StatePassthrough { get; init; }
	public string? StateEntered { get; init; }
	public string? StateExit { get; init; }
	public string[]? Role { get; init; }
	public TransitionConfig[] Transition { get; init; }
	public StatechartConfig[] Statechart { get; init; }

	public StateConfig()
	{
		Name = String.Empty;
		Transition = Array.Empty<TransitionConfig>();
		Statechart = Array.Empty<StatechartConfig>();
	}

	public StateConfig(string name, int? id = null, string? description = null, string? guard = null, string? stateEnter = null, string? statePassthrough = null, string? stateEntered = null, string? stateExit = null, IReadOnlyCollection<string>? roles = null, IReadOnlyCollection<StatechartConfig>? statechart = null, IReadOnlyCollection<TransitionConfig>? transition = null)
	{
		if (name is null || (name = name.Trim()).Length == 0)
			throw new ArgumentNullException(nameof(name));
		(Id, Name) = FixName(id, name);
		Description = description;
		Guard = guard;
		StateEnter = stateEnter;
		StatePassthrough = statePassthrough;
		StateEntered = stateEntered;
		StateExit = stateExit;
		Role = roles?.ToArray();
		Statechart = statechart?.ToArray() ?? Array.Empty<StatechartConfig>();
		Transition = transition?.ToArray() ?? Array.Empty<TransitionConfig>();
		foreach (var item in Transition)
		{
			item.Source = Name ?? Id.ToString();
		}
	}

	public TransitionConfig[] GetTransitions()
	{
		foreach (var item in Transition)
		{
			item.Source = Name ?? Id.ToString();
		}
		return Transition;
	}

	internal State<T> Create<T>(ITokenScope scope, Func<string, IStateAction<T>?> actionBuilder, Func<string, IStateCondition<T>?> conditionBuilder, Func<string, StatechartConfig>? referenceResolver)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (actionBuilder is null)
			throw new ArgumentNullException(nameof(actionBuilder));
		if (conditionBuilder is null)
			throw new ArgumentNullException(nameof(conditionBuilder));

		var token = CreateToken(scope, Id, Name, Description);
		var state = new State<T>(
			token: token,
			guard: String.IsNullOrEmpty(Guard) ? null: conditionBuilder(Guard!),
			roles: Role,
			charts: Statechart.Length == 0 ? null: Statechart.Select(o => o.Create(scope.Scope(token), actionBuilder, conditionBuilder, referenceResolver)).ToList());

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
		return id is null ? scope.Token(name, description): scope.Token(id.GetValueOrDefault(), name, description);
	}

	internal static (int? Id, string Name) FixName(int? id, string name)
	{
#pragma warning disable CA1846 // Prefer 'AsSpan' over 'Substring'
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		if (id is null)
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
#pragma warning restore CA1846 // Prefer 'AsSpan' over 'Substring'
		}
		return (id, name);
	}

	internal void GenerateCode(TextWriter writer, string objName, string varName, string scope, string temp, Dictionary<StatechartConfig, string> chartMethods, string indent)
	{
		writer.WriteLine(TrimEnd(
			$"{indent}var {varName} = new State<{objName}>({T(scope, Id, Name, Description, Statechart.Length > 0 ? temp: null)}, {CC(Statechart, chartMethods, $"s.Scope({temp})")}, {P(Guard, objName)}, {RR(Role)}",
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
		if (id is null && name is null)
			return "null";
		var text = new StringBuilder();
		if (!String.IsNullOrEmpty(temp))
			text.Append(temp).Append(" = ");
		text.Append(scope).Append('.').Append("Token(");
		if (id is null)
			text.Append(S(name));
		else if (name is null)
			text.Append(id);
		else
			text.Append(id).Append(", ").Append(S(name));
		if (description is null)
			text.Append(')');
		else
			text.Append(", ").Append(S(description)).Append(')');
		return text.ToString();
	}

	// TODO: Handle special characters \n \x01 etc.
	internal static string S(string? value)
		=> value is null ? "null" : Strings.EscapeCsString(value);

	internal static string P(string? value, string objName)
	{
		return String.IsNullOrEmpty(value) ? "null": $"StateCondition.Create<{objName}>((obj, chart, state, transition) => {Code(value!)})";
	}

	internal static string A(string? value, string objName)
	{
		return String.IsNullOrEmpty(value) ? "null" : $"StateAction.Create<{objName}>((obj, chart, state, transition) => {Code(value!)})";
	}

	private static string Code(string value) =>
		!value.StartsWith("{", StringComparison.Ordinal) && (value.Contains('\n') || value.TrimEnd(Semicolons).Contains(';')) ?
			"{" + value + "}":
		value.StartsWith("return ", StringComparison.Ordinal) ?
			value.Substring(7).TrimEnd(Semicolons):
			value.TrimEnd(Semicolons);

	private static readonly char[] Semicolons = { ';' };

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
