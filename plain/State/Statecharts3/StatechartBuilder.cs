using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State.Statecharts30
{
	class StatechartBuilder
	{
		public List<StateSettings> States { get; } = new List<StateSettings>();
		public List<TransitionSettings> Transitions { get; } = new List<TransitionSettings>();

		public StatechartBuilder Add(StateSettings state)
		{
			return this;
		}

		public StatechartBuilder Add(TransitionSettings state)
		{
			return this;
		}

		//public Statechart Create()
		//{
		//	return null;
		//}
	}

	public class StatechartSettings
	{
		public string Name { get; }
		public string InitialState { get; }
		public List<StateSettings> States { get; set; }

		public StatechartSettings(string name = null, string start = null)
		{
			Name = name;
			InitialState = start;
		}
	}

	public class StateSettings
	{
		public string Name { get; }
		public string Value { get; }
		public string Permission { get; }
		public string StateChartReference { get; }
		public string Condition { get; }
		public string OnEnter { get; }
		public string OnEntered { get; }
		public string OnExit { get; }
		public List<TransitionSettings> Transitions { get; set; }
		public StatechartSettings SubChart { get; set; }

		public StateSettings(string name, string value = null, string permission = null, string subchart = null, string condition = null, string onenter = null, string onentered = null, string onexit = null)
		{
			Name = name;
			Value = value ?? name;
			Permission = permission;
			Transitions = new List<TransitionSettings>();
			StateChartReference = subchart;
			Condition = condition;
			OnEnter = onenter;
			OnEntered = onentered;
			OnExit = onexit;
		}
	}

	public class TransitionSettings
	{
		public string Event { get; }
		public string Target { get; }
		public string Condition { get; }
		public string Action { get; }

		public TransitionSettings(string @event, string target, string condition = null, string action = null)
		{
			Event = @event;
			Target = target;
			Condition = condition;
			Action = action;
		}
	}

}
