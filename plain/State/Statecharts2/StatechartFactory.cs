using System.Collections.Generic;

using Lexxys;
using System.Collections.Concurrent;

namespace State.Statecharts2
{
	public static class StatechartFactory
	{
		//private static readonly ConcurrentDictionary<string, Statechart> _charts = new ConcurrentDictionary<string, Statechart>();

		//public static void Append(IEnumerable<Statechart> collection)
		//{
		//	if (collection == null)
		//		return;
		//	foreach (var item in collection)
		//	{
		//		if (item != null)
		//			_charts[item.Name] = item;
		//	}
		//}

		//public static void Append(string configurationNode)
		//{
		//	if (configurationNode == null)
		//		return;
		//	Append(Config.GetList<Statechart>(configurationNode));
		//}

		//public static Statechart Find(string name)
		//{
		//	Statechart chart;
		//	_charts.TryGetValue(name, out chart);
		//	return chart;
		//}
	}
}