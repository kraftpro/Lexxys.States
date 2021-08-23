using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States
{
	public class State<T>
	{
		public static readonly State<T> Empty = new State<T>();

		public Token Token { get; }
		public IReadOnlyCollection<string> Roles { get; }
		public IStateCondition<T>? Guard { get; }
		public IReadOnlyCollection<Statechart<T>> Charts { get; }

		private State()
		{
			Token = Token.Empty;
			Roles = Array.Empty<string>();
			Charts = Array.Empty<Statechart<T>>();
		}

		public State(Token token, IReadOnlyCollection<Statechart<T>>? charts = null, IStateCondition<T>? guard = null, IReadOnlyCollection<string>? roles = null)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
			Charts = charts ?? Array.Empty<Statechart<T>>();
			Roles = roles ?? Array.Empty<string>();
			Guard = guard;
		}

		public int? Id => IsEmpty ? null : Token.Id;
		public string Name => Token.Name;
		public string? Description => Token.Description;
		public bool IsEmpty => this == Empty;

		public StateActionChain<T> StateEnter;
		public StateActionChain<T> StatePassthrough;
		public StateActionChain<T> StateEntered;
		public StateActionChain<T> StateExit;

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

		#region Sync

		internal bool CanEnter(T value, Statechart<T> statechart, IPrincipal? principal) => IsInRole(principal) && InvokeGuard(value, statechart);

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

		private bool InvokeGuard(T value, Statechart<T> statechart) => Guard == null || Guard.Invoke(value, statechart, this, null);

		internal void OnStateEnter(T value, Statechart<T> statechart, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Enter");
#endif
			StateEnter.Invoke(value, statechart, this, transition);
		}

		internal void OnStateEntered(T value, Statechart<T> statechart, Transition<T> transition, IPrincipal? principal)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Entered");
#endif
			StateEntered.Invoke(value, statechart, this, transition);
			foreach (var chart in Charts)
			{
				chart.Start(value, principal);
			}
		}

		internal void OnStatePassthrough(T value, Statechart<T> statechart, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Passthrough");
#endif
			StatePassthrough.Invoke(value, statechart, this, transition);
		}

		internal void OnStateExit(T value, Statechart<T> statechart, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Exit");
#endif
			StateExit.Invoke(value, statechart, this, transition);
		}

		#endregion

		#region Async

		internal async Task<bool> CanEnterAsync(T value, Statechart<T> statechart, IPrincipal? principal) => IsInRole(principal) && await InvokeGuardAsync(value, statechart);

		private async Task<bool> InvokeGuardAsync(T value, Statechart<T> statechart) => Guard == null || await Guard.InvokeAsync(value, statechart, this, null);

		internal async Task OnStateEnterAsync(T value, Statechart<T> statechart, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Enter");
#endif
			await StateEnter.InvokeAsync(value, statechart, this, transition);
		}

		internal async Task OnStateEnteredAsync(T value, Statechart<T> statechart, Transition<T> transition, IPrincipal? principal)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Entered");
#endif
			await StateEntered.InvokeAsync(value, statechart, this, transition);
			foreach (var chart in Charts)
			{
				await chart.StartAsync(value, principal);
			}
		}

		internal async Task OnStatePassthroughAsync(T value, Statechart<T> statechart, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Passthrough");
#endif
			await StatePassthrough.InvokeAsync(value, statechart, this, transition);
		}

		internal async Task OnStateExitAsync(T value, Statechart<T> statechart, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Exit");
#endif
			await StateExit.InvokeAsync(value, statechart, this, transition);
		}

		#endregion
	}
}
