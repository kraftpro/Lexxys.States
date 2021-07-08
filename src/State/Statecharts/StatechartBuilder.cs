using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Statecharts
{
	class StatechartBuilder0
	{
		private readonly Dictionary<int, StateX> _states;

		public StatechartBuilder0()
		{
			_states = new Dictionary<int, StateX>();
			States = ReadOnly.Wrap(_states);
		}

		public StatechartBuilder0(object parent) : this()
		{
			Parent = parent;
		}

		public object Parent { get; }
		public IReadOnlyDictionary<int, StateX> States { get; }

		public event Action<dynamic, int> ChartEnter;
		public event Action<dynamic, int> ChartExit;

		/// <summary>
		/// Creates a new <see cref="StateX"/> in the <see cref="StatechartBuilder{int, object, object}"/>.
		/// </summary>
		/// <param name="id">State ID</param>
		/// <returns></returns>
		public StateX State(int id)
		{
			if (!_states.TryGetValue(id, out var state))
				_states.Add(id, state = new StateX(this, id));
			return state;
		}

		public StateX State(Enum id) => State(((IConvertible)id).ToInt32(null));

		/// <summary>
		/// Close curernt <see cref="StatechartBuilder{int, object, object}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StateX Close()
		{
			return Parent as StateX;
		}

		/// <summary>
		/// Close curernt <see cref="StatechartBuilder{int, object, object}"/> and return parent state if any or null.
		/// </summary>
		/// <returns></returns>
		public StatechartBuilder<TState2, TEvent2, TEntity2>.StateX Close<TState2, TEvent2, TEntity2>()
		{
			return Parent as StatechartBuilder<TState2, TEvent2, TEntity2>.StateX;
		}

		public StatechartBuilder0 OnEnter(Action<dynamic, int> action)
		{
			ChartEnter += action;
			return this;
		}

		public StatechartBuilder0 OnExit(Action<dynamic, int> action)
		{
			ChartExit += action;
			return this;
		}

		[DebuggerDisplay("{Id} Events.Count={Events.Count}")]
		public class StateX
		{
			private readonly List<EventX> _events;

			public StateX(StatechartBuilder0 chart, int id)
			{
				Id = id;
				Chart = chart;
				_events = new List<EventX>();
				Events = ReadOnly.Wrap(_events);
			}

			public StatechartBuilder0 Chart { get; }
			public int Id { get; }
			public string Permission { get; }
			public IReadOnlyCollection<EventX> Events { get; }
			public object Subchart { get; private set; }
			public bool IsFinal { get; private set; }

			public Action<dynamic, int> StateEntering;
			public Action<dynamic, int> StateEnter;
			public Action<dynamic, int> StatePassthrough;
			public Action<dynamic, int> StateExit;
			public Func<object, int, bool> StateGuard;

			public StateX OnEntering(Action<dynamic, int> action)
			{
				StateEntering += action;
				return this;
			}

			public StateX OnEnter(Action<dynamic, int> action)
			{
				StateEnter += action;
				return this;
			}

			public StateX OnPassthrough(Action<dynamic, int> action)
			{
				StatePassthrough += action;
				return this;
			}

			public StateX OnExit(Action<dynamic, int> action)
			{
				StateExit += action;
				return this;
			}

			public StateX Guard(Func<object, int, bool> condition)
			{
				StateGuard += condition;
				return this;
			}

			public StatechartBuilder0 SubChart()
			{
				var builder = new StatechartBuilder0(this);
				Subchart = builder;
				return builder;
			}

			public StatechartBuilder<TState2, TEvent2, TEntity2> SubChart<TState2, TEvent2, TEntity2>()
			{
				var builder = new StatechartBuilder<TState2, TEvent2, TEntity2>(this);
				Subchart = builder;
				return builder;
			}

			public StateX OnEntering(Action<object> action) => OnEnter((o, _) => action?.Invoke(o));
			public StateX OnEnter(Action<object> action) => OnEnter((o, _) => action?.Invoke(o));
			public StateX OnPassthrough(Action<object> action) => OnPassthrough((o, _) => action?.Invoke(o));
			public StateX OnExit(Action<object> action) => OnExit((o, _) => action?.Invoke(o));
			public StateX Guard(Func<object, bool> condition) => Guard((o, _) => condition?.Invoke(o) ?? true);

			public StateX State(int id)
			{
				return Chart.State(id);
			}

			public EventX When(object command)
			{
				var e = new EventX(this, command);
				_events.Add(e);
				return e;
			}

			public GuardX When(Func<object, bool> condition)
			{
				var e = new EventX(this);
				_events.Add(e);
				return e.And(condition);
			}

			public StatechartBuilder0 Final()
			{
				IsFinal = true;
				return Chart;
			}

			public StatechartBuilder0 Close()
			{
				return Chart;
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

			public Action<int, object, dynamic> TransitionAction;

			public StateX State(int id) => Source.Chart.State(id);

			public StateX State(Enum id) => State(((IConvertible)id).ToInt32(null));

			public EventX When(object command) => Source.When(command);

			public GuardX When(Func<dynamic, bool> condition) => Source.When(condition);

			public TransitionX Action(Action<int, object, dynamic> action)
			{
				TransitionAction += action;
				return this;
			}
			public TransitionX Action(Action<object, dynamic> action) => Action((_, e, o) => action?.Invoke(e, o));
			public TransitionX Action(Action<dynamic> action) => Action((_, e, o) => action?.Invoke(o));
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

			internal EventX(StateX node, object @event)
			{
				Node = node;
				Event = @event;
			}

			public object Event { get; }
			public bool EmptyEvent { get; }
			public StateX Node { get; }
			public GuardX Guard { get; private set; }

			public GuardX And(Func<dynamic, bool> condition) => Guard = new GuardX(Node, condition);

			public TransitionX GoTo(int state) => (Guard = new GuardX(Node)).GoTo(state);

			public TransitionX GoTo(Enum state) => GoTo(((IConvertible)state).ToInt32(null));
		}

		[DebuggerDisplay("Guard for {Transition.Target.Id}")]
		public class GuardX
		{
			internal GuardX(StateX node)
			{
				Node = node;
			}

			internal GuardX(StateX node, Func<object, bool> condition)
			{
				Node = node;
				Condition = condition;
			}

			public StateX Node { get; }
			public Func<dynamic, bool> Condition { get; }
			public TransitionX Transition { get; private set; }

			public TransitionX GoTo(int state) => Transition = new TransitionX(Node, Node.Chart.State(state));

			public TransitionX GoTo(Enum state) => GoTo(((IConvertible)state).ToInt32(null));
		}
	}

	public readonly struct StateEnumItem: IEnum, IEquatable<StateEnumItem>
	{
		public int Value { get; }
		public string Name { get; }

		public StateEnumItem(int value, string name)
		{
			Value = value;
			Name = name;
		}

		public StateEnumItem(Enum value)
		{
			Value = ((IConvertible)value).ToInt32(null);
			Name = ((IConvertible)value).ToString(null);
		}

		public override bool Equals(object obj)
		{
			return obj is StateEnumItem item && Equals(item);
		}

		public override int GetHashCode()
		{
			return Lexxys.HashCode.Join(Value, Name?.GetHashCode() ?? 0);
		}

		public bool Equals(StateEnumItem other)
		{
			return Value == other.Value && Name == other.Name;
		}

		public static bool operator ==(StateEnumItem left, StateEnumItem right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(StateEnumItem left, StateEnumItem right)
		{
			return !(left==right);
		}

		public static implicit operator StateEnumItem(Enum value) => new StateEnumItem(value);

		public override string ToString()
		{
			return $"{Name} ({Value})";
		}
	}

}
