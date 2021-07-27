using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Lexxys.States
{
	public class State<T> //: IToken
	{
		public Token Token{ get; }
		public IReadOnlyList<Transition<T>> Transitions { get; }
		public IReadOnlyList<string> Roles { get; }
		public IStateCondition<T>? Guard { get; }
		public IReadOnlyList<Statechart<T>> Subcharts { get; }

		public State(Token token, IReadOnlyList<Transition<T>>? transitions, IReadOnlyList<Statechart<T>>? subcharts = null, IStateCondition<T>? guard = null, string[]? roles = default)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
			Subcharts = subcharts ?? Array.Empty<Statechart<T>>();
			Transitions = transitions ?? Array.Empty<Transition<T>>();
			Roles = ReadOnly.Wrap(roles, true);
			Guard = guard;
		}

		public int Id => Token.Id;
		public string Name => Token.Name;
		public string? Description => Token.Description;
		public bool IsFinal => Transitions.Count == 0;

		public event Action<T, State<T>, Transition<T>?>? StateEnter;
		public event Action<T, State<T>, Transition<T>?>? StateEntered;
		public event Action<T, State<T>, Transition<T>?>? StatePassthrough;
		public event Action<T, State<T>, Transition<T>?>? StateExit;

		public void Accept(IStatechartVisitor<T> visitor)
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

		internal Transition<T>? FirstTransition(Token? @event, T context, IPrincipal? principal)
		{
			var evt = @event ?? Token.Empty;
			var transitions = Transitions
				.Where(o => o.Event == evt && o.CanMoveAlong(context, principal))
				.GetEnumerator();
			if (!transitions.MoveNext())
				return default;
			var transition = transitions.Current;
			if (transitions.MoveNext())
				throw new InvalidOperationException($"More than one transitions found for state {Name} and event {@event}.");

			return transition;
		}

		internal bool CanEnter(T context, IPrincipal? principal)
		{
			return IsInRole(principal) && (Guard == null || Guard.Invoke(context));
		}

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

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

		//public StatePath<T> CurrentPath()
		//{
		//	var state = this;
		//	var items = new List<StatePathItem<T>>();
		//	while (state != null)
		//	{
		//		items.Add(new StatePathItem<T>(state.Statechart, state));
		//		state = state.Statechart.st
		//	}

		//	if (Subcharts == null || Subcharts.Count == 0)
		//		return new StatePath<T>(new [] { new StatePathItem<T>(Statechart, this) });
		//	var result = new List<StatePath<T>>();
		//	foreach (var item in Subcharts)
		//	{
		//		if (item.CurrentState != null)
		//		{
		//			foreach (var path in item.CurrentPath())
		//			{
		//				var path2 = new List<StatePathItem<T>>()
		//				{
		//					new StatePathItem<T>(Statechart, this)
		//				};
		//				//path2.AddRange(path);
		//			}
		//		}
		//			result.AddRange(item.CurrentPath());
		//	}
		//	return result;
		//}
	}
}
