using Lexxys;

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

#nullable enable

namespace State.Test1
{
	public class Transition<T>
	{
		public Token Event { get; }
		public IStateCondition<T>? Guard { get; }
		public IStateAction<T>? Action { get; }
		public State<T> Source { get; }
		public State<T> Destination { get; }
		public IReadOnlyList<string> Roles { get; }

		public Transition(State<T> source, State<T> destination, Token? @event = null, IStateAction<T>? action = null, IStateCondition<T>? guard = null, string[]? roles = default)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			Destination = destination ?? throw new ArgumentNullException(nameof(destination));
			Event = @event ?? Token.Empty;
			Action = action;
			Guard = guard;
			Roles = ReadOnly.Wrap(roles, true);
		}

		public void Accept(IStatechartVisitor<T> visitor)
		{
			visitor.Visit(this);
		}

		public bool CanMoveAlong(T context, IPrincipal? principal)
		{
			return IsInRole(principal) && (Guard == null || Guard.Invoke(context)) &&
				Destination.CanEnter(context, principal);
		}

		internal void OnMoveAlong(T context)
		{
#if TRACE_EVENTS
			System.Console.WriteLine($"# {Source.Name}>{Destination.Name} [{Event}]: Action");
#endif
			Action?.Invoke(context, Source, this);
		}

		private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);
	}

	//private class EmptyGuard : ITransitionGuard<T>
	//{
	//	public static readonly ITransitionGuard<T> Istance = new EmptyGuard();

	//	private EmptyGuard()
	//	{
	//	}

	//	public bool Allow(Transition<T> transition, T entity) => true;
	//}

	//public interface ITransitionGuard<T>
	//{
	//	bool Allow(Transition<T> transition, T entity);
	//}

	//public class TransitionAction
	//{
	//	public int Id { get; }
	//	public string Name { get; }
	//	public string? Description { get; }

	//	public TransitionAction(int id, string name, string? description = null)
	//	{
	//		Id = id;
	//		Name = name;
	//		Description = description;
	//	}

	//	public override string ToString()
	//	{
	//		StringBuilder text = new StringBuilder();
	//		text.Append('(').Append(Id).Append(')')
	//			.Append(' ').Append(Name);
	//		if (Description != null)
	//			text.Append(' ').Append(Description);
	//		return text.ToString();
	//	}
	//}
}
