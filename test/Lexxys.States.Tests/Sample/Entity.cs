using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests.Sample;

public class Entity
{
	public int?[] State { get; }
	public string? Value1 { get; set; }
	public string? Value2 { get; set; }
	public string? Value3 { get; set; }

	public Entity(int width)
	{
		State = new int?[width];
	}

	public void SetStates(IReadOnlyList<int> items)
	{
		if (items.Count != State.Length)
			throw new ArgumentException($"The number of the items differs from the number of state slots", nameof(items));
		for (int i = 0; i < State.Length; ++i)
		{
			State[i] = Nil(items[i]);
		}

		static int? Nil(int value) => value == 0 ? null: value;
	}
}
