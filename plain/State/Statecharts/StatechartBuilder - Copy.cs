using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Statecharts
{
	//class StatechartBuilder<T>
	//{
	//	private readonly Dictionary<IEnum, StateX> _states;

	//	public StatechartBuilder()
	//	{
	//		_states = new Dictionary<IEnum, StateX>();
	//		States = ReadOnly.Wrap(_states);
	//	}

	//	public StatechartBuilder(object parent) : this()
	//	{
	//		Parent = parent;
	//	}

	//	public object Parent { get; }
	//	public IReadOnlyDictionary<IEnum, StateX> States { get; }

	//	public event Action<T, IEnum> ChartEnter;
	//	public event Action<T, IEnum> ChartExit;

	//	/// <summary>
	//	/// Creates a new <see cref="StateX"/> in the <see cref="StatechartBuilder{IEnum, IEnum, T}"/>.
	//	/// </summary>
	//	/// <param name="id">State ID</param>
	//	/// <returns></returns>
	//	public StateX State(IEnum id)
	//	{
	//		if (!_states.TryGetValue(id, out var state))
	//			_states.Add(id, state = new StateX(this, id));
	//		return state;
	//	}

	//	/// <summary>
	//	/// Close curernt <see cref="StatechartBuilder{IEnum, IEnum, T}"/> and return parent state if any or null.
	//	/// </summary>
	//	/// <returns></returns>
	//	public StateX Close()
	//	{
	//		return Parent as StateX;
	//	}

	//	/// <summary>
	//	/// Close curernt <see cref="StatechartBuilder{IEnum, IEnum, T}"/> and return parent state if any or null.
	//	/// </summary>
	//	/// <returns></returns>
	//	public StatechartBuilder<T2>.StateX Close<T2>()
	//	{
	//		return Parent as StatechartBuilder<T2>.StateX;
	//	}

	//	public StatechartBuilder<T> OnEnter(Action<T, IEnum> action)
	//	{
	//		ChartEnter += action;
	//		return this;
	//	}

	//	public StatechartBuilder<T> OnExit(Action<T, IEnum> action)
	//	{
	//		ChartExit += action;
	//		return this;
	//	}

	//	[DebuggerDisplay("{Id} Events.Count={Events.Count}")]
	//	public class StateX
	//	{
	//		private readonly List<EventX> _events;

	//		public StateX(StatechartBuilder<T> chart, IEnum id)
	//		{
	//			Id = id;
	//			Chart = chart;
	//			_events = new List<EventX>();
	//			Events = ReadOnly.Wrap(_events);
	//		}

	//		public StatechartBuilder<T> Chart { get; }
	//		public IEnum Id { get; }
	//		public string Permission { get; }
	//		public IReadOnlyCollection<EventX> Events { get; }
	//		public object Subchart { get; private set; }
	//		public bool IsFinal { get; private set; }

	//		public Action<T, IEnum> StateEntering;
	//		public Action<T, IEnum> StateEnter;
	//		public Action<T, IEnum> StatePassthrough;
	//		public Action<T, IEnum> StateExit;
	//		public Func<T, IEnum, bool> StateGuard;

	//		public StateX OnEntering(Action<T, IEnum> action)
	//		{
	//			StateEntering += action;
	//			return this;
	//		}

	//		public StateX OnEnter(Action<T, IEnum> action)
	//		{
	//			StateEnter += action;
	//			return this;
	//		}

	//		public StateX OnPassthrough(Action<T, IEnum> action)
	//		{
	//			StatePassthrough += action;
	//			return this;
	//		}

	//		public StateX OnExit(Action<T, IEnum> action)
	//		{
	//			StateExit += action;
	//			return this;
	//		}

	//		public StateX Guard(Func<T, IEnum, bool> condition)
	//		{
	//			StateGuard += condition;
	//			return this;
	//		}

	//		public StatechartBuilder<T> SubChart()
	//		{
	//			var builder = new StatechartBuilder<T>(this);
	//			Subchart = builder;
	//			return builder;
	//		}

	//		public StatechartBuilder<T2> SubChart<T2>()
	//		{
	//			var builder = new StatechartBuilder<T2>(this);
	//			Subchart = builder;
	//			return builder;
	//		}

	//		public StateX OnEntering(Action<T> action) => OnEnter((o, _) => action?.Invoke(o));
	//		public StateX OnEnter(Action<T> action) => OnEnter((o, _) => action?.Invoke(o));
	//		public StateX OnPassthrough(Action<T> action) => OnPassthrough((o, _) => action?.Invoke(o));
	//		public StateX OnExit(Action<T> action) => OnExit((o, _) => action?.Invoke(o));
	//		public StateX Guard(Func<T, bool> condition) => Guard((o, _) => condition?.Invoke(o) ?? true);

	//		public StateX State(IEnum id)
	//		{
	//			return Chart.State(id);
	//		}

	//		public EventX When(IEnum command)
	//		{
	//			var e = new EventX(this, command);
	//			_events.Add(e);
	//			return e;
	//		}

	//		public GuardX When(Func<T, bool> condition)
	//		{
	//			var e = new EventX(this);
	//			_events.Add(e);
	//			return e.And(condition);
	//		}

	//		public StatechartBuilder<T> Final()
	//		{
	//			IsFinal = true;
	//			return Chart;
	//		}

	//		public StatechartBuilder<T> Close()
	//		{
	//			return Chart;
	//		}
	//	}

	//	[DebuggerDisplay("{Source.Id} -> {Target.Id}")]
	//	public class TransitionX
	//	{
	//		public TransitionX(StateX source, StateX target)
	//		{
	//			Source = source;
	//			Target = target;
	//		}

	//		public StateX Source { get; }
	//		public StateX Target { get; }

	//		public Action<IEnum, IEnum, T> TransitionAction;

	//		public StateX State(IEnum id)
	//		{
	//			return Source.Chart.State(id);
	//		}

	//		public EventX When(IEnum command)
	//		{
	//			return Source.When(command);
	//		}

	//		public GuardX When(Func<T, bool> condition)
	//		{
	//			return Source.When(condition);
	//		}

	//		public TransitionX Action(Action<IEnum, IEnum, T> action)
	//		{
	//			TransitionAction += action;
	//			return this;
	//		}
	//		public TransitionX Action(Action<IEnum, T> action) => Action((_, e, o) => action?.Invoke(e, o));
	//		public TransitionX Action(Action<T> action) => Action((_, e, o) => action?.Invoke(o));
	//		public TransitionX Action(Action action) => Action((_, e, o) => action?.Invoke());
	//	}

	//	[DebuggerDisplay("When {Event}")]
	//	public class EventX
	//	{
	//		internal EventX(StateX node)
	//		{
	//			Node = node;
	//			EmptyEvent = true;
	//		}

	//		internal EventX(StateX node, IEnum @event)
	//		{
	//			Node = node;
	//			Event = @event;
	//		}

	//		public IEnum Event { get; }
	//		public bool EmptyEvent { get; }
	//		public StateX Node { get; }
	//		public GuardX Guard { get; private set; }

	//		public GuardX And(Func<T, bool> condition)
	//		{
	//			return Guard = new GuardX(Node, condition);
	//		}

	//		public TransitionX GoTo(IEnum state)
	//		{
	//			Guard = new GuardX(Node);
	//			return Guard.GoTo(state);
	//		}
	//	}

	//	[DebuggerDisplay("Guard for {Transition.Target.Id}")]
	//	public class GuardX
	//	{
	//		internal GuardX(StateX node)
	//		{
	//			Node = node;
	//		}

	//		internal GuardX(StateX node, Func<T, bool> condition)
	//		{
	//			Node = node;
	//			Condition = condition;
	//		}

	//		public StateX Node { get; }
	//		public Func<T, bool> Condition { get; }
	//		public TransitionX Transition { get; private set; }

	//		public TransitionX GoTo(IEnum state)
	//		{
	//			return Transition = new TransitionX(Node, Node.Chart.State(state));
	//			//return Transition.Source;
	//		}
	//	}
	//}

	//public readonly struct StateEnumItem: IEnum, IEquatable<StateEnumItem>
	//{
	//	public int Value { get; }
	//	public string Name { get; }

	//	public StateEnumItem(int value, string name)
	//	{
	//		Value = value;
	//		Name = name;
	//	}

	//	public override bool Equals(object obj)
	//	{
	//		return obj is StateEnumItem item && Equals(item);
	//	}

	//	public override int GetHashCode()
	//	{
	//		return HashCode.Join(Value, Name?.GetHashCode() ?? 0);
	//	}

	//	public bool Equals(StateEnumItem other)
	//	{
	//		return Value == other.Value && Name == other.Name;
	//	}

	//	public static bool operator ==(StateEnumItem left, StateEnumItem right)
	//	{
	//		return left.Equals(right);
	//	}

	//	public static bool operator !=(StateEnumItem left, StateEnumItem right)
	//	{
	//		return !(left==right);
	//	}

	//	public override string ToString()
	//	{
	//		return $"{Name} ({Value})";
	//	}
	//}
}
