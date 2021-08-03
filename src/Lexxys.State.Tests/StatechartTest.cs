using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.State.Tests
{
	using States;

	using System.Threading.Tasks;

	[TestClass]
	public class StatechartTest
	{
		[TestMethod]
		public void CreateTest()
		{
			var chart = CreateLoginChart();
			Assert.IsNotNull(chart);
			Assert.IsFalse(chart.IsInProgress);
			Assert.IsFalse(chart.IsFinished);
		}

		[TestMethod]
		public void StartTest()
		{
			var chart = CreateLoginChart();
			var x = new Login(true);
			chart.Start(x);
			Assert.IsTrue(chart.IsInProgress);
		}

		[TestMethod]
		public void GetActiveActionsTest()
		{
			var chart = CreateLoginChart();
			var x = new Login(true);
			chart.Start(x);
			Assert.IsTrue(chart.IsInProgress);
			var actions = chart.GetActiveActions(x).ToIList();
			Assert.IsNotNull(actions);
			Assert.AreEqual(2, actions.Count);
		}

		private static Statechart<Login> CreateLoginChart()
		{
			var login = TokenFactory.Create("statecharts", "Login");
			var s = TokenFactory.Create(login, "State");
			var t = TokenFactory.Create(login, "Transition");

			var initialized = new State<Login>(s.Token(LoginStates.Initialized, "Initial login state"));
			var nameEntered = new State<Login>(s.Token(LoginStates.NameEntered, "Name has been entered"));
			var passwordEntered = new State<Login>(s.Token(LoginStates.PasswordEntered, "Password has been entered"));
			var nameAndPasswordEntered = new State<Login>(s.Token(LoginStates.NameAndPasswordEntered, "Both name and password are entered"));
			var authenticated = new State<Login>(s.Token(LoginStates.Authenticated, "The user is authenticated"));
			var notAuthenticated = new State<Login>(s.Token(LoginStates.NotAuthenticated, "Wrong user name and password combination"));

			var start = new Transition<Login>(State<Login>.Empty, initialized);
			var enterName1 = new Transition<Login>(initialized, nameEntered, t.Token("Name"));
			var enterParrword1 = new Transition<Login>(initialized, passwordEntered, t.Token("Password"));

			var enterName2 = new Transition<Login>(passwordEntered, nameAndPasswordEntered, t.Token("Name"));
			var enterParrword2 = new Transition<Login>(nameEntered, nameAndPasswordEntered, t.Token("Password"));

			var authenticate1 = new Transition<Login>(nameAndPasswordEntered, authenticated, t.Token("authenticate"),
				guard: StateCondition.Create<Login>(o => o.Success(), o => Task.FromResult(o.Success())));
			var authenticate2 = new Transition<Login>(nameAndPasswordEntered, notAuthenticated, t.Token("authenticate"),
				guard: StateCondition.Create<Login>(o => !o.Success(), o => Task.FromResult(!o.Success())));

			var reset1 = new Transition<Login>(nameAndPasswordEntered, initialized, t.Token("Reset"));
			var reset2 = new Transition<Login>(nameEntered, initialized, t.Token("Reset"));
			var reset3 = new Transition<Login>(passwordEntered, initialized, t.Token("Reset"));

			var loginChart = new Statechart<Login>(TokenFactory.Create("statecharts").Token("Login"),
				new[] { initialized, nameEntered, passwordEntered, nameAndPasswordEntered, authenticated, notAuthenticated },
				new[] { start, enterName1, enterName2, enterParrword1, enterParrword2, authenticate1, authenticate2, reset1, reset2, reset3 });
			return loginChart;
		}
	}
}
