using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests
{
	[TestClass]
	public class StateConfigTest
	{
		public StateConfigTest()
		{

		}

		[TestInitialize]
		public void Initialize()
		{
			ChartConfig.RegisterConfiguration(ChartConfig.LoginChartConfigText("Login", "return true;", "return false;"));
			ChartConfig.RegisterConfiguration(ChartConfig.Login2ChartConfigText("Login2"));
		}

		[TestMethod]
		public void CanLoadConfigTest()
		{
			LoadLoginConfig();
		}

		[TestMethod]
		public void CanCreateLoginChartTest()
		{
			LoadLoginChart();
		}

		[TestMethod]
		public void CreatedLoginChartIsOperational()
		{
			var x = new Login(true);
			var chart = LoadLoginChart();
			chart.Start(x);
			Assert.IsTrue(chart.IsInProgress);
			Assert.AreEqual("Initialized", chart.CurrentState.Name);
			var events = chart.GetActiveEvents(x).ToIList();

			chart.OnEvent(events[0], x);

			var reset = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Reset");
			Assert.IsNotNull(reset);
			chart.OnEvent(reset, x);

			Assert.AreEqual("Initialized", chart.CurrentState.Name);
		}

		[TestMethod]
		public void CanGoToEndWithSimpleScript()
		{
			var x = new Login(true);
			var chart = LoadLoginChart();
			chart.Start(x);
			Assert.IsTrue(chart.IsInProgress);
			Assert.AreEqual("Initialized", chart.CurrentState.Name);

			TransitionEvent<Login>? evt;
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Name");
			Assert.IsNotNull(evt);
			chart.OnEvent(evt, x);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Password");
			Assert.IsNotNull(evt);
			chart.OnEvent(evt, x);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Authenticate");
			Assert.IsNotNull(evt);
			chart.OnEvent(evt, x);

			Assert.AreEqual("Authenticated", chart.CurrentState.Name);
		}

		[TestMethod]
		public void CanGoToEndWithCsScript()
		{
			var x = new Login(true);
			var chart = LoadLoginChartScScript();
			chart.Start(x);
			Assert.IsTrue(chart.IsInProgress);
			Assert.AreEqual("Initialized", chart.CurrentState.Name);

			TransitionEvent<Login>? evt;
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Name");
			Assert.IsNotNull(evt);
			chart.OnEvent(evt, x);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Password");
			Assert.IsNotNull(evt);
			chart.OnEvent(evt, x);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event.Name == "Authenticate");
			Assert.IsNotNull(evt);
			chart.OnEvent(evt, x);

			Assert.AreEqual("Authenticated", chart.CurrentState.Name);
		}

		static StatechartConfig LoadLoginConfig()
		{
			var config = ChartConfig.LoadLoginConfig();
			Assert.IsNotNull(config);
			return config;
		}

		static Statechart<Login> LoadLoginChart()
		{
			var chart = ChartConfig.LoadLoginChart();
			Assert.IsNotNull(chart);
			return chart;
		}

		static Statechart<Login> LoadLoginChartScScript()
		{
			var chart = ChartConfig.LoadLoginChartScScript();
			Assert.IsNotNull(chart);
			return chart;
		}
	}
}