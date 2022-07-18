using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests.Sample;

public class Entity
{
	public int?[] State { get; }

	public Entity(int width)
	{
		State = new int?[width];
	}

	public void SetStates(IReadOnlyList<int?> items)
	{
		if (items.Count != State.Length)
			throw new ArgumentException($"The number of the items differs from the number of state slots", nameof(items));
		for (int i = 0; i < State.Length; ++i)
		{
			State[i] = items[i];
		}
	}
}
