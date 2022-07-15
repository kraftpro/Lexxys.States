using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests.Sample;


internal class ChartFactory
{
	private static IValue<IReadOnlyList<StatechartConfig>> Configuration = GetConfig();

	static IValue<IReadOnlyList<StatechartConfig>> GetConfig()
	{
		return Config.Current.GetCollection<StatechartConfig>("statecharts.statechart");
	}

	public static IEnumerable<string> ListStatecharts() => Configuration.Value.Select(o => o.Name);

	public static Statechart<T>? GetStatechart<T>(string name) => Configuration.Value.FirstOrDefault(x => x.Name == name)?.Create<T>();
}
