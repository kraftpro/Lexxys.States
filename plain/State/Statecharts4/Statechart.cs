using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;

using Lexxys;
using Lexxys.Xml;

namespace State.Statecharts4
{
	using Statecharts;

	public class Statechart<T>
	{

		//public Statechart<T>(Statechart<T> other)
		//{
		//	Contract.Requires(other != null);
		//	Name = other.Name;
		//	States = ReadOnly.WrapCopy(other.States.Select(o => new State<T>(o)));
		//	InitialState = States[other.States.FindIndex(other.InitialState)];
		//	_current = new State<T>(other._current);
		//}

		private static Statechart<T> Empty = new Statechart<T>();

		public string Name { get; }

		public IReadOnlyList<State<T>> States { get; }

		public State<T> InitialState { get; }

		/// <summary>
		/// Current <see cref="State{T}"/> in the <see cref="Statechart{T}"/>. null if the Statechart<T> is not starterd yet.
		/// </summary>
		public State<T> Current { get; private set; }

		/// <summary>
		/// Checks whether the <see cref="Statechart{T}"/> has been started.
		/// </summary>
		public bool IsStarted => Current != null;

		/// <summary>
		/// Indicates that the <see cref="Statechart{T}"/> is in progress state (i.e. started and not finished)
		/// </summary>
		public bool IsInProgress => Current != null && !Current.IsFinal;

		/// <summary>
		/// Indicates that the <see cref="Statechart{T}"/> is in final state.
		/// </summary>
		public bool IsFinished => Current != null && Current.IsFinal;

		/// <summary>
		/// Action executed when the <see cref="Statechart{T}"/> starting
		/// </summary>
		public virtual event Action<T, Statechart<T>> ChartStart;
		/// <summary>
		/// Action executed when the <see cref="Statechart{T}"/> switched to the final state.
		/// </summary>
		public virtual event Action<T, Statechart<T>> ChartFinish;

		/// <summary>
		/// Executes when the <see cref="State{T}"/> object is trying to become a corrent state in the <see cref="Statechart{T}"/>.
		/// </summary>
		public virtual event Action<T, State<T>, Transition<T>, Statechart<T>> StateEnter;
		/// <summary>
		/// Executes when the <see cref="State{T}"/> object became a current state.
		/// </summary>
		public virtual event Action<T, State<T>, Transition<T>, Statechart<T>> StateEntered;
		/// <summary>
		/// Executes when instead of setting as a current state, the <see cref="State{T}"/> object switches to another one by condition.
		/// </summary>
		public virtual event Action<T, State<T>, Transition<T>, Statechart<T>> StatePassthrough;
		/// <summary>
		/// Executes when the <see cref="State{T}"/> object exits the current state condition.
		/// </summary>
		public virtual event Action<T, State<T>, Transition<T>, Statechart<T>> StateExit;

		/// <summary>
		/// Cteates a new empty <see cref="Statechart{T}"/>.
		/// </summary>
		private Statechart()
		{
			Name = "()";
			States = Array.Empty<State<T>>();
		}


		//public Statechart(Statechart<T> that, Dictionary<State<T>, State<T>> states = null)
		//{
		//	if (that == null)
		//		throw new ArgumentNullException(nameof(that));

		//	if (states == null)
		//	{
		//		states = new HashSet<State<T>>();
		//		Accept(new StatechartVisitor<T>(state: o => states.Add(o)));
		//	}

		//	var ss = new State<T>[States.Count];
		//	for (int i = 0; i < ss.Length; i++)
		//	{
		//		ss[i] = new State<T>(that.States[i], states);
		//	}

		//	Name = that.Name;
		//	States = ReadOnly.Wrap(ss);
		//}

		internal void Accept(IStatechartVisitor<T> visitor)
		{
			if (visitor == null)
				throw new ArgumentNullException(nameof(visitor));

			visitor.Visit(this);
			foreach (var item in States)
			{
				item.Accept(visitor);
			}
		}

		/// <summary>
		/// Starts the current <see cref="Statechart{T}"/>.
		/// </summary>
		/// <param name="context">Execution context</param>
		/// <param name="principal">Current principals</param>
		public void Start(T context, IPrincipal principal = null, bool continues = false)
		{
			if (IsInProgress)
				return;
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			if (!continues || Current == null)
				Current = InitialState;

			OnStart(context);
			Current.OnStateEnter(context, null);
			Idle(Current, context, principal);
			Current.OnStateEntered(context, null);
			foreach (var item in Current.Subcharts)
			{
				item.Start(context, principal);
			}
		}

		public IReadOnlyList<string> CurrentPath(string prefix = null)
		{
			prefix += String.IsNullOrEmpty(Name) ? "[.]": "[" + Name + "]";
			if (Current == null)
				return new [] { prefix };
			return Current.CurrentPath(prefix);
		}

		public List<Transition<T>> GetActiveTransitions(T context, IPrincipal principal = null)
		{
			return AddActiveTransitions(new List<Transition<T>>(), context, principal);
		}

		public List<int?> SaveState()
		{
			var items = new List<int?>();
			SaveState(items);
			return items;
		}

		public void RestoreState(IReadOnlyList<int?> items)
		{
			RestoreState(items, 0);
		}

		public void SaveState(IList<int?> items)
		{
			items.Add(Current?.Id);
			foreach (var item in States.Where(o => o.Subcharts.Count > 0))
			{
				foreach (var chart in item.Subcharts)
				{
					chart.SaveState(items);
				}
			}
		}

		public int RestoreState(IReadOnlyList<int?> items, int index = 0)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			if (index < 0 || index >= items.Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			int? i = items[index];
			Current = States.FirstOrDefault(o => o.Id == i);
			if (Current == null && i != null)
				throw new ArgumentOutOfRangeException($"{nameof(items)}[{index}]", i, null);
			++index;
			foreach (var item in States.Where(o => o.Subcharts.Count > 0))
			{
				foreach (var chart in item.Subcharts)
				{
					index = RestoreState(items, index);
				}
			}
			return index;
		}

		public bool OnEvent(object @event, T context, IPrincipal principal = null)
		{
			if (Current == null)
				return false;

			bool moved = false;
			foreach (var item in Current.Subcharts)
			{
				moved |= item.OnEvent(@event, context, principal);
			}
			if (moved)
				if (Current.Subcharts.All(o => o.IsFinished))
					@event = Transition<T>.Finished;
				else
					return true;

			State<T> initial = Current;
			var transition = Current.FindTransition(@event, context, principal) ?? Current.FindTransition(null, context, principal);
			if (transition == null)
				return moved;

			MoveAlong(transition, context, principal);
			Idle(initial, context, principal);
			Current.OnStateEntered(context, transition);
			return true;
		}

		#region Implementation

		public Statechart(StatechartSettings setting)
			: this(setting, null)
		{
			var map = FillStatesMap(new Dictionary<string, State<T>>(StringComparer.OrdinalIgnoreCase));
			InitializeTransitions(null, null, setting, map);
		}

		private Statechart(StatechartSettings setting, string path)
		{
			if (setting == null)
				throw new ArgumentNullException(nameof(setting));

			string name = MakeName(setting.Name, path);
			var stt = setting.States?.Select(o => o.Name == "*" ? State<T>.Finished:
				new State<T>(
					id: o.Id,
					name: o.Name,
					description: o.Description,
					permission: o.Permission,
					subcharts: o.Subcharts?.Select(o => Statechart<T>.Create(o, name)).ToList(),
					guard: StateCondition.Create<T>(o.Guard),
					enter: StateAction.Create<T>(o.Enter),
					entered: StateAction.Create<T>(o.Entered),
					exit: StateAction.Create<T>(o.Exit),
					passthrough: StateAction.Create<T>(o.Passthrough))
					).ToList();
			if (stt == null)
				stt = new List<State<T>> { State<T>.Finished };
			else if (!stt.Contains(State<T>.Finished))
				stt.Add(State<T>.Finished);
			foreach (var item in stt)
			{
				if (item != State<T>.Finished)
				{
					item.StateEnter += OnStateEnter;
					item.StateEntered += OnStateEntered;
					item.StatePassthrough += OnStatePassthrough;
					item.StateExit += OnStateExit;
				}
			}
			Name = name;
			States = ReadOnly.Wrap(stt);
			InitialState = stt.FirstOrDefault(o => o.Name == setting.InitialState) ?? stt[0];
		}

		private void OnStateExit(T context, State<T> state, Transition<T> transition) => StateExit?.Invoke(context, state, transition, this);

		private void OnStateEntered(T context, State<T> state, Transition<T> transition) => StateEntered?.Invoke(context, state, transition, this);

		private void OnStatePassthrough(T context, State<T> state, Transition<T> transition) => StatePassthrough?.Invoke(context, state, transition, this);

		private void OnStateEnter(T context, State<T> state, Transition<T> transition) => StateEnter?.Invoke(context, state, transition, this);

		private void OnStart(T context) => ChartStart?.Invoke(context, this);

		private void OnFinish(T context) => ChartFinish?.Invoke(context, this);

		private void SetCurrent(State<T> value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (!States.Contains(value))
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			Current = value;
		}

		private bool MoveAlong(Transition<T> transition, T context, IPrincipal principal = null)
		{
			if (transition == null)
				return false;
			Current.OnStateExit(context, transition);
			transition.OnMoveAlong(context);
			SetCurrent(transition.Target);
			Current.OnStateEnter(context, transition);
			foreach (var chart in Current.Subcharts)
			{
				chart.Start(context, principal);
			}
			return true;
		}

		private void Idle(State<T> initial, T context, IPrincipal principal)
		{
			int limit = States.Count * 2;
			Transition<T> transition;
			while ((transition = Current?.FindTransition(null, context, principal)) != default)
			{
				if (--limit < 0)
					throw EX.InvalidOperation($"Transition<T> loop for state chart {Name} state {initial?.Name}");
				Current.OnStatePassthrough(context, transition);
				MoveAlong(transition, context, principal);
			}
		}

		private static Statechart<T> Create(StatechartSettings setting, string path)
		{
			return setting == null ? null : new Statechart<T>(setting, path);
		}

		private void InitializeTransitions(string stateName, string path, StatechartSettings setting, Dictionary<string, State<T>> map)
		{
			string name = MakeName(stateName, path);
			for (int i = 0; i < setting.States.Count; ++i)
			{
				var ss = setting.States[i];
				var st = States[i];
				st.Transitions = ReadOnly.WrapCopy(ss.Transitions
					.Select(o => new Transition<T>(o.IsStarEvent ? Transition<T>.Finished: o.IsEmptyEvent ? null: o.Event, st, FindTargetState(o, name, map), StateCondition.Create<T>(o.Guard), StateAction.Create<T>(o.Action))));
				for (int j = 0; j < st.Subcharts.Count; ++j)
				{
					st.Subcharts[j].InitializeTransitions(st.Name, name, ss.Subcharts[j], map);
				}
				//if (st is SupperState sst)
				//	sst.Subchart.InitializeTransitions(st.Name, name, ss.SubChart, map);
			}
		}

		private static State<T> FindTargetState(TransitionSettings transition, string currentState, Dictionary<string, State<T>> map)
		{
			var targetState = transition.Target;
			if (targetState == "*")
				return State<T>.Finished;

			string key = targetState;
			if (targetState.StartsWith("."))
			{

				key = targetState.TrimStart('.');
				int k = targetState.Length - key.Length - 1;
				if (k > 0)
				{
					var xx = currentState.Split('.');
					if (xx.Length <= k)
						return map[targetState];
					key = string.Join(".", xx.Take(xx.Length - k)) + "." + key;
				}
			}
			else if (currentState != null)
			{
				key = currentState + "." + targetState;
			}
			string keyPart = key;
			for (; ;)
			{
				if (map.TryGetValue(keyPart, out State<T> result))
					return result;
				int i = keyPart.IndexOf('.');
				if (i < 0)
					throw new ArgumentOutOfRangeException(nameof(key), key, $"Cannot find target state '{targetState}' for event '{transition.Event}' in state '{currentState}'.");
				keyPart = keyPart.Substring(i + 1);
			}
		}

		private List<Transition<T>> AddActiveTransitions(List<Transition<T>> collection, T context, IPrincipal principal)
		{
			if (Current != null)
			{
				foreach (var chart in Current.Subcharts)
				{
					chart.AddActiveTransitions(collection, context, principal);
				}
				collection.AddRange(Current.Transitions.Where(o => o.Event != Transition<T>.Finished && o.CanMoveAlong(context, principal)));
			}
			return collection;
		}

		private Dictionary<string, State<T>> FillStatesMap(Dictionary<string, State<T>> map, string parentName = null)
		{
			if (States != null)
			{
				foreach (var item in States)
				{
					if (item == null)
						continue;
					string name = MakeName(item.Name, parentName);
					Debug.Assert(!map.ContainsKey(name));
					map.Add(name, item);
					foreach (var chart in item.Subcharts)
					{
						chart.FillStatesMap(map, name);
					}
				}
			}
			return map;
		}

		private static string MakeName(string name, string parentName)
		{
			return name == null ? null :
				parentName == null ? name.Replace('.', '-') : parentName + "." + name.Replace('.', '-');
		}

		#endregion

		#region Configuration

		public static Statechart<T> FromXml(XmlLiteNode node)
		{
			StatechartSettings settings = XmlTools.GetValue<StatechartSettings>(node, null);
			return settings == null ? null: new Statechart<T>(settings);
		}

		public class StatechartSettings
		{
			public static readonly StatechartSettings Empty = new StatechartSettings("*", "*", null);

			public string Name { get; }
			public string InitialState { get; }
			public IReadOnlyList<StateSettings> States { get; set; }

			public StatechartSettings(string name, string start, IEnumerable<StateSettings> states)
			{
				Name = name;
				InitialState = start;
				States = states == null ? ReadOnly.Empty<StateSettings>():  ReadOnly.WrapCopy(states.Where(o => o != null));
			}

			public static StatechartSettings FromXml(XmlLiteNode x)
			{
				var ss = new StatechartSettings(x["name"], x["start"], x.Where("state").Select(o => o.AsValue<StateSettings>()).Where(o => o != null));
				return ss;
			}

		}

		public class StateSettings
		{
			public int Id { get; }
			public string Name { get; }
			public string Description { get; }
			public string Permission { get; }
			public string Guard { get; }
			public string Enter { get; }
			public string Entered { get; }
			public string Passthrough { get; }
			public string Exit { get; }
			public IReadOnlyList<TransitionSettings> Transitions { get; set; }
			public IReadOnlyList<StatechartSettings> Subcharts { get; set; }

			private StateSettings(string name, int? id = null, string description = null, string permission = null, string guard = null, string enter = null, string exit = null, string entered = null, string passthrough = null,
				IEnumerable<TransitionSettings> transitions = null,
				IEnumerable<StatechartSettings> subcharts = null
				)
			{
				Name = name;
				Id = id ?? 0;
				Description = description;
				if (id == null)
				{
					int i = name.IndexOf(':');
					if (i >= 0)
					{
						if (int.TryParse(name.Substring(0, i), out int value))
						{
							Name = name.Substring(i + 1).Trim();
							Id = value;
						}
					}
				}
				Permission = permission;
				Guard = guard;
				Enter = enter;
				Entered = entered;
				Exit = exit;
				Passthrough = passthrough;
				Transitions = transitions == null ? ReadOnly.Empty<TransitionSettings>() : ReadOnly.WrapCopy(transitions);
				Subcharts = subcharts == null ? ReadOnly.Empty<StatechartSettings>(): ReadOnly.WrapCopy(subcharts);
			}

			public static StateSettings FromXml(XmlLiteNode x)
			{
				var ss = new StateSettings(x["name"], x["id"].AsInt32(null), x["description"], x["permission"], x["guard"], x["enter"], x["exit"], x["entered"], x["passthrough"],
					x.Where("transition").Select(o => o.AsValue<TransitionSettings>()),
					x.Where("subchart").Select(o => o.AsValue<StatechartSettings>())
					);
				return ss;
			}
		}

		public class TransitionSettings
		{
			public string Event { get; }
			public string Target { get; }
			public string Guard { get; }
			public string Action { get; }

			public TransitionSettings(string @event, string target, string guard = null, string action = null)
			{
				Event = @event;
				Target = target;
				Guard = guard;
				Action = action;
			}

			public bool IsStarEvent => Event == "*";
			public bool IsEmptyEvent => String.IsNullOrEmpty(Event) || Event == "-";
		}

		#endregion
	}


	interface IStatechartVisitor<T>
	{
		void Visit(State<T> state);
		void Visit(Transition<T> state);
		void Visit(Statechart<T> statechart);
	}

	class StatechartVisitor<T>: IStatechartVisitor<T>
	{
		private readonly Action<Statechart<T>> _chart;
		private readonly Action<State<T>> _state;
		private readonly Action<Transition<T>> _transition;

		public StatechartVisitor(Action<Statechart<T>> chart = null, Action<State<T>> state = null, Action<Transition<T>> transition = null)
		{
			_chart = chart;
			_state = state;
			_transition = transition;
		}

		public void Visit(Statechart<T> statechart) => _chart?.Invoke(statechart);
		public void Visit(State<T> state) => _state?.Invoke(state);
		public void Visit(Transition<T> transition) => _transition?.Invoke(transition);
	}
}