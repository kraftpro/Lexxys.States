using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State.Statecharts2
{
	public class State: State<int, object>
	{
		public State(int value, string name, string permission = null, StateCondition<int, object> condition = null, StateAction<int, object> onpassthrough = null, StateAction<int, object> onenter = null, StateAction<int, object> onexit = null)
			: base(value, name, permission, condition, onpassthrough, onenter, onexit)
		{
		}

		public State(State other): base(other)
		{
		}
	}

	//public abstract class StateCondition: StateCondition<int, object>
	//{
	//	private StateCondition()
	//	{
	//	}

	//	public static new StateCondition Create(string expression)
	//	{
	//		return StateCondition.Create<int, object>(expression);
	//	}
	//}


}
