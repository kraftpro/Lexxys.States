using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Lexxys.States;

public static class StateAction
{
	public static void Invoke<T>(this IStateAction<T> action, T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
		=> (action ?? throw new ArgumentNullException(nameof(action))).GetDelegate().Invoke(value, chart, state, transition);
	public static Task InvokeAsync<T>(this IStateAction<T> action, T value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
		=> (action ?? throw new ArgumentNullException(nameof(action))).GetAsyncDelegate().Invoke(value, chart, state, transition);

	public static IStateAction<T> Empty<T>() => EmptyAction<T>.Instance;

	public static IStateAction<T> Create<T>(Action<T, Statechart<T>, State<T>?, Transition<T>?>? expression, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? asyncExpression = null)
	{
		if (expression is null && asyncExpression is null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateAction<T>(expression, asyncExpression);
	}

	public static IStateAction<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> asyncExpression)
	{
		if (asyncExpression is null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateAction<T>(null, asyncExpression);
	}

	public static IStateAction<T> Create<T>(Action<T, Statechart<T>>? expression, Func<T, Statechart<T>, Task>? asyncExpression = null)
	{
		if (expression is null && asyncExpression is null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateAction<T>(expression is null ? null: (o, c, _,_) => expression(o, c), asyncExpression is null ? null: (o, c, _,_) => asyncExpression(o, c));
	}

	public static IStateAction<T> Create<T>(Func<T, Statechart<T>, Task> asyncExpression)
	{
		if (asyncExpression is null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateAction<T>(null, (o, c, _,_) => asyncExpression(o, c));
	}

	public static IStateAction<T> Create<T>(Action<T>? expression, Func<T, Task>? asyncExpression = null)
	{
		if (expression is null && asyncExpression is null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateAction<T>(expression is null ? null: (o, _,_,_) => expression(o), asyncExpression is null ? null: (o, _,_,_) => asyncExpression(o));
	}

	public static IStateAction<T> Create<T>(Func<T, Task> asyncExpression)
	{
		if (asyncExpression is null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateAction<T>(null, (o, _,_,_) => asyncExpression(o));
	}

	public static IStateAction<T> CSharpScript<T>(string expression) => RoslynAction<T>.Create(expression);

	#region Implemetation: EmptyAction, SimpleAction, RoslynAction

	private class EmptyAction<T>: IStateAction<T>
	{
		public static readonly IStateAction<T> Instance = new EmptyAction<T>();

		private EmptyAction()
		{
		}

		public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate() => (_,_,_,_) => { };

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => (_,_,_,_) => Task.CompletedTask;
	}

	private class DelegateAction<T>: IStateAction<T>
	{
		private readonly Action<T, Statechart<T>, State<T>?, Transition<T>?> _syncAction;
		private readonly Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> _asyncAction;

		public DelegateAction(Action<T, Statechart<T>, State<T>?, Transition<T>?>? syncAction, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? asyncAction)
		{
			if (syncAction is null && asyncAction is null)
				throw new ArgumentNullException(nameof(syncAction));
			_syncAction = syncAction ?? ((o, c, s, t) => asyncAction!(o, c, s, t).ConfigureAwait(false).GetAwaiter().GetResult());
			_asyncAction = asyncAction ?? ((o, c, s, t) => { return Task.Run(() => syncAction!.Invoke(o, c, s, t)); });
		}

		public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate() => _syncAction;

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => _asyncAction;
	}

	private class RoslynAction<T>: IStateAction<T>
	{
		private static ILogger Log => __log ??= Statics.GetLogger<RoslynAction<T>>();
		private static ILogger? __log;

		private readonly string _expression;
		private Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? _asyncHandler;
		private Action<T, Statechart<T>, State<T>?, Transition<T>?>? _syncHandler;

		public RoslynAction(string expression)
		{
			if (expression is null || expression.Length <= 0)
				throw new ArgumentNullException(nameof(expression));
			_expression = expression;
		}

		public static IStateAction<T> Create(string? expression)
		{
			return (expression = expression.TrimToNull()) is null ? Empty<T>():
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
			if (_asyncHandler is not null)
				return;
			#pragma warning disable CA2002 // Safe for private class
			lock (this)
			#pragma warning restore CA2002
			{
				if (_asyncHandler is not null)
					return;
				if (Log.IsEnabled(LogType.Trace))
					Log.Trace($"Compile '{_expression}'");

				var runner = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<bool>(_expression,
					ScriptOptions.Default
						.AddReferences(RoslynHelper.GetReferences<T>())
						.AddImports(RoslynHelper.GetImports()),
					typeof(StateActionGlobals<T>)).CreateDelegate();

				_asyncHandler = (o, c, s, t) =>
				{
					if (Log.IsEnabled(LogType.Trace))
						Log.Trace($"InvokeAsync '{_expression}' with obj={o}, state={s} and Transition={t}");
					return runner.Invoke(new StateActionGlobals<T>(o, c, s, t));
				};
				_syncHandler = (o, c, s, t) =>
				{
					if (Log.IsEnabled(LogType.Trace))
						Log.Trace($"Invoke '{_expression}' with obj={o}, state={s} and Transition={t}");
					runner.Invoke(new StateActionGlobals<T>(o, c, s, t)).GetAwaiter().GetResult();
				};
			}
		}
	}

	#endregion
}