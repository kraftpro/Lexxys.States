using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Linq;

using Lexxys;
using System.Security.Principal;
using System;

namespace State.Statecharts2
{
	[DebuggerDisplay("{Name,nq} Transitions.Count={Transitions.Count}, Subchart={Subchart?.Name,nq}")]
	public class SupperState<TState, TEntity>: State<TState, TEntity>
	{
		public Statechart<TState, TEntity> Subchart { get; }

		public SupperState(TState value, string name, string permission = null, Statechart<TState, TEntity> subchart = null, StateCondition<TState, TEntity> condition = null, StateAction<TState, TEntity> onpassthrough = null, StateAction<TState, TEntity> onenter = null, StateAction<TState, TEntity> onexit = null)
			:base(value, name, permission, condition, onpassthrough, onenter, onexit)
		{
			Subchart = subchart ?? throw new ArgumentNullException(nameof(subchart));
		}

		public SupperState(SupperState<TState, TEntity> that)
			: base(that)
		{
			Contract.Requires(that != null);
			Subchart = new Statechart<TState, TEntity>(that.Subchart);
		}
	}
}