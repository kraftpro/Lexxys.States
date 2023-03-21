using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexxys.States.Con;

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
				machine.Charts.First(o => o.Name == "main").SetCurrentState(value.Step1);
				machine.Charts.First(o => o.Name == "subchart").SetCurrentState(value.Step2);
			}

			System.Console.WriteLine($": {String.Join(", ", machine.GetActiveStates().GetLeaves().Select(o => o.GetPath(includeChartName: true)))}");
			var actions = machine.GetActiveEvents(value, user).ToList();
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
				var s = System.Console.ReadLine();
				k = s.AsInt32(0) - 1;
			}
			machine.OnEvent(actions[k], value, user);

			if (auto != null)
			{
				machine.Update(value);
			}
			else
			{
				value.SetStep1(machine.Charts.First(o => o.Name == "main").CurrentState.Id);
				value.SetStep2(machine.Charts.First(o => o.Name == "subchart").CurrentState.Id);
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
					st.OnLoad += (e, o) =>
					{
						o.Charts.First(o => o.Name == "main").SetCurrentState(e.Step1);
						if (e.Step2 != null)
							o.Charts.First(o => o.Name == "subchart").SetCurrentState(e.Step2);
					};
					st.OnUpdate += (e, o) =>
					{
						switch (o.Name)
						{
							case "main":
								e.SetStep1(o.CurrentState.Id);
								break;
							case "subchart":
								e.SetStep2(o.CurrentState.Id);
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
					st.OnLoad += (e, o) => Array.ForEach(e.GetState(), s => o.Charts.First(c => c.Id == s.ChartId).SetCurrentState(s.ChartState));
					st.OnUpdate += (e, o) => e.SetState(o.Charts.Select(c => (c.Id, c.CurrentState.Id)));
				}
				break;

			case "direct":
				// Load/Update states of the statecharts directly;
				{
					var st1 = st.Charts.First(o => o.Name == "main");
					var st2 = st.Charts.First(o => o.Name == "subchart");

					st1.OnLoad += (e, o) => o.SetCurrentState(e.Step1);
					st2.OnLoad += (e, o) => o.SetCurrentState(e.Step2);

					st1.OnUpdate += (e, o) => e.SetStep1(o.CurrentState.Id);
					st2.OnUpdate += (e, o) => e.SetStep2(o.CurrentState.Id);
				}
				break;

			default:
				break;
		}
		return st;
	}


	public static void Go(string[] args)
	{
		IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureLogging((context, builder) => {
			})
			.ConfigureServices((context, services) => {
				services.AddHostedService<Worker>();
			})
			.Build();
	}

	class Worker: IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
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
