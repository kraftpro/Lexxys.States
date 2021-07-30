using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.State.Con
{
	using States;

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
				var machine = CreateDiagram("one");
				if (auto != null)
				{
					machine.Load(value);
				}
				else
				{
					machine.ChartsByName["main"].SetCurrentState(value.Step1);
					machine.ChartsByName["subchart"].SetCurrentState(value.Step2);
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
					value.SetStep1((machine.ChartsByName["main"]?.CurrentState?.Id) ?? 0);
					value.SetStep2(machine.ChartsByName["subchart"]?.CurrentState?.Id);
				}
			}
		}

		public static Statechart<Entity> CreateDiagram(string name, string? method = null)
		{
			var st = StateFactory.Create(name);
			switch (method)
			{
				case "name":
					// Load/Update states of the statecharts by name;
					{
						st.OnLoad += (o, e) =>
						{
							o.ChartsByName["main"].SetCurrentState(e.Step1);
							if (e.Step2 != null)
								o.ChartsByName["subchart"].SetCurrentState(e.Step2);
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
						st.OnLoad += (o, e) => Array.ForEach(e.GetState(), s => o.ChartsById[s.ChartId].SetCurrentState(s.ChartState));
						st.OnUpdate += (o, e) => e.SetState(o.Charts.Select(c => (c.Id, c.CurrentState.Id)));
					}
					break;

				case "direct":
					// Load/Update states of the statecharts directly;
					{
						var st1 = st.ChartsByName["main"];
						var st2 = st.ChartsByName["subchart"];

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
}
