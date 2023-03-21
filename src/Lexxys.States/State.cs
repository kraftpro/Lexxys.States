using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Lexxys.States;

public class State<T>
{
	private static ILogger Log => __log ??= Statics.GetLogger<State<T>>();
	private static ILogger? __log;

	/// <summary>
	/// Represents an empty state.
	/// </summary>
	public static readonly State<T> Empty = new State<T>();

	/// <summary>
	/// State ID
	/// </summary>
	public Token Token { get; }

	/// <summary>
	/// List of roles allowed to enter this state.
	/// </summary>
	public IReadOnlyCollection<string> Roles { get; }

	/// <summary>
	/// Condition to enter this state.
	/// </summary>
	public IStateCondition<T>? Guard { get; }

	/// <summary>
	/// Statecharts collection running in parralel
	/// </summary>
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

	/// <summary>
	/// State ID from the <see cref="Token"/>
	/// </summary>
	public int Id => Token.Id;

	/// <summary>
	/// State name from the <see cref="Token"/>
	/// </summary>
	public string Name => Token.Name;

	/// <summary>
	/// State description from the <see cref="Token"/>
	/// </summary>
	public string? Description => Token.Description;

	/// <summary>
	/// Determines if this state is empty.
	/// </summary>
	public bool IsEmpty => this == Empty;

	/// <summary>
	/// Checks that this state doesn't have active (not finished) statecharts.
	/// </summary>
	public bool IsFinished => Charts.Count == 0 || Charts.All(o => o.IsFinished);

	#region Events
#pragma warning disable CA1051

	/// <summary>
	/// Executes when the workflow entering to the current <see cref="State{T}"/>. (the sequence: <b>StateEnter</b> -> ... -> StateExit)
	/// </summary>
	public StateActionChain<T> StateEnter;
	/// <summary>
	/// Executes when the workflow skips the current <see cref="State{T}"/>. (the sequence: StateEnter -> <b>StatePassthrough</b> -> StateExit)
	/// </summary>
	public StateActionChain<T> StatePassthrough;
	/// <summary>
	/// Executes when the workflow entered the current <see cref="State{T}"/>. (the sequence: StateEnter -> <b>StateEntered</b> -> StateExit)
	/// </summary>
	public StateActionChain<T> StateEntered;
	/// <summary>
	/// Executes when the workflow leaves the current <see cref="State{T}"/>. (the sequence: StateEnter -> ... -> <b>StateExit</b>)
	/// </summary>
	public StateActionChain<T> StateExit;

	#pragma warning restore CA1051
	#endregion

	/// <summary>
	/// Accepts the <see cref="IStatechartVisitor{T}"/> for this <see cref="State{T}"/> and inner <see cref="Statechart{T}"/>s.
	/// </summary>
	/// <param name="visitor">The <see cref="IStatechartVisitor{T}"/> to walk through the <see cref="State{T}"/>/<see cref="Statechart{T}"/> tree</param>
	/// <exception cref="ArgumentNullException"></exception>
	public void Accept(IStatechartVisitor<T> visitor)
	{
		if (visitor is null)
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
		text.Append(Token.ToString(false));
		if (Guard is not null)
			text.Append(" [...]");
		if (Roles.Count > 0)
			text.Append(" [").Append(String.Join(",", Roles)).Append(']');
		if (Charts.Count > 0)
			text.Append(" {").Append(String.Join(",", Charts.Select(o => o.Name))).Append('}');
		if (Description is not null)
			text.Append(" - ").Append(Description);
		return text.ToString();
	}

	#region Sync

	internal bool CanEnter(T value, Statechart<T> statechart, IPrincipal? principal) => IsInRole(principal) && InvokeGuard(value, statechart);

	private bool IsInRole(IPrincipal? principal) => principal is null || Roles.Count == 0 || principal.IsInRole(Roles);

	private bool InvokeGuard(T value, Statechart<T> statechart) => Guard is null || Guard.Invoke(value, statechart, this, null);

	internal void OnStateEnter(T value, Statechart<T> statechart, Transition<T> transition)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStateEnter)}");
		StateEnter.Invoke(value, statechart, this, transition);
	}

	internal void OnStateEntered(T value, Statechart<T> statechart, Transition<T> transition, IPrincipal? principal)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStateEntered)}");
		StateEntered.Invoke(value, statechart, this, transition);
		foreach (var chart in Charts)
		{
			if (!transition.Continues || !chart.IsStarted)
				chart.Start(value, principal);
		}
	}

	internal void OnStatePassthrough(T value, Statechart<T> statechart, Transition<T> transition)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStatePassthrough)}");
		StatePassthrough.Invoke(value, statechart, this, transition);
	}

	internal void OnStateExit(T value, Statechart<T> statechart, Transition<T> transition)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStateExit)}");
		StateExit.Invoke(value, statechart, this, transition);
	}

	#endregion

	#region Async

	internal Task<bool> CanEnterAsync(T value, Statechart<T> statechart, IPrincipal? principal)
		=> IsInRole(principal) ? InvokeGuardAsync(value, statechart): Task.FromResult(false);

	private Task<bool> InvokeGuardAsync(T value, Statechart<T> statechart)
		=> Guard is not null ? Guard.InvokeAsync(value, statechart, this, null): Task.FromResult(true);

	internal Task OnStateEnterAsync(T value, Statechart<T> statechart, Transition<T> transition)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStateEnterAsync)}");
		return StateEnter.InvokeAsync(value, statechart, this, transition);
	}

	internal async Task OnStateEnteredAsync(T value, Statechart<T> statechart, Transition<T> transition, IPrincipal? principal)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStateEnteredAsync)}");
		await StateEntered.InvokeAsync(value, statechart, this, transition).ConfigureAwait(false);
		if (Charts.Count > 0)
			await Task.WhenAll(Charts.Select(o => o.StartAsync(value, principal))).ConfigureAwait(false);
	}

	internal Task OnStatePassthroughAsync(T value, Statechart<T> statechart, Transition<T> transition)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStatePassthroughAsync)}");
		return StatePassthrough.InvokeAsync(value, statechart, this, transition);
	}

	internal Task OnStateExitAsync(T value, Statechart<T> statechart, Transition<T> transition)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStateExitAsync)}");
		return StateExit.InvokeAsync(value, statechart, this, transition);
	}

	#endregion
}
