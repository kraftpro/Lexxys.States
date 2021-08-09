using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Lexxys.States
{
	public class State<T>
	{
		public static readonly State<T> Empty = new State<T>();

		public Token Token { get; }
		public IReadOnlyList<string> Roles { get; }
		public IStateCondition<T>? Guard { get; }
		public IReadOnlyList<Statechart<T>> Charts { get; }

		private State()
		{
			Token = Token.Empty;
			Roles = Array.Empty<string>();
			Charts = Array.Empty<Statechart<T>>();
		}

		public State(Token token, IReadOnlyList<Statechart<T>>? charts = null, IStateCondition<T>? guard = null, string[]? roles = default)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
			Charts = charts ?? Array.Empty<Statechart<T>>();
			Roles = ReadOnly.Wrap(roles, true);
			Guard = guard;
		}

		public int? Id => IsEmpty ? null : Token.Id;
		public string Name => Token.Name;
		public string? Description => Token.Description;
		public bool IsEmpty => this == Empty;

		public event Action<T, State<T>, Transition<T>>? StateEnter;
		public event Action<T, State<T>, Transition<T>>? StatePassthrough;
		public event Action<T, State<T>, Transition<T>>? StateEntered;
		public event Action<T, State<T>, Transition<T>>? StateExit;

		public void Accept(IStatechartVisitor<T> visitor)
		{
			if (visitor == null)
				throw new ArgumentNullException(nameof(visitor));
			visitor.Visit(this);
			foreach (var item in Charts)
			{
				item.Accept(visitor);
			}
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			if (Id > 0)
				text.Append(Id).Append('.');
			text.Append(Name);
			if (Guard != null)
				text.Append(" [...]");
			if (Roles.Count > 0)
				text.Append(" [").Append(String.Join(',', Roles)).Append(']');
			if (Charts.Count > 0)
				text.Append(" {").Append(String.Join(',', Charts.Select(o => o.Name))).Append('}');
			if (Description != null)
				text.Append(" - ").Append(Description);
			return text.ToString();
		}

		public static IStateCondition<T> AllFinishedCondition
			=> __allFinished;
		private static readonly IStateCondition<T> __allFinished = StateCondition.Create<T>((o, c, s, t) => s!.Charts.All(x => x.IsFinished));

		public static IStateCondition<T> SomeFinishedCondition
			=> __someFinished;
		private static readonly IStateCondition<T> __someFinished = StateCondition.Create<T>((o, c, s, t) => s!.Charts.Count == 0 || s!.Charts.Any(x => x.IsFinished));

		internal bool CanEnter(T value, Statechart<T> statechart, IPrincipal? principal) => IsInRole(principal) && InvokeGuard(value, statechart);

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

		private bool InvokeGuard(T value, Statechart<T> statechart) => Guard == null || Guard.Invoke(value, statechart, this, null);

		internal void OnStateEnter(T value, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Enter");
#endif
			StateEnter?.Invoke(value, this, transition);
		}

		internal void OnStateEntered(T value, Transition<T> transition, IPrincipal? principal)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Entered");
#endif
			StateEntered?.Invoke(value, this, transition);
			foreach (var chart in Charts)
			{
				chart.Start(value, principal);
			}
		}

		internal void OnStatePassthrough(T value, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Passthrough");
#endif
			StatePassthrough?.Invoke(value, this, transition);
		}

		internal void OnStateExit(T value, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Exit");
#endif
			StateExit?.Invoke(value, this, transition);
		}
	}
}
