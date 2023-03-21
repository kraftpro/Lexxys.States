using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Lexxys.States.Tests.Sample
{
	class Runner: IHostedService
	{
		const int LOAD = 98;
		const int UPDATE = 99;

		private readonly IChartFactory _factory;
		private readonly IHost? _host;

		public Runner(IChartFactory chartFactory, IHost? host)
		{
			_factory = chartFactory;
			_host = host;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Console.WriteLine("StartAsync");
			return RunAsync(new[] {"async"}, cancellationToken);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			Console.WriteLine("StopAsync");
			return Task.CompletedTask;
		}

		public async Task Run(string[] args, CancellationToken cancellationToken = default)
		{
			var (chart, sync) = GetStatechart(args);
			if (chart == null)
				return;

			if (sync)
				Run(new Entity(chart.Charts.Count), chart, cancellationToken);
			else
				await RunAsync(new Entity(chart.Charts.Count), chart, cancellationToken);
		}

		public Task RunAsync(string[] args, CancellationToken cancellationToken = default)
		{
			var (chart, _) = GetStatechart(args, true);
			if (chart == null)
				return Task.CompletedTask;
			return RunAsync(new Entity(chart.Charts.Count), chart, cancellationToken);
		}

		private (Statechart<Entity>? Chart, bool Sync) GetStatechart(string[] args, bool async = false)
		{
			async |= args.Any(o => o == "async");
			bool sync = args.Any(o => o == "sync");
			bool compile = args.Any(o => o == "compile");
			var name = GetChartName(_factory, args);
			if (name == null)
				return default;

			if (!compile)
			{
				Console.Write("Compile [N] ? ");
				var k = Console.ReadKey(true);
				if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					compile = true;
				Console.WriteLine(compile ? "Y" : "N");
			}
			if (!sync && !async)
			{
				Console.Write("Sync [N] ? ");
				var k = Console.ReadKey(true);
				if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					sync = true;
				Console.WriteLine(sync ? "Y" : "N");
			}
			var chart = GetStatechart(_factory, name, compile);
			return (chart, sync);
		}

		private static Statechart<Entity>? GetStatechart(IChartFactory factory, string? name, bool compile)
		{
			if (name == null)
				return null;
			var chart = factory.GetStatechart<Entity>(name, compile);
			if (chart == null)
			{
				Console.WriteLine("Cannot create a statechart");
				return null;
			}
			chart.OnUpdate += StateAction.Create<Entity>(
				(o, c, s, _) => {
					Console.WriteLine("run Update");
					o.SetStates(c.Charts.Select(s => s.CurrentState.Id).ToList());
				},
				(o, c, s, _) => {
					Console.WriteLine("run UpdateAsync");
					o.SetStates(c.Charts.Select(s => s.CurrentState.Id).ToList()); return Task.CompletedTask;
				});
			chart.OnLoad += StateAction.Create<Entity>(
				(o, c, s, _) => {
					Console.WriteLine("run Load");
					foreach (var x in c.Charts.Zip(o.State, (Chart, State) => (Chart, State)))
					{
						x.Chart.SetCurrentState(x.State);
					}
				},
				(o, c, s, _) => {
					Console.WriteLine("run LoadAsync");
					foreach (var x in c.Charts.Zip(o.State, (Chart, State) => (Chart, State)))
					{
						x.Chart.SetCurrentState(x.State);
					}
					return Task.CompletedTask;
				});

			// chart.CurrentState
			return chart;
		}

		static string? GetChartName(IChartFactory factory, string[] args)
		{
			var items = factory.ListStatecharts().ToList();
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

		void Run<T>(T obj, Statechart<T> chart, CancellationToken cancellationToken)
		{
			Console.WriteLine("Run");
			chart.Load(obj);
			if (!chart.IsStarted)
				chart.Start(obj);
			for (;;)
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				List<TransitionEvent<T>> events = new List<TransitionEvent<T>>();
				foreach (var item in chart.GetActiveEvents(obj))
				{
					events.Add(item);
					Console.WriteLine($"{events.Count}. {item.Transition.Event.Name} at {item.Chart.Token.ToString(false)}:{item.Transition.Source.Token.ToString(false)}");
				}
				Console.WriteLine($"{LOAD}. Load");
				Console.WriteLine($"{UPDATE}. Update");
				Console.Write("> ");
				if (cancellationToken.IsCancellationRequested)
					return;
				var s = Console.ReadLine();
				if (cancellationToken.IsCancellationRequested)
					return;

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

		async Task RunAsync<T>(T obj, Statechart<T> chart, CancellationToken cancellationToken)
		{
			await Console.Out.WriteLineAsync("RunAsync");
			await chart.LoadAsync(obj);
			if (!chart.IsStarted)
				await chart.StartAsync(obj);
			for (; ; )
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				List<TransitionEvent<T>> events = new List<TransitionEvent<T>>();
				await foreach (var item in chart.GetActiveEventsAsync(obj))
				{
					events.Add(item);
					Console.WriteLine($"{events.Count}. {item.Transition.Event.Name} at {item.Transition.Source.Token.ToString(false)} ({item.Chart.Token.ToString(false)})");
				}
				Console.WriteLine($"{LOAD}. Load");
				Console.WriteLine($"{UPDATE}. Update");
				Console.Write("> ");
				if (cancellationToken.IsCancellationRequested)
					return;
				var s = Console.ReadLine();
				if (cancellationToken.IsCancellationRequested)
					return;

				if (!(int.TryParse(s, out var j) && j >= 1 && (j <= events.Count || j == LOAD || j == UPDATE)))
				{
					if (String.IsNullOrWhiteSpace(s))
						break;
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
			if (_host != null)
				await _host.StopAsync();
		}
	}
}
