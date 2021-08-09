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
		public void SetStep2(int? value) => Step2 = value;

		public (int ChartId, int? ChartState)[] GetState() => new [] { (1, Step1), (2, Step2) };
		public void SetState(IEnumerable<(int ChartId, int? ChartState)> states)
		{
			Step1 = states.FirstOrDefault(o => o.ChartId == 1).ChartState ?? 0;
			Step2 = states.FirstOrDefault(o => o.ChartId == 2).ChartState;
		}

		public void Update() { }
	}

	//public readonly struct StatePath<T>
	//{
	//	public IReadOnlyList<StatePathItem<T>> Items { get; }

	//	public StatePath(IEnumerable<StatePathItem<T>> items)
	//	{
	//		Items = ReadOnly.WrapCopy(items ?? throw new ArgumentNullException(nameof(items)));
	//	}

	//	public StatePath(StatePathItem<T> item, StatePath<T> path)
	//	{
	//		var items = new StatePathItem<T>[path.Items.Count + 1];
	//		items[0] = item;
	//		for (int i = 1; i < items.Length; i++)
	//		{
	//			items[i] = path.Items[i - 1];
	//		}
	//		Items = items;
	//	}

	//	public override string ToString() => String.Join(" > ", Items);

	//	public static StatePath<T> Concat(StatePath<T> left, StatePath<T> right) => new StatePath<T>(left.Items.Concat(right.Items));
	//}

	//public readonly struct StatePathItem<T>
	//{
	//	public Statechart<T> Statechart { get; }
	//	public State<T> State { get; }

	//	public StatePathItem(Statechart<T> statechart, State<T> state)
	//	{
	//		Statechart = statechart ?? throw new ArgumentNullException(nameof(statechart));
	//		State = state ?? throw new ArgumentNullException(nameof(state));
	//	}

	//	public override string ToString() => $"[{Statechart.Name}]:{State.Name}";
	//}
}
