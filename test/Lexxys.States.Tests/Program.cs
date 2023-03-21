using System;
using System.Collections.Generic;
using System.Linq;

using Lexxys;
using Lexxys.Configuration;
using Lexxys.Logging;
using Lexxys.States;
using Lexxys.States.Tests.Sample;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

const bool IsLegacyDefault = false;

Console.WriteLine($"Testing {Lxx.Framework}.");

var legacy = IsLegacyDefault ^ args.Contains(IsLegacyDefault ? "modern": "legacy", StringComparer.OrdinalIgnoreCase);

if (Lxx.Framework == "standard" && !legacy)
{
	Console.WriteLine("Switching to legacy");
	legacy = true;
}


if (legacy)
{
	Statics.AddServices(o => o
		.AddConfigService(c => c
			.AddConfiguration("sample-1.config.txt"))
		.AddOptions<StatechartConfig[]>("statecharts:statechart").Services
		.AddSingleton<IChartFactory, ChartFactoryInstance>()
		.AddLoggingService(o => o.AddConsole()));

	var r = new Runner(new LocalChartFactory(), null);

	await r.Run(args);
}
else
{
	var builder = Host.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration((_, c) => c
			.AddTextFile("sample-1.config.txt"))
		.ConfigureServices((c, s) => s
			.AddOptions<List<StatechartConfig>>().Bind(c.Configuration.GetSection("statecharts:statechart")).Services
			.AddLoggingService(o => o.AddConsole())
			.AddSingleton<IChartFactory, ChartFactoryInstance>()
			.AddHostedService<Runner>()
			.AddConfigService()
			.AddStatics());
	var host = builder.Build();

	await host.RunAsync();
}
