﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests.Sample
{
	static class Runner
	{
		const int LOAD = 55;
		const int UPDATE = 66;

		public static async Task Go(string[] args)
		{
			StaticServices.AddFactory(ConsoleLoggerFactory.Instance);
			StaticServices.ConfigService().AddConfiguration(new Uri(".\\sample-1.config.txt", UriKind.RelativeOrAbsolute));

			bool sync = args.Any(o => o == "sync");
			bool compile = args.Any(o => o == "compile");
			var name = GetChartName(args);
			if (name == null)
				return;
			if (!compile)
			{
				Console.Write("Compile [N] ? ");
				var k = Console.ReadKey(true);
				if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					compile = true;
				Console.WriteLine(compile ? "Y" : "N");
			}
			var chart = GetStatechart(name, compile);
			if (chart == null)
				return;

			if (!sync)
			{
				Console.Write("Sync [N] ? ");
				var k = Console.ReadKey(true);
				if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					sync = true;
				Console.WriteLine(sync ? "Y": "N");
			}


			if (sync)
				Run(new Entity(chart.Charts.Count), chart);
			else
				await RunAsync(new Entity(chart.Charts.Count), chart);
		}

		private static Statechart<Entity>? GetStatechart(string name, bool compile)
		{
			var chart = ChartFactory.GetStatechart<Entity>(name, compile);
			if (chart == null)
			{
				Console.WriteLine("Cannot create a statechart");
				return null;
			}
			chart.OnUpdate += StateAction.Create<Entity>(
				(o, c, s, t) => {
					Console.WriteLine("Running Update");
					o.SetStates(c.Charts.Select(s => s.CurrentState.Id).ToList());
				},
				(o, c, s, t) => {
					Console.WriteLine("Running UpdateAsync");
					o.SetStates(c.Charts.Select(s => s.CurrentState.Id).ToList()); return Task.CompletedTask;
				});
			chart.OnLoad += StateAction.Create<Entity>(
				(o, c, s, t) => {
					Console.WriteLine("Running Load");
					foreach (var x in c.Charts.Zip(o.State, (Chart, State) => (Chart, State)))
					{
						x.Chart.SetCurrentState(x.State);
					}
				},
				(o, c, s, t) => {
					Console.WriteLine("Running LoadAsync");
					foreach (var x in c.Charts.Zip(o.State, (Chart, State) => (Chart, State)))
					{
						x.Chart.SetCurrentState(x.State);
					}
					return Task.CompletedTask;
				});

			// chart.CurrentState
			return chart;
		}

		static string? GetChartName(string[] args)
		{
			var items = ChartFactory.ListStatecharts().ToList();
			var name = args.FirstOrDefault(o => items.Contains(o));
			if (name != null)
				return name;

			for (; ; )
			{
				for (int i = 0; i < items.Count; ++i)
				{
					Console.Write(i + 1);
					Console.Write(". ");
					Console.WriteLine(items[i]);
				}
				Console.Write("> ");
				var s = Console.ReadLine();
				if (String.IsNullOrEmpty(s))
					return null;
				if (int.TryParse(s, out var j) && j >= 1 && j <= items.Count)
					return items[j - 1];
			}
		}

		static void Run<T>(T obj, Statechart<T> chart)
		{
			chart.Load(obj);
			if (!chart.IsStarted)
				chart.Start(obj);
			for (;;)
			{
				List<TransitionEvent<T>> events = new List<TransitionEvent<T>>();
				foreach (var item in chart.GetActiveEvents(obj))
				{
					events.Add(item);
					Console.WriteLine($"{events.Count}. {item.Transition.Event.Name} at {item.Chart.Token.ToString(false)}:{item.Transition.Source.Token.ToString(false)}");
				}
				Console.WriteLine($"{LOAD}. Load");
				Console.WriteLine($"{UPDATE}. Update");
				Console.Write("> ");
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

		static async Task RunAsync<T>(T obj, Statechart<T> chart)
		{

			await chart.LoadAsync(obj);
			if (!chart.IsStarted)
				await chart.StartAsync(obj);
			for (; ; )
			{
				List<TransitionEvent<T>> events = new List<TransitionEvent<T>>();
				await foreach (var item in chart.GetActiveEventsAsync(obj))
				{
					events.Add(item);
					Console.WriteLine($"{events.Count}. {item.Transition.Event.Name} at {item.Chart.Token.ToString(false)}:{item.Transition.Source.Token.ToString(false)}");
				}
				Console.WriteLine($"{LOAD}. Load");
				Console.WriteLine($"{UPDATE}. Update");
				Console.Write("> ");
				var s = Console.ReadLine();

				if (!(int.TryParse(s, out var j) && j >= 1 && (j <= events.Count || j == LOAD || j == UPDATE)))
				{
					if (String.IsNullOrWhiteSpace(s))
						return;
					continue;
				}
				if (j == LOAD)
				{
					await chart.LoadAsync(obj);
					if (!chart.IsStarted)
						await chart.StartAsync(obj);
				}
				else if (j == UPDATE)
				{
					await chart.UpdateAsync(obj);
				}
				else
				{
					await chart.OnEventAsync(events[j - 1], obj);
				}
			}
		}
	}
}
