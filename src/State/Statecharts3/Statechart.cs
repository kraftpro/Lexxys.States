using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State.Statecharts3
{
	class Statechart
	{
		public IReadOnlyList<StateX> States { get; }
		public IReadOnlyList<TransitionX> Transitions { get; }
	}

	class ChartX
	{
		public static void Go()
		{
			//Span<byte> span;
			//ReadOnlySpan<byte> rospan;
			//ISequence<int> ii;
			//ISequence<ReadOnlyMemory<byte>> x;
			var b = new ChartX();
			b
				.State(1)
					.OnEnter(o => o.ToString())
					.OnExit(o => o.ToString())
					.Transition(2, command: "go2")
						.Action(o => Console.WriteLine(o))
						.Target
						.Transition(3, condition: o => o != null)
						.Transition(4)
				.State(5)
					.Chart
					.State(1)
						.Guard(p => p != null)
						.OnEnter((p, q) => { })
						.Transition(30, "go30");
		}

		public ChartX()
		{
		}

		public ChartX(StateX state)
		{
			Container = state;
		}

		public StateX Container { get; }

		private List<StateX> States { get; }  = new List<StateX>();
		private List<TransitionX> Transitions { get; } = new List<TransitionX>();

		public StateX State(int id)
		{
			var state = States.Find(o => o.Id == id);
			if (state == null)
				States.Add(state = new StateX(this, id));
			return state;
		}

		public TransitionX Transition(StateX source, StateX target, object command = null, Func<object, bool> guard = null, Func<object, bool> condition = null, Action<object> action = null)
		{
			var transition = Transitions.Find(o => o.Source == source && o.Target == target);
			if (transition == null)
				Transitions.Add(transition = new TransitionX(source, target, command, guard, condition, action));
			return transition;
		}

		public TransitionX Transition(StateX source, int targetId, object command = null, Func<object, bool> guard = null, Func<object, bool> condition = null, Action<object> action = null)
		{
			return Transition(source, State(targetId), command, guard, condition, action);
		}

		public TransitionX Transition(int sourceId, int targetId, object command = null, Func<object, bool> guard = null, Func<object, bool> condition = null, Action<object> action = null)
		{
			return Transition(State(sourceId), State(targetId), command, guard, condition, action);
		}
	}

	public delegate void StateAction2(object state, object context);
	public delegate void StateAction(object state);
	public delegate bool Guard(object value);

	class StateX
	{

		public StateX(ChartX container)
		{
			Contaier = container;
		}

		public StateX(ChartX container, int id)
		{
			Id = id;
			Contaier = container;
		}

		public int? Id { get; }
		public ChartX Contaier { get; }

		public ChartX Chart
		{
			get { return _chart ?? (_chart = new ChartX(this)); }
		}
		private ChartX _chart;

		public EventHandler<Object> StateEnter;
		public EventHandler<Object> StateExit;
		public EventHandler<Object> StatePassThrough;
		public Func<object, bool> StateGuard;

		public StateX OnEnter(EventHandler<Object> p)
		{
			StateEnter += p;
			return this;
		}

		public StateX OnExit(EventHandler<Object> p)
		{
			StateExit += p;
			return this;
		}

		public StateX OnPassThrough(EventHandler<Object> p)
		{
			StatePassThrough += p;
			return this;
		}

		public StateX OnEnter(Action<Object> action) => OnEnter((p, q) => action(q));
		public StateX OnExit(Action<Object> action) => OnExit((p, q) => action(q));
		public StateX OnPassThrough(Action<Object> action) => OnPassThrough((p, q) => action(q));

		public StateX State(int id)
		{
			return Contaier.State(id);
		}

		public StateX Guard(Func<object, bool> p)
		{
			StateGuard += p;
			return this;
		}

		public TransitionX Transition(StateX target, object command = null, Func<object, bool> guard = null, Func<object, bool> condition = null, Action<object> action = null)
		{
			return Contaier.Transition(this, target, command, guard, condition, action);
		}

		public TransitionX Transition(int targetId, object command = null, Func<object, bool> guard = null, Func<object, bool> condition = null, Action<object> action = null)
		{
			return Contaier.Transition(this, targetId, command, guard, condition, action);
		}
	}

	class TransitionX
	{
		public TransitionX(StateX source, StateX target, object command = null, Func<object, bool> guard = null, Func<object, bool> condition = null, Action<object> action = null)
		{
			Source = source;
			Target = target;
			TransitionCommand = command;
			TransitionGuard = guard;
			TransitionCondition = condition;
			TransitionAction = action;
		}

		public object TransitionCommand { get; private set; }
		public Func<object, bool> TransitionGuard { get; private set; }
		public Func<object, bool> TransitionCondition { get; private set; }
		public Action<object> TransitionAction { get; private set; }
		public StateX Source { get; }
		public StateX Target { get; }

		public TransitionX Command(object value)
		{
			TransitionCommand = value;
			return this;
		}

		public TransitionX Guard(Func<object, bool> predicate)
		{
			TransitionGuard = predicate;
			return this;
		}

		public TransitionX Condition(Func<object, bool> predicate)
		{
			TransitionCondition = predicate;
			return this;
		}

		public TransitionX Action(Action<object> acton)
		{
			TransitionAction = acton;
			return this;
		}

		public StateX State(int id)
		{
			return Source.State(id);
		}

		public TransitionX Transition(StateX state)
		{
			return Source.Transition(state);
		}

		public TransitionX Transition(int stateId)
		{
			return Source.Transition(stateId);
		}
	}
}
