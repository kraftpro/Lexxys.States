using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

using Lexxys;

#nullable enable

namespace State.Test1
{
	public class Statechart<T>
	{
		//public delegate void StateChangedAction(State<T> source, State<T> destination, bool passThrough);

		private readonly IReadOnlyList<State<T>> _states;
		public State<T>? _currentState;

		public event Action<Statechart<T>, T>? OnLoad;
		public event Action<Statechart<T>, T>? OnUpdate;

		public Statechart()
		{
			_states = Array.Empty<State<T>>();
			Token = Token.Empty;
			//Actions = new StateFactory(Array.Empty<Token>());
		}

		public State<T>? CurrentState
		{
			get => _currentState;
			private set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (!_states.Contains(value))
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
		public bool IsStarted => CurrentState != null;

		/// <summary>
		/// Indicates that the <see cref="Statechart{T}"/> is in progress state (i.e. started and not finished)
		/// </summary>
		public bool IsInProgress => CurrentState != null && !CurrentState.IsFinal;

		/// <summary>
		/// Indicates that the <see cref="Statechart{T}"/> is in final state.
		/// </summary>
		public bool IsFinished => CurrentState != null && CurrentState.IsFinal;

		#region Events

		/// <summary>
		/// Action executed when the <see cref="Statechart{T}"/> starting
		/// </summary>
		public event Action<T, Statechart<T>>? ChartStart;
		/// <summary>
		/// Action executed when the <see cref="Statechart{T}"/> switched to the final state.
		/// </summary>
		public event Action<T, Statechart<T>>? ChartFinish;

		/// <summary>
		/// Executes when the <see cref="State{T}"/> object is trying to become a corrent state in the <see cref="Statechart{T}"/>.
		/// </summary>
		public event Action<T, State<T>, Transition<T>, Statechart<T>>? StateEnter;
		/// <summary>
		/// Executes when the <see cref="State{T}"/> object became a current state.
		/// </summary>
		public event Action<T, State<T>, Transition<T>, Statechart<T>>? StateEntered;
		/// <summary>
		/// Executes when instead of setting as a current state, the <see cref="State{T}"/> object switches to another one by condition.
		/// </summary>
		public event Action<T, State<T>, Transition<T>, Statechart<T>>? StatePassthrough;
		/// <summary>
		/// Executes when the <see cref="State{T}"/> object exits the current state condition.
		/// </summary>
		public event Action<T, State<T>, Transition<T>, Statechart<T>>? StateExit;

		#endregion

		public IReadOnlyDictionary<string, Statechart<T>> Charts => _charts ??= CollectCharts();
        private IReadOnlyDictionary<string, Statechart<T>>? _charts;

		public void Accept(IStatechartVisitor<T> visitor)
		{
			visitor.Visit(this);
			foreach (var state in _states)
			{
				state.Accept(visitor);
			}
		}

		private IReadOnlyDictionary<string, Statechart<T>> CollectCharts()
        {
			OrderedDictionary<string, Statechart<T>> list = new() { [Name] = this };
			foreach (var state in _states)
			{
				foreach (var chart in state.Subcharts)
				{
					list.AddRange(chart.Charts);
				}
			}
			return ReadOnly.Wrap((IDictionary<string, Statechart<T>>)list);
        }

		public void SetCurrentState(int stateId) => CurrentState = FindState(stateId) ?? throw new ArgumentOutOfRangeException(nameof(stateId), stateId, null);

		public void SetCurrentState(params int?[] states)
		{
			var charts = Charts;
			if (charts.Count != states.Length)
				throw new ArgumentOutOfRangeException(nameof(states), states.Length, null);
			var cc = charts.Values.GetEnumerator();
			foreach (var state in states)
			{
				cc.MoveNext();
				if (state == null)
					cc.Current.Reset();
				else
					cc.Current.SetCurrentState(state.Value);
			}
		}

		public int?[] GetCurrentState()
		{
			var result = new int?[Charts.Count];
			int i = 0;
			foreach (var ch in Charts.Values)
			{
				result[i++] = ch.CurrentState?.Id;
			}
			return result;
		}

		private State<T>? FindState(int stateId) => _states.FirstOrDefault(o => o.Id == stateId);

		private State<T>? FindState(Token token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			var chart = FindStatechart(token.Domain);
			if (chart == null)
				return null;
			foreach (var state in chart._states)
			{
				if (state.Token == token)
					return state;
			}
			return null;
		}

		private Statechart<T>? FindStatechart(Token token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			if (Token == token)
				return this;
			if (!Token.Contains(token))
				return null;

			foreach (var state in _states)
			{
				foreach (var chart in state.Subcharts)
				{
					if (chart.Token == token)
						return chart;
				}
			}
			return null;
		}

		private State<T> FindInitialState() => _states[0];

		public void Start(T value, IPrincipal? principal = null)
		{
			if (IsInProgress)
				return;
			Reset();
			CurrentState = FindInitialState();
			OnStart(value);
			CurrentState.OnStateEnter(value, null);
			OnStateEnter(value, CurrentState, null);
			var transition = Idle(null, value, principal);
			CurrentState.OnStateEntered(value, transition, principal);
			OnStateEntered(value, CurrentState, transition);
			if (CurrentState.IsFinal)
				OnFinish(value);
		}

		public bool OnEvent(Token? @event, T value, IPrincipal? principal = null)
		{
			if (CurrentState == null)
				return false;

			bool moved = false;
			foreach (var item in CurrentState.Subcharts)
			{
				moved |= item.OnEvent(@event, value, principal);
			}
			if (moved)
				if (CurrentState.Subcharts.All(o => o.IsFinished))
					@event = null;
				else
					return true;

			State<T> initial = CurrentState;
			var transition = CurrentState.FirstTransition(@event, value, principal);
			if (transition == null)
			{
				if (@event == null || @event == Token.Empty)
					return moved;
				transition = CurrentState.FirstTransition(Token.Empty, value, principal);
				if (transition == null)
					return moved;
			}

			MoveAlong(transition, value, principal);
			var transition2 = Idle(transition, value, principal);
			CurrentState.OnStateEntered(value, transition2, principal);
			OnStateEntered(value, CurrentState, transition2);
			if (CurrentState.IsFinal)
				OnFinish(value);
			return true;
		}

		public void Reset()
		{
			foreach (var item in Charts.Values)
			{
				item.CurrentState = null;
			}
		}

		public void Load(T value)
		{
			foreach (var item in Charts.Values)
			{
				item.OnLoad?.Invoke(this, value);
			}
		}

		public void Update(T value)
		{
			foreach (var item in Charts.Values)
			{
				item.OnUpdate?.Invoke(this, value);
			}
		}

		public IEnumerable<Token> GetActiveActions(T value, IPrincipal principal)
			=> GetActiveTransitions(value, principal)
				.Select(o => o.Event);

		public IEnumerable<Transition<T>> GetActiveTransitions(T value, IPrincipal principal)
			=> GetCurrentTree()
				.SelectMany(o => o.State.Transitions.Where(t => t.CanMoveAlong(value, principal)));

		public StateTree GetCurrentTree() => CollectTree(new StateTree(), null);

		private StateTree CollectTree(StateTree tree, StateTreeItem? parent)
		{
			if (CurrentState == null)
				return tree;
			var root = new StateTreeItem(parent, this, CurrentState);
			tree.Add(root);
			if (CurrentState.Subcharts.Count > 0)
			{
				foreach (var chart in CurrentState.Subcharts)
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
					throw new ArgumentException();
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

		private void OnStateExit(T context, State<T> state, Transition<T> transition) => StateExit?.Invoke(context, state, transition, this);

		private void OnStateEntered(T context, State<T> state, Transition<T>? transition) => StateEntered?.Invoke(context, state, transition, this);

		private void OnStatePassthrough(T context, State<T> state, Transition<T>? transition) => StatePassthrough?.Invoke(context, state, transition, this);

		private void OnStateEnter(T context, State<T> state, Transition<T>? transition) => StateEnter?.Invoke(context, state, transition, this);

		private void OnStart(T context) => ChartStart?.Invoke(context, this);

		private void OnFinish(T context) => ChartFinish?.Invoke(context, this);

		private Transition<T>? Idle(Transition<T>? transition, T value, IPrincipal? principal)
		{
			if (CurrentState == null)
				return null;
			State<T> initial = CurrentState;
			int limit = _states.Count * 2;
			Transition<T>? temp;
			while ((temp = CurrentState.FirstTransition(null, value, principal)) != default)
			{
				transition = temp;
				if (--limit < 0)
					throw EX.InvalidOperation($"Transition<T> loop for state chart {Name} state {initial.Name}");
				CurrentState.OnStatePassthrough(value, transition);
				OnStatePassthrough(value, CurrentState, transition);
				MoveAlong(transition, value, principal);
			}
			return transition;
		}

		private bool MoveAlong(Transition<T> transition, T value, IPrincipal? principal)
		{
			if (CurrentState == null)
				return false;
			if (transition == null)
				return false;
			CurrentState.OnStateExit(value, transition);
			OnStateExit(value, CurrentState, transition);
			transition.OnMoveAlong(value);
			CurrentState = transition.Destination;
			CurrentState.OnStateEnter(value, transition);
			OnStateEnter(value, CurrentState, transition);
			return true;
		}
	}
}
