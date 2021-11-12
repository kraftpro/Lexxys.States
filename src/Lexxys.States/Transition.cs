using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States
{
	public class Transition<T>
	{
		private static Logger Log => __log ??= new Logger(nameof(Transition<T>));
		private static Logger? __log;

		public Token Event { get; }
		public IStateCondition<T>? Guard { get; }
		public IStateAction<T>? Action { get; }
		public State<T> Source { get; }
		public State<T> Destination { get; }
		public bool Continues { get; }
		public IReadOnlyCollection<string> Roles { get; }

		public Transition(State<T>? source, State<T> destination, Token? @event = null, bool continues = false, IStateAction<T>? action = null, IStateCondition<T>? guard = null, IReadOnlyCollection<string>? roles = default)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			if (destination.IsEmpty)
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			Source = source ?? State<T>.Empty;
			Destination = destination;
			Event = @event ?? Token.Empty;
			Continues = continues;
			Action = action;
			Guard = guard;
			Roles = roles ?? Array.Empty<string>();
		}

		public void Accept(IStatechartVisitor<T> visitor) => visitor.Visit(this);

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append('(').Append(Source.Id).Append('.').Append(Source.Name)
				.Append(") -> (")
				.Append(Destination.Id).Append('.').Append(Destination.Name).Append(')');

			if (!Event.IsEmpty())
			{
				text.Append(" /");
				if (Event.Id > 0)
					text.Append(Event.Id).Append('.');
				text.Append(Event.Name);
			}

			if (Guard != null)
				text.Append(" [...]");
			if (Event.Description != null)
				text.Append(" - ").Append(Event.Description);
			return text.ToString();
		}

		#region Sync

		internal bool CanMoveAlong(T value, Statechart<T> statechart, IPrincipal? principal) => IsInRole(principal) && InvokeGuard(value, statechart) && Destination.CanEnter(value, statechart, principal);

		internal void OnMoveAlong(T value, Statechart<T> statechart)
		{
			Log.Trace($"{Event.FullName()}: {nameof(OnMoveAlong)} {Source.Name} -> {Destination.Name}");
			Action?.Invoke(value, statechart, Source, this);
		}

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

		private bool InvokeGuard(T value, Statechart<T> statechart) => Guard == null || Guard.Invoke(value, statechart, Source, this);

		#endregion

		#region Async

		internal async Task<bool> CanMoveAlongAsync(T value, Statechart<T> statechart, IPrincipal? principal) => IsInRole(principal) && await InvokeGuardAsync(value, statechart) && await Destination.CanEnterAsync(value, statechart, principal);

		internal async Task OnMoveAlongAsync(T value, Statechart<T> statechart)
		{
			Log.Trace($"{Event.FullName()}: {nameof(OnMoveAlongAsync)} {Source.Name} -> {Destination.Name}");
			if (Action != null)
				await Action.InvokeAsync(value, statechart, Source, this);
		}

		private async Task<bool> InvokeGuardAsync(T value, Statechart<T> statechart) => Guard == null || await Guard.InvokeAsync(value, statechart, Source, this);

		#endregion
	}

	public record TransitionEvent<T>(Statechart<T> Chart, Transition<T> Transition)
	{
	}
}
