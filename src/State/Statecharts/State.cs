using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Linq;

using Lexxys;
using System.Security.Principal;
using System;

namespace State.Statecharts
{
	[DebuggerDisplay("{Name,nq} Transitions.Count={Transitions.Count}, Subcharts={SubchartsDisplay,nq}")]
	public class State<T>
	{
		public static readonly State<T> Finished = new State<T>(null, "*");

		public int? Id { get; }
		public string Name { get; }
		public string Permission { get; }
		public IStateCondition<T> Guard { get; }
//		public Statechart<T> Statechart<T> { get; }
		public IReadOnlyList<Transition<T>> Transitions { get; internal set; }
		public IReadOnlyList<Statechart<T>> Subcharts { get; }
		public IStateAction<T> Enter { get; }
		public IStateAction<T> Exit { get; }

		public event Action<T, State<T>, Transition<T>> StateEnter;
		public event Action<T, State<T>, Transition<T>> StateEntered;
		public event Action<T, State<T>, Transition<T>> StatePassthrough;
		public event Action<T, State<T>, Transition<T>> StateExit;

		public State(int? id, string name, string permission = null, IReadOnlyList<Statechart<T>> subcharts = null, IStateCondition<T> guard = null, IStateAction<T> enter = null, IStateAction<T> entered = null, IStateAction<T> exit = null, IStateAction<T> passthrough = null)
		{
			Id = id;
			Name = name;
			Guard = guard ?? StateCondition.True<T>();
			Permission = permission;
			Subcharts = subcharts ?? Array.Empty<Statechart<T>>();
			StateEnter += enter?.GetDelegate();
			StateEntered += entered?.GetDelegate();
			StatePassthrough += passthrough?.GetDelegate();
			StateExit += exit?.GetDelegate();
			Transitions = Array.Empty<Transition<T>>();
		}

		internal void Accept(IStatechartVisitor<T> visitor)
		{
			if (visitor == null)
				throw new ArgumentNullException(nameof(visitor));
			visitor.Visit(this);
			foreach (var item in Transitions)
			{
				item.Accept(visitor);
			}
			foreach (var item in Subcharts)
			{
				item.Accept(visitor);
			}
		}

		private string SubchartsDisplay => Subcharts == null ? null : string.Join(", ", Subcharts.Select(o => o.Name));

		public bool IsFinal => Transitions.Count == 0;

		internal void OnStateEnter(T scope, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Enter");
#endif
			StateEnter?.Invoke(scope, this, transition);
		}

		internal void OnStateEntered(T scope, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Entered");
#endif
			StateEntered?.Invoke(scope, this, transition);
		}

		internal void OnStateExit(T scope, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Exit");
#endif
			StateExit?.Invoke(scope, this, transition);
		}

		internal void OnStatePassthrough(T scope, Transition<T> transition)
		{
#if TRACE_EVENTS
			Console.WriteLine($"# {Name}: Passthrough");
#endif
			StatePassthrough?.Invoke(scope, this, transition);
		}

		internal bool CanEnter(T context, IPrincipal principal)
		{
			return (principal == null || Permission == null || principal.IsInRole(Permission)) &&
				Guard.Invoke(context);
		}

		internal Transition<T> FindTransition(object @event, T context, IPrincipal principal)
		{
			var transitions = Transitions.Where(o => o.Event == @event && o.CanMoveAlong(context, principal)).ToList();
			if (transitions.Count == 0)
				return default;
			if (transitions.Count > 1)
				throw new InvalidOperationException($"More than one transitions found for state {Name} and event {@event}.");
			var transition = transitions[0];

			return transition;
		}

		internal IReadOnlyList<string> CurrentPath(string path)
		{
			path += "." + Name;
			if (Subcharts.Count == 0)
				return new [] { path };
			if (Subcharts.Count == 1)
				return Subcharts[0].CurrentPath(path);
			return Subcharts.SelectMany(o => o.CurrentPath(path)).ToList();
		}
	}
}