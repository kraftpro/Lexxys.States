using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

#nullable enable

namespace State.Test1
{
	public class StateUsage
	{
		//public void Testing()
		//{
		//	var st = CreateDiagram();
		//	foreach (var item in st.GetActiveActions())
		//	{
		//		Console.WriteLine(item.Name);
		//	}
		//	var command = st.Actions.Create("send");
		//	//st.Action(command);
		//}

		public static void TestConsole()
		{
			string? auto = null;
			var value = new Entity();
			var user = new Principal("user");

			for (;;)
			{
				var machine = CreateDiagram();
				if (auto != null)
				{
					machine.Load(value);
				}
				else
				{
					machine.Charts["main"].SetCurrentState(value.Step1);
					machine.Charts["subchart"].SetCurrentState(value.Step2);
				}

				System.Console.WriteLine($": {String.Join(", ", machine.GetCurrentTree().GetLeafs().Select(o => o.GetPath(includeChartName: true)))}");
				var actions = machine.GetActiveActions(value, user).ToList();
				if (actions.Count == 0)
				{
					System.Console.WriteLine("Exiting");
					return;
				}
				int k = -1;
				while (k < 0 || k >= actions.Count)
				{
					for (int i = 0; i < actions.Count; ++i)
					{
						System.Console.WriteLine(" {0} - {1}", i + 1, actions[i]);
					}
					System.Console.Write(">");
					k = System.Console.ReadLine().AsInt32(0) - 1;
				}
				machine.OnEvent(actions[k], value, user);

				if (auto != null)
				{
					machine.Update(value);
				}
				else
				{
					value.SetStep1((machine.Charts["main"]?.CurrentState?.Id) ?? 0);
					value.SetStep2(machine.Charts["subchart"]?.CurrentState?.Id);
				}
			}
		}

		public static Statechart<Entity> CreateDiagram(string? method = null)
		{
			var st = new Statechart<Entity>();
			switch (method)
			{
				case "name":
					// Load/Update states of the statecharts by name;
					{
						st.OnLoad += (o, e) =>
						{
							o.Charts["main"].SetCurrentState(e.Step1);
							if (e.Step2 != null)
								o.Charts["subchart"].SetCurrentState(e.Step2);
						};
						st.OnUpdate += (o, e) =>
						{
							switch (o.Name)
							{
								case "main":
									e.SetStep1((o.CurrentState?.Id) ?? 0);
									break;
								case "subchart":
									e.SetStep2(o.CurrentState?.Id);
									break;
								default:
									throw new InvalidOperationException();
							}
						};
					}
					break;

				case "list":
					// Load/Update states of the statecharts by list of statesc;
					{
						st.OnLoad += (o, e) => o.SetCurrentState(e.GetState());
						st.OnUpdate += (o, e) => e.SetState(o.GetCurrentState());
					}
					break;

				case "direct":
					// Load/Update states of the statecharts directly;
					{
						var st1 = st.Charts["main"];
						var st2 = st.Charts["subchart"];

						st1.OnLoad += (o, e) => o.SetCurrentState(e.Step1);
						st2.OnLoad += (o, e) => o.SetCurrentState(e.Step2);

						st1.OnUpdate += (o, e) => e.SetStep1((o.CurrentState?.Id) ?? 0);
						st2.OnUpdate += (o, e) => e.SetStep2(o.CurrentState?.Id);
					}
					break;

				default:
					break;
			}
			return st;
		}
	}

	public class Principal: IPrincipal
	{
		public static readonly Principal Empty = new Principal();

		private IReadOnlyCollection<string> _roles;
		public IIdentity Identity { get; }

		private Principal()
		{
			Identity = new DirectIdentity("auto", "anonymous", false);
			_roles = Array.Empty<string>();
		}

		public Principal(string? authenticationType, string name, IEnumerable<string> roles)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));

			Identity = new DirectIdentity(authenticationType ?? "auto", name, true);
			_roles = roles.ToList();
		}

		public Principal(string name, IEnumerable<string> roles)
			: this(null, name, roles)
		{
		}

		public Principal(string? authenticationType, string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Identity = new DirectIdentity(authenticationType ?? "auto", name, true);
			_roles = Array.Empty<string>();
		}

		public Principal(string name)
			: this(null, name)
		{
		}

		public bool IsInRole(string role)
			=> _roles == null || _roles.Any(o => String.Equals(o, role, StringComparison.OrdinalIgnoreCase));

		class DirectIdentity: IIdentity
		{
			public string AuthenticationType { get; }
			public bool IsAuthenticated { get; }
			public string Name { get; }

			public DirectIdentity(string authenticationType, string name, bool isAuthenticated)
			{
				AuthenticationType = authenticationType ?? throw new ArgumentNullException(nameof(authenticationType));
				Name = name ?? throw new ArgumentNullException(nameof(name));
				IsAuthenticated = isAuthenticated;
			}
		}
	}

	public class Entity
	{
		public int Step1 { get; set; }
		public int? Step2 { get; set; }
		public void SetStep1(int value) => Step1 = value;
		public void SetStep2(int? value) => Step2 = value;

		public int?[] GetState() => new [] { Step1, Step2 };
		public void SetState(int?[] state)
		{
			Step1 = state[0] ?? 0;
			Step2 = state[1];
		}

		public void Update() { }
	}

	public class Statechart<T>
	{
		//public delegate void StateChangedAction(State<T> source, State<T> destination, bool passThrough);

		public event Action<Statechart<T>, T>? OnLoad;
		public event Action<Statechart<T>, T>? OnUpdate;

		public Statechart()
		{
			States = Array.Empty<State<T>>();
			Name = "*";
			//Actions = new StateFactory(Array.Empty<Token>());
		}

		public IReadOnlyList<State<T>> States { get; }

		public State<T>? CurrentState { get; private set; }

		//public ITokenFactory Actions { get; }

		//public IReadOnlyCollection<IToken> GetActiveActions() => Array.Empty<IToken>();

		public string Name { get; }

		public IReadOnlyDictionary<string, Statechart<T>> Charts => _charts ??= CollectCharts();
        private IReadOnlyDictionary<string, Statechart<T>>? _charts;

        private IReadOnlyDictionary<string, Statechart<T>> CollectCharts()
        {
			OrderedDictionary<string, Statechart<T>> list = new() { [Name] = this };
			foreach (var state in States)
			{
				foreach (var chart in state.Subcharts)
				{
					list.AddRange(chart.Charts);
				}
			}
			return ReadOnly.Wrap((IDictionary<string, Statechart<T>>)list);
        }

		public void SetCurrentState(int? stateId) => CurrentState = stateId == null ? null: FindState(stateId.Value) ?? throw new ArgumentOutOfRangeException(nameof(stateId), stateId, null);

		private State<T>? FindState(int stateId) => States.FirstOrDefault(o => o.Id == stateId);

		public void SetCurrentState(params int?[] states)
		{
			var charts = Charts;
			if (charts.Count != states.Length)
				throw new ArgumentOutOfRangeException(nameof(states), states.Length, null);
			var cc = charts.Values.GetEnumerator();
			foreach (var state in states)
			{
				cc.MoveNext();
				cc.Current.SetCurrentState(state);
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

		public void OnEvent(TransitionAction command, T value, IPrincipal principal)
		{
			throw new NotImplementedException();
			//foreach(var item in GetActiveTransitions(value, principal))
			//{
			//	if (item.Action == command)
			//	{
			//		item.Source.Action(command);
			//	}
			//}
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

		public IEnumerable<TransitionAction> GetActiveActions(T value, IPrincipal principal)
		{
			return GetActiveTransitions(value, principal)
				.Select(o => o.Action)
				.Where(o => o != null)!;
		}

		public IEnumerable<Transition<T>> GetActiveTransitions(T value, IPrincipal principal) => GetCurrentTree()
				.SelectMany(o => o.State.Transitions.Where(t => t.Roles.Any(r => principal.IsInRole(r)) && t.Guard.Allow(t, value)));

		public StateTree GetCurrentTree()
		{
			var tree = new StateTree();
			if (CurrentState != null)
				CollectTree(tree, null);
			return tree;
		}

		private void CollectTree(StateTree tree, StateTreeItem? parent)
		{
			if (CurrentState == null)
				return;
			var root = new StateTreeItem(parent, this, CurrentState);
			tree.Add(root);
			if (CurrentState.Subcharts.Count > 0)
			{
				foreach (var chart in CurrentState.Subcharts)
				{
					chart.CollectTree(tree, parent);
				}
			}
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
	}

	//public readonly struct StatePath<T>
	//{
	//	public IReadOnlyList<StatePathItem<T>> Items { get; }

	//	public StatePath(IEnumerable<StatePathItem<T>> items)
	//	{
	//		Items = ReadOnly.WrapCopy(items ?? throw new ArgumentNullException(nameof(items)));
	//	}

	//	public StatePath(StatePathItem<T> item, StatePath<T> path)
	//	{
	//		var items = new StatePathItem<T>[path.Items.Count + 1];
	//		items[0] = item;
	//		for (int i = 1; i < items.Length; i++)
	//		{
	//			items[i] = path.Items[i - 1];
	//		}
	//		Items = items;
	//	}

	//	public override string ToString() => String.Join(" > ", Items);

	//	public static StatePath<T> Concat(StatePath<T> left, StatePath<T> right) => new StatePath<T>(left.Items.Concat(right.Items));
	//}

	//public readonly struct StatePathItem<T>
	//{
	//	public Statechart<T> Statechart { get; }
	//	public State<T> State { get; }

	//	public StatePathItem(Statechart<T> statechart, State<T> state)
	//	{
	//		Statechart = statechart ?? throw new ArgumentNullException(nameof(statechart));
	//		State = state ?? throw new ArgumentNullException(nameof(state));
	//	}

	//	public override string ToString() => $"[{Statechart.Name}]:{State.Name}";
	//}

	public class TransitionAction
	{
		public int Id { get; }
		public string Name { get; }
		public string? Description { get; }

		public TransitionAction(int id, string name, string? description = null)
		{
			Id = id;
			Name = name;
			Description = description;
		}

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();
			text.Append('(').Append(Id).Append(')')
				.Append(' ').Append(Name);
			if (Description != null)
				text.Append(' ').Append(Description);
			return text.ToString();
		}
	}

	public class State<T> //: IToken
	{
		public int Id { get; }
		public string Name { get; }
		public string? Description { get; }
		public IList<Statechart<T>> Subcharts { get; }
		public IList<Transition<T>> Transitions { get; }

		public State(int id, string name, string? description, IList<Transition<T>>? transitions, IList<Statechart<T>>? subcharts)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;
			Subcharts = subcharts ?? Array.Empty<Statechart<T>>();
			Transitions = transitions ?? Array.Empty<Transition<T>>();
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

	public class Transition<T>
	{
		public TransitionAction? Action { get; }
		public ITransitionGuard<T> Guard { get; }
		public State<T> Source { get; }
		public State<T> Destination { get; }
		public string[] Roles { get; }

		public Transition(State<T> source, State<T> destination, TransitionAction? action, ITransitionGuard<T>? guard, string[]? roles = default)
		{
			Source = source;
			Destination = destination;
			Action = action;
			Guard = guard ?? EmptyGuard.Istance;
			Roles = roles ?? Array.Empty<string>();
		}

		private class EmptyGuard: ITransitionGuard<T>
		{
			public static readonly ITransitionGuard<T> Istance = new EmptyGuard();

			private EmptyGuard()
			{
			}

			public bool Allow(Transition<T> transition, T entity) => true;
		}
	}


	public interface ITransitionGuard<T>
	{
		bool Allow(Transition<T> transition, T entity);
	}
}
