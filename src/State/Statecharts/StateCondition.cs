//#define TRACE_ROSLYN
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Lexxys;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace State.Statecharts
{
	public interface IStateCondition<T>
	{
		Func<T, bool> GetDelegate();
		Func<T, Task<bool>> GetAsyncDelegate();
	}

	public static class StateCondition
	{
		public static bool Invoke<T>(this IStateCondition<T> condition, T context) => condition.GetDelegate().Invoke(context);
		public static Task<bool> InvokeAsync<T>(this IStateCondition<T> condition, T context) => condition.GetAsyncDelegate().Invoke(context);

		public static IStateCondition<T> True<T>() => TrueCondition<T>.Instance;
		public static IStateCondition<T> False<T>() => FalseCondition<T>.Instance;

		public static IStateCondition<T> Not<T>(IStateCondition<T> condition) => NotCondition<T>.Create(condition);

		public static IStateCondition<T> Or<T>(IStateCondition<T> left, IStateCondition<T> right) => OrCondition<T>.Create(left, right);

		public static IStateCondition<T> And<T>(IStateCondition<T> left, IStateCondition<T> right) => AndCondition<T>.Create(left, right);

		public static IStateCondition<T> Create<T>(string expression) => RoslynCondition<T>.Create(expression);

		public static IStateCondition<T> Create<T>(Func<T, bool> expression, Func<T, Task<bool>> expression2 = null)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new SimpleCondition<T>(expression, expression2);
		}

		public static IStateCondition<T> Create<T>(Func<T, Task<bool>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return new SimpleCondition<T>(expression);
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

			public Func<T, Task<bool>> GetAsyncDelegate() => async o => !await _predicate.InvokeAsync(o);

			public Func<T, bool> GetDelegate() => o => !_predicate.Invoke(o);

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

			public Func<T, Task<bool>> GetAsyncDelegate() => async o => await _left.InvokeAsync(o) || await _right.InvokeAsync(o);
			public Func<T, bool> GetDelegate() => o => _left.Invoke(o) || _right.Invoke(o);

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

			public Func<T, Task<bool>> GetAsyncDelegate() => async o => await _left.InvokeAsync(o) || await _right.InvokeAsync(o);
			public Func<T, bool> GetDelegate() => o => _left.Invoke(o) || _right.Invoke(o);

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

			public Func<T, Task<bool>> GetAsyncDelegate() => o => Task.FromResult(false);

			public Func<T, bool> GetDelegate() => o => false;
		}

		private class TrueCondition<T>: IStateCondition<T>
		{
			public static IStateCondition<T> Instance = new TrueCondition<T>();

			private TrueCondition()
			{
			}

			public bool Evaluate(T context) => true;

			public Task<bool> EvaluateAsync(T context) => Task.FromResult(true);

			public Func<T, Task<bool>> GetAsyncDelegate() => o => Task.FromResult(true);

			public Func<T, bool> GetDelegate() => o =>true;
		}

		private class SimpleCondition<T>: IStateCondition<T>
		{
			private readonly Func<T, bool> _condition;
			private readonly Func<T, Task<bool>> _condition2;

			public SimpleCondition(Func<T, bool> condition, Func<T, Task<bool>> condition2 = null)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				_condition2 = condition2 ?? (o => Task.FromResult(condition(o)));
			}

			public SimpleCondition(Func<T, Task<bool>> condition)
			{
				_condition2 = condition ?? throw new ArgumentNullException(nameof(condition));
				_condition = o => condition(o).ConfigureAwait(false).GetAwaiter().GetResult();
			}

			public Func<T, bool> GetDelegate() => _condition;

			public Func<T, Task<bool>> GetAsyncDelegate() => _condition2;
		}

		private class RoslynCondition<T>: IStateCondition<T>
		{
			private readonly string _expression;
			private Func<T, bool> _predicate;
			private Func<T, Task<bool>> _asyncPredicate;

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

			public Func<T, bool> GetDelegate()
			{
				Compile();
				return _predicate;
			}

			public Func<T, Task<bool>> GetAsyncDelegate()
			{
				Compile();
				return _asyncPredicate;
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
							var runner = CSharpScript.Create<bool>(_expression, globalsType: typeof(T)).CreateDelegate();
							_asyncPredicate = o =>
							{
#if TRACE_ROSLYN
								Console.WriteLine($"  RoslynCondition.Evaluate '{_expression}' on {context ?? "null"}");
#endif
								return runner.Invoke(o);
							};
							_predicate = o =>
							{
#if TRACE_ROSLYN
				Console.WriteLine($"  RoslynCondition.EvaluateAsync '{_expression}' on {context ?? "null"}");
#endif
								return runner.Invoke(o).ConfigureAwait(false).GetAwaiter().GetResult();
							};
						}
					}
			}
		}
	}
}