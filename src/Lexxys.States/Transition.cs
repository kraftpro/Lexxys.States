using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States;

public class Transition<T>
{
	private static ILogging Log => __log ??= StaticServices.Create<ILogging<Transition<T>>>();
	private static ILogging? __log;

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

	public void Accept(IStatechartVisitor<T> visitor) => (visitor ?? throw new ArgumentNullException(nameof(visitor))).Visit(this);

	public override string ToString()
	{
		var text = new StringBuilder();
		text.Append('(').Append(Source.Token.ToString(false))
			.Append(") -> (")
			.Append(Destination.Token.ToString(false)).Append(')');

		if (!Event.IsEmpty())
			text.Append(" /").Append(Event.ToString(false));

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
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Event.FullName()}: {nameof(OnMoveAlong)} {Source.Name} -> {Destination.Name}");
		Action?.Invoke(value, statechart, Source, this);
	}

	private bool IsInRole(IPrincipal? principal) => principal == null || Roles.Count == 0 || principal.IsInRole(Roles);

	private bool InvokeGuard(T value, Statechart<T> statechart) => Guard == null || Guard.Invoke(value, statechart, Source, this);

	#endregion

	#region Async

	internal async Task<bool> CanMoveAlongAsync(T value, Statechart<T> statechart, IPrincipal? principal)
		=> IsInRole(principal) &&
			await InvokeGuardAsync(value, statechart).ConfigureAwait(false) &&
			await Destination.CanEnterAsync(value, statechart, principal).ConfigureAwait(false);

	internal Task OnMoveAlongAsync(T value, Statechart<T> statechart)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Event.FullName()}: {nameof(OnMoveAlongAsync)} {Source.Name} -> {Destination.Name}");
		return Action != null ? Action.InvokeAsync(value, statechart, Source, this): Task.CompletedTask;
	}

	private Task<bool> InvokeGuardAsync(T value, Statechart<T> statechart)
		=> Guard != null ? Guard.InvokeAsync(value, statechart, Source, this): Task.FromResult(true);

	#endregion
}
