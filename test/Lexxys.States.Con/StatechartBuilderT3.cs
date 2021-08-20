using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Lexxys;

namespace Lexxys.States.Con
{
	//public StatechartBuilder Begin(string name)
	//{
	//	var chart = new StatechartBuilder(StatechartBuilder.Empty.Parent);
	//	return chart;
	//}

	public static class StatechartBuilder
	{
		public static StatechartBuilder<T> Create<T>(int id, string name, string? description = null)
		{
			return new StatechartBuilder<T>(id, name, TokenFactory.Create("statechart"), description, null);
		}

		public static StatechartBuilder<T> Create<T>(string name, string? description = null)
		{
			return new StatechartBuilder<T>(name, TokenFactory.Create("statechart"), description, null);
		}
	}

	public class StatechartBuilder<TEntity>
	{
		private readonly IDictionary<Token, StateBuilder> _states;

		public StatechartBuilder(int id, string name, ITokenScope tokenScope, string? description = null, StateBuilder? parent = null)
		{
			_states = new Dictionary<Token, StateBuilder>();
			Token = tokenScope.Token(id, name, description);
			TokenScope = tokenScope.WithDomain(Token);
			Parent = parent ?? new StateBuilder(this, Token.Empty);
			States = ReadOnly.Wrap(_states);
		}

		public StatechartBuilder(string name, ITokenScope tokenScope, string? description = null, StateBuilder? parent = null)
		{
			_states = new Dictionary<Token, StateBuilder>();
			Token = tokenScope.Token(name, description);
			TokenScope = tokenScope.WithDomain(Token);
			Parent = parent ?? new StateBuilder(this, Token.Empty);
			States = ReadOnly.Wrap(_states);
		}

		public StatechartBuilder(Enum value, ITokenScope tokenScope, string? description = null, StateBuilder? parent = null)
		{
			_states = new Dictionary<Token, StateBuilder>();
			Token = tokenScope.Token(value, description);
			TokenScope = tokenScope.WithDomain(Token);
			Parent = parent ?? new StateBuilder(this, Token.Empty);
			States = ReadOnly.Wrap(_states);
		}

		public Token Token { get; }
		public ITokenScope TokenScope { get; }
		public StateBuilder Parent { get; }
		public IReadOnlyDictionary<Token, StateBuilder> States { get; }

		public StatechartBuilder<TEntity> Out(out StatechartBuilder<TEntity> value)
		{
			value = this;
			return this;
		}

		#region Events

		public event Action<TEntity, Statechart<TEntity>>? LoadAction;
		public event Action<TEntity, Statechart<TEntity>>? UpdateAction;
		public event Action<TEntity, Statechart<TEntity>>? ChartStartAction;
		public event Action<TEntity, Statechart<TEntity>>? ChartFinishAction;
		public event Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>>? StateEnterAction;
		public event Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>>? StateEnteredAction;
		public event Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>>? StatePassthroughAction;
		public event Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>>? StateExitAction;

		public StatechartBuilder<TEntity> OnLoad(Action<TEntity, Statechart<TEntity>> action)
		{
			LoadAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> OnUpdate(Action<TEntity, Statechart<TEntity>> action)
		{
			UpdateAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> ChartStart(Action<TEntity, Statechart<TEntity>> action)
		{
			ChartStartAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> ChartFinish(Action<TEntity, Statechart<TEntity>> action)
		{
			ChartFinishAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> StateEnter(Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>> action)
		{
			StateEnterAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> StateEntered(Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>> action)
		{
			StateEnteredAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> StatePassthrough(Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>> action)
		{
			StatePassthroughAction += action;
			return this;
		}
		public StatechartBuilder<TEntity> StateExit(Action<TEntity, Statechart<TEntity>, State<TEntity>, Transition<TEntity>> action)
		{
			StateExitAction += action;
			return this;
		}

		#endregion

		/// <summary>
		/// Creates a new <see cref="State"/> in the <see cref="StatechartBuilder{Token, Token, TEntity}"/>.
		/// </summary>
		/// <param name="id">State ID</param>
		/// <param name="name">State name</param>
		/// <param name="description">State description</param>
		/// <returns></returns>
		public StateBuilder State(int id, string name, string? description = null)
		{
			var t = TokenScope.Token(id, name, description);
			if (!_states.TryGetValue(t, out var state))
				_states.Add(t, state = new StateBuilder(this, t));
			return state;
		}

		public StateBuilder State(string name, string? description = null)
		{
			var t = TokenScope.Token(name, description);
			if (!_states.TryGetValue(t, out var state))
				_states.Add(t, state = new StateBuilder(this, t));
			return state;
		}

		public StateBuilder State(Enum value, string? description = null)
		{
			var t = TokenScope.Token(value, description);
			if (!_states.TryGetValue(t, out var state))
				_states.Add(t, state = new StateBuilder(this, t));
			return state;
		}

		/// <summary>
		/// Close curernt <see cref="StatechartBuilder{Token, Token, TEntity}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StateBuilder Close()
		{
			return Parent;
		}

		//public StatechartBuilder<TEntity> OnEnter(Action<TEntity, Token> action)
		//{
		//	ChartEnter += action;
		//	return this;
		//}

		//public StatechartBuilder<TEntity> OnExit(Action<TEntity, Token> action)
		//{
		//	ChartExit += action;
		//	return this;
		//}



		[DebuggerDisplay("{Id} Events.Count={Events.Count}")]
		public class StateBuilder
		{
			private readonly List<EventBuilder> _events;
			private readonly List<StatechartBuilder<TEntity>> _subcharts;
			private readonly List<string> _roles;

			public StateBuilder(StatechartBuilder<TEntity> chart, Token token)
			{
				Token = token;
				Chart = chart;
				_events = new List<EventBuilder>();
				Events = ReadOnly.Wrap(_events);
				_subcharts = new List<StatechartBuilder<TEntity>>();
				Subcharts = ReadOnly.Wrap(_subcharts);
				_roles = new List<string>();
				Roles = ReadOnly.Wrap(_roles);
				TokenScope = chart.TokenScope.WithDomain(token);
			}

			public ITokenScope TokenScope { get; }

			public StatechartBuilder<TEntity> Chart { get; }
			public Token Token { get; }
			public IReadOnlyCollection<string> Roles { get; }
			public IReadOnlyCollection<EventBuilder> Events { get; }
			public IReadOnlyCollection<StatechartBuilder<TEntity>> Subcharts { get; }

			public event Action<TEntity, State<TEntity>, Transition<TEntity>>? StateEnter;
			public event Action<TEntity, State<TEntity>, Transition<TEntity>>? StatePassthrough;
			public event Action<TEntity, State<TEntity>, Transition<TEntity>>? StateEntered;
			public event Action<TEntity, State<TEntity>, Transition<TEntity>>? StateExit;

			public StateBuilder Ref(out StateBuilder value)
			{
				value = this;
				return this;
			}

			public StateBuilder OnEnter(Action<TEntity, State<TEntity>, Transition<TEntity>> action)
			{
				StateEnter += action;
				return this;
			}

			public StateBuilder OnPassthrough(Action<TEntity, State<TEntity>, Transition<TEntity>> action)
			{
				StatePassthrough += action;
				return this;
			}

			public StateBuilder OnEntered(Action<TEntity, State<TEntity>, Transition<TEntity>> action)
			{
				StateEntered += action;
				return this;
			}

			public StateBuilder OnExit(Action<TEntity, State<TEntity>, Transition<TEntity>> action)
			{
				StateExit += action;
				return this;
			}

			public StateBuilder Role(string role)
			{
				_roles.Add(role);
				return this;
			}

			public StatechartBuilder<TEntity> Begin(int id, string name, string? description)
			{
				var chart = new StatechartBuilder<TEntity>(id, name, TokenScope, description, this);
				_subcharts.Add(chart);
				return chart;
			}

			public StatechartBuilder<TEntity> Begin(string name, string? description)
			{
				var chart = new StatechartBuilder<TEntity>(name, TokenScope, description, this);
				_subcharts.Add(chart);
				return chart;
			}

			public StatechartBuilder<TEntity> Begin(Enum value, string? description)
			{
				var chart = new StatechartBuilder<TEntity>(value, TokenScope, description, this);
				_subcharts.Add(chart);
				return chart;
			}

			public StateBuilder End() => Chart.Parent;

			public StateBuilder OnEnter(Action<TEntity> action) => OnEnter((o, _, _) => action?.Invoke(o));
			public StateBuilder OnPassthrough(Action<TEntity> action) => OnPassthrough((o, _, _) => action?.Invoke(o));
			public StateBuilder OnEntered(Action<TEntity> action) => OnEntered((o, _, _) => action?.Invoke(o));
			public StateBuilder OnExit(Action<TEntity> action) => OnExit((o, _, _) => action?.Invoke(o));

			public StateBuilder State(int id, string name, string? description = null) => Chart.State(id, name, description);
			public StateBuilder State(string name, string? description = null) => Chart.State(name, description);
			public StateBuilder State(Enum value, string? description = null) => Chart.State(value, description);

			public EventBuilder When(int id, string name, string? description = null) => When(Chart.TokenScope.Token(id, name, description));
			public EventBuilder When(string name, string? description = null) => When(Chart.TokenScope.Token(name, description));
			public EventBuilder When(Enum value, string? description = null) => When(Chart.TokenScope.Token(value, description));
			public GuardBuilder When(Func<TEntity, bool> condition) => When((Token?)null).And(condition);

			private EventBuilder When(Token? command)
			{
				var e = new EventBuilder(this, command);
				_events.Add(e);
				return e;
			}

			public StatechartBuilder<TEntity> Close() => Chart;
		}

		[DebuggerDisplay("{Source.Id} -> {Target.Id}")]
		public class TransitionBuilder
		{
			public TransitionBuilder(StateBuilder source, StateReference target)
			{
				Source = source;
				Target = target;
			}

			public StateBuilder Source { get; }
			public StateReference Target { get; }
			public bool ContinuesFlag { get; private set; }

			public Action<Token, Token, TEntity>? TransitionAction;

			public TransitionBuilder Ref(out TransitionBuilder value)
			{
				value = this;
				return this;
			}

			public TransitionBuilder Continues()
			{
				ContinuesFlag = true;
				return this;
			}

			public StateBuilder State(int id, string name, string? description = null) => Source.Chart.State(id, name, description);
			public StateBuilder State(string name, string? description = null) => Source.Chart.State(name, description);
			public StateBuilder State(Enum value, string? description = null) => Source.Chart.State(value, description);

			public EventBuilder When(int id, string name, string? description = null) => Source.When(id, name, description);
			public EventBuilder When(string name, string? description = null) => Source.When(name, description);
			public EventBuilder When(Enum value, string? description = null) => Source.When(value, description);
			public GuardBuilder When(Func<TEntity, bool> condition) => Source.When(condition);

			public TransitionBuilder Action(Action<Token, Token, TEntity> action)
			{
				TransitionAction += action;
				return this;
			}
			public TransitionBuilder Action(Action<Token, TEntity> action) => Action((_, e, o) => action?.Invoke(e, o));
			public TransitionBuilder Action(Action<TEntity> action) => Action((_, e, o) => action?.Invoke(o));
			public TransitionBuilder Action(Action action) => Action((_, e, o) => action?.Invoke());
		}

		[DebuggerDisplay("When {Event}")]
		public class EventBuilder
		{
			internal EventBuilder(StateBuilder node, Token? @event = null)
			{
				Node = node;
				Event = @event ?? Token.Empty;
			}

			public Token Event { get; }
			public StateBuilder Node { get; }
			public GuardBuilder? Guard { get; private set; }

			public GuardBuilder And(Func<TEntity, bool> condition, Func<TEntity, Task<bool>>? asyncCondition = null) => Guard = new GuardBuilder(Node, condition, asyncCondition);
			public GuardBuilder And(Func<TEntity, Task<bool>>? asyncCondition) => Guard = new GuardBuilder(Node, null, asyncCondition);

			public TransitionBuilder GoTo(int id, string? name = null) => (Guard = new GuardBuilder(Node)).GoTo(id, name);
			public TransitionBuilder GoTo(string name) => (Guard = new GuardBuilder(Node)).GoTo(name);
			public TransitionBuilder GoTo(Enum value) => (Guard = new GuardBuilder(Node)).GoTo(value);
		}

		[DebuggerDisplay("Guard for {Transition.Target.Id}")]
		public class GuardBuilder
		{
			internal GuardBuilder(StateBuilder node, Func<TEntity, bool>? condition = null, Func<TEntity, Task<bool>>? asyncCondition = null)
			{
				Node = node;
				Condition = condition;
				AsyncCondition = asyncCondition;
			}

			public StateBuilder Node { get; }
			public Func<TEntity, bool>? Condition { get; }
			public Func<TEntity, Task<bool>>? AsyncCondition { get; }
			public TransitionBuilder? Transition { get; private set; }

			public TransitionBuilder GoTo(int id, string? name = null) => Transition = new TransitionBuilder(Node, new StateReference(id, name));
			public TransitionBuilder GoTo(string name) => Transition = new TransitionBuilder(Node, new StateReference(name));
			public TransitionBuilder GoTo(Enum value) => Transition = new TransitionBuilder(Node, new StateReference(value));
		}

		public class StateReference
		{
			int? Id { get; }
			string? Name { get; }

			public StateReference(int id, string? name = null)
			{
				Id = id;
				Name = name;
			}

			public StateReference(string name)
			{
				Name = name;
			}

			public StateReference(Enum value)
			{
				Id = ((IConvertible)value).ToInt32(null);
				Name = ((IConvertible)value).ToString(null);
			}
		}
	}
}
