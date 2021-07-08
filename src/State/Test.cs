using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace State
{
	using Statecharts;

	class Test
	{

		public static void Console(Statechart<Expense> machine, Expense value)
		{
			machine.Start(value, null);

			for (; ;)
			{
				System.Console.WriteLine($": {String.Join(", ", machine.CurrentPath())}");
				var tt = machine.GetActiveTransitions(value, null);
				if (tt.Count == 0)
				{
					System.Console.WriteLine("Exiting");
					return;
				}
				int k = -1;
				while (k < 0 || k >= tt.Count)
				{
					for (int i = 0; i < tt.Count; ++i)
					{
						System.Console.WriteLine(" {0} - {1}", i + 1, tt[i].Event);
					}
					System.Console.Write(">");
					k = System.Console.ReadLine().AsInt32(0) - 1;
				}
				machine.OnEvent(tt[k].Event, value, null);
			}
		}
	}
}
