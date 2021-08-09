using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Statecharts
{

	class StatechartBuilder2
	{
		public StatechartX<TEntity, TState, TEvent> Begin<TEntity, TState, TEvent>(string name)
		{
			var chart = new StatechartX<TEntity, TState, TEvent>(null);
			Charts.Add(new Item(name, typeof(TEntity), typeof(TState), typeof(TEvent), chart));
			return chart;
		}

		public StatechartX<TEntity, TState, object> Begin<TEntity, TState>(string name)
		{
			var chart = new StatechartX<TEntity, TState, object>(null);
			Charts.Add(new Item(name, typeof(TEntity), typeof(TState), typeof(object), chart));
			return chart;
		}

		public StatechartX<TEntity, int, object> Begin<TEntity>(string name)
		{
			var chart = new StatechartX<TEntity, int, object>(null);
			Charts.Add(new Item(name, typeof(TEntity), typeof(int), typeof(object), chart));
			return chart;
		}

		public StatechartX<object, int, object> Begin(string name)
		{
			var chart = new StatechartX<object, int, object>(null);
			Charts.Add(new Item(name, typeof(object), typeof(int), typeof(object), chart));
			return chart;
		}

		public List<Item> Charts { get; } = new List<Item>();

		public class Item
		{
			public string Name { get; }
			public Type EntityType { get; }
			public Type StateType { get; }
			public Type EventType { get; }
			public object Chart { get; }

			public Item(string name, Type entityType, Type stateType, Type eventType, object chart)
			{
				Name = name ?? throw new ArgumentNullException(nameof(name));
				EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
				StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
				EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
				Chart = chart ?? throw new ArgumentNullException(nameof(chart));
			}
		}
	}

	class StatechartX<TEntity, TState, TEvent>
	{
		private readonly Dictionary<TState, StateX> _states;

		public StatechartX()
		{
			_states = new Dictionary<TState, StateX>();
			States = ReadOnly.Wrap(_states);
		}

		public StatechartX(object parent): this()
		{
			Parent = parent;
		}

		public object Parent { get; }
		public IReadOnlyDictionary<TState, StateX> States { get; }

		public event Action<TEntity, TState> ChartEnter;
		public event Action<TEntity, TState> ChartExit;

		public StatechartX<TEntity, TState, TEvent> Me(Action<StatechartX<TEntity, TState, TEvent>> action)
		{
			action(this);
			return this;
		}

		/// <summary>
		/// Creates a new <see cref="StateX"/> in the <see cref="StatechartX{TState, TEvent, TEntity}"/>.
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
		/// Creates a new <see cref="StateX"/> in the <see cref="StatechartX{TState, TEvent, TEntity}"/>.
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
		/// Close curernt <see cref="StatechartX{TState, TEvent, TEntity}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StateX End()
		{
			return Parent as StateX;
		}

		/// <summary>
		/// Close curernt <see cref="StatechartX{TState, TEvent, TEntity}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StatechartX<TState2, TEvent2, TEntity2>.StateX End<TState2, TEvent2, TEntity2>()
		{
			return Parent as StatechartX<TState2, TEvent2, TEntity2>.StateX;
		}

		public StatechartX<TEntity, TState, TEvent> OnEnter(Action<TEntity, TState> action)
		{
			ChartEnter += action;
			return this;
		}

		public StatechartX<TEntity, TState, TEvent> OnExit(Action<TEntity, TState> action)
		{
			ChartExit += action;
			return this;
		}

		[DebuggerDisplay("{Id} Events.Count={Events.Count}")]
		public class StateX
		{
			private readonly List<EventX> _events;

			public StateX(StatechartX<TEntity, TState, TEvent> chart, TState id)
			{
				Id = id;
				Chart = chart;
				_events = new List<EventX>();
				Events = ReadOnly.Wrap(_events);
			}

			public StatechartX<TEntity, TState, TEvent> Chart { get; }
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

			public object End() => Chart.Parent;

			public StateX Me(Action<StateX> action)
			{
				action(this);
				return this;
			}

			public StatechartX<TEntity2, TState2, TEvent2>.StateX End<TEntity2, TState2, TEvent2>()
				=> (StatechartX<TEntity2, TState2, TEvent2>.StateX)Chart.Parent;

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

			public StatechartX<TEntity, TState, TEvent> Final()
			{
				IsFinal = true;
				return Chart;
			}

			public StatechartX<TEntity, TState, TEvent> Close() => Chart;
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

			public TransitionX Me(Action<TransitionX> action)
			{
				action(this);
				return this;
			}

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
