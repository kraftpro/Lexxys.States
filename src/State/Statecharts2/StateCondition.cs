using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using Lexxys;

namespace State.Statecharts2
{
	public abstract class StateCondition<TState, TEntity>
	{
		public abstract bool Evaluate(TEntity scope);
	}

	public static class StateCondition
	{
		public static StateCondition<TState, TEntity> Create<TState, TEntity>(string expression)
		{
			return (expression = expression.TrimToNull()) == null ? null: new ActualCondition<TState, TEntity>(expression);
		}

		public static StateCondition<TState, TEntity> Create<TState, TEntity>(Func<TEntity, bool> expression)
		{
			return expression == null ? EmptyCondition<TState, TEntity>.Value : new SimpleCondition<TState, TEntity>(expression);
		}

		public static StateCondition<TState, TEntity> Or<TState, TEntity>(StateCondition<TState, TEntity> left, StateCondition<TState, TEntity> right)
		{
			return IsEmpty(left) ? right ?? EmptyCondition<TState, TEntity>.Value :
				IsEmpty(right) ? left ?? EmptyCondition<TState, TEntity>.Value : new OrCondition<TState, TEntity>(left, right);
		}

		public static StateCondition<TState, TEntity> And<TState, TEntity>(StateCondition<TState, TEntity> left, StateCondition<TState, TEntity> right)
		{
			return IsEmpty(left) ? right ?? EmptyCondition<TState, TEntity>.Value:
				IsEmpty(right) ? left ?? EmptyCondition<TState, TEntity>.Value : new AndCondition<TState, TEntity>(left, right);
		}

		private static bool IsEmpty<TState, TEntity>(StateCondition<TState, TEntity> value)
		{
			return value == null || value == EmptyCondition<TState, TEntity>.Value;
		}

		private class OrCondition<TState, TEntity>: StateCondition<TState, TEntity>
		{
			private readonly StateCondition<TState, TEntity> _left;
			private readonly StateCondition<TState, TEntity> _right;

			public OrCondition(StateCondition<TState, TEntity> left, StateCondition<TState, TEntity> right)
			{
				_left = left;
				_right = right;
			}

			public override bool Evaluate(TEntity scope)
			{
				return _left.Evaluate(scope) || _right.Evaluate(scope);
			}
		}

		class AndCondition<TState, TEntity>: StateCondition<TState, TEntity>
		{
			private readonly StateCondition<TState, TEntity> _left;
			private readonly StateCondition<TState, TEntity> _right;

			public AndCondition(StateCondition<TState, TEntity> left, StateCondition<TState, TEntity> right)
			{
				_left = left;
				_right = right;
			}

			public override bool Evaluate(TEntity scope)
			{
				return _left.Evaluate(scope) && _right.Evaluate(scope);
			}
		}

		class EmptyCondition<TState, TEntity>: StateCondition<TState, TEntity>
		{
			public static readonly StateCondition<TState, TEntity> Value = new EmptyCondition<TState, TEntity>();

			public override bool Evaluate(TEntity scope)
			{
				return true;
			}
		}

		class SimpleCondition<TState, TEntity>: StateCondition<TState, TEntity>
		{
			private readonly Func<TEntity, bool> _condition;

			public SimpleCondition(Func<TEntity, bool> condition)
			{
				_condition = condition;
			}

			public override bool Evaluate(TEntity scope)
			{
				return _condition(scope);
			}
		}

		class ActualCondition<TState, TEntity>: StateCondition<TState, TEntity>
		{
			private readonly string _expression;
			private Type _scopeType;
			private Script<bool> _script;
			private ScriptRunner<bool> _function;

			public ActualCondition(string expression)
			{
				_expression = expression;
			}

			public override bool Evaluate(TEntity scope)
			{
				Type type = scope?.GetType();
				if (_function == null || type != _scopeType)
				{
					_scopeType = type;
					_script = type == null ? CSharpScript.Create<bool>(_expression, globalsType: type): CSharpScript.Create<bool>(_expression, globalsType: type);
					_function = _script.CreateDelegate();
				}
				return scope == null ? _function().Result: _function(scope).Result;
			}
		}
	}
}