using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Statecharts
{
	class StatechartBuilder: StatechartBuilder<object, int, object>
	{
	}

	class StatechartBuilder<TEntity>: StatechartBuilder<TEntity, int, object>
	{
	}

	class StatechartBuilder<TEntity, TState>: StatechartBuilder<TEntity, TState, object>
	{
	}

	class StatechartBuilder<TEntity, TState, TEvent>
	{
		private readonly Dictionary<TState, StateX> _states;

		public StatechartBuilder()
		{
			_states = new Dictionary<TState, StateX>();
			States = ReadOnly.Wrap(_states);
		}

		public StatechartBuilder(object parent): this()
		{
			Parent = parent;
		}

		public object Parent { get; }
		public IReadOnlyDictionary<TState, StateX> States { get; }

		public event Action<TEntity, TState> ChartEnter;
		public event Action<TEntity, TState> ChartExit;

		public StatechartX Begin()
		{
			return Chart = new StatechartX(null);
		}

		public StatechartX Chart { get; private set; }

		/// <summary>
		/// Creates a new <see cref="StateX"/> in the <see cref="StatechartBuilder{TState, TEvent, TEntity}"/>.
		/// </summary>
		/// <param name="id">State ID</param>
		/// <returns></returns>
		public StateX State(TState id)
		{
			if (!_states.TryGetValue(id, out var state))
				_states.Add(id, state = new StateX(this, id));
			return state;
		}

		/// <summary>
		/// Creates a new <see cref="StateX"/> in the <see cref="StatechartBuilder{TState, TEvent, TEntity}"/>.
		/// </summary>
		/// <param name="id">State ID</param>
		/// <param name="state">Created state to be used in the future references</param>
		/// <returns></returns>
		public StateX State(TState id, out StateX state)
		{
			if (!_states.TryGetValue(id, out state))
				_states.Add(id, state = new StateX(this, id));
			return state;
		}

		/// <summary>
		/// Close curernt <see cref="StatechartBuilder{TState, TEvent, TEntity}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StateX Close()
		{
			return Parent as StateX;
		}

		/// <summary>
		/// Close curernt <see cref="StatechartBuilder{TState, TEvent, TEntity}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StatechartBuilder<TState2, TEvent2, TEntity2>.StateX Close<TState2, TEvent2, TEntity2>()
		{
			return Parent as StatechartBuilder<TState2, TEvent2, TEntity2>.StateX;
		}

		public StatechartBuilder<TEntity, TState, TEvent> OnEnter(Action<TEntity, TState> action)
		{
			ChartEnter += action;
			return this;
		}

		public StatechartBuilder<TEntity, TState, TEvent> OnExit(Action<TEntity, TState> action)
		{
			ChartExit += action;
			return this;
		}

		[DebuggerDisplay("{Id} Events.Count={Events.Count}")]
		public class StateX
		{
			private readonly List<EventX> _events;

			public StateX(StatechartBuilder<TEntity, TState, TEvent> chart, TState id)
			{
				Id = id;
				Chart = chart;
				_events = new List<EventX>();
				Events = ReadOnly.Wrap(_events);
			}

			public StatechartBuilder<TEntity, TState, TEvent> Chart { get; }
			public TState Id { get; }
			public string Permission { get; }
			public IReadOnlyCollection<EventX> Events { get; }
			public object Subchart { get; private set; }
			public bool IsFinal { get; private set; }

			public Action<TEntity, TState> StateEntering;
			public Action<TEntity, TState> StateEnter;
			public Action<TEntity, TState> StatePassthrough;
			public Action<TEntity, TState> StateExit;
			public Func<TEntity, TState, bool> StateGuard;

			public StateX OnEntering(Action<TEntity, TState> action)
			{
				StateEntering += action;
				return this;
			}

			public StateX OnEnter(Action<TEntity, TState> action)
			{
				StateEnter += action;
				return this;
			}

			public StateX OnPassthrough(Action<TEntity, TState> action)
			{
				StatePassthrough += action;
				return this;
			}

			public StateX OnExit(Action<TEntity, TState> action)
			{
				StateExit += action;
				return this;
			}

			public StateX Guard(Func<TEntity, TState, bool> condition)
			{
				StateGuard += condition;
				return this;
			}

			public StatechartX<TEntity, TState, TEvent> Begin()
			{
				var builder = new StatechartX<TEntity, TState, TEvent>(this);
				Subchart = builder;
				return builder;
			}

			public StatechartX<TState2, TEvent2, TEntity2> Begin<TState2, TEvent2, TEntity2>()
			{
				var builder = new StatechartX<TState2, TEvent2, TEntity2>(this);
				Subchart = builder;
				return builder;
			}

			public StateX End() => (StateX)Chart.Parent;

			public StatechartBuilder<TEntity2, TState2, TEvent2>.StateX End<TEntity2, TState2, TEvent2>()
				=> (StatechartBuilder<TEntity2, TState2, TEvent2>.StateX)Chart.Parent;

			public StateX OnEntering(Action<TEntity> action) => OnEnter((o, _) => action?.Invoke(o));
			public StateX OnEnter(Action<TEntity> action) => OnEnter((o, _) => action?.Invoke(o));
			public StateX OnPassthrough(Action<TEntity> action) => OnPassthrough((o, _) => action?.Invoke(o));
			public StateX OnExit(Action<TEntity> action) => OnExit((o, _) => action?.Invoke(o));
			public StateX Guard(Func<TEntity, bool> condition) => Guard((o, _) => condition?.Invoke(o) ?? true);

			public StateX State(TState id) => Chart.State(id);

			public EventX When(TEvent command)
			{
				var e = new EventX(this, command);
				_events.Add(e);
				return e;
			}

			public GuardX When(Func<TEntity, bool> condition)
			{
				var e = new EventX(this);
				_events.Add(e);
				return e.And(condition);
			}

			public StatechartBuilder<TEntity, TState, TEvent> Final()
			{
				IsFinal = true;
				return Chart;
			}

			public StatechartBuilder<TEntity, TState, TEvent> Close() => Chart;
		}

		public class StatechartX: StatechartBuilder<TEntity, TState, TEvent>
		{
			public StatechartX(StateX state): base(state)
			{
			}
		}

		public class StatechartX<TEntity2, TState2, TEvent2>: StatechartBuilder<TEntity2, TState2, TEvent2>
		{
			public StatechartX(StatechartBuilder<TEntity, TState, TEvent>.StateX state): base(state)
			{
			}
		}

		[DebuggerDisplay("{Source.Id} -> {Target.Id}")]
		public class TransitionX
		{
			public TransitionX(StateX source, StateX target)
			{
				Source = source;
				Target = target;
			}

			public StateX Source { get; }
			public StateX Target { get; }

			public Action<TState, TEvent, TEntity> TransitionAction;

			public StateX State(TState id) => Source.Chart.State(id);

			public EventX When(TEvent command) => Source.When(command);

			public GuardX When(Func<TEntity, bool> condition) => Source.When(condition);

			public TransitionX Action(Action<TState, TEvent, TEntity> action)
			{
				TransitionAction += action;
				return this;
			}
			public TransitionX Action(Action<TEvent, TEntity> action) => Action((_, e, o) => action?.Invoke(e, o));
			public TransitionX Action(Action<TEntity> action) => Action((_, e, o) => action?.Invoke(o));
			public TransitionX Action(Action action) => Action((_, e, o) => action?.Invoke());
		}

		[DebuggerDisplay("When {Event}")]
		public class EventX
		{
			internal EventX(StateX node)
			{
				Node = node;
				EmptyEvent = true;
			}

			internal EventX(StateX node, TEvent @event)
			{
				Node = node;
				Event = @event;
			}

			public TEvent Event { get; }
			public bool EmptyEvent { get; }
			public StateX Node { get; }
			public GuardX Guard { get; private set; }

			public GuardX And(Func<TEntity, bool> condition) => Guard = new GuardX(Node, condition);

			public TransitionX GoTo(TState state) => (Guard = new GuardX(Node)).GoTo(state);
		}

		[DebuggerDisplay("Guard for {Transition.Target.Id}")]
		public class GuardX
		{
			internal GuardX(StateX node)
			{
				Node = node;
			}

			internal GuardX(StateX node, Func<TEntity, bool> condition)
			{
				Node = node;
				Condition = condition;
			}

			public StateX Node { get; }
			public Func<TEntity, bool> Condition { get; }
			public TransitionX Transition { get; private set; }

			public TransitionX GoTo(TState state) => Transition = new TransitionX(Node, Node.Chart.State(state));
		}
	}
}
