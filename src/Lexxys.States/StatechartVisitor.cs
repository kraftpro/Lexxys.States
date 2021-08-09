using System;

namespace Lexxys.States
{
	public interface IStatechartVisitor<T>
	{
		void Visit(State<T> state);
		void Visit(Transition<T> state);
		void Visit(Statechart<T> statechart);
	}

	public class StatechartVisitor<T> : IStatechartVisitor<T>
	{
		private readonly Action<Statechart<T>>? _chart;
		private readonly Action<State<T>>? _state;
		private readonly Action<Transition<T>>? _transition;

		public StatechartVisitor(Action<Statechart<T>>? chart = null, Action<State<T>>? state = null, Action<Transition<T>>? transition = null)
		{
			_chart = chart;
			_state = state;
			_transition = transition;
		}

		public void Visit(Statechart<T> statechart) => _chart?.Invoke(statechart);
		public void Visit(State<T> state) => _state?.Invoke(state);
		public void Visit(Transition<T> transition) => _transition?.Invoke(transition);
	}

}
