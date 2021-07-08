using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Linq;

using Lexxys;
using System.Security.Principal;
using System;

namespace State.Statecharts2
{
	[DebuggerDisplay("{Name,nq} Transitions.Count={Transitions.Count}, Subchart={Subchart == null ? null: Subchart.Name,nq}")]
	public class State<TState, TEntity>
	{
		public static readonly State<TState, TEntity> Star = new State<TState, TEntity>(default, "*") { Transitions = ReadOnly.Empty<Transition<TState, TEntity>>() };

		public TState Value { get; }
		public string Name { get; }
		public string Permission { get; }
		public StateCondition<TState, TEntity> Condition { get; }
		public IReadOnlyList<Transition<TState, TEntity>> Transitions { get; internal set; }

		public event Action<State<TState, TEntity>, TEntity> StateEnter;
		public event Action<State<TState, TEntity>, TEntity> StateExit;
		public event Action<State<TState, TEntity>, TEntity> StatePassThrough;

		public State(TState value, string name, string permission = null, StateCondition<TState, TEntity> condition = null, StateAction<TState, TEntity> onpassthrough = null, StateAction<TState, TEntity> onenter = null, StateAction<TState, TEntity> onexit = null)
		{
			Value = value;
			Name = name;
			Condition = condition;
			Permission = permission;
			StatePassThrough += onpassthrough?.GetHandler();
			StateEnter += onenter?.GetHandler();
			StateExit += onexit?.GetHandler();
		}

		public State(State<TState, TEntity> that)
		{
			Contract.Requires(that != null);
			Name = that.Name;
			Permission = that.Permission;
			Transitions = ReadOnly.Wrap(that.Transitions.Select(o => new Transition<TState, TEntity>(o)).ToList());
		}

		public bool IsFinal => Transitions.Count == 0;

		public bool CanEnter(TEntity context, IPrincipal principal)
		{
			return (principal == null || Permission == null || principal.IsInRole(Permission)) &&
				Condition.Evaluate(context);
		}

		public State<TState, TEntity> OnEvent(string eventName, TEntity context, IPrincipal principal)
		{
			var transition = Transitions.SingleOrDefault(o => o.Event == eventName && o.CanMoveAlong(context, principal));
			if (transition == null)
				return null;

			OnStateExit(context);
			transition.Action?.Evaluate(this, context);
			transition.Target.OnStateEnter(context);

			return transition.Target;
		}

		protected virtual void OnStateEnter(TEntity e) => StateEnter?.Invoke(this, e);

		protected virtual void OnStateExit(TEntity e) => StateExit?.Invoke(this, e);

		protected virtual void OnStatePassThrough(TEntity e) => StatePassThrough?.Invoke(this, e);
	}
}