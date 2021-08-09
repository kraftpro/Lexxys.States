using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Xml;

using Lexxys;

namespace State.Statecharts
{
	public static class StatechartFactory
	{
		//private static readonly ConcurrentDictionary<string, object> _charts = new ConcurrentDictionary<string, object>();

		//public static void Append(IEnumerable collection)
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
		//		throw new ArgumentNullException(nameof(configurationNode));
		//	var items = Config.GetList<Statechart>(configurationNode);
		//	if (items == null)
		//		throw new ArgumentException("Statechart configuration is not found.", nameof(configurationNode));

		//	Append(items);
		//}

		public static Statechart<T> Find<T>(string configurationNode, string name)
		{
			var rootNode = Config.GetValue<Lexxys.Xml.XmlLiteNode>(configurationNode);
			var chartNode = rootNode.Element("name", StringComparer.OrdinalIgnoreCase);

			Statechart<T>.StatechartSettings settings = Lexxys.Xml.XmlTools.GetValue<Statechart<T>.StatechartSettings>(chartNode, null);
			return settings == null ? null : new Statechart<T>(settings);

			//return chartNode == null ? null : chartNode.AsValue<Statechart<T>>(null);
		}
	}
}