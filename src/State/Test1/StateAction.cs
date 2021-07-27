//#define TRACE_ROSLYN
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Lexxys;

#nullable enable

namespace State.Test1
{
	public interface IStateAction<T>
	{
		Action<T, State<T>?, Transition<T>?> GetDelegate();
		Func<T, State<T>?, Transition<T>?, Task> GetAsyncDelegate();
	}

	public static class StateAction
	{
		public static void Invoke<T>(this IStateAction<T> action, T scope, State<T>? state, Transition<T>? transition) => action.GetDelegate().Invoke(scope, state, transition);
		public static Task InvokeAsync<T>(this IStateAction<T> action, T scope, State<T>? state, Transition<T>? transition) => action.GetAsyncDelegate().Invoke(scope, state, transition);

		public static IStateAction<T> Empty<T>() => EmptyAction<T>.Instance;

		public static IStateAction<T> Create<T>(string expression) => RoslynAction<T>.Create(expression);

		public static IStateAction<T> Create<T>(Action<T>? expression, Func<T, Task>? asyncExpression = null)
			=> expression == null && asyncExpression == null ? Empty<T>(): new DelegateAction<T>(expression, asyncExpression);

		public static IStateAction<T> Create<T>(Action<T, State<T>?>? expression, Func<T, State<T>?, Task>? asyncExpression = null)
			=> expression == null && asyncExpression == null ? Empty<T>(): new DelegateAction<T>(expression, asyncExpression);

		public static IStateAction<T> Create<T>(Action<T, State<T>?, Transition<T>?>? expression, Func<T, State<T>?, Transition<T>?, Task>? asyncExpression = null)
			=> expression == null && asyncExpression == null ? Empty<T>(): new DelegateAction<T>(expression, asyncExpression);

		#region Implemetation: EmptyAction, SimpleAction, RoslynAction

		private class EmptyAction<T>: IStateAction<T>
		{
			public static readonly IStateAction<T> Instance = new EmptyAction<T>();

			private EmptyAction()
			{
			}

			public Action<T, State<T>?, Transition<T>?> GetDelegate() => (a, b, c) => { };

			public Func<T, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => (a, b, c) => Task.CompletedTask;
		}

		private class DelegateAction<T>: IStateAction<T>
		{
			private readonly Action<T, State<T>?, Transition<T>?> _syncAction;
			private readonly Func<T, State<T>?, Transition<T>?, Task> _asyncAction;

			public DelegateAction(Action<T, State<T>?, Transition<T>?>? syncAction, Func<T, State<T>?, Transition<T>?, Task>? asyncAction)
			{
				if (syncAction == null && asyncAction == null)
					throw new ArgumentNullException(nameof(syncAction));
				_syncAction = syncAction ?? ((o, s, t) => asyncAction!(o, s, t).GetAwaiter().GetResult());
				_asyncAction = asyncAction ?? ((o, s, t) => { syncAction!(o, s, t); return Task.CompletedTask; });
			}

			public DelegateAction(Action<T, State<T>?>? syncAction, Func<T, State<T>?, Task>? asyncAction)
			{
				if (syncAction == null && asyncAction == null)
					throw new ArgumentNullException(nameof(syncAction));

				if (syncAction != null)
					_syncAction = (o, s, t) => syncAction(o, s);
				else
					_syncAction = (o, s, t) => asyncAction!(o, s).GetAwaiter().GetResult();
				if (asyncAction != null)
					_asyncAction = (o, s, t) => asyncAction(o, s);
				else
					_asyncAction = (o, s, t) => { syncAction!(o, s); return Task.CompletedTask; };
			}

			public DelegateAction(Action<T>? syncAction, Func<T, Task>? asyncAction)
			{
				if (syncAction == null && asyncAction == null)
					throw new ArgumentNullException(nameof(syncAction));
				if (syncAction != null)
					_syncAction = (o, s, t) => syncAction(o);
				else
					_syncAction = (o, s, t) => asyncAction!(o).GetAwaiter().GetResult();
				if (asyncAction != null)
					_asyncAction = (o, s, t) => asyncAction(o);
				else
					_asyncAction = (o, s, t) => { syncAction!(o); return Task.CompletedTask; };
			}

			public Action<T, State<T>?, Transition<T>?> GetDelegate() => _syncAction;

			public Func<T, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => _asyncAction;
		}

		private class RoslynAction<T>: IStateAction<T>
		{
			private readonly string _expression;
			private Func<T, State<T>?, Transition<T>?, Task>? _asyncHandler;
			private Action<T, State<T>?, Transition<T>?>? _syncHandler;

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

			public Action<T, State<T>?, Transition<T>?> GetDelegate()
			{
				Compile();
				return _syncHandler!;
			}

			public Func<T, State<T>?, Transition<T>?, Task> GetAsyncDelegate()
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
					var action = CSharpScript.Create(code: _expression, globalsType: typeof(StateActionGlobals<T>))
						.CreateDelegate();
					_asyncHandler = (c, s, t) =>
					{
#if TRACE_ROSLYN
						Console.WriteLine($"  RoslynAction.InvokeAsync '{_expression}' with o = {context}, State = {state} and Transition = {transition}");
#endif
						return action.Invoke(new StateActionGlobals<T>(c, s, t));
					};
					_syncHandler = (c, s, t) =>
					{
#if TRACE_ROSLYN
						Console.WriteLine($"  RoslynAction.Invoke '{_expression}' with o = {context}, State = {state} and Transition = {transition}");
#endif
						action.Invoke(new StateActionGlobals<T>(c, s, t)).GetAwaiter().GetResult();
					};
				}
			}
		}

		#endregion
	}

	public class StateActionGlobals<T>
	{
		public readonly T obj;
		public readonly State<T>? state;
		public readonly Transition<T>? transition;

		public StateActionGlobals(T obj, State<T>? state = null, Transition<T>? transition = null)
		{
			this.obj = obj;
			this.state = state;
			this.transition = transition;
		}
	}
}