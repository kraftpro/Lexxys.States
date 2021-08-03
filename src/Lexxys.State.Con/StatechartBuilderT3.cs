using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace Lexxys.States
{
	//public StatechartBuilder Begin(string name)
	//{
	//	var chart = new StatechartBuilder(StatechartBuilder.Empty.Parent);
	//	return chart;
	//}

	public class StatechartBuilder<TEntity>
	{
		private static readonly StatechartBuilder<TEntity> Empty = new StatechartBuilder<TEntity>();
		private readonly Dictionary<Token, StateBuilder> _states;

		private StatechartBuilder()
		{
			Token = Token.Empty;
			Parent = new StateBuilder(this, Token.Empty);
			_states = new Dictionary<Token, StateBuilder>();
			States = ReadOnly.Wrap(_states);
		}

		public StatechartBuilder(Token token)
		{
			Token = token;
			Parent = Empty.Parent;
			_states = new Dictionary<Token, StateBuilder>();
			States = ReadOnly.Wrap(_states);
		}

		private StatechartBuilder(Token token, StateBuilder parent)
		{
			Token = token;
			Parent = parent;
			_states = new Dictionary<Token, StateBuilder>();
			States = ReadOnly.Wrap(_states);
		}

		public Token Token { get; }
		public StateBuilder Parent { get; }
		public IReadOnlyDictionary<Token, StateBuilder> States { get; }

		public bool IsEmpty => this == Empty;

		//public event Action<TEntity, Token>? ChartEnter;
		//public event Action<TEntity, Token>? ChartExit;

		/// <summary>
		/// Creates a new <see cref="State"/> in the <see cref="StatechartBuilder{Token, Token, TEntity}"/>.
		/// </summary>
		/// <param name="token">State ID</param>
		/// <returns></returns>
		public StateBuilder State(Token token)
		{
			if (!_states.TryGetValue(token, out var state))
				_states.Add(token, state = new StateBuilder(this, token));
			return state;
		}

		/// <summary>
		/// Creates a new <see cref="State"/> in the <see cref="StatechartBuilder{Token, Token, TEntity}"/>.
		/// </summary>
		/// <param name="token">State ID</param>
		/// <param name="state">Created state to be used in the future references</param>
		/// <returns></returns>
		public StateBuilder State(Token token, out StateBuilder state)
		{
			if (!_states.TryGetValue(token, out state!))
				_states.Add(token, state = new StateBuilder(this, token));
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

			public StateBuilder(StatechartBuilder<TEntity> chart, Token token)
			{
				Token = token;
				Chart = chart;
				_events = new List<EventBuilder>();
				Events = ReadOnly.Wrap(_events);
				_subcharts = new List<StatechartBuilder<TEntity>>();
				Subcharts = ReadOnly.Wrap(_subcharts);
			}

			public StatechartBuilder<TEntity> Chart { get; }
			public Token Token { get; }
			public string[]? Roles { get; }
			public IReadOnlyCollection<EventBuilder> Events { get; }
			public IReadOnlyCollection<StatechartBuilder<TEntity>> Subcharts { get; }

			public Action<TEntity, Token>? StateEntering;
			public Action<TEntity, Token>? StateEnter;
			public Action<TEntity, Token>? StatePassthrough;
			public Action<TEntity, Token>? StateExit;
			public Func<TEntity, Token, bool>? StateGuard;

			public bool IsEmpty => Chart.IsEmpty;

			public StateBuilder OnEntering(Action<TEntity, Token> action)
			{
				StateEntering += action;
				return this;
			}

			public StateBuilder OnEnter(Action<TEntity, Token> action)
			{
				StateEnter += action;
				return this;
			}

			public StateBuilder OnPassthrough(Action<TEntity, Token> action)
			{
				StatePassthrough += action;
				return this;
			}

			public StateBuilder OnExit(Action<TEntity, Token> action)
			{
				StateExit += action;
				return this;
			}

			public StateBuilder Guard(Func<TEntity, Token, bool> condition)
			{
				StateGuard += condition;
				return this;
			}

			public StatechartBuilder<TEntity> Begin(Token token)
			{
				var chart = new StatechartBuilder<TEntity>(token, this);
				_subcharts.Add(chart);
				return chart;
			}

			public StateBuilder End() => Chart.Parent;

			public StateBuilder OnEntering(Action<TEntity> action) => OnEnter((o, _) => action?.Invoke(o));
			public StateBuilder OnEnter(Action<TEntity> action) => OnEnter((o, _) => action?.Invoke(o));
			public StateBuilder OnPassthrough(Action<TEntity> action) => OnPassthrough((o, _) => action?.Invoke(o));
			public StateBuilder OnExit(Action<TEntity> action) => OnExit((o, _) => action?.Invoke(o));
			public StateBuilder Guard(Func<TEntity, bool> condition) => Guard((o, _) => condition?.Invoke(o) ?? true);

			public StateBuilder State(Token token) => Chart.State(token);

			public EventBuilder When(Token command)
			{
				var e = new EventBuilder(this, command);
				_events.Add(e);
				return e;
			}

			public GuardBuilder When(Func<TEntity, bool> condition)
			{
				var e = new EventBuilder(this);
				_events.Add(e);
				return e.And(condition);
			}

			public StatechartBuilder<TEntity> Close() => Chart;
		}

		[DebuggerDisplay("{Source.Id} -> {Target.Id}")]
		public class TransitionBuilder
		{
			public TransitionBuilder(StateBuilder source, StateBuilder target)
			{
				Source = source;
				Target = target;
			}

			public StateBuilder Source { get; }
			public StateBuilder Target { get; }

			public Action<Token, Token, TEntity>? TransitionAction;

			public StateBuilder State(Token id) => Source.Chart.State(id);

			public EventBuilder When(Token command) => Source.When(command);

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
			internal EventBuilder(StateBuilder node)
			{
				Node = node;
				Event = Token.Empty;
			}

			internal EventBuilder(StateBuilder node, Token @event)
			{
				Node = node;
				Event = @event;
			}

			public Token Event { get; }
			public StateBuilder Node { get; }
			public GuardBuilder? Guard { get; private set; }

			public GuardBuilder And(Func<TEntity, bool> condition) => Guard = new GuardBuilder(Node, condition);

			public TransitionBuilder GoTo(Token state) => (Guard = new GuardBuilder(Node)).GoTo(state);
		}

		[DebuggerDisplay("Guard for {Transition.Target.Id}")]
		public class GuardBuilder
		{
			internal GuardBuilder(StateBuilder node)
			{
				Node = node;
			}

			internal GuardBuilder(StateBuilder node, Func<TEntity, bool> condition)
			{
				Node = node;
				Condition = condition;
			}

			public StateBuilder Node { get; }
			public Func<TEntity, bool>? Condition { get; }
			public TransitionBuilder? Transition { get; private set; }

			public TransitionBuilder GoTo(Token state) => Transition = new TransitionBuilder(Node, Node.Chart.State(state));
		}
	}
}
