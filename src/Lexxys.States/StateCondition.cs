﻿//#define TRACE_ROSLYN
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Lexxys.States
{
	public interface IStateCondition<T>
	{
		Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate();
		Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate();
	}

	public static class StateCondition
	{
		delegate bool StateDelegate<T>(T value, Statechart<T> chart, State<T>? state, Transition<T>? transition);

		public static bool Invoke<T>(this IStateCondition<T> condition, T value, Statechart<T> statechart, State<T>? state, Transition<T>? transition) => condition.GetDelegate().Invoke(value, statechart, state, transition);
		public static Task<bool> InvokeAsync<T>(this IStateCondition<T> condition, T value, Statechart<T> statechart, State<T>? state, Transition<T>? transition) => condition.GetAsyncDelegate().Invoke(value, statechart, state, transition);

		public static IStateCondition<T> Subcharts<T>(Func<IReadOnlyList<Statechart<T>>, bool> condition) => Create<T>((o, c, s, t) => condition(s!.Charts));

		public static IStateCondition<T> True<T>() => TrueCondition<T>.Instance;
		public static IStateCondition<T> False<T>() => FalseCondition<T>.Instance;

		public static IStateCondition<T> Not<T>(IStateCondition<T> condition) => NotCondition<T>.Create(condition);

		public static IStateCondition<T> Or<T>(IStateCondition<T> left, IStateCondition<T> right) => OrCondition<T>.Create(left, right);

		public static IStateCondition<T> And<T>(IStateCondition<T> left, IStateCondition<T> right) => AndCondition<T>.Create(left, right);

		public static IStateCondition<T> Create<T>(string expression) => RoslynCondition<T>.Create(expression);

		public static IStateCondition<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> expression, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>>? expression2 = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>(expression, expression2);
		}

		public static IStateCondition<T> Create<T>(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>(expression);
		}

		public static IStateCondition<T> Create<T>(Func<T, State<T>?, bool> expression, Func<T, State<T>?, Task<bool>>? expression2 = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>((o,c,s,t) => expression(o,s), expression2 == null ? null: (o,c,s,t) => expression2(o, s));
		}

		public static IStateCondition<T> Create<T>(Func<T, State<T>?, Task<bool>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>((o, c, s, t) => expression(o, s));
		}

		public static IStateCondition<T> Create<T>(Func<T, Transition<T>?, bool> expression, Func<T, Transition<T>?, Task<bool>>? expression2 = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>((o, c, s, t) => expression(o, t), expression2 == null ? null : (o, c, s, t) => expression2(o, t));
		}

		public static IStateCondition<T> Create<T>(Func<T, Transition<T>?, Task<bool>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>((o, c, s, t) => expression(o, t));
		}

		public static IStateCondition<T> Create<T>(Func<T, bool> expression, Func<T, Task<bool>>? expression2 = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>((o, c, s, t) => expression(o), expression2 == null ? null : (o, c, s, t) => expression2(o));
		}

		public static IStateCondition<T> Create<T>(Func<T, Task<bool>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new DelegateCondition<T>((o, c, s, t) => expression(o));
		}

		private static bool IsTrue<T>(IStateCondition<T> condition) => condition == TrueCondition<T>.Instance;
		private static bool IsFalse<T>(IStateCondition<T> condition) => condition == FalseCondition<T>.Instance;

		private class NotCondition<T>: IStateCondition<T>
		{
			private readonly IStateCondition<T> _predicate;

			private NotCondition(IStateCondition<T> predicate)
			{
				_predicate = predicate;
			}

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => async (o, c, s, t) => !await _predicate.InvokeAsync(o, c, s, t);

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => (o, c, s, t) => !_predicate.Invoke(o, c, s, t);

			public override string ToString() => $"~({_predicate})";

			public static IStateCondition<T> Create(IStateCondition<T> condition)
			{
				if (condition == null)
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

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => async (o, c, s, t) => await _left.InvokeAsync(o, c, s, t) || await _right.InvokeAsync(o, c, s, t);
			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => (o, c, s, t) => _left.Invoke(o, c, s, t) || _right.Invoke(o, c, s, t);

			public override string ToString() => $"({_left}) | ({_right})";

			public static IStateCondition<T> Create(IStateCondition<T> left, IStateCondition<T> right)
			{
				if (left == null)
					throw new ArgumentNullException(nameof(left));
				if (right == null)
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

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => async (o, c, s, t) => await _left.InvokeAsync(o, c, s, t) || await _right.InvokeAsync(o, c, s, t);
			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => (o, c, s, t) => _left.Invoke(o, c, s, t) || _right.Invoke(o, c, s, t);

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

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => (o, c, s, t) => Task.FromResult(false);

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => (o, c, s, t) => false;

			public override string ToString() => "false";
		}

		private class TrueCondition<T>: IStateCondition<T>
		{
			public static IStateCondition<T> Instance = new TrueCondition<T>();

			private TrueCondition()
			{
			}

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => (o, c, s, t) => Task.FromResult(true);

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => (o, c, s, t) => true;

			public override string ToString() => "true";
		}

		private class DelegateCondition<T>: IStateCondition<T>
		{
			private readonly Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> _condition;
			private readonly Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> _condition2;

			public DelegateCondition(Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> condition, Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>>? condition2 = null)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				_condition2 = condition2 ?? ((o,c,s,t) => Task.FromResult(condition(o, c, s, t)));
			}

			public DelegateCondition(Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> condition)
			{
				_condition2 = condition ?? throw new ArgumentNullException(nameof(condition));
				_condition = (o, c, s, t) => condition(o, c, s, t).ConfigureAwait(false).GetAwaiter().GetResult();
			}

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate() => _condition;

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate() => _condition2;

			public override string ToString()
			{
				return $"Condition<{typeof(T).Name}>";
			}
		}

		private class RoslynCondition<T>: IStateCondition<T>
		{
			private readonly string _expression;
			private Func<T, Statechart<T>, State<T>?, Transition<T>?, bool>? _predicate;
			private Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>>? _asyncPredicate;

			private RoslynCondition(string expression)
			{
				_expression = expression;
			}

			public static IStateCondition<T> Create(string expression)
			{
				expression = expression.TrimToNull();
				if (expression == null)
					throw new ArgumentNullException(nameof(expression));
				return __compiledConditions.GetOrAdd(expression, CreateRoslyn);

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
				if (_asyncPredicate == null)
					lock (this)
					{
						if (_asyncPredicate == null)
						{
#if TRACE_ROSLYN
							Console.WriteLine($"  RoslynAction.Compile '{_expression}' for {typeof(T)}");
#endif
							var runner = CSharpScript.Create<bool>(_expression, globalsType: typeof(StateActionGlobals<T>)).CreateDelegate();
							_asyncPredicate = (o, c, s, t) =>
							{
#if TRACE_ROSLYN
								Console.WriteLine($"  RoslynCondition.Evaluate '{_expression}' on {context ?? "null"}");
#endif
								return runner.Invoke(new StateActionGlobals<T>(o, c, s, t));
							};
							_predicate = (o, c, s, t) =>
							{
#if TRACE_ROSLYN
				Console.WriteLine($"  RoslynCondition.EvaluateAsync '{_expression}' on {context ?? "null"}");
#endif
								return runner.Invoke(new StateActionGlobals<T>(o, c, s, t)).ConfigureAwait(false).GetAwaiter().GetResult();
							};
						}
					}
			}
		}
	}
}