using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.States.Con
{
	public class Entity
	{
		public int Step1 { get; set; }
		public int? Step2 { get; set; }
		public void SetStep1(int value) => Step1 = value;
		public void SetStep2(int? value) => Step2 = value == 0 ? null: value;

		public (int ChartId, int? ChartState)[] GetState() => new [] { (1, Step1), (2, Step2) };
		public void SetState(IEnumerable<(int ChartId, int ChartState)> states)
		{
			SetStep1(states.FirstOrDefault(o => o.ChartId == 1).ChartState);
			SetStep2(states.FirstOrDefault(o => o.ChartId == 2).ChartState);
		}

		public void Update() { }
	}
}
