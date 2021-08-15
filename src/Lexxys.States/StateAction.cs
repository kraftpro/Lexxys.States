//#define TRACE_ROSLYN
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Lexxys.States
{
	public interface IStateAction<T>
	{
		Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate();
		Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate();
	}

	public static class StateAction
	{
		public static void Invoke<T>(this IStateAction<T> action, T value, Statechart<T> chart, State<T>? state, Transition<T>? transition) => action.GetDelegate().Invoke(value, chart, state, transition);
		public static Task InvokeAsync<T>(this IStateAction<T> action, T value, Statechart<T> chart, State<T>? state, Transition<T>? transition) => action.GetAsyncDelegate().Invoke(value, chart, state, transition);

		public static IStateAction<T> Empty<T>() => EmptyAction<T>.Instance;

		public static IStateAction<T> Create<T>(Action<T, Statechart<T>, State<T>?, Transition<T>?> expression, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? asyncExpression = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateAction<T>(expression, asyncExpression);
		}

		public static IStateAction<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateAction<T>(null, expression);
		}

		public static IStateAction<T> Create<T>(Action<T, Statechart<T>> expression, Func<T, Statechart<T>, Task>? asyncExpression = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateAction<T>((o,c,s,t) => expression(o, c), asyncExpression == null ? null: (o,c,s,t) => asyncExpression(o, c));
		}

		public static IStateAction<T> Create<T>(Func<T, Statechart<T>, Task> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateAction<T>(null, (o,c,s,t) => expression(o, c));
		}

		public static IStateAction<T> Create<T>(Action<T> expression, Func<T, Task>? asyncExpression = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateAction<T>((o, c, s, t) => expression(o), asyncExpression == null ? null : (o, c, s, t) => asyncExpression(o));
		}

		public static IStateAction<T> Create<T>(Func<T, Task> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateAction<T>(null, (o, c, s, t) => expression(o));
		}

		public static IStateAction<T> CSharpScript<T>(string expression) => RoslynAction<T>.Create(expression);

		#region Implemetation: EmptyAction, SimpleAction, RoslynAction

		private class EmptyAction<T>: IStateAction<T>
		{
			public static readonly IStateAction<T> Instance = new EmptyAction<T>();

			private EmptyAction()
			{
			}

			public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate() => (o, c, s, t) => { };

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => (o, c, s, t) => Task.CompletedTask;
		}

		private class DelegateAction<T>: IStateAction<T>
		{
			private readonly Action<T, Statechart<T>, State<T>?, Transition<T>?> _syncAction;
			private readonly Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> _asyncAction;

			public DelegateAction(Action<T, Statechart<T>, State<T>?, Transition<T>?>? syncAction, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? asyncAction)
			{
				if (syncAction == null && asyncAction == null)
					throw new ArgumentNullException(nameof(syncAction));
				_syncAction = syncAction ?? ((o, c, s, t) => asyncAction!(o, c, s, t).GetAwaiter().GetResult());
				_asyncAction = asyncAction ?? ((o, c, s, t) => { syncAction!(o, c, s, t); return Task.CompletedTask; });
			}

			public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate() => _syncAction;

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => _asyncAction;
		}

		private class RoslynAction<T>: IStateAction<T>
		{
			private readonly string _expression;
			private Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? _asyncHandler;
			private Action<T, Statechart<T>, State<T>?, Transition<T>?>? _syncHandler;

			public RoslynAction(string expression)
			{
				if (expression == null || expression.Length <= 0)
					throw new ArgumentNullException(nameof(expression));
				_expression = expression;
			}

			public static IStateAction<T> Create(string expression)
			{
				return (expression = expression.TrimToNull()) == null ? Empty<T>():
					__compiledActions.GetOrAdd(expression, o => new RoslynAction<T>(o));
			}
			private static readonly ConcurrentDictionary<string, IStateAction<T>> __compiledActions = new ConcurrentDictionary<string, IStateAction<T>>();

			public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate()
			{
				Compile();
				return _syncHandler!;
			}

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate()
			{
				Compile();
				return _asyncHandler!;
			}

			private void Compile()
			{
				if (_asyncHandler != null)
					return;
				lock (this)
				{
					if (_asyncHandler != null)
						return;
#if TRACE_ROSLYN
					Console.WriteLine($"  RoslynAction.Compile '{_expression}' for {typeof(T)}");
#endif
					var action = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create(code: _expression, globalsType: typeof(StateActionGlobals<T>))
						.CreateDelegate();
					_asyncHandler = (o, c, s, t) =>
					{
#if TRACE_ROSLYN
						Console.WriteLine($"  RoslynAction.InvokeAsync '{_expression}' with o = {context}, State = {state} and Transition = {transition}");
#endif
						return action.Invoke(new StateActionGlobals<T>(o, c, s, t));
					};
					_syncHandler = (o, c, s, t) =>
					{
#if TRACE_ROSLYN
						Console.WriteLine($"  RoslynAction.Invoke '{_expression}' with o = {context}, State = {state} and Transition = {transition}");
#endif
						action.Invoke(new StateActionGlobals<T>(o, c, s, t)).GetAwaiter().GetResult();
					};
				}
			}
		}

		#endregion
	}

	public readonly struct StateActionChain<T>
	{
		public static readonly StateActionChain<T> Empty = new StateActionChain<T>();
		private readonly IStateAction<T>[]? _actions;

		public StateActionChain(params IStateAction<T>[] actions)
		{
			_actions = actions;
		}

		public StateActionChain<T> Add(IStateAction<T>? action)
		{
			if (action == null)
				return this;
			if (_actions == null || _actions.Length == 0)
				return new StateActionChain<T>(action);
			if (Array.IndexOf(_actions, action) >= 0)
				return this;
			var actions = new IStateAction<T>[_actions.Length + 1];
			Array.Copy(_actions, actions, _actions.Length);
			actions[_actions.Length] = action;
			return new StateActionChain<T>(actions);
		}

		public StateActionChain<T> Remove(IStateAction<T>? action)
		{
			if (action == null || _actions == null)
				return this;
			int i = Array.IndexOf(_actions, action);
			if (i < 0)
				return this;
			if (_actions.Length == 1)
				return Empty;
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

		public async Task InvokeAsync(T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
		{
			if (_actions == null)
				return;
			for (int i = 0; i < _actions.Length; ++i)
			{
				await _actions[i].InvokeAsync(value, chart, state, transition);
			}
		}

		public static StateActionChain<T> operator +(StateActionChain<T> chain, IStateAction<T> item) => chain.Add(item);
		public static StateActionChain<T> operator -(StateActionChain<T> chain, IStateAction<T> item) => chain.Remove(item);
		public static StateActionChain<T> operator +(StateActionChain<T> chain, Action<T, Statechart<T>, State<T>?, Transition<T>?> item) => chain.Add(item);
		public static StateActionChain<T> operator +(StateActionChain<T> chain, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> item) => chain.Add(item);
		public static StateActionChain<T> operator +(StateActionChain<T> chain, (Action<T, Statechart<T>, State<T>?, Transition<T>?> Sync, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> Async) item) => chain.Add(item);
		public static StateActionChain<T> operator +(StateActionChain<T> chain, Action<T, Statechart<T>> item) => chain.Add(item);
		public static StateActionChain<T> operator +(StateActionChain<T> chain, Func<T, Statechart<T>, Task> item) => chain.Add(item);
		public static StateActionChain<T> operator +(StateActionChain<T> chain, (Action<T, Statechart<T>> Sync, Func<T, Statechart<T>, Task> Async) item) => chain.Add(item);
	}

	public class StateActionGlobals<T>
	{
		public readonly T Obj;
		public readonly Statechart<T> Chart;
		public readonly State<T>? State;
		public readonly Transition<T>? Transition;

		public StateActionGlobals(T obj, Statechart<T> chart, State<T>? state = null, Transition<T>? transition = null)
		{
			Obj = obj;
			State = state;
			Chart = chart;
			Transition = transition;
		}
	}
}