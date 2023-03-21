using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lexxys.States;

#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA2225 // Operator overloads have named alternates

public readonly struct StateActionChain<T>
{
	//private readonly IStateAction<T>[]? _actions;
	private readonly object? _action;

	private StateActionChain(IStateAction<T>? actions)
		=> _action = actions;

	private StateActionChain(params IStateAction<T>[]? actions)
		=> _action = actions is null || actions.Length == 0 ? null: actions.Length == 1 ? actions[0]: actions;

	public StateActionChain<T> Add(IStateAction<T>? action)
	{
		if (action is null || _action == action)
			return this;
		if (_action is null)
			return new StateActionChain<T>(action);
		if (_action is IStateAction<T>)
			return new StateActionChain<T>((IStateAction<T>)_action, action);

		var array = (IStateAction<T>[])_action;
		if (Array.IndexOf(array, action) >= 0)
			return this;
		var actions = new IStateAction<T>[array.Length + 1];
		Array.Copy(array, actions, array.Length);
		actions[array.Length] = action;
		return new StateActionChain<T>(actions);
	}

	public bool IsEmpty => _action is null;

	public StateActionChain<T> Remove(IStateAction<T>? action)
	{
		if (action is null || _action is null)
			return this;
		if (_action == action)
			return default;

		if (_action is IStateAction<T>)
			return this;

		var array = (IStateAction<T>[])_action;
		int i = Array.IndexOf(array, action);
		if (i < 0)
			return this;
		if (array.Length == 2)
			return new StateActionChain<T>(array[1 - i]);
		var actions = new IStateAction<T>[array.Length - 1];
		Array.Copy(array, 0, actions, 0, i);
		Array.Copy(array, i + 1, actions, i, actions.Length - i);
		return new StateActionChain<T>(actions);
	}

	public StateActionChain<T> Add(Action<T, Statechart<T>, State<T>?, Transition<T>?> action) => Add(StateAction.Create(action));

	public StateActionChain<T> Add(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> action) => Add(StateAction.Create(action));

	public StateActionChain<T> Add((Action<T, Statechart<T>, State<T>?, Transition<T>?> Sync, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> Async) action) => Add(StateAction.Create(action.Sync, action.Async));

	public StateActionChain<T> Add(Action<T, Statechart<T>> action) => Add(StateAction.Create<T>((o, c, _,_) => action(o, c)));

	public StateActionChain<T> Add(Func<T, Statechart<T>, Task> action) => Add(StateAction.Create(action));

	public StateActionChain<T> Add((Action<T, Statechart<T>> Sync, Func<T, Statechart<T>, Task> Async) action) => Add(StateAction.Create(action.Sync, action.Async));

	public void Invoke(T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
	{
		if (_action is null)
			return;

		if (_action is IStateAction<T>)
		{
			((IStateAction<T>)_action).Invoke(value, chart, state, transition);
			return;
		}

		var array = (IStateAction<T>[])_action;
		for (int i = 0; i < array.Length; ++i)
		{
			array[i].Invoke(value, chart, state, transition);
		}
	}

	public Task InvokeAsync(T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
	{
		return _action is null ? Task.CompletedTask:
			_action is IStateAction<T> ?
				((IStateAction<T>)_action).InvokeAsync(value, chart, state, transition):
				Task.WhenAll(((IStateAction<T>[])_action).Select(o => o.InvokeAsync(value, chart, state, transition)));
	}

	public static StateActionChain<T> operator +(StateActionChain<T> chain, IStateAction<T>? item) => chain.Add(item);
	public static StateActionChain<T> operator -(StateActionChain<T> chain, IStateAction<T>? item) => chain.Remove(item);
	public static StateActionChain<T> operator +(StateActionChain<T> chain, Action<T, Statechart<T>, State<T>?, Transition<T>?> item) => chain.Add(item);
	public static StateActionChain<T> operator +(StateActionChain<T> chain, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> item) => chain.Add(item);
	public static StateActionChain<T> operator +(StateActionChain<T> chain, (Action<T, Statechart<T>, State<T>?, Transition<T>?> Sync, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> Async) item) => chain.Add(item);
	public static StateActionChain<T> operator +(StateActionChain<T> chain, Action<T, Statechart<T>> item) => chain.Add(item);
	public static StateActionChain<T> operator +(StateActionChain<T> chain, Func<T, Statechart<T>, Task> item) => chain.Add(item);
	public static StateActionChain<T> operator +(StateActionChain<T> chain, (Action<T, Statechart<T>> Sync, Func<T, Statechart<T>, Task> Async) item) => chain.Add(item);
}