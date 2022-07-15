using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lexxys.States
{
	public class Statechart<T>
	{
		private static ILogging Log => __log ??= StaticServices.Create<ILogging<Statechart<T>>>();
		private static ILogging? __log;

		private readonly List<State<T>> _states;
		private readonly Dictionary<State<T>, List<Transition<T>>> _transitions;
		public State<T> _currentState;

		public Statechart(Token token, IEnumerable<State<T>> states, IEnumerable<Transition<T>> transitions)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			if (token.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(token), token, null);
			if (states == null)
				throw new ArgumentNullException(nameof(states));
			if (transitions == null)
				throw new ArgumentNullException(nameof(transitions));

			_states = states.ToList();
			var tt = transitions.ToIReadOnlyCollection();
			foreach (var item in tt)
			{
				if (!item.Source.IsEmpty && !_states.Contains(item.Source))
					throw new ArgumentException("Transitioning from an external state chart is not supported.");
				if (!_states.Contains(item.Destination))
					throw new ArgumentException("Transition outside of the state chart is not supported.");
			}

			_transitions = tt.GroupBy(o => o.Source).ToDictionary(o => o.Key, o => o.ToList());
			if (!_transitions.TryGetValue(State<T>.Empty, out var initial))
				throw new ArgumentException("Missing initial transition.");
			if (initial.Count != 1)
				throw new ArgumentException("Multiple initial transitions found.");
			_currentState = State<T>.Empty;
			Token = token;
		}

		public State<T> CurrentState
		{
			get => _currentState;
			private set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (!value.IsEmpty && !_states.Contains(value))
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				_currentState = value;
			}
		}
		public Token Token { get; }

		public int Id => Token.Id;
		public string Name => Token.Name;
		public string? Description => Token.Description;

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

		public StateActionChain<T> OnLoad;
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
		/// Executes when the <see cref="State{T}"/> object is trying to become a corrent state in the <see cref="Statechart{T}"/>.
		/// </summary>
		public StateActionChain<T> StateEnter;
		/// <summary>
		/// Executes when the <see cref="State{T}"/> object became a current state.
		/// </summary>
		public StateActionChain<T> StateEntered;
		/// <summary>
		/// Executes when instead of setting as a current state, the <see cref="State{T}"/> object switches to another one by condition.
		/// </summary>
		public StateActionChain<T> StatePassthrough;
		/// <summary>
		/// Executes when the <see cref="State{T}"/> object exits the current state condition.
		/// </summary>
		public StateActionChain<T> StateExit;

		#endregion

		/// <summary>
		/// Returns all the statecharts, including this one.
		/// </summary>
		public IReadOnlyList<Statechart<T>> Charts => __charts ??= CollectCharts();
		private IReadOnlyList<Statechart<T>>? __charts;

		/// <summary>
		/// Returns statechart by it's name. Throws <see cref="ArgumentException"/> for duplicate names.
		/// </summary>
		public IReadOnlyDictionary<string, Statechart<T>> ChartsByName => __chartsByName ??= ReadOnly.Wrap(Charts.ToDictionary(o => o.Name))!;
		private IReadOnlyDictionary<string, Statechart<T>>? __chartsByName;

		/// <summary>
		/// Returns statechart by it's ID. Throws <see cref="ArgumentException"/> for duplicate IDs.
		/// </summary>
		public IReadOnlyDictionary<int, Statechart<T>> ChartsById
			=> __chartsById ??= ReadOnly.Wrap(Charts.ToDictionary(o => o.Id))!;
		private IReadOnlyDictionary<int, Statechart<T>>? __chartsById;

		private IReadOnlyList<Statechart<T>> CollectCharts()
		{
			List<Statechart<T>> list = new() { this };
			foreach (var state in _states)
			{
				foreach (var chart in state.Charts)
				{
					list.AddRange(chart.Charts);
				}
			}
			return ReadOnly.Wrap(list)!;
		}

		public void Accept(IStatechartVisitor<T> visitor)
		{
			visitor.Visit(this);
			foreach (var state in _states)
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

		public void SetCurrentState(int? stateId)
			=> CurrentState = stateId is null ? State<T>.Empty : _states.FirstOrDefault(o => o.Id == stateId) ?? throw new ArgumentOutOfRangeException(nameof(stateId), stateId, null);

		public void SetCurrentState(string? stateName)
			=> CurrentState = stateName is null ? State<T>.Empty : _states.FirstOrDefault(o => o.Name == stateName) ?? throw new ArgumentOutOfRangeException(nameof(stateName), stateName, null);

		public void SetCurrentState(Token stateToken)
			=> CurrentState = stateToken.IsEmpty() ? State<T>.Empty : _states.FirstOrDefault(o => o.Token == stateToken) ?? throw new ArgumentOutOfRangeException(nameof(stateToken), stateToken, null);

		public void Start(T value, IPrincipal? principal = null)
		{
			Reset();
			var start = FindInitialTransition();
			OnStart(value);

			Continue(start, value, principal);
		}

		public async Task StartAsync(T value, IPrincipal? principal = null)
		{
			Reset();
			var start = FindInitialTransition();
			await OnStartAsync(value);

			await ContinueAsync(start, value, principal);
		}

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
				Log.Trace($"{item.Token.FullName()} OnLoad");
				item.OnLoad.Invoke(value, this, null, null);
			}
		}

		/// <summary>
		/// Asynchroniously invokes <see cref="OnLoad"/> for this state chart and all the sub-charts.
		/// </summary>
		/// <param name="value">Object the statechart corresponds to.</param>
		public async Task LoadAsync(T value)
		{
			//foreach (var item in Charts.Where(o => !o.OnLoad.IsEmpty))
			//{
			//	Log.Trace($"{item.Token.FullName()} OnLoadAsync");
			//	await item.OnLoad.InvokeAsync(value, item, null, null);
			//}
			await Task.WhenAll(Charts.Where(o => !o.OnLoad.IsEmpty).Select(o =>
			{
				Log.Trace($"{o.Token.FullName()} OnLoadAsync");
				return o.OnLoad.InvokeAsync(value, o, null, null);
			}));
		}

		public void Update(T value)
		{
			foreach (var item in Charts.Where(o => !o.OnUpdate.IsEmpty))
			{
				Log.Trace($"{item.Token.FullName()} OnUpdate");
				item.OnUpdate.Invoke(value, this, null, null);
			}
		}

		public async Task UpdateAsync(T value)
		{
			//foreach (var item in Charts.Where(o => !o.OnUpdate.IsEmpty))
			//{
			//	Log.Trace($"{item.Token.FullName()} OnUpdateAsync");
			//	await item.OnUpdate.InvokeAsync(value, this, null, null);
			//}
			await Task.WhenAll(Charts.Where(o => !o.OnUpdate.IsEmpty).Select(o =>
			{
				Log.Trace($"{o.Token.FullName()} OnUpdateAsync");
				return o.OnUpdate.InvokeAsync(value, o, null, null);
			}));
		}

		/// <summary>
		/// Execute the transition event <see cref="TransitionEvent{T}"/>.  Returns <see cref="true"/> if the state was chaged.
		/// </summary>
		/// <param name="event">The transition event</param>
		/// <param name="value">Entity object</param>
		/// <param name="principal">Permissions</param>
		/// <returns>True if the state was changes</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public bool OnEvent(TransitionEvent<T> @event, T value, IPrincipal? principal = null)
		{
			if (@event == null)
				throw new ArgumentNullException(nameof(@event));
			if (value == null)
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

		public async Task<bool> OnEventAsync(TransitionEvent<T> @event, T value, IPrincipal? principal = null)
		{
			if (@event == null)
				throw new ArgumentNullException(nameof(@event));
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (@event.Chart != this)
				return await OnTransitionEventSubchartsAsync(@event, value, principal);

			if (@event.Transition?.Source != CurrentState)
				throw new ArgumentOutOfRangeException(nameof(@event), @event, "Event.Transition.Source is not equal to the current state.");
			if (!_transitions.TryGetValue(CurrentState, out var transitions) || !transitions.Contains(@event.Transition))
				throw new ArgumentOutOfRangeException(nameof(@event), @event, "Specified Event.Transition is not in the list of availabe transitions.");

			await ContinueAsync(@event.Transition, value, principal);
			return true;
		}

		public IEnumerable<TransitionEvent<T>> GetActiveEvents(T value, IPrincipal? principal = null)
			=> GetActjveStates()
				.SelectMany(o => o.Chart.GetStateTransitions(o.State)
					.Where(t => (o.State.IsFinished || !t.Event.IsEmpty()) && t.CanMoveAlong(value, this, principal))
					.Select(t => new TransitionEvent<T>(o.Chart, t)));

		public async IAsyncEnumerable<TransitionEvent<T>> GetActiveEventsAsync(T value, IPrincipal? principal = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
		{
			foreach (var item in GetActjveStates())
			{
				foreach (var transition in item.Chart.GetStateTransitions(item.State))
				{
					if (cancellation.IsCancellationRequested)
						yield break;
					if ((item.State.IsFinished || !transition.Event.IsEmpty()) && await transition.CanMoveAlongAsync(value, this, principal))
						yield return new TransitionEvent<T>(item.Chart, transition);
				}
			}
		}

		public StateTree GetActjveStates() => CollectTree(new StateTree(), null);

		public override string ToString()
		{
			var text = new StringBuilder();
			if (Id > 0)
				text.Append(Id).Append('.');
			text.Append(Name).Append(' ')
				.Append(_states.Count).Append(" states, ");
			if (CurrentState.IsEmpty)
				text.Append("(empty) not started");
			else
				text.Append('(').Append(CurrentState.Id)
					.Append(' ').Append(CurrentState.Name).Append(')')
					.Append(IsFinished ? " finished" : " in progress");
			if (Description != null)
				text.Append(" - ").Append(Description);
			return text.ToString();
		}

		private IReadOnlyList<Transition<T>> GetStateTransitions(State<T> state)
			=> _transitions.TryGetValue(state, out var transitions) ? transitions : Array.Empty<Transition<T>>();

		private Transition<T> FindInitialTransition()
			=> _transitions.GetValueOrDefault(State<T>.Empty)[0];

		#region Sync

		private bool OnTransitionEventSubcharts(TransitionEvent<T> @event, T value, IPrincipal? principal)
		{
			foreach (var item in CurrentState.Charts)
			{
				if (item.OnEvent(@event, value, principal))
				{
					var transition = FindAutoTransition(value, principal);
					if (transition != null)
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
			foreach (var item in all.Where(o => o.Event.IsEmpty() && o.CanMoveAlong(context, this, principal)))
			{
				if (transition == null)
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
			Log.Trace($"Invoke {nameof(OnStateExit)} {state.Token.FullName()}");
			StateExit.Invoke(context, this, state, transition);
		}

		private void OnStateEntered(T context, State<T> state, Transition<T> transition)
		{
			if (StateEntered.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStateEntered)} {state.Token.FullName()}");
			StateEntered.Invoke(context, this, state, transition);
		}

		private void OnStatePassthrough(T context, State<T> state, Transition<T> transition)
		{
			if (StatePassthrough.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStatePassthrough)} {state.Token.FullName()}");
			StatePassthrough.Invoke(context, this, state, transition);
		}

		private void OnStateEnter(T context, State<T> state, Transition<T> transition)
		{
			if (StateEnter.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStateEnter)} {state.Token.FullName()}");
			StateEnter.Invoke(context, this, state, transition);
		}

		private void OnStart(T context)
		{
			if (ChartStart.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStart)} {Token.FullName()}");
			ChartStart.Invoke(context, this, null, null);
		}

		private void OnFinish(T context)
		{
			if (ChartFinish.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnFinish)} {Token.FullName()}");
			ChartFinish.Invoke(context, this, null, null);
		}

		private Transition<T> Idle(Transition<T> transition, T value, IPrincipal? principal)
		{
			State<T> initial = CurrentState;
			int limit = _states.Count * 2;
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
				if (await item.OnEventAsync(@event, value, principal))
				{
					var transition = await FindAutoTransitionAsync(value, principal);
					if (transition != null)
						await ContinueAsync(transition, value, principal);
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
				if (item.Event.IsEmpty() && await item.CanMoveAlongAsync(context, this, principal))
				{
					if (transition == null)
						transition = item;
					else
						throw new InvalidOperationException($"More than one transitions found for state {Name}.");
				}
			}
			return transition;
		}

		private async Task ContinueAsync(Transition<T> transition, T value, IPrincipal? principal)
		{
			await MoveAlongAsync(transition, value);
			var transition2 = await IdleAsync(transition, value, principal);
			await CurrentState.OnStateEnteredAsync(value, this, transition2, principal);
			await OnStateEnteredAsync(value, CurrentState, transition2);
			if (!CurrentState.IsFinished && IsInFinalState())
				await OnFinishAsync(value);
		}

		private async Task OnStateExitAsync(T context, State<T> state, Transition<T> transition)
		{
			if (StateExit.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStateExitAsync)} {state.Token.FullName()}");
			await StateExit.InvokeAsync(context, this, state, transition);
		}

		private async Task OnStateEnteredAsync(T context, State<T> state, Transition<T> transition)
		{
			if (StateEntered.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStateEnteredAsync)} {state.Token.FullName()}");
			await StateEntered.InvokeAsync(context, this, state, transition);
		}

		private async Task OnStatePassthroughAsync(T context, State<T> state, Transition<T> transition)
		{
			if (StatePassthrough.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStatePassthroughAsync)} {state.Token.FullName()}");
			await StatePassthrough.InvokeAsync(context, this, state, transition);
		}

		private async Task OnStateEnterAsync(T context, State<T> state, Transition<T> transition)
		{
			if (StateEnter.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStateEnterAsync)} {state.Token.FullName()}");
			await StateEnter.InvokeAsync(context, this, state, transition);
		}

		private async Task OnStartAsync(T context)
		{
			if (ChartStart.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnStartAsync)} {Token.FullName()}");
			await ChartStart.InvokeAsync(context, this, null, null);
		}

		private async Task OnFinishAsync(T context)
		{
			if (ChartFinish.IsEmpty)
				return;
			Log.Trace($"Invoke {nameof(OnFinishAsync)} {Token.FullName()}");
			await ChartFinish.InvokeAsync(context, this, null, null);
		}

		private async Task<Transition<T>> IdleAsync(Transition<T> transition, T value, IPrincipal? principal)
		{
			State<T> initial = CurrentState;
			int limit = _states.Count * 2;
			Transition<T>? temp;
			while ((temp = await FindAutoTransitionAsync(value, principal)) != default)
			{
				transition = temp;
				if (--limit < 0)
					throw new InvalidOperationException($"Transition<T> loop for state chart {Name} state {initial.Name}");
				await CurrentState.OnStatePassthroughAsync(value, this, transition);
				await OnStatePassthroughAsync(value, CurrentState, transition);
				await MoveAlongAsync(transition, value);
			}
			return transition;
		}

		private async Task MoveAlongAsync(Transition<T> transition, T value)
		{
			if (!CurrentState.IsEmpty)
			{
				await CurrentState.OnStateExitAsync(value, this, transition);
				await OnStateExitAsync(value, CurrentState, transition);
			}
			await transition.OnMoveAlongAsync(value, this);
			CurrentState = transition.Destination;
			await CurrentState.OnStateEnterAsync(value, this, transition);
			await OnStateEnterAsync(value, CurrentState, transition);
		}

		#endregion

		#region StateTree

		private StateTree CollectTree(StateTree tree, StateTreeItem? parent)
		{
			var root = new StateTreeItem(parent, this, CurrentState);
			tree.Add(root);
			if (CurrentState.Charts.Count > 0)
			{
				foreach (var chart in CurrentState.Charts)
				{
					chart.CollectTree(tree, parent);
				}
			}
			return tree;
		}

		public class StateTree: IReadOnlyList<StateTreeItem>
		{
			private readonly List<StateTreeItem> _items;

			public StateTree()
			{
				_items = new List<StateTreeItem>();
			}

			public StateTreeItem this[int index] => _items[index];

			public int Count => _items.Count;

			public void Add(StateTreeItem item)
			{
				if (item.Parent != null && !_items.Contains(item.Parent))
					throw new ArgumentException("Provided tree item is not compatible with the tree.");
				_items.Add(item);
			}

			public IEnumerable<StateTreeItem> GetLeafs() => _items.Where(o => !_items.Any(p => p.Parent == o));

			public IEnumerator<StateTreeItem> GetEnumerator() => _items.GetEnumerator();

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
		}

		public record StateTreeItem(StateTreeItem? Parent, Statechart<T> Chart, State<T> State)
		{
			public string GetPath(string? delimiter = null, bool includeChartName = false)
			{
				var text = new StringBuilder();
				var item = this;
				if (delimiter == null)
					delimiter = " > ";
				while (item != null)
				{
					if (text.Length != 0)
						text.Append(delimiter);
					if (includeChartName)
						text.Append(item.Chart.Name).Append(':');
					text.Append(item.State.Name);
					item = item.Parent;
				}
				return text.ToString();
			}
		}

		#endregion
	}
}
