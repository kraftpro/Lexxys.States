using Lexxys;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States
{
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
		public IReadOnlyCollection<StateConfig> States { get; }
		public IReadOnlyCollection<TransitionConfig> Transitions { get; }

		public StatechartConfig(int? id, string name, string? description, string? onLoad, string? onUpdate, string? chartStart, string? chartFinish, string? stateEnter, string? stateEntered, string? statePassthrough, string? stateExit, IReadOnlyCollection<StateConfig>? states, IReadOnlyCollection<TransitionConfig>? transitions)
		{
			if (name == null || (name = name.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(name));

			Id = id;
			Name = name;
			Description = description;
			OnLoad = onLoad;
			OnUpdate = onUpdate;
			ChartStart = chartStart;
			ChartFinish = chartFinish;
			StateEnter = stateEnter;
			StateEntered = stateEntered;
			StatePassthrough = statePassthrough;
			StateExit = stateExit;
			States = states ?? Array.Empty<StateConfig>();
			Transitions = transitions ?? Array.Empty<TransitionConfig>();

			StateConfig.ValidateTokens(States);
		}

		public Statechart<T> Create<T>(ITokenScope? scope = null, Func<string, IStateAction<T>>? action = null, Func<string, IStateCondition<T>>? condition = null)
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
			var transitions = Transitions.Select(o => o.Create<T>(scope, states, action, condition));
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
		}

		static Token CreateToken(ITokenScope scope, int? id, string name, string? description)
		{
			return id == null ? scope.Token(name, description) : scope.Token(id.GetValueOrDefault(), name, description);
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
			Id = id;
			Name = name;
			Description = description;
			Guard = guard;
			StateEnter = stateEnter;
			StatePassthrough = statePassthrough;
			StateEntered = stateEntered;
			StateExit = stateExit;
			Roles = roles;
			Charts = charts;
		}

		public State<T> Create<T>(ITokenScope scope, Func<string, IStateAction<T>> action, Func<string, IStateCondition<T>> condition)
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

		static Token CreateToken(ITokenScope scope, int? id, string name, string? description)
		{
			return id == null ? scope.Token(name, description): scope.Token(id.GetValueOrDefault(), name, description);
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

		public TransitionConfig(int? id, string? name, string? description, string? source, string destination, string? guard, string? action, bool continues, IReadOnlyCollection<string>? roles)
		{
			if (destination == null || (destination = destination.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(destination));
			Id = id;
			Name = name;
			Description = description;
			Source = source;
			Destination = destination;
			Guard = guard;
			Action = action;
			Continues = continues;
			Roles = roles;
		}

		public Transition<T> Create<T>(ITokenScope scope, IReadOnlyCollection<State<T>> states, Func<string, IStateAction<T>> action, Func<string, IStateCondition<T>> condition)
		{
			var transition = new Transition<T>(
				source: String.IsNullOrEmpty(Source) ? null: FindState(Source, states),
				destination: FindState(Destination, states),
				@event: CreateToken(scope, Id, Name, Description),
				continues: Continues,
				action: String.IsNullOrEmpty(Action) ? null: action(Action),
				guard: String.IsNullOrEmpty(Guard) ? null: condition(Guard),
				roles: Roles);
			return transition;
		}

		private static State<T> FindState<T>(string reference, IReadOnlyCollection<State<T>> states)
		{
			var state = states.FirstOrDefault(o => String.Equals(o.Name, reference, StringComparison.OrdinalIgnoreCase));
			if (state == null)
				throw new InvalidOperationException($"Cannot find state \"{reference}\".");
			return state;
		}

		static Token CreateToken(ITokenScope scope, int? id, string? name, string? description)
		{
			if (id != null)
				return scope.Token(id.GetValueOrDefault(), name, description, scope.Token(TokenDomain));
			if (name != null)
				return scope.Token(name, description, scope.Token(TokenDomain));
			return Token.Empty;
		}
	}
}
