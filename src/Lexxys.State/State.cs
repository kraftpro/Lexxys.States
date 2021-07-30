using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Lexxys.States
{
	public class State<T>
	{
		public static readonly State<T> Empty = new State<T>();

		public Token Token{ get; }
		public IReadOnlyList<string> Roles { get; }
		public IStateCondition<T>? Guard { get; }
		public IReadOnlyList<Statechart<T>> Subcharts { get; }

		private State()
		{
			Token = Token.Empty;
			Roles = Array.Empty<string>();
			Subcharts = Array.Empty<Statechart<T>>();
		}

		public State(Token token, IReadOnlyList<Statechart<T>>? subcharts = null, IStateCondition<T>? guard = null, string[]? roles = default)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
			Subcharts = subcharts ?? Array.Empty<Statechart<T>>();
			Roles = ReadOnly.Wrap(roles, true);
			Guard = guard;
		}

		public int? Id => IsEmpty ? null: Token.Id;
		public string Name => Token.Name;
		public string? Description => Token.Description;
		public bool IsEmpty => this == Empty;

		public event Action<T, State<T>, Transition<T>?>? StateEnter;
		public event Action<T, State<T>, Transition<T>?>? StatePassthrough;
		public event Action<T, State<T>, Transition<T>?>? StateEntered;
		public event Action<T, State<T>, Transition<T>?>? StateExit;

		public void Accept(IStatechartVisitor<T> visitor)
		{
			if (visitor == null)
				throw new ArgumentNullException(nameof(visitor));
			visitor.Visit(this);
			foreach (var item in Subcharts)
			{
				item.Accept(visitor);
			}
		}

		internal bool CanEnter(T value, IPrincipal? principal) => IsInRole(principal) && InvokeGuard(value);

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

		private bool InvokeGuard(T value) => Guard == null || Guard.Invoke(value);

		internal void OnStateEnter(T value, Transition<T>? transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Enter");
#endif
			StateEnter?.Invoke(value, this, transition);
		}

		internal void OnStateEntered(T value, Transition<T>? transition, IPrincipal? principal)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Entered");
#endif
			StateEntered?.Invoke(value, this, transition);
			foreach (var chart in Subcharts)
			{
				chart.Start(value, principal);
			}
		}

		internal void OnStatePassthrough(T value, Transition<T>? transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Passthrough");
#endif
			StatePassthrough?.Invoke(value, this, transition);
		}

		internal void OnStateExit(T value, Transition<T>? transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Exit");
#endif
			StateExit?.Invoke(value, this, transition);
		}
	}
}
