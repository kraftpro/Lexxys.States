using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.States;

public class StatesChain<T>: IReadOnlyList<StatesChainItem<T>>
{
	private readonly List<StatesChainItem<T>> _items;

	public StatesChain()
	{
		_items = new List<StatesChainItem<T>>();
	}

	public StatesChainItem<T> this[int index] => _items[index];

	public int Count => _items.Count;

	internal void Add(StatesChainItem<T> item)
	{
		if (item.Parent != null && !_items.Contains(item.Parent))
			throw new ArgumentException("Provided tree item is not compatible with the tree.");
		_items.Add(item);
	}

	public IEnumerable<StatesChainItem<T>> GetLeafs() => _items.Where(o => !_items.Any(p => p.Parent == o));

	public IEnumerator<StatesChainItem<T>> GetEnumerator() => _items.GetEnumerator();

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

public record StatesChainItem<T>(StatesChainItem<T>? Parent, Statechart<T> Chart, State<T> State)
{
	public string GetPath(string? delimiter = null, bool includeChartName = false)
	{
		var text = new StringBuilder();
		var item = this;
		delimiter ??= " > ";
		do
		{
			if (text.Length != 0)
				text.Append(delimiter);
			if (includeChartName)
				text.Append(item.Chart.Name).Append(':');
			text.Append(item.State.Name);
			item = item.Parent;
		} while (item != null);
		return text.ToString();
	}
}

