using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Lexxys.States;

/// <summary>
/// Represents a state chart of a state machine for the object of the type of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Statechart<T>
{
	private static ILogger Log => __log ??= Statics.GetLogger<Statechart<T>>();
	private static ILogger? __log;

	private readonly IReadOnlyDictionary<State<T>, IReadOnlyList<Transition<T>>> _transitions;
	private State<T> _currentState;

	/// <summary>
	/// Creates a new <see cref="Statechart{T}"/> with the specified list of <paramref name="states"/> and <paramref name="transitions"/>, marked with the specified <paramref name="token"/>
	/// </summary>
	/// <param name="token">The state chart marker.</param>
	/// <param name="states">List of the state chart states.</param>
	/// <param name="transitions">List of transitions between the states.</param>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="token"/> should not be empty.</exception>
	/// <exception cref="ArgumentException">The initial transition should be the only transition in the list.</exception>
	public Statechart(Token token, IEnumerable<State<T>> states, IEnumerable<Transition<T>> transitions)
	{
		if (token is null)
			throw new ArgumentNullException(nameof(token));
		if (token.IsEmpty)
			throw new ArgumentOutOfRangeException(nameof(token), token, null);
		if (states is null)
			throw new ArgumentNullException(nameof(states));
		if (transitions is null)
			throw new ArgumentNullException(nameof(transitions));

		States = ReadOnly.WrapCopy(states)!;
		var tt = transitions.ToIReadOnlyCollection();
		foreach (var item in tt)
		{
			if (!item.Source.IsEmpty && !States.Contains(item.Source))
				throw new ArgumentException("Transitioning from an external state chart is not supported.");
			if (!States.Contains(item.Destination))
				throw new ArgumentException("Transition outside of the state chart is not supported.");
		}

		_transitions = ReadOnly.Wrap(tt.GroupBy(o => o.Source).ToDictionary(o => o.Key, o => (IReadOnlyList<Transition<T>>)ReadOnly.WrapCopy(o)!))!;
		if (!_transitions.TryGetValue(State<T>.Empty, out var initial))
			throw new ArgumentException("Missing initial transition.");
		if (initial.Count != 1)
			throw new ArgumentException("Multiple initial transitions found.");
		_currentState = State<T>.Empty;
		Token = token;
	}

	/// <summary>
	/// Collection of <see cref="State{T}"/>s in the <see cref="Statechart{T}"/>.
	/// </summary>
	public IReadOnlyList<State<T>> States {  get; }
	/// <summary>
	/// Returns current <see cref="State{T}"/> or <see cref="State{T}.Empty"/> if the current state is not set.
	/// </summary>
	public State<T> CurrentState
	{
		get => _currentState;
		private set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (!value.IsEmpty && !States.Contains(value))
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			_currentState = value;
		}
	}
	/// <summary>
	/// <see cref="Statechart{T}"/>'s token.
	/// </summary>
	public Token Token { get; }

	/// <summary>
	/// ID of the <see cref="Statechart{T}"/>'s token.
	/// </summary>
	public int Id => Token.Id;
	/// <summary>
	/// Name of the <see cref="Statechart{T}"/>'s token.
	/// </summary>
	public string Name => Token.Name;
	/// <summary>
	/// Description of the <see cref="Statechart{T}"/>'s token.
	/// </summary>
	public string? Description => Token.Description;

	/// <summary>
	/// Returns collection of <see cref="Transition{T}"/>s for the specified <paramref name="state"/>.
	/// </summary>
	/// <param name="state"><see cref="State{T}"/> for witch to get a collection of <see cref="Transition{T}"/>s.</param>
	/// <returns></returns>
	public IReadOnlyList<Transition<T>> GetStateTransitions(State<T> state)
		=> _transitions.TryGetValue(state, out var transitions) ? transitions: Array.Empty<Transition<T>>();

	/// <summary>
	/// Checks whether the <see cref="Statechart{T}"/> has been started.
	/// </summary>
	public bool IsStarted => !CurrentState.IsEmpty;

	/// <summary>
	/// Indicates that the <see cref="Statechart{T}"/> is in progress state (i.e. started and not finished)
	/// </summary>
	public bool IsInProgress => !CurrentState.IsEmpty && !IsInFinalState();

	/// <summary>
	/// Indicates that the <see cref="Statechart{T}"/> is in final state.
	/// </summary>
	public bool IsFinished => !CurrentState.IsEmpty && IsInFinalState();

	#region Events
	#pragma warning disable CA1051

	/// <summary>
	/// Executes when the <see cref="Statechart{T}"/> loads state from the object <see cref="T"/>.
	/// </summary>
	public StateActionChain<T> OnLoad;
	/// <summary>
	/// Executes when the <see cref="Statechart{T}"/> saves state to the object <see cref="T"/>.
	/// </summary>
	public StateActionChain<T> OnUpdate;

	/// <summary>
	/// Action executed when the <see cref="Statechart{T}"/> starting
	/// </summary>
	public StateActionChain<T> ChartStart;
	/// <summary>
	/// Action executed when the <see cref="Statechart{T}"/> switched to the final state.
	/// </summary>
	public StateActionChain<T> ChartFinish;

	/// <summary>
	/// Executes before the <see cref="State{T}"/> becomes a current state in the <see cref="Statechart{T}"/> diagram. (<b>StateEnter</b> -> ... -> StateExit)
	/// </summary>
	public StateActionChain<T> StateEnter;
	/// <summary>
	/// Executes when the <see cref="State{T}"/> becomes a current state in the <see cref="Statechart{T}"/> diagram. (StateEnter -> <b>StateEntered</b> -> StateExit)
	/// </summary>
	public StateActionChain<T> StateEntered;
	/// <summary>
	/// Executes when control flows to another <see cref="State{T}"/> by condition. (StateEnter -> <b>StatePassthrough</b> -> StateExit)
	/// </summary>
	public StateActionChain<T> StatePassthrough;
	/// <summary>
	/// Executes when the <see cref="State{T}"/> object exits the current state condition. (the sequence: StateEnter -> StatePassthrough -> <b>StateExit</b>)
	/// </summary>
	public StateActionChain<T> StateExit;

	#pragma warning restore CA1051
	#endregion

	/// <summary>
	/// Returns all the statecharts, including this one.
	/// </summary>
	public IReadOnlyList<Statechart<T>> Charts => _charts ??= CollectCharts();
	private IReadOnlyList<Statechart<T>>? _charts;

	private IReadOnlyList<Statechart<T>> CollectCharts()
	{
		List<Statechart<T>> list = new() { this };
		foreach (var state in States)
		{
			foreach (var chart in state.Charts)
			{
				list.AddRange(chart.Charts);
			}
		}
		return ReadOnly.Wrap(list)!;
	}

	/// <summary>
	/// Accepts the <see cref="IStatechartVisitor{T}"/> for this <see cref="Statechart{T}"/> and inner <see cref="State{T}"/>s.
	/// </summary>
	/// <param name="visitor">The <see cref="IStatechartVisitor{T}"/> to walk through the <see cref="State{T}"/>/<see cref="Statechart{T}"/> tree</param>
	/// <exception cref="ArgumentNullException"></exception>
	public void Accept(IStatechartVisitor<T> visitor)
	{
		if (visitor is null)
			throw new ArgumentNullException(nameof(visitor));
		visitor.Visit(this);
		foreach (var state in States)
		{
			state.Accept(visitor);
			if (_transitions.TryGetValue(state, out var transitions))
			{
				foreach (var item in transitions)
				{
					item.Accept(visitor);
				}
			}
		}
	}

	private bool IsInFinalState()
		=> !_transitions.ContainsKey(CurrentState);

	/// <summary>
	/// Set <see cref="CurrentState"/> by <see cref="Token"/> ID.
	/// </summary>
	/// <param name="stateId"><see cref="Token"/> ID.</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public void SetCurrentState(int? stateId)
		=> CurrentState = stateId is null || stateId == 0 ? State<T>.Empty: States.FirstOrDefault(o => o.Id == stateId) ?? throw new ArgumentOutOfRangeException(nameof(stateId), stateId, null);

	/// <summary>
	/// Set <see cref="CurrentState"/> by <see cref="Token"/> Name.
	/// </summary>
	/// <param name="stateName"><see cref="Token"/> Name.</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public void SetCurrentState(string? stateName)
		=> CurrentState = stateName is null ? State<T>.Empty: States.FirstOrDefault(o => o.Name == stateName) ?? throw new ArgumentOutOfRangeException(nameof(stateName), stateName, null);

	/// <summary>
	/// Set <see cref="CurrentState"/> by <see cref="Token"/>.
	/// </summary>
	/// <param name="token"><see cref="Token"/></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public void SetCurrentState(Token token)
		=> CurrentState = (token ?? throw new ArgumentNullException(nameof(token))).IsEmpty ? State<T>.Empty: States.FirstOrDefault(o => o.Token == token) ?? throw new ArgumentOutOfRangeException(nameof(token), token, null);

	/// <summary>
	/// Starts the <see cref="Statechart{T}"/>.
	/// </summary>
	/// <param name="value">Object context</param>
	/// <param name="principal">Actual security principals</param>
	public void Start(T value, IPrincipal? principal = null)
	{
		Reset();
		var start = GetInitialTransition();
		OnStart(value);

		Continue(start, value, principal);
	}

	/// <summary>
	/// Starts the <see cref="Statechart{T}"/>.
	/// </summary>
	/// <param name="value">Object context</param>
	/// <param name="principal">Actual security principals</param>
	public async Task StartAsync(T value, IPrincipal? principal = null)
	{
		Reset();
		var start = GetInitialTransition();
		await OnStartAsync(value).ConfigureAwait(false);
		await ContinueAsync(start, value, principal).ConfigureAwait(false);
	}

	/// <summary>
	/// Resets the <see cref="Statechart{T}"/> to the initial (not started) state.
	/// </summary>
	public void Reset()
	{
		foreach (var item in Charts)
		{
			item._currentState = State<T>.Empty;
		}
	}

	/// <summary>
	/// Invokes <see cref="OnLoad"/> for this state chart and all the sub-charts.
	/// </summary>
	/// <param name="value">Object the statechart corresponds to.</param>
	public void Load(T value)
	{
		foreach (var item in Charts.Where(o => !o.OnLoad.IsEmpty))
		{
			if (Log.IsEnabled(LogType.Trace))
				Log.Trace($"{item.Token.FullName()} OnLoad");
			item.OnLoad.Invoke(value, this, null, null);
		}
	}

	/// <summary>
	/// Asynchronously invokes <see cref="OnLoad"/> for this state chart and all the sub-charts.
	/// </summary>
	/// <param name="value">Object the statechart corresponds to.</param>
	public Task LoadAsync(T value)
	{
		return Task.WhenAll(Charts.Where(o => !o.OnLoad.IsEmpty).Select(o =>
		{
			if (Log.IsEnabled(LogType.Trace))
				Log.Trace($"{o.Token.FullName()} OnLoadAsync");
			return o.OnLoad.InvokeAsync(value, o, null, null);
		}));
	}

	/// <summary>
	/// Invokes <see cref="OnUpdate"/> for this state chart and all the sub-charts.
	/// </summary>
	/// <param name="value">Object the statechart corresponds to.</param>
	public void Update(T value)
	{
		foreach (var item in Charts.Where(o => !o.OnUpdate.IsEmpty))
		{
			if (Log.IsEnabled(LogType.Trace))
				Log.Trace($"{item.Token.FullName()} OnUpdate");
			item.OnUpdate.Invoke(value, this, null, null);
		}
	}

	/// <summary>
	/// Asynchronously invokes <see cref="OnUpdate"/> for this state chart and all the sub-charts.
	/// </summary>
	/// <param name="value">Object the statechart corresponds to.</param>
	public Task UpdateAsync(T value)
	{
		return Task.WhenAll(Charts.Where(o => !o.OnUpdate.IsEmpty).Select(o =>
		{
			if (Log.IsEnabled(LogType.Trace))
				Log.Trace($"{o.Token.FullName()} OnUpdateAsync");
			return o.OnUpdate.InvokeAsync(value, o, null, null);
		}));
	}

	/// <summary>
	/// Executes the transition event <see cref="TransitionEvent{T}"/>.  Returns true if the state was changed.
	/// </summary>
	/// <param name="event">The transition event</param>
	/// <param name="value">Entity object</param>
	/// <param name="principal">Actual security principals</param>
	/// <returns>True if the state was changes</returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public bool OnEvent(TransitionEvent<T> @event, T value, IPrincipal? principal = null)
	{
		if (@event is null)
			throw new ArgumentNullException(nameof(@event));
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (@event.Chart != this)
			return OnTransitionEventSubcharts(@event, value, principal);

		if (@event.Transition?.Source != CurrentState)
			throw new ArgumentOutOfRangeException(nameof(@event), @event, "Event.Transition.Source is not equal to the current state.");
		if (!_transitions.TryGetValue(CurrentState, out var transitions) || !transitions.Contains(@event.Transition))
			throw new ArgumentOutOfRangeException(nameof(@event), @event, "Specified Event.Transition is not in the list of availabe transitions.");

		if (!@event.Transition.CanMoveAlong(value, this, principal))
			return false;

		Continue(@event.Transition, value, principal);
		return true;
	}

	/// <summary>
	/// Asynchronously executes the transition event <see cref="TransitionEvent{T}"/>.  Returns <see cref="true"/> if the state was changed.
	/// </summary>
	/// <param name="event">The transition event</param>
	/// <param name="value">Entity object</param>
	/// <param name="principal">Actual security principals</param>
	/// <returns>True if the state was changes</returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public async Task<bool> OnEventAsync(TransitionEvent<T> @event, T value, IPrincipal? principal = null)
	{
		if (@event is null)
			throw new ArgumentNullException(nameof(@event));
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (@event.Chart != this)
			return await OnTransitionEventSubchartsAsync(@event, value, principal).ConfigureAwait(false);

		if (@event.Transition?.Source != CurrentState)
			throw new ArgumentOutOfRangeException(nameof(@event), @event, "Event.Transition.Source is not equal to the current state.");
		if (!_transitions.TryGetValue(CurrentState, out var transitions) || !transitions.Contains(@event.Transition))
			throw new ArgumentOutOfRangeException(nameof(@event), @event, "Specified Event.Transition is not in the list of availabe transitions.");

		await ContinueAsync(@event.Transition, value, principal).ConfigureAwait(false);
		return true;
	}

	/// <summary>
	/// Collects a list of <see cref="TransitionEvent{T}"/>s applicable to the <see cref="Statechart{T}"/> in the current state.
	/// </summary>
	/// <param name="value">Object value</param>
	/// <param name="principal">Actual security principals</param>
	/// <returns></returns>
	public IEnumerable<TransitionEvent<T>> GetActiveEvents(T value, IPrincipal? principal = null)
		=> GetActiveStates()
			.SelectMany(o => o.Chart.GetStateTransitions(o.State)
				.Where(t => (o.State.IsFinished || !t.Event.IsEmpty) && t.CanMoveAlong(value, this, principal))
				.Select(t => new TransitionEvent<T>(o.Chart, t)));

	/// <summary>
	/// Asynchronously collects a list of <see cref="TransitionEvent{T}"/>s applicable to the <see cref="Statechart{T}"/> in the current state.
	/// </summary>
	/// <param name="value">Object value</param>
	/// <param name="principal">Actual security principals</param>
	/// <param name="cancellation">The cancelation token</param>
	/// <returns></returns>
	public async IAsyncEnumerable<TransitionEvent<T>> GetActiveEventsAsync(T value, IPrincipal? principal = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		foreach (var item in GetActiveStates())
		{
			foreach (var transition in item.Chart.GetStateTransitions(item.State))
			{
				if (cancellation.IsCancellationRequested)
					yield break;
				if ((item.State.IsFinished || !transition.Event.IsEmpty) && await transition.CanMoveAlongAsync(value, this, principal).ConfigureAwait(false))
					yield return new TransitionEvent<T>(item.Chart, transition);
			}
		}
	}

	/// <summary>
	/// Collects the active states for this state chart and all the sub-charts.
	/// </summary>
	/// <returns></returns>
	public StatesChain<T> GetActiveStates() => CollectTree(new StatesChain<T>(), null);

	public override string ToString()
	{
		var text = new StringBuilder();
		text.Append(Token.ToString(false))
			.Append(' ')
			.Append(States.Count).Append(" states, ");
		if (CurrentState.IsEmpty)
			text.Append("(empty) not started");
		else
			text.Append('(').Append(CurrentState.Id)
				.Append(' ').Append(CurrentState.Name).Append(')')
				.Append(IsFinished ? " finished": " in progress");
		if (Description is not null)
			text.Append(" - ").Append(Description);
		return text.ToString();
	}

	private Transition<T> GetInitialTransition() => _transitions[State<T>.Empty][0];

	#region Sync

	private bool OnTransitionEventSubcharts(TransitionEvent<T> @event, T value, IPrincipal? principal)
	{
		foreach (var item in CurrentState.Charts)
		{
			if (item.OnEvent(@event, value, principal))
			{
				var transition = FindAutoTransition(value, principal);
				if (transition is not null)
					Continue(transition, value, principal);
				return true;
			}
		}
		return false;
	}

	private Transition<T>? FindAutoTransition(T context, IPrincipal? principal)
	{
		if (!_transitions.TryGetValue(CurrentState, out var all))
			return null;
		if (!CurrentState.IsFinished)
			return null;

		Transition<T>? transition = null;
		foreach (var item in all.Where(o => o.Event.IsEmpty && o.CanMoveAlong(context, this, principal)))
		{
			if (transition is null)
				transition = item;
			else
				throw new InvalidOperationException($"Multiple transitions found for state {Name}.");
		}
		return transition;
	}

	private void Continue(Transition<T> transition, T value, IPrincipal? principal)
	{
		MoveAlong(transition, value);
		var transition2 = Idle(transition, value, principal);
		CurrentState.OnStateEntered(value, this, transition2, principal);
		OnStateEntered(value, CurrentState, transition2);
		if (CurrentState.IsFinished && IsInFinalState())
			OnFinish(value);
	}

	private void OnStateExit(T context, State<T> state, Transition<T> transition)
	{
		if (StateExit.IsEmpty)
			return;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStateExit)} {state.Token.FullName()}");
		StateExit.Invoke(context, this, state, transition);
	}

	private void OnStateEntered(T context, State<T> state, Transition<T> transition)
	{
		if (StateEntered.IsEmpty)
			return;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStateEntered)} {state.Token.FullName()}");
		StateEntered.Invoke(context, this, state, transition);
	}

	private void OnStatePassthrough(T context, State<T> state, Transition<T> transition)
	{
		if (StatePassthrough.IsEmpty)
			return;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStatePassthrough)} {state.Token.FullName()}");
		StatePassthrough.Invoke(context, this, state, transition);
	}

	private void OnStateEnter(T context, State<T> state, Transition<T> transition)
	{
		if (StateEnter.IsEmpty)
			return;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStateEnter)} {state.Token.FullName()}");
		StateEnter.Invoke(context, this, state, transition);
	}

	private void OnStart(T context)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStart)}");
		ChartStart.Invoke(context, this, null, null);
	}

	private void OnFinish(T context)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnFinish)}");
		ChartFinish.Invoke(context, this, null, null);
	}

	private Transition<T> Idle(Transition<T> transition, T value, IPrincipal? principal)
	{
		State<T> initial = CurrentState;
		int limit = States.Count * 2;
		Transition<T>? temp;
		while ((temp = FindAutoTransition(value, principal)) != default)
		{
			transition = temp;
			if (--limit < 0)
				throw new InvalidOperationException($"Transition<T> loop for state chart {Name} state {initial.Name}");
			CurrentState.OnStatePassthrough(value, this, transition);
			OnStatePassthrough(value, CurrentState, transition);
			MoveAlong(transition, value);
		}
		return transition;
	}

	private void MoveAlong(Transition<T> transition, T value)
	{
		if (!CurrentState.IsEmpty)
		{
			CurrentState.OnStateExit(value, this, transition);
			OnStateExit(value, CurrentState, transition);
		}
		transition.OnMoveAlong(value, this);
		CurrentState = transition.Destination;
		CurrentState.OnStateEnter(value, this, transition);
		OnStateEnter(value, CurrentState, transition);
	}

	#endregion

	#region Async

	private async Task<bool> OnTransitionEventSubchartsAsync(TransitionEvent<T> @event, T value, IPrincipal? principal)
	{
		foreach (var item in CurrentState.Charts)
		{
			if (await item.OnEventAsync(@event, value, principal).ConfigureAwait(false))
			{
				var transition = await FindAutoTransitionAsync(value, principal).ConfigureAwait(false);
				if (transition is not null)
					await ContinueAsync(transition, value, principal).ConfigureAwait(false);
				return true;
			}
		}
		return false;
	}

	private async Task<Transition<T>?> FindAutoTransitionAsync(T context, IPrincipal? principal)
	{
		if (!_transitions.TryGetValue(CurrentState, out var all))
			return null;

		Transition<T>? transition = null;
		foreach (var item in all)
		{
			if (item.Event.IsEmpty && await item.CanMoveAlongAsync(context, this, principal).ConfigureAwait(false))
			{
				if (transition is null)
					transition = item;
				else
					throw new InvalidOperationException($"More than one transitions found for state {Name}.");
			}
		}
		return transition;
	}

	private async Task ContinueAsync(Transition<T> transition, T value, IPrincipal? principal)
	{
		await MoveAlongAsync(transition, value).ConfigureAwait(false);
		var transition2 = await IdleAsync(transition, value, principal).ConfigureAwait(false);
		await CurrentState.OnStateEnteredAsync(value, this, transition2, principal).ConfigureAwait(false);
		await OnStateEnteredAsync(value, CurrentState, transition2).ConfigureAwait(false);
		if (CurrentState.IsFinished && IsInFinalState())
			await OnFinishAsync(value).ConfigureAwait(false);
	}

	private Task OnStateExitAsync(T context, State<T> state, Transition<T> transition)
	{
		if (StateExit.IsEmpty)
			return Task.CompletedTask;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStateExitAsync)} {state.Token.FullName()}");
		return StateExit.InvokeAsync(context, this, state, transition);
	}

	private Task OnStateEnteredAsync(T context, State<T> state, Transition<T> transition)
	{
		if (StateEntered.IsEmpty)
			return Task.CompletedTask;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStateEnteredAsync)} {state.Token.FullName()}");
		return StateEntered.InvokeAsync(context, this, state, transition);
	}

	private Task OnStatePassthroughAsync(T context, State<T> state, Transition<T> transition)
	{
		if (StatePassthrough.IsEmpty)
			return Task.CompletedTask;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStatePassthroughAsync)} {state.Token.FullName()}");
		return StatePassthrough.InvokeAsync(context, this, state, transition);
	}

	private Task OnStateEnterAsync(T context, State<T> state, Transition<T> transition)
	{
		if (StateEnter.IsEmpty)
			return Task.CompletedTask;
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"Invoke {nameof(OnStateEnterAsync)} {state.Token.FullName()}");
		return StateEnter.InvokeAsync(context, this, state, transition);
	}

	private Task OnStartAsync(T context)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnStartAsync)}");
		if (ChartStart.IsEmpty)
			return Task.CompletedTask;
		return ChartStart.InvokeAsync(context, this, null, null);
	}

	private Task OnFinishAsync(T context)
	{
		if (Log.IsEnabled(LogType.Trace))
			Log.Trace($"{Token.FullName()}: {nameof(OnFinishAsync)}");
		if (ChartFinish.IsEmpty)
			return Task.CompletedTask;
		return ChartFinish.InvokeAsync(context, this, null, null);
	}

	private async Task<Transition<T>> IdleAsync(Transition<T> transition, T value, IPrincipal? principal)
	{
		State<T> initial = CurrentState;
		int limit = States.Count * 2;
		Transition<T>? temp;
		while ((temp = await FindAutoTransitionAsync(value, principal).ConfigureAwait(false)) != default)
		{
			transition = temp;
			if (--limit < 0)
				throw new InvalidOperationException($"Transition<T> loop for state chart {Name} state {initial.Name}");
			await CurrentState.OnStatePassthroughAsync(value, this, transition).ConfigureAwait(false);
			await OnStatePassthroughAsync(value, CurrentState, transition).ConfigureAwait(false);
			await MoveAlongAsync(transition, value).ConfigureAwait(false);
		}
		return transition;
	}

	private async Task MoveAlongAsync(Transition<T> transition, T value)
	{
		if (!CurrentState.IsEmpty)
		{
			await CurrentState.OnStateExitAsync(value, this, transition).ConfigureAwait(false);
			await OnStateExitAsync(value, CurrentState, transition).ConfigureAwait(false);
		}
		await transition.OnMoveAlongAsync(value, this).ConfigureAwait(false);
		CurrentState = transition.Destination;
		await CurrentState.OnStateEnterAsync(value, this, transition).ConfigureAwait(false);
		await OnStateEnterAsync(value, CurrentState, transition).ConfigureAwait(false);
	}

	#endregion

	#region StateTree

	private StatesChain<T> CollectTree(StatesChain<T> tree, StatesChainItem<T>? parent)
	{
		var root = new StatesChainItem<T>(parent, this, CurrentState);
		tree.Add(root);
		if (CurrentState.Charts.Count > 0)
		{
			foreach (var chart in CurrentState.Charts)
			{
				chart.CollectTree(tree, root);
			}
		}
		return tree;
	}

	#endregion
}


