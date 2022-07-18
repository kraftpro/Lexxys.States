//#define TRACE_ROSLYN
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

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
		if (expression == null && asyncExpression == null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateAction<T>(expression, asyncExpression);
	}

	public static IStateAction<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> asyncExpression)
	{
		if (asyncExpression == null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateAction<T>(null, asyncExpression);
	}

	public static IStateAction<T> Create<T>(Action<T, Statechart<T>>? expression, Func<T, Statechart<T>, Task>? asyncExpression = null)
	{
		if (expression == null && asyncExpression == null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateAction<T>(expression == null ? null: (o,c,s,t) => expression(o, c), asyncExpression == null ? null: (o,c,s,t) => asyncExpression(o, c));
	}

	public static IStateAction<T> Create<T>(Func<T, Statechart<T>, Task> asyncExpression)
	{
		if (asyncExpression == null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateAction<T>(null, (o,c,s,t) => asyncExpression(o, c));
	}

	public static IStateAction<T> Create<T>(Action<T>? expression, Func<T, Task>? asyncExpression = null)
	{
		if (expression == null && asyncExpression == null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateAction<T>(expression == null ? null: (o, c, s, t) => expression(o), asyncExpression == null ? null: (o, c, s, t) => asyncExpression(o));
	}

	public static IStateAction<T> Create<T>(Func<T, Task> asyncExpression)
	{
		if (asyncExpression == null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateAction<T>(null, (o, c, s, t) => asyncExpression(o));
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
			_syncAction = syncAction ?? ((o, c, s, t) => asyncAction!(o, c, s, t).ConfigureAwait(false).GetAwaiter().GetResult());
			_asyncAction = asyncAction ?? ((o, c, s, t) => { return Task.Run(() => syncAction!.Invoke(o, c, s, t)); });
		}

		public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate() => _syncAction;

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate() => _asyncAction;
	}

	private class RoslynAction<T>: IStateAction<T>
	{
		private static ILogging Log => __log ??= StaticServices.GetLogger<RoslynAction<T>>();
		private static ILogging? __log;

		private readonly string _expression;
		private Func<T, Statechart<T>, State<T>?, Transition<T>?, Task>? _asyncHandler;
		private Action<T, Statechart<T>, State<T>?, Transition<T>?>? _syncHandler;

		public RoslynAction(string expression)
		{
			if (expression == null || expression.Length <= 0)
				throw new ArgumentNullException(nameof(expression));
			_expression = expression;
		}

		public static IStateAction<T> Create(string? expression)
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
			#pragma warning disable CA2002 // Safe for private class
			lock (this)
			#pragma warning restore CA2002
			{
				if (_asyncHandler != null)
					return;
				Log.Trace($"Compile '{_expression}'");

				var references = new List<MetadataReference>
				{
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Statechart<>).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
				};
#if NETSTANDARD
				var location = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
				var netstandard = Path.Combine(location, "netstandard.dll");
				if (File.Exists(netstandard))
					references.Add(MetadataReference.CreateFromFile(netstandard));
#endif
				var entry = Assembly.GetEntryAssembly();
				if (entry != null)
					references.AddRange(
						entry.GetReferencedAssemblies()
							.Select(o => MetadataReference.CreateFromFile(Assembly.Load(o).Location))
						);

				var runner = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<bool>(_expression,
					ScriptOptions.Default
						.WithReferences(references),
					typeof(StateActionGlobals<T>)).CreateDelegate();
				_asyncHandler = (o, c, s, t) =>
				{
					Log.Trace($"InvokeAsync '{_expression}' with obj={o}, state={s} and Transition={t}");
					return runner.Invoke(new StateActionGlobals<T>(o, c, s, t));
				};
				_syncHandler = (o, c, s, t) =>
				{
					Log.Trace($"Invoke '{_expression}' with obj={o}, state={s} and Transition={t}");
					runner.Invoke(new StateActionGlobals<T>(o, c, s, t)).GetAwaiter().GetResult();
				};
			}
		}
	}

	#endregion
}