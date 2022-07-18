using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lexxys.States;

#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA2225 // Operator overloads have named alternates

public readonly struct StateActionChain<T>
{
	private readonly IStateAction<T>[]? _actions;

	private StateActionChain(params IStateAction<T>[] actions)
		=> _actions = actions;

	public StateActionChain<T> Add(IStateAction<T>? action)
	{
		if (action == null)
			return this;
		if (_actions == null)
			return new StateActionChain<T>(action);
		if (Array.IndexOf(_actions, action) >= 0)
			return this;
		var actions = new IStateAction<T>[_actions.Length + 1];
		Array.Copy(_actions, actions, _actions.Length);
		actions[_actions.Length] = action;
		return new StateActionChain<T>(actions);
	}

	public bool IsEmpty => _actions == null || _actions.Length == 0;

	public StateActionChain<T> Remove(IStateAction<T>? action)
	{
		if (action == null || _actions == null)
			return this;
		int i = Array.IndexOf(_actions, action);
		if (i < 0)
			return this;
		if (_actions.Length == 1)
			return default;
		var actions = new IStateAction<T>[_actions.Length - 1];
		Array.Copy(_actions, 0, actions, 0, i);
		Array.Copy(_actions, i + 1, actions, i, actions.Length - i);
		return new StateActionChain<T>(actions);
	}

	public StateActionChain<T> Add(Action<T, Statechart<T>, State<T>?, Transition<T>?> action) => Add(StateAction.Create(action));

	public StateActionChain<T> Add(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> action) => Add(StateAction.Create(action));

	public StateActionChain<T> Add((Action<T, Statechart<T>, State<T>?, Transition<T>?> Sync, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> Async) action) => Add(StateAction.Create(action.Sync, action.Async));

	public StateActionChain<T> Add(Action<T, Statechart<T>> action) => Add(StateAction.Create<T>((o, c, s, t) => action(o, c)));

	public StateActionChain<T> Add(Func<T, Statechart<T>, Task> action) => Add(StateAction.Create<T>(action));

	public StateActionChain<T> Add((Action<T, Statechart<T>> Sync, Func<T, Statechart<T>, Task> Async) action) => Add(StateAction.Create<T>(action.Sync, action.Async));

	public void Invoke(T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
	{
		if (_actions == null)
			return;
		for (int i = 0; i < _actions.Length; ++i)
		{
			_actions[i].Invoke(value, chart, state, transition);
		}
	}

	public Task InvokeAsync(T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
	{
		return IsEmpty ?
			Task.CompletedTask:
			Task.WhenAll(_actions!.Select(o => o.InvokeAsync(value, chart, state, transition)));
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