using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Principal;

namespace State.Statecharts
{
	[DebuggerDisplay("[{Event,nq}] => {Target.Name,nq}")]
	public class Transition<T>
	{
		/// <summary>
		/// The special event which is fired when all <see cref="Source.Subchats"/> are finished
		/// </summary>
		public static readonly object Finished = new object();

		public State<T> Source { get; }
		public State<T> Target { get; }
		public object Event { get; }
		public IStateCondition<T> Guard { get; }
		public IStateAction<T> Action { get; }

		public Transition(object @event, State<T> source, State<T> target, IStateCondition<T> guard, IStateAction<T> action)
		{
			Event = @event;
			Source = source ?? throw new ArgumentNullException(nameof(source));
			Target = target ?? throw new ArgumentNullException(nameof(source));
			Guard = guard ?? StateCondition.True<T>();
			Action = action;

			//Condition = Event != null || source.Subcharts == null ? condition :
			//	StateCondition<T>.Or(condition, StateCondition<T>.Create(o => Source.Subcharts.IsFinished));

			//if (@event == StarEvent || !(source is SupperState supper))
			//	Condition = condition;
			//else
			//	Condition = StateCondition<T>.Or(
			//		condition,
			//		StateCondition<T>.Create(o => supper.Subchart.Current.IsFinal));
		}

		//public Transition<T>(Transition<T> that)
		//{
		//	Contract.Requires(that != null);
		//	Source = new State<T>(that.Source);
		//	Target = new State<T>(that.Target);
		//	Event = that.Event;
		//	Condition = that.Condition;
		//	Action = that.Action;
		//}

		internal void Accept(IStatechartVisitor<T> visitor)
		{
			if (visitor == null)
				throw new ArgumentNullException(nameof(visitor));
			visitor.Visit(this);
		}

		public bool CanMoveAlong(T context, IPrincipal principal)
		{
			return Guard.Invoke(context) && Target.CanEnter(context, principal);
		}

		internal void OnMoveAlong(T context)
		{
#if TRACE_EVENTS
			System.Console.WriteLine($"# {Source.Name}> {Target.Name} [{Event}]: Action");
#endif
			Action?.Invoke(context, Source, this);
		}
	}
}