using Lexxys;
using Lexxys.Logging;
using Lexxys.States.Tests.Sample;

using Microsoft.Extensions.DependencyInjection;

Statics.Register(o => o
	.AddConfigService()
	.AddLoggingService(o => o.AddConsole()));

await Runner.Go(args);
