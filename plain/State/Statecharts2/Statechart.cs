using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Principal;
using System.Threading;

using Lexxys;
using Lexxys.Xml;

namespace State.Statecharts2
{

	public class Statechart<TState, TEntity>
	{
		private State<TState, TEntity> _current;

		public Statechart(Statechart<TState, TEntity> other)
		{
			Contract.Requires(other != null);
			Name = other.Name;
			States = ReadOnly.Wrap(other.States.Select(o => new State<TState, TEntity>(o)).ToList());
			InitialState = States[other.States.FindIndex(other.InitialState)];
			_current = new State<TState, TEntity>(other._current);
		}

		public Statechart(StatechartSettings setting, Func<string, TState> stateParser)
			: this(setting, stateParser, null)
		{
			var map = FillStatesMap(new Dictionary<string, State<TState, TEntity>>(StringComparer.OrdinalIgnoreCase));
			InitializeTransitions(null, null, setting, map);
		}

		public string Name { get; }

		public IReadOnlyList<State<TState, TEntity>> States { get; }

		public State<TState, TEntity> InitialState { get; }

		public State<TState, TEntity> Current
		{
			get => _current;
			private set => _current = States.Contains(value) ? value : throw EX.ArgumentOutOfRange(nameof(value), value);
		}

		public void Start() => Current = InitialState;

		public IList<Transition<TState, TEntity>> GetActiveTransitions(TEntity context, IPrincipal principal)
		{
			return AddActiveTransitions(new List<Transition<TState, TEntity>>(), context, principal);
		}

		#region Implementation

		private Statechart(StatechartSettings setting, Func<string, TState> stateParser, string path)
		{
			if (setting == null)
				throw new ArgumentNullException(nameof(setting));

			string name = MakeName(setting.Name, path);
			var stt = setting.States.Select( o=>
				{
					if (o.Name == State<TState, TEntity>.Star.Name)
						return State<TState, TEntity>.Star;
					if (o.SubChart == null)
						return new State<TState, TEntity>(stateParser(o.Value), o.Name, o.Permission, StateCondition.Create<TState, TEntity>(o.Condition), StateAction.Create<TState, TEntity>(o.OnEnter), StateAction.Create<TState, TEntity>(o.OnEntered), StateAction.Create<TState, TEntity>(o.OnExit));
					return new SupperState<TState, TEntity>(stateParser(o.Value), o.Name, o.Permission, Statechart<TState, TEntity>.Create(o.SubChart, stateParser, name), StateCondition.Create<TState, TEntity>(o.Condition), StateAction.Create<TState, TEntity>(o.OnEnter), StateAction.Create<TState, TEntity>(o.OnEntered), StateAction.Create<TState, TEntity>(o.OnExit));
				}
				).ToList();
			if (!stt.Contains(State<TState, TEntity>.Star))
				stt.Add(State<TState, TEntity>.Star);
			Name = name;
			States = ReadOnly.Wrap(stt);
			InitialState = stt.FirstOrDefault(o => o.Name == setting.InitialState) ?? stt[0];
			_current = InitialState;
		}

		private static Statechart<TState, TEntity> Create(StatechartSettings setting, Func<string, TState> stateParser, string path)
		{
			return setting == null ? null : new Statechart<TState, TEntity>(setting, stateParser, path);
		}

		private void InitializeTransitions(string stateName, string path, StatechartSettings setting, Dictionary<string, State<TState, TEntity>> map)
		{
			string name = MakeName(stateName, path);
			for (int i = 0; i < setting.States.Count; ++i)
			{
				var ss = setting.States[i];
				var st = States[i];
				st.Transitions = ReadOnly.WrapCopy(ss.Transitions
					.Select(o => new Transition<TState, TEntity>(o.Event, st, FindByName(o.Target, name, map), StateCondition.Create<TState, TEntity>(o.Condition), StateAction.Create<TState, TEntity>(o.Action))));
				if (st is SupperState<TState, TEntity> sst)
					sst.Subchart.InitializeTransitions(st.Name, name, ss.SubChart, map);
			}
		}

		private static State<TState, TEntity> FindByName(string name, string current, Dictionary<string, State<TState, TEntity>> map)
		{
			if (name == "*")
				return State<TState, TEntity>.Star;

			string key = name;
			if (name.StartsWith("."))
			{

				key = name.TrimStart('.');
				int k = name.Length - key.Length - 1;
				if (k > 0)
				{
					var xx = current.Split('.');
					if (xx.Length <= k)
						return map[name];
					key = string.Join(".", xx.Take(xx.Length - k)) + "." + key;
				}
			}
			else if (current != null)
			{
				key = current + "." + name;
			}
			string keyPart = key;
			for (; ;)
			{
				if (map.TryGetValue(keyPart, out State<TState, TEntity> result))
					return result;
				int i = keyPart.IndexOf('.');
				if (i < 0)
					throw EX.ArgumentOutOfRange(nameof(key), key)
						.Add(nameof(current), current)
						.Add(nameof(name), name);
				keyPart = keyPart.Substring(i + 1);
			}
		}

		private List<Transition<TState, TEntity>> AddActiveTransitions(List<Transition<TState, TEntity>> collection, TEntity context, IPrincipal principal)
		{
			if (Current is SupperState<TState, TEntity> sst)
				sst.Subchart.AddActiveTransitions(collection, context, principal);
			collection.AddRange(Current.Transitions.Where(o => o.Event != Transition<TState, TEntity>.StarEvent && o.CanMoveAlong(context, principal)));
			return collection;
		}

		private Dictionary<string, State<TState, TEntity>> FillStatesMap(Dictionary<string, State<TState, TEntity>> map, string parentName = null)
		{
			if (States != null)
			{
				foreach (var item in States)
				{
					if (item == null)
						continue;
					string name = MakeName(item.Name, parentName);
					map.Add(name, item);
					if (item is SupperState<TState, TEntity> sst)
						sst.Subchart.FillStatesMap(map, name);
				}
			}
			return map;
		}

		public bool Execute(string eventName, TEntity context, IPrincipal principal)
		{
			State<TState, TEntity> target;
			State<TState, TEntity> past = Current;
			if (!(Current is SupperState<TState, TEntity> sst) || !sst.Subchart.Execute(eventName, context, principal))
			{
				target = Current.OnEvent(eventName, context, principal);
				if (target == null)
					return false;
				Current = target;
			}

			int limit = States.Count * 2;
			while ((target = Current.OnEvent(null, context, principal)) != null)
			{
				if (--limit < 0)
					throw EX.InvalidOperation($"Transition loop for statechart {this.Name} and state {past.Name}");
				Current = target;
			}
			return true;
		}

		private static string MakeName(string name, string parentName)
		{
			return name == null ? null :
				parentName == null ? name.Replace('.', '-') : parentName + "." + name.Replace('.', '-');
		}

		#endregion

		#region Configuration

		public static Statechart<TState, TEntity> FromXml(XmlLiteNode node)
		{
			StatechartSettings settings = XmlTools.GetValue<StatechartSettings>(node, null);
			return settings == null ? null: new Statechart<TState, TEntity>(settings, ParseEnum);
		}

		public static TState ParseEnum(string value)
		{
			return typeof(TState).IsEnum ? (TState)Enum.Parse(typeof(TState), value) : default;
		}

		public class StatechartSettings
		{
			public string Name { get; }
			public string InitialState { get; }
			public List<StateSettings> States { get; set; }

			public StatechartSettings(string name = null, string start = null)
			{
				Name = name;
				InitialState = start;
			}
		}

		public class StateSettings
		{
			public string Name { get; }
			public string Value { get; }
			public string Permission { get; }
			public string StateChartReference { get; }
			public string Condition { get; }
			public string OnEnter { get; }
			public string OnEntered { get; }
			public string OnExit { get; }
			public List<TransitionSettings> Transitions { get; set; }
			public StatechartSettings SubChart { get; set; }

			public StateSettings(string name, string value = null, string permission = null, string subchart = null, string condition = null, string onenter = null, string onentered = null, string onexit = null)
			{
				Name = name;
				Value = value ?? name;
				Permission = permission;
				Transitions = new List<TransitionSettings>();
				StateChartReference = subchart;
				Condition = condition;
				OnEnter = onenter;
				OnEntered = onentered;
				OnExit = onexit;
			}
		}

		public class TransitionSettings
		{
			public string Event { get; }
			public string Target { get; }
			public string Condition { get; }
			public string Action { get; }

			public TransitionSettings(string @event, string target, string condition = null, string action = null)
			{
				Event = @event;
				Target = target;
				Condition = condition;
				Action = action;
			}
		}

		#endregion
	}
}