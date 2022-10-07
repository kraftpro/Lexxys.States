using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests.Sample;


internal class ChartFactory
{
	private static IValue<IReadOnlyList<StatechartConfig>> Configuration => __config ??= GetConfig();
	private static IValue<IReadOnlyList<StatechartConfig>>? __config;

	static IValue<IReadOnlyList<StatechartConfig>> GetConfig()
	{
		return Config.Current.GetCollection<StatechartConfig>("statecharts.statechart");
	}

	public static IEnumerable<string> ListStatecharts() => Configuration.Value.Select(o => o.Name);

	public static Statechart<T>? GetStatechart<T>(string name, bool compile, Func<string, IStateAction<T>?>? actionBuilder = null)
	{
		var config = Configuration.Value.FirstOrDefault(x => x.Name == name);
		return
			config == null ? null:
			compile ?
				config.GenerateLambda<T>(referenceResolver: ReferenceResolver).Invoke(null):
				config.Create<T>(actionBuilder: actionBuilder, referenceResolver: ReferenceResolver);

		static StatechartConfig ReferenceResolver(string name) => Configuration.Value.First(o => o.Name == name);
	}

}
