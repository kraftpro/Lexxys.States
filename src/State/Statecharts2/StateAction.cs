using System;
using System.Threading.Tasks;
using Lexxys;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace State.Statecharts2
{
	public abstract class StateAction<TState, TEntity>
	{
		public abstract void Evaluate(State<TState, TEntity> state, TEntity scope);

		public virtual Action<State<TState, TEntity>, TEntity> GetHandler()
		{
			return Evaluate;
		}
	}

	public static class StateAction
	{
		public static StateAction<TState, TEntity> Create<TState, TEntity>(string expression)
		{
			return (expression = expression.TrimToNull()) == null ? null : new RoslynAction<TState, TEntity>(expression);
		}

		public static StateAction<TState, TEntity> Create<TState, TEntity>(Action<State<TState, TEntity>, TEntity> expression)
		{
			return expression == null ? EmptyAction<TState, TEntity>.Value : new SimpleAction<TState, TEntity>(expression);
		}

		private class EmptyAction<TState, TEntity>: StateAction<TState, TEntity>
		{
			public static readonly StateAction<TState, TEntity> Value = new EmptyAction<TState, TEntity>();

			public override void Evaluate(State<TState, TEntity> state, TEntity scope)
			{
			}

			public override Action<State<TState, TEntity>, TEntity> GetHandler()
			{
				return null;
			}
		}

		class SimpleAction<TState, TEntity>: StateAction<TState, TEntity>
		{
			private readonly Action<State<TState, TEntity>, TEntity> _action;

			public SimpleAction(Action<State<TState, TEntity>, TEntity> action)
			{
				_action = action ?? throw new ArgumentNullException(nameof(action));
			}

			public override void Evaluate(State<TState, TEntity> state, TEntity scope)
			{
				_action(state, scope);
			}
		}

		class RoslynAction<TState, TEntity>: StateAction<TState, TEntity>
		{
			private readonly string _expression;
			private ScriptRunner<object> _action;
			private Script<object> _script;

			public RoslynAction(string expression)
			{
				_expression = expression;
			}

			public override void Evaluate(State<TState, TEntity> state, TEntity scope)
			{
				EvaluateAsync(state, scope).GetAwaiter().GetResult();
			}

			public Task EvaluateAsync(State<TState, TEntity> state, TEntity entity)
			{
				if (_action == null)
				{
					Console.WriteLine($"ActualAction.Compile '{_expression}' for entity {typeof(TEntity)}");
					_script = CSharpScript.Create(_expression, globalsType: typeof(RoslynGlobals<TState, TEntity>));
					_action = _script.CreateDelegate();
				}
				Console.WriteLine($"ActualAction.Evaluate '{_expression}' on {(object)entity ?? "null"}");
				return _action(new RoslynGlobals<TState, TEntity>(entity, state));
			}
		}

		public class RoslynGlobals<TState, TEntity>
		{
			public State<TState, TEntity> State { get; }
			public Transition<TState, TEntity> Transition { get; }
			public TEntity Entity { get; }

			public RoslynGlobals(TEntity entity, State<TState, TEntity> state = null, Transition<TState, TEntity> transition = null)
			{
				State = state;
				Transition = transition;
				Entity = entity;
			}
		}
	}
}