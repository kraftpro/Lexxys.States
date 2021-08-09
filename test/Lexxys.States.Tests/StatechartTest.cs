using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests
{
	[TestClass]
	public class StatechartTest
	{
		[TestMethod]
		public void CreateTest()
		{
			var chart = Charts.CreateLoginChart();
			Assert.IsNotNull(chart);
			Assert.IsFalse(chart.IsInProgress);
			Assert.IsFalse(chart.IsFinished);
		}

		[TestMethod]
		public void StartTest()
		{
			var x = new Login(true);
			var chart = Charts.CreateLoginChart();
			chart.Start(x);
			Assert.IsTrue(chart.IsInProgress);
		}

		[TestMethod]
		public void StartWithoutStartTest()
		{
			var x = new Login(true);
			var chart = Charts.CreateLoginChart();
			var start = chart.GetActiveEvents(x).ToList();
			Assert.AreEqual(1, start.Count);
			chart.OnTransitionEvent(start[0], x);
			Assert.IsTrue(chart.IsInProgress);
			Assert.AreEqual(nameof(LoginStates.Initialized), chart.CurrentState.Name);
		}

		[TestMethod]
		public void GetActiveEventsTest()
		{
			var x = new Login(true);
			var chart = Charts.CreateLoginChart();
			var events = chart.GetActiveEvents(x).ToList();
			Assert.AreEqual(1, events.Count);

			chart.Start(x);
			events = chart.GetActiveEvents(x).ToList();
			Assert.AreEqual(2, events.Count);
		}

		[TestMethod]
		public void MoveAlongNameTest()
		{
			var x = new Login(true);
			var chart = Charts.CreateLoginChart();
			var tf = chart.GetTokenFactory();
			var stf = tf.WithDomain("stt");
			var ttf = tf.WithDomain("trn");

			chart.Start(x);
			var events = chart.GetActiveEvents(x);

			var name = events.FirstOrDefault(o => o.Transition.Event == ttf.Token("Name"));
			Assert.IsNotNull(name);

			chart.OnTransitionEvent(name, x);
			Assert.AreEqual(chart.CurrentState, name.Transition.Destination);
			Assert.AreEqual(stf.Token(LoginStates.NameEntered), chart.CurrentState.Token);
		}

		[TestMethod]
		public void MoveToTheEndTest()
		{
			var x = new Login(true);
			var chart = Charts.CreateLoginChart();
			var tf = chart.GetTokenFactory();
			var stf = tf.WithDomain("stt");
			var ttf = tf.WithDomain("trn");

			chart.Start(x);

			var evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Name"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			Assert.AreEqual(chart.CurrentState, evt.Transition.Destination);
			Assert.AreEqual(stf.Token(LoginStates.NameEntered), chart.CurrentState.Token);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Password"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			Assert.AreEqual(chart.CurrentState, evt.Transition.Destination);
			Assert.AreEqual(stf.Token(LoginStates.NameAndPasswordEntered), chart.CurrentState.Token);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("authenticate"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			Assert.AreEqual(chart.CurrentState, evt.Transition.Destination);
			Assert.AreEqual(stf.Token(LoginStates.Authenticated), chart.CurrentState.Token);

			Assert.IsTrue(chart.IsFinished);
		}

		[TestMethod]
		public void ActivateSubchartTest()
		{
			var x = new Inside<Login> { Item = new Login(true) };
			var chart = Charts.CreateLogin2Chart();
			var tf = chart.GetTokenFactory();
			var stf = tf.WithDomain("stt");
			var ttf = tf.WithDomain("trn");

			chart.Start(x);

			var evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Inside"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			Assert.AreEqual(chart.CurrentState, evt.Transition.Destination);
			Assert.AreEqual(stf.Token(InsideState.Action), chart.CurrentState.Token);

			Assert.AreEqual(1, chart.CurrentState.Charts.Count);
			var chart2 = chart.CurrentState.Charts[0];
			Assert.IsFalse(chart2.CurrentState.IsEmpty);
			Assert.AreEqual(stf.Token(LoginStates.Initialized), chart2.CurrentState.Token);
		}

		[TestMethod]
		public void SubchartPassedTest()
		{
			var x = new Inside<Login> { Item = new Login(true) };
			var chart = Charts.CreateLogin2Chart();
			var tf = chart.GetTokenFactory();
			var stf = tf.WithDomain("stt");
			var ttf = tf.WithDomain("trn");

			chart.Start(x);

			var evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Inside"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);

			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Name"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Password"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Authenticate"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);

			Assert.AreEqual(stf.Token(InsideState.Done), chart.CurrentState.Token);
			Assert.IsTrue(chart.IsFinished);
		}

		[TestMethod]
		public void HoldUnholdTest()
		{
			var x = new Login(true);
			var chart = Charts.HoldPattern(TokenFactory.Create("statecharts"), Charts.CreateLoginChart());

			var tf = chart.GetTokenFactory();
			var stf = tf.WithDomain("stt");
			var ttf = tf.WithDomain("trn");

			chart.Start(x);

			var evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Resume"));
			Assert.IsNull(evt);
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Hold"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);

			var actions = chart.GetActiveEvents(x).ToList();
			Assert.AreEqual(1, actions.Count);
			evt = actions.FirstOrDefault(o => o.Transition.Event == ttf.Token("Resume"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);


			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Name"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Password"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);
			evt = chart.GetActiveEvents(x).FirstOrDefault(o => o.Transition.Event == ttf.Token("Authenticate"));
			Assert.IsNotNull(evt);
			chart.OnTransitionEvent(evt, x);

			Assert.AreEqual(stf.Token(HoldState.Continues), chart.CurrentState.Token);
			Assert.IsTrue(chart.IsFinished);
		}
	}
}
