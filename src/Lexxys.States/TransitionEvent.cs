using System;

namespace Lexxys.States;

public class TransitionEvent<T>
{
	public TransitionEvent(Statechart<T> chart, Transition<T> transition)
	{
		this.Chart = chart ?? throw new ArgumentNullException(nameof(chart));
		this.Transition = transition ?? throw new ArgumentNullException(nameof(transition));
	}

	public Statechart<T> Chart { get; }
	public Transition<T> Transition { get; }

	public void Deconstruct(out Statechart<T> chart, out Transition<T> transition)
	{
		chart = this.Chart;
		transition = this.Transition;
	}
}
