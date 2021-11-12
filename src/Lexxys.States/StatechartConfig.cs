using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States
{
	using Xml;

	public class StatechartConfig
	{
		public int? Id { get; }
		public string Name { get; }
		public string? Description { get; }
		public string? OnLoad { get; }
		public string? OnUpdate { get; }
		public string? ChartStart { get; }
		public string? ChartFinish { get; }
		public string? StateEnter { get; }
		public string? StateEntered { get; }
		public string? StatePassthrough { get; }
		public string? StateExit { get; }
		public string? InitialState { get; }
		public IReadOnlyCollection<StateConfig> States { get; }
		public IReadOnlyCollection<TransitionConfig> Transitions { get; }

		public StatechartConfig(int? id, string name, string? description, string? onLoad, string? onUpdate, string? chartStart, string? chartFinish, string? stateEnter, string? stateEntered, string? statePassthrough, string? stateExit, string? initialState, IReadOnlyCollection<StateConfig>? states, IReadOnlyCollection<TransitionConfig>? transitions)
		{
			if (name == null || (name = name.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(name));

			(Id, Name) = FixName(id, name);
			Description = description;
			OnLoad = onLoad;
			OnUpdate = onUpdate;
			ChartStart = chartStart;
			ChartFinish = chartFinish;
			StateEnter = stateEnter;
			StateEntered = stateEntered;
			StatePassthrough = statePassthrough;
			StateExit = stateExit;
			InitialState = initialState;
			States = states ?? Array.Empty<StateConfig>();
			Transitions = transitions ?? Array.Empty<TransitionConfig>();

			StateConfig.ValidateTokens(States);
		}

		public Statechart<T> Create<T>(ITokenFactory? scope = null, Func<string, IStateAction<T>>? action = null, Func<string, IStateCondition<T>>? condition = null)
		{
			if (String.IsNullOrEmpty(Name))
				throw new ArgumentNullException(nameof(scope));
			if (scope == null)
				scope = TokenFactory.Create("statechart");
			if (action == null)
				action = StateAction.CSharpScript<T>;
			if (condition == null)
				condition = StateCondition.CSharpScript<T>;
			var token = CreateToken(scope, Id, Name, Description);
			scope = scope.WithDomain(token);

			var states = States.Select(o => o.Create<T>(scope, action, condition)).ToList();
			var transitions = WithInitial().Select(o => o.Create<T>(scope, states, action, condition));
			var chart = new Statechart<T>(token, states, transitions);

			if (!String.IsNullOrEmpty(OnLoad))
				chart.OnLoad += action(OnLoad);
			if (!String.IsNullOrEmpty(OnUpdate))
				chart.OnUpdate += action(OnUpdate);
			if (!String.IsNullOrEmpty(ChartStart))
				chart.ChartStart += action(ChartStart);
			if (!String.IsNullOrEmpty(ChartFinish))
				chart.ChartFinish += action(ChartFinish);
			if (!String.IsNullOrEmpty(StateEnter))
				chart.StateEnter += action(StateEnter);
			if (!String.IsNullOrEmpty(StateEntered))
				chart.StateEntered += action(StateEntered);
			if (!String.IsNullOrEmpty(StatePassthrough))
				chart.StatePassthrough += action(StatePassthrough);
			if (!String.IsNullOrEmpty(StateExit))
				chart.StateExit += action(StateExit);
			return chart;

			static Token CreateToken(ITokenFactory scope, int? id, string name, string? description)
				=> id == null ? scope.Token(name, description) : scope.Token(id.GetValueOrDefault(), name, description);
		}

		private IEnumerable<TransitionConfig> WithInitial()
		{
			if (States.Count == 0)
				return Transitions;

			var first = States.First();
			var initial = !String.IsNullOrEmpty(InitialState) ? InitialState: first.Id?.ToString() ?? first.Name;
			return Transitions.Prepend(new TransitionConfig(destination: initial));
		}

		public void GenerateCode(TextWriter writer, string objName, string? methodBase = null, string? visibility = null, bool nullable = false, string? indent = null, string? tab = null)
		{
			if (indent == null)
				indent = "\t\t";
			if (tab == null)
				tab = "\t";
			if (visibility == null)
				visibility = "internal";
			string q = nullable ? "?": "";
			string methodName = (methodBase ?? "Create") + Name;
			var stateNames = new Dictionary<StateConfig, string>();
			var transitionNames = new Dictionary<TransitionConfig, string>();
			var chartMethods = States.SelectMany(o => o.Charts ?? Array.Empty<StatechartConfig>()).ToDictionary(o => o, o => methodName + "_" + o.Name);

			writer.WriteLine($"{indent}{visibility} static Statechart<{objName}> {methodName}(ITokenScope{q} root = null)");
			writer.WriteLine($"{indent}{{");
			var indent0 = indent;
			indent += tab;
			writer.WriteLine($"{indent}if (root == null)");
			writer.WriteLine($"{indent}{tab}root = TokenFactory.Create(\"statechart\");");
			writer.WriteLine($"{indent}var token = root.Token({S(Name)});");
			writer.WriteLine($"{indent}var s = root.WithDomain(token);");
			writer.WriteLine($"{indent}var t = s.WithTransitionDomain();");
			writer.WriteLine();

			int index = 0;
			foreach (var state in States)
			{
				string name = $"s{++index}";
				stateNames.Add(state, name);
				state.GenerateCode(writer, objName, name, chartMethods, indent);
			}

			index = 0;
			foreach (var transition in WithInitial())
			{
				string name = $"t{++index}";
				transition.GenerateCode(writer, objName, name, stateNames, indent);
				transitionNames.Add(transition, name);
			}

			writer.WriteLine($"{indent}var statechart = new Statechart<{objName}>(token, {AA(stateNames.Values)}, {AA(transitionNames.Values)});");
			if (!String.IsNullOrEmpty(OnLoad))
				writer.WriteLine($"{indent}statechart.OnLoad += {A(OnLoad, objName)};");
			if (!String.IsNullOrEmpty(OnUpdate))
				writer.WriteLine($"{indent}statechart.OnUpdate += {A(OnUpdate, objName)};");
			if (!String.IsNullOrEmpty(ChartStart))
				writer.WriteLine($"{indent}statechart.ChartStart += {A(ChartStart, objName)};");
			if (!String.IsNullOrEmpty(ChartFinish))
				writer.WriteLine($"{indent}statechart.ChartFinish += {A(ChartFinish, objName)};");
			if (!String.IsNullOrEmpty(StateEnter))
				writer.WriteLine($"{indent}statechart.StateEnter += {A(StateEnter, objName)};");
			if (!String.IsNullOrEmpty(StatePassthrough))
				writer.WriteLine($"{indent}statechart.StatePassthrough += {A(StatePassthrough, objName)};");
			if (!String.IsNullOrEmpty(StateEntered))
				writer.WriteLine($"{indent}statechart.StateEntered += {A(StateEntered, objName)};");
			if (!String.IsNullOrEmpty(StateExit))
				writer.WriteLine($"{indent}statechart.StateExit += {A(StateExit, objName)};");

			writer.WriteLine($"{indent}return statechart;");
			writer.WriteLine($"{indent0}}}");

			foreach (var s in States)
			{
				if (s.Charts?.Count > 0)
				{
					foreach (var chart in s.Charts)
					{
						writer.WriteLine();
						chart.GenerateCode(writer, objName, methodName + "_", "private", nullable, indent0, tab);
					}
				}
			}

			static string AA(IEnumerable<string> values) => values?.Any() == true ? "new[] { " + String.Join(", ", values) + " }": "null";
		}

		internal static (int? Id, string Name) FixName(int? id, string name) => StateConfig.FixName(id, name);

		private static string A(string value, string objName) => StateConfig.A(value, objName);
		private static string S(string value) => StateConfig.S(value);

		public static StatechartConfig? FromXml(XmlLiteNode node)
		{
			if (node == null || node.IsEmpty)
				return null;

			List<StateConfig> states = new List<StateConfig>();
			List<TransitionConfig> transitions = new List<TransitionConfig>();

			foreach (var state in node.Where("state"))
			{
				List<StatechartConfig>? charts = null;
				foreach (var xml in state.Where("statechart"))
				{
					var chart = FromXml(xml);
					if (chart != null)
					{
						if (charts == null)
							charts = new List<StatechartConfig>();
						charts.Add(chart);
					}
				}
				var stateConfig = new StateConfig(
					id: state["id"].AsInt32(null),
					name: state["name"],
					description: state["description"],
					guard: state["guard"],
					stateEnter: state["stateEnter"],
					statePassthrough: state["statePassthrough"],
					stateEntered: state["stateEntered"],
					stateExit: state["stateExit"],
					roles: state["role"]?.Split(',').Select(o => o.Trim()).Where(o => !String.IsNullOrEmpty(o)).ToList(),
					charts: charts
					);
				states.Add(stateConfig);

				foreach (var transition in state.Where("transition"))
				{
					if (String.IsNullOrEmpty(transition["destination"]))
						continue;
					var transitionConfig = new TransitionConfig(
						id: transition["id"].AsInt32(null),
						name: transition["name"] ?? transition["event"],
						description: transition["description"],
						source: state["name"] ?? state["id"],
						destination: transition["destination"] ?? transition["target"],
						guard: transition["guard"],
						action: transition["action"],
						continues: transition["continues"].AsBoolean(false),
						roles: transition["role"]?.Split(',').Select(o => o.Trim()).Where(o => !String.IsNullOrEmpty(o)).ToList()
						);
					transitions.Add(transitionConfig);
				}
			}

			return new StatechartConfig(
				id: node["id"].AsInt32(null),
				name: node["name"] ?? node.Name,
				description: node["description"],
				onLoad: node["onLoad"],
				onUpdate: node["onUpdate"],
				chartStart: node["chartStart"],
				chartFinish: node["chartFinish"],
				stateEnter: node["stateEnter"],
				stateEntered: node["stateEntered"],
				statePassthrough: node["statePassthrough"],
				stateExit: node["stateExit"],
				initialState: node["initialState"],
				states: states,
				transitions: transitions);
		}
	}

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

		public StateConfig(int? id, string name, string? description, string? guard, string? stateEnter, string? statePassthrough, string? stateEntered, string? stateExit, IReadOnlyCollection<string>? roles, IReadOnlyCollection<StatechartConfig>? charts)
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

		public State<T> Create<T>(ITokenFactory scope, Func<string, IStateAction<T>> action, Func<string, IStateCondition<T>> condition)
		{
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (condition == null)
				throw new ArgumentNullException(nameof(condition));

			var token = CreateToken(scope, Id, Name, Description);
			var state = new State<T>(
				token: token,
				guard: String.IsNullOrEmpty(Guard) ? null: condition(Guard),
				roles: Roles,
				charts: Charts?.Select(o => o.Create<T>(scope.WithDomain(token), action, condition)).ToList());

			if (!String.IsNullOrEmpty(StateEnter))
				state.StateEnter += action(StateEnter);
			if (!String.IsNullOrEmpty(StatePassthrough))
				state.StatePassthrough += action(StatePassthrough);
			if (!String.IsNullOrEmpty(StateEntered))
				state.StateEntered += action(StateEntered);
			if (!String.IsNullOrEmpty(StateExit))
				state.StateExit += action(StateExit);
			return state;
		}

		public static void ValidateTokens(IReadOnlyCollection<StateConfig> states)
		{
			var dupId = states.Where(o => o.Id.HasValue).GroupBy(o => o.Id).FirstOrDefault(o => o.Count() > 1);
			if (dupId != null)
				throw new InvalidOperationException($"State ID value is not unique ({dupId.Key})");
			var dupName = states.Where(o => o.Name != null).GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(o => o.Count() > 1);
			if (dupName != null)
				throw new InvalidOperationException($"State Name value is not unique ({dupName.Key})");
		}

		static Token CreateToken(ITokenFactory scope, int? id, string name, string? description)
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
				else if (name.EndsWith(')') && (i = name.IndexOf('(')) > 0)
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

		internal void GenerateCode(TextWriter writer, string objName, string varName, Dictionary<StatechartConfig, string> chartMethods, string indent)
		{
			writer.WriteLine(TrimEnd(
				$"{indent}var {varName} = new State<{objName}>({T("s", Id, Name, Description)}, {CC(Charts, chartMethods)}, {P(Guard, objName)}, {RR(Roles)}",
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

		internal static string T(string scope, int? id, string? name, string? description)
		{
			if (id == null && name == null)
				return "null";
			var text = new StringBuilder();
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
			=> value == null ? "null" : "\"" + value.Replace("\"", "\\\"") + "\"";

		internal static string P(string? value, string objName)
			=> String.IsNullOrEmpty(value) ? "null" : $"StateCondition.Create<{objName}>((obj, chart, state, transition) => " + (value.Contains('\n') ? "{ " + value + " }" : value.StartsWith("return ") ? value.Substring(7).TrimEnd(';'): value.TrimEnd(';')) + ")";

		internal static string A(string? value, string objName)
			=> String.IsNullOrEmpty(value) ? "null" : $"StateAction.Create<{objName}>((obj, chart, state, transition) => " + (value.Contains('\n') ? "{ " + value + " }" : value.TrimEnd(';')) + ")";

		internal static string RR(IReadOnlyCollection<string>? roles)
			=> roles?.Count > 0 ? "new[] { " + String.Join(", ", roles.Select(o => S(o))) + " }" : "null";

		internal static string CC(IReadOnlyCollection<StatechartConfig>? charts, Dictionary<StatechartConfig, string> chartMethods)
			=> charts?.Count > 0 ? "new[] { " + String.Join(", ", charts.Select(o => $"{chartMethods[o]}(s)")) + " }" : "null";

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

	public class TransitionConfig
	{
		public const string TokenDomain = "^";

		public int? Id { get; }
		public string? Name { get; }
		public string? Description { get; }
		public string? Source { get; }
		public string Destination { get; }
		public string? Guard { get; }
		public string? Action { get; }
		public bool Continues { get; }
		public IReadOnlyCollection<string>? Roles { get; }

		public TransitionConfig(string destination)
		{
			Destination = destination;
		}

		public TransitionConfig(int? id, string? name, string? description, string? source, string destination, string? guard, string? action, bool continues, IReadOnlyCollection<string>? roles)
		{
			if (destination == null || (destination = destination.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(destination));
			(Id, Name) = name == null ? (id, name): FixName(id, name);
			Description = description;
			Source = source;
			Destination = destination;
			Guard = guard;
			Action = action;
			Continues = continues;
			Roles = roles;
		}

		public Transition<T> Create<T>(ITokenFactory scope, IReadOnlyCollection<State<T>> states, Func<string, IStateAction<T>> action, Func<string, IStateCondition<T>> condition)
		{
			var transition = new Transition<T>(
				source: IsEmptyReference(Source) ? null: FindState(Source!, states),
				destination: FindState(Destination, states),
				@event: CreateToken(scope, Id, Name, Description),
				continues: Continues,
				action: String.IsNullOrEmpty(Action) ? null: action(Action),
				guard: String.IsNullOrEmpty(Guard) ? null: condition(Guard),
				roles: Roles);
			return transition;
		}

		internal static (int? Id, string Name) FixName(int? id, string name) => StateConfig.FixName(id, name);

		internal static bool IsEmptyReference(string? reference) => String.IsNullOrEmpty(reference) || reference == ".";

		internal static State<T> FindState<T>(string reference, IReadOnlyCollection<State<T>> states)
		{
			int? intReference = int.TryParse(reference, out var id) ? id: null;
			var state = states.FirstOrDefault(o => String.Equals(o.Name, reference, StringComparison.OrdinalIgnoreCase) || o.Token.Id == intReference);
			if (state == null)
				throw new InvalidOperationException($"Cannot find state \"{reference}\".");
			return state;
		}

		internal void GenerateCode(TextWriter writer, string objName, string varName, Dictionary<StateConfig, string> stateNames, string indent)
		{
			var source = FindState(Source, objName, stateNames);
			var destination = FindState(Destination, objName, stateNames);

			writer.WriteLine(TrimEnd(
				$"{indent}var {varName} = new Transition<{objName}>({source}, {destination}, {T("t", Id, Name, Description)}, {(Continues ? "true" : "false")}, {A(Action, objName)}, {P(Guard, objName)}, {RR(Roles)}",
				", null, false, null, null, null") + ");");
		}

		private static string FindState(string? reference, string objName, Dictionary<StateConfig, string> states)
		{
			if (IsEmptyReference(reference))
				return $"State<{objName}>.Empty";
			int? intReference = int.TryParse(reference, out var id) ? id : null;
			var state = states.FirstOrDefault(o => String.Equals(o.Key.Name, reference, StringComparison.OrdinalIgnoreCase) || (intReference != null && o.Key.Id == intReference));
			return state.Value ?? $"\"Cannot find state variable for '{reference}'.\"";
		}

		static Token CreateToken(ITokenFactory scope, int? id, string? name, string? description)
		{
			if (id != null)
				return scope.Token(id.GetValueOrDefault(), name, description, scope.Token(TokenDomain));
			if (name != null)
				return scope.Token(name, description, scope.Token(TokenDomain));
			return Token.Empty;
		}

		private static string TrimEnd(string value, string defaults) => StateConfig.TrimEnd(value, defaults);
		private static string T(string scope, int? id, string? name, string? description) => StateConfig.T(scope, id, name, description);
		private static string P(string? value, string objName) => StateConfig.P(value, objName);
		private static string A(string? value, string objName) => StateConfig.A(value, objName);
		private static string RR(IReadOnlyCollection<string>? roles) => StateConfig.RR(roles);
	}
}
