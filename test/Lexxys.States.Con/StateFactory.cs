using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Con
{
	class StateFactory
	{
		public static Statechart<Entity> Create(string name)
			=> name switch
			{
				"one" => CreateStatechart1(),
				_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
			};

		private static Statechart<Entity> CreateStatechart1()
		{
			return new Statechart<Entity>(TokenFactory.Default.Token("Entity-Chart"), Array.Empty<State<Entity>>(), Array.Empty<Transition<Entity>>());
		}
	}
}
