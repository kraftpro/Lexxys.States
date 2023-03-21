using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Lexxys.States.Tests.Sample;


public interface IChartFactory
{
	public IEnumerable<string> ListStatecharts();
	public Statechart<T>? GetStatechart<T>(string name, bool compile, Func<string, IStateAction<T>?>? actionBuilder = null, Func<string, IStateCondition<T>?>? conditionBuilder = null);
}

public class LocalChartFactory: IChartFactory
{
	private static IValue<IReadOnlyList<StatechartConfig>> Configuration => __config ??= GetConfig();
	private static IValue<IReadOnlyList<StatechartConfig>>? __config;

	public IEnumerable<string> ListStatecharts() => Configuration.Value.Select(o => o.Name);

	public Statechart<T>? GetStatechart<T>(string name, bool compile, Func<string, IStateAction<T>?>? actionBuilder = null, Func<string, IStateCondition<T>?>? conditionBuilder = null)
	{
		var config = Configuration.Value.FirstOrDefault(x => x.Name == name);
		return
			config == null ? null :
			compile ?
				config.GenerateLambda<T>(referenceResolver: ReferenceResolver).Invoke(null) :
				config.Create<T>(actionBuilder: actionBuilder, referenceResolver: ReferenceResolver);

		static StatechartConfig ReferenceResolver(string name) => Configuration.Value.First(o => o.Name == name);
	}

	static IValue<IReadOnlyList<StatechartConfig>> GetConfig() => Config.Current.GetCollection<StatechartConfig>("statecharts.statechart");
}

public class ChartFactoryInstance: IChartFactory
{
	private readonly IOptions<List<StatechartConfig>> _config;

	public ChartFactoryInstance(IOptions<List<StatechartConfig>> config)
		=> _config = config;

	public IEnumerable<string> ListStatecharts()
		=> ((IReadOnlyCollection<StatechartConfig>)_config.Value ?? Array.Empty<StatechartConfig>()).Select(o => o.Name);

	public Statechart<T>? GetStatechart<T>(string name, bool compile, Func<string, IStateAction<T>?>? actionBuilder = null, Func<string, IStateCondition<T>?>? conditionBuilder = null)
	{
		var config = _config.Value?.FirstOrDefault(x => x.Name == name);
		return
			config == null ? null :
			compile ?
				config.GenerateLambda<T>(referenceResolver: ReferenceResolver).Invoke(null) :
				config.Create<T>(actionBuilder: actionBuilder, referenceResolver: ReferenceResolver);

		StatechartConfig ReferenceResolver(string name) => _config.Value!.First(o => o.Name == name);
	}
}