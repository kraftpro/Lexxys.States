using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests.Sample
{
	static class Runner
	{
		public static void Go(string[] args)
		{
			StaticServices.AddFactory(ConsoleLoggerFactory.Instance);
			StaticServices.ConfigService().AddConfiguration(new Uri(".\\sample-1.config.txt", UriKind.RelativeOrAbsolute));

			string chartName = GetChartName(args);
			var chart = ChartFactory.GetStatechart<Entity>(chartName);
			if (chart == null)
			{
				Console.WriteLine("Cannot create a statechart");
				return;
			}
			var obj = new Entity(chart.Charts.Count);
			chart.OnUpdate += (o, c) => o.SetStates(c.Charts.Select(s => s.CurrentState?.Id).ToList());
			chart.OnLoad += (o, c) => {
				foreach (var x in c.Charts.Zip(o.State, (Chart, State) => (Chart, State)))
				{
					x.Chart.SetCurrentState(x.State);
				}
			};
			Run(obj, chart);
		}

		static string GetChartName(string[] args)
		{
			if (args.Length > 0)
				return args[0];

			var items = ChartFactory.ListStatecharts().ToList();
			for (; ; )
			{
				for (int i = 0; i < items.Count; ++i)
				{
					Console.Write(i + 1);
					Console.Write(". ");
					Console.Write(items[i]);
				}
				Console.Write("> ");
				var s = Console.ReadLine();
				if (int.TryParse(s, out var j) && j >= 1 && j <= items.Count)
					return items[j - 1];
			}
		}

		static void Run<T>(T obj, Statechart<T> chart)
		{
			const int LOAD = 91;
			const int UPDATE = 92;

			chart.Load(obj);
			if (!chart.IsStarted)
				chart.Start(obj);
			for (;;)
			{
				var events = chart.GetActiveEvents(obj).ToList();
				for (int i = 0; i < events.Count; ++i)
				{
					Console.WriteLine($"{i + 1}. {events[i].Transition.Event.Name} at {events[i].Chart.Token.ToString(false)}:{events[i].Transition.Source.Token.ToString(false)}");
				}
				Console.WriteLine($"{LOAD}. Load");
				Console.WriteLine($"{UPDATE}. Update");
				var s = Console.ReadLine();
				if (!(int.TryParse(s, out var j) && j >= 1 && (j <= events.Count || j == LOAD || j == UPDATE)))
				{
					if (String.IsNullOrWhiteSpace(s))
						return;
					continue;
				}
				if (j == LOAD)
				{
					chart.Load(obj);
					if (!chart.IsStarted)
						chart.Start(obj);
				}
				else if (j == UPDATE)
				{
					chart.Update(obj);
				}
				else
				{
					chart.OnEvent(events[j - 1], obj);
				}
			}
		}
	}
}
