using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Security.Principal;

namespace State.Statecharts2
{


	[DebuggerDisplay("[{Event,nq}] => {Target.Name,nq}")]
	public class Transition<TState, TEntity>
	{
		public const string StarEvent = "*";

		public State<TState, TEntity> Source { get; }
		public State<TState, TEntity> Target { get; }
		public string Event { get; }
		public StateCondition<TState, TEntity> Condition { get; }
		public StateAction<TState, TEntity> Action { get; }

		public Transition(string @event, State<TState, TEntity> source, State<TState, TEntity> target, StateCondition<TState, TEntity> condition, StateAction<TState, TEntity> action)
		{
			Event = @event == StarEvent ? null : @event;
			Source = source;
			Target = target;
			var supper = source as SupperState<TState, TEntity>;
			Condition = @event != StarEvent || supper == null ? condition:
				StateCondition.Or(
					condition,
					StateCondition.Create<TState, TEntity>(o => supper.Subchart != null && supper.Subchart.Current.IsFinal));
			Action = action;
		}

		public Transition(Transition<TState, TEntity> that)
		{
			Contract.Requires(that != null);
			Source = new State<TState, TEntity>(that.Source);
			Target = new State<TState, TEntity>(that.Target);
			Event = that.Event;
			Condition = that.Condition;
			Action = that.Action;
		}

		public bool CanMoveAlong(TEntity context, IPrincipal principal)
		{
			return Condition.Evaluate(context) && Target.CanEnter(context, principal);
		}
	}
}