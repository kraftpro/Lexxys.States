using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Lexxys.States;

public static class StateCondition
{
	public static bool Invoke<T>(this IStateCondition<T> condition, T value, Statechart<T> statechart, State<T>? state, Transition<T>? transition)
		=> (condition ?? throw new ArgumentNullException(nameof(condition))).GetDelegate().Invoke(value, statechart, state, transition);
	public static Task<bool> InvokeAsync<T>(this IStateCondition<T> condition, T value, Statechart<T> statechart, State<T>? state, Transition<T>? transition)
		=> (condition ?? throw new ArgumentNullException(nameof(condition))).GetAsyncDelegate().Invoke(value, statechart, state, transition);

	public static IStateCondition<T> Subcharts<T>(Func<IReadOnlyCollection<Statechart<T>>, bool> condition) => Create<T>((_,_,s, _) => condition(s!.Charts));

	public static IStateCondition<T> True<T>() => TrueCondition<T>.Instance;
	public static IStateCondition<T> False<T>() => FalseCondition<T>.Instance;

	public static IStateCondition<T> Not<T>(IStateCondition<T> condition) => NotCondition<T>.Create(condition);

	public static IStateCondition<T> Or<T>(IStateCondition<T> left, IStateCondition<T> right) => OrCondition<T>.Create(left, right);

	public static IStateCondition<T> And<T>(IStateCondition<T> left, IStateCondition<T> right) => AndCondition<T>.Create(left, right);

	public static IStateCondition<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, bool>? expression, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>>? asyncExpression = null)
	{
		if (expression is null && asyncExpression is null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateCondition<T>(expression, asyncExpression);
	}

	public static IStateCondition<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> asyncExpression)
	{
		if (asyncExpression is null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateCondition<T>(null, asyncExpression);
	}

	public static IStateCondition<T> Create<T>(Func<T, Statechart<T>, bool>? expression, Func<T, Statechart<T>, Task<bool>>? asyncExpression = null)
	{
		if (expression is null && asyncExpression is null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateCondition<T>(expression is null ? null: (o, c, _,_) => expression(o,c), asyncExpression is null ? null: (o, c, _,_) => asyncExpression(o, c));
	}

	public static IStateCondition<T> Create<T>(Func<T, Statechart<T>, Task<bool>> asyncExpression)
	{
		if (asyncExpression is null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateCondition<T>(null, (o, c, _,_) => asyncExpression(o, c));
	}

	public static IStateCondition<T> Create<T>(Func<T, bool>? expression, Func<T, Task<bool>>? asyncExpression = null)
	{
		if (expression is null && asyncExpression is null)
			throw new ArgumentNullException(nameof(expression));
		return new DelegateCondition<T>(expression is null ? null: (o, _,_,_) => expression(o), asyncExpression is null ? null: (o, _,_,_) => asyncExpression(o));
	}

	public static IStateCondition<T> Create<T>(Func<T, Task<bool>> asyncExpression)
	{
		if (asyncExpression is null)
			throw new ArgumentNullException(nameof(asyncExpression));
		return new DelegateCondition<T>(null, (o, _,_,_) => asyncExpression(o));
	}

	public static IStateCondition<T> CSharpScript<T>(string expression) => RoslynCondition<T>.Create(expression);

	private static bool IsTrue<T>(IStateCondition<T> condition) => condition == TrueCondition<T>.Instance;
	private static bool IsFalse<T>(IStateCondition<T> condition) => condition == FalseCondition<T>.Instance;

	private class NotCondition<T>: IStateCondition<T>
	{
		private readonly IStateCondition<T> _predicate;

		private NotCondition(IStateCondition<T> predicate)
		{
			_predicate = predicate;
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
			=> async (o, c, s, t) => !await _predicate.InvokeAsync(o, c, s, t).ConfigureAwait(false);

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => (o, c, s, t)
			=> !_predicate.Invoke(o, c, s, t);

		public override string ToString() => $"~({_predicate})";

		public static IStateCondition<T> Create(IStateCondition<T> condition)
		{
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			return IsFalse(condition) ? True<T>() : IsTrue(condition) ? False<T>() : new NotCondition<T>(condition);
		}
	}

	private class OrCondition<T>: IStateCondition<T>
	{
		private readonly IStateCondition<T> _left;
		private readonly IStateCondition<T> _right;

		private OrCondition(IStateCondition<T> left, IStateCondition<T> right)
		{
			_left = left;
			_right = right;
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
			=> async (o, c, s, t) => await _left.InvokeAsync(o, c, s, t).ConfigureAwait(false) || await _right.InvokeAsync(o, c, s, t).ConfigureAwait(false);
		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate()
			=> (o, c, s, t) => _left.Invoke(o, c, s, t) || _right.Invoke(o, c, s, t);

		public override string ToString() => $"({_left}) | ({_right})";

		public static IStateCondition<T> Create(IStateCondition<T> left, IStateCondition<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left));
			if (right is null)
				throw new ArgumentNullException(nameof(right));
			return
				IsTrue(left) || IsTrue(right) ? True<T>() :
				IsFalse(left) ? right :
				IsFalse(right) ? left : new OrCondition<T>(left, right);
		}
	}

	private class AndCondition<T>: IStateCondition<T>
	{
		private readonly IStateCondition<T> _left;
		private readonly IStateCondition<T> _right;

		private AndCondition(IStateCondition<T> left, IStateCondition<T> right)
		{
			_left = left;
			_right = right;
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
			=> async (o, c, s, t) => await _left.InvokeAsync(o, c, s, t).ConfigureAwait(false) || await _right.InvokeAsync(o, c, s, t).ConfigureAwait(false);
		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate()
			=> (o, c, s, t) => _left.Invoke(o, c, s, t) || _right.Invoke(o, c, s, t);

		public override string ToString() => $"({_left}) & ({_right})";

		public static IStateCondition<T> Create(IStateCondition<T> left, IStateCondition<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left));
			if (right is null)
				throw new ArgumentNullException(nameof(right));
			return
				IsFalse(left) || IsFalse(right) ? False<T>() :
				IsTrue(left) ? right :
				IsTrue(right) ? left : new AndCondition<T>(left, right);
		}
	}

	private class FalseCondition<T>: IStateCondition<T>
	{
		public static IStateCondition<T> Instance = new FalseCondition<T>();

		private FalseCondition()
		{
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
			=> (_,_,_,_) => Task.FromResult(false);

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate()
			=> (_,_,_,_) => false;

		public override string ToString() => "false";
	}

	private class TrueCondition<T>: IStateCondition<T>
	{
		public static IStateCondition<T> Instance = new TrueCondition<T>();

		private TrueCondition()
		{
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
			=> (_,_,_,_) => Task.FromResult(true);

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate()
			=> (_,_,_,_) => true;

		public override string ToString() => "true";
	}

	private class DelegateCondition<T>: IStateCondition<T>
	{
		private readonly Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> _syncCondition;
		private readonly Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> _asyncCondition;

		public DelegateCondition(Func<T, Statechart<T>, State<T>?, Transition<T>?, bool>? syncCondition, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>>? asyncCondition = null)
		{
			if (syncCondition is null && asyncCondition is null)
				throw new ArgumentNullException(nameof(syncCondition));
			_syncCondition = syncCondition ?? ((o, c,  s, t) => asyncCondition!(o, c, s, t).ConfigureAwait(false).GetAwaiter().GetResult());
			_asyncCondition = asyncCondition ?? ((o, c, s, t) => Task.Run(() => syncCondition!(o, c, s, t)));
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => _syncCondition;

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => _asyncCondition;

		public override string ToString()
		{
			return $"Condition<{typeof(T).Name}>";
		}
	}

	private class RoslynCondition<T>: IStateCondition<T>
	{
		private static ILogger Log => __log ??= Statics.GetLogger<RoslynCondition<T>>();
		private static ILogger? __log;

		private readonly string _expression;
		private Func<T, Statechart<T>, State<T>?, Transition<T>?, bool>? _predicate;
		private Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>>? _asyncPredicate;

		private RoslynCondition(string expression)
		{
			_expression = expression;
		}

		public static IStateCondition<T> Create(string expression)
		{
			var exp = expression.TrimToNull();
			if (exp is null)
				throw new ArgumentNullException(nameof(expression));
			return __compiledConditions.GetOrAdd(exp, CreateRoslyn);

			static RoslynCondition<T> CreateRoslyn(string expression) => new RoslynCondition<T>(expression);
		}
		private static readonly ConcurrentDictionary<string, RoslynCondition<T>> __compiledConditions = new();

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate()
		{
			Compile();
			return _predicate!;
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
		{
			Compile();
			return _asyncPredicate!;
		}

		public override string ToString()
		{
			return _expression;
		}

		private void Compile()
		{
			if (_asyncPredicate is not null)
				return;
			#pragma warning disable CA2002 // Safe for private class
			lock (this)
			#pragma warning restore CA2002
			{
				if (_asyncPredicate is not null)
					return;
				if (Log.IsEnabled(LogType.Trace))
					Log.Trace($"Compile '{_expression}'");

				var runner = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<bool>(_expression,
					ScriptOptions.Default
						.AddReferences(RoslynHelper.GetReferences<T>())
						.AddImports(RoslynHelper.GetImports()),
					typeof(StateActionGlobals<T>)).CreateDelegate();

				_asyncPredicate = (o, c, s, t) =>
				{
					if (Log.IsEnabled(LogType.Trace))
						Log.Trace($"InvokeAsync '{_expression}' with obj={o}, state={s} and Transition={t}");
					return runner.Invoke(new StateActionGlobals<T>(o, c, s, t));
				};
				_predicate = (o, c, s, t) =>
				{
					if (Log.IsEnabled(LogType.Trace))
						Log.Trace($"Invoke '{_expression}' with obj={o}, state={s} and Transition={t}");
					return runner.Invoke(new StateActionGlobals<T>(o, c, s, t)).ConfigureAwait(false).GetAwaiter().GetResult();
				};
			}
		}
	}
}