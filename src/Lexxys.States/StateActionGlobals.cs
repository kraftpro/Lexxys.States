namespace Lexxys.States;

#pragma warning disable CA1051 // Do not declare visible instance fields

public class StateActionGlobals<T>
{
	public T obj;
	public Statechart<T> chart;
	public State<T>? state;
	public Transition<T>? transition;

	public StateActionGlobals(T obj, Statechart<T> chart, State<T>? state = null, Transition<T>? transition = null)
	{
		this.obj = obj;
		this.state = state;
		this.chart = chart;
		this.transition = transition;
	}
}