using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;

namespace Lexxys.States
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class Transition<T>
	{
		public Token Event { get; }
		public IStateCondition<T>? Guard { get; }
		public IStateAction<T>? Action { get; }
		public State<T> Source { get; }
		public State<T> Destination { get; }
		public IReadOnlyList<string> Roles { get; }

		public Transition(State<T>? source, State<T> destination, Token? @event = null, IStateAction<T>? action = null, IStateCondition<T>? guard = null, string[]? roles = default)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			if (destination.IsEmpty)
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			Source = source ?? State<T>.Empty;
			Destination = destination;
			Event = @event ?? Token.Empty;
			Action = action;
			Guard = guard;
			Roles = ReadOnly.Wrap(roles, true);
		}

		private string DebuggerDisplay
		{
			get
			{
				var text = new StringBuilder();
				text.Append(Source.Id).Append(' ').Append(Source.Name)
					.Append(" -> ")
					.Append(Destination.Id).Append(' ').Append(Destination.Name);

				if (!Event.IsEmpty)
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
		}

		public void Accept(IStatechartVisitor<T> visitor) => visitor.Visit(this);

		public bool CanMoveAlong(T value, IPrincipal? principal) => IsInRole(principal) && InvokeGuard(value) && Destination.CanEnter(value, principal);

		internal void OnMoveAlong(T value)
		{
#if TRACE_EVENTS
			System.Console.WriteLine($"# {Source.Name}>{Destination.Name} [{Event}]: Action");
#endif
			Action?.Invoke(value, Source, this);
		}

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

		private bool InvokeGuard(T value) => Guard == null || Guard.Invoke(value);
	}
}
