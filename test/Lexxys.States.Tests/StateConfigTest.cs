using Lexxys.Configuration;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Linq;
using System.Threading.Tasks;

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
			Config.AddConfiguration(new ConfigurationLocator("string:[txt]" + LoginChartConfig("Login", "return true;", "return false;")), null);
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

		#region Helpers

		static StatechartConfig LoadLoginConfig()
		{
			var config = Config.GetValue<StatechartConfig>($"statecharts.Login");
			Assert.IsNotNull(config);
			return config;
		}

		static Statechart<Login> LoadLoginChart()
		{
			var config = LoadLoginConfig();
			var chart = config.Create<Login>(TokenFactory.Create("statecharts"), SimpleAction<Login>, o => SimpleCondition<Login>(x => x.Contains("true"), o));
			Assert.IsNotNull(chart);
			return chart;
		}

		public static string LoginChartConfig(string name, string authenticated, string notAuthenticated)
		{
			return LoginTextConfig
				.Replace("{Name}", name)
				.Replace("{Authenticated}", authenticated)
				.Replace("{NotAuthenticated}", notAuthenticated);
		}

		public static IStateAction<T> SimpleAction<T>(string value) => new WriteLineAction<T>(value);

		public static IStateCondition<T> SimpleCondition<T>(Func<string, bool> predicate, string value) => new WriteLineCondition<T>(predicate, value);

		class WriteLineCondition<T> : IStateCondition<T>
		{
			public WriteLineCondition(Func<string, bool> predicate, string value)
			{
				Predicate = predicate;
				Value = value;
			}

			public Func<string, bool> Predicate { get; }
			public string Value { get; }

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate()
				=> (a, b, c, d) =>
				{
					Write(Value, b, c, d);
					return Task.FromResult(Predicate(Value));
				};

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate()
				=> (a, b, c, d) =>
				{
					Write(Value, b, c, d);
					return Predicate(Value);
				};
		}

		class WriteLineAction<T> : IStateAction<T>
		{
			public WriteLineAction(string value)
			{
				Value = value;
			}

			public string Value { get; }

			public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate()
				=> (a, b, c, d) =>
				{
					Write(Value, b, c, d);
					return Task.CompletedTask;
				};

			public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate()
				=> (a, b, c, d) =>
				{
					Write(Value, b, c, d);
				};
		}

		private static void Write<T>(string value, Statechart<T> chart, State<T>? state, Transition<T>? transition)
		{
			Console.Write(chart.Name);
			if (state != null)
			{
				Console.Write('.');
				Console.Write(state.Name);
			}
			if (transition != null)
			{
				Console.Write('.');
				Console.Write(transition.Event.Name);
			}
			if (!String.IsNullOrEmpty(value))
			{
				Console.Write(": ");
				Console.Write(value);
			}
			Console.WriteLine();
		}

		const int LoginStateCount = 6;
		const int LoginTransitionCount = 6;

		static readonly string LoginTextConfig = @"
statecharts
	%{Name} name
	%{Name}(**)/state		name
	%{Name}(**)/state/transition	name destination
	{Name}
		:description	Sample {Name} statechart
		state Initialized
			:id				1
			:description	Initial login state
			:initial		true

			transition Name => NameEntered
			transition Password => PasswordEntered

		state NameEntered
			:id				2
			:description	Name has been entered

			transition Password => NameAndPasswordEntered
			transition Reset => Initialized

		state PasswordEntered
			:id				3
			:description	Password has been entered

			transition Name => NameAndPasswordEntered
			transition Reset => Initialized

		state NameAndPasswordEntered
			:id				4
			:description	Both name and password are entered

			transition Authenticate => Authenticated
				:guard {Authenticated}
			transition Authenticate => NotAuthenticated
				:guard {NotAuthenticated}
			transition Reset => Initialized

		state Authenticated
			:id				5
			:description	The user is authenticated

		state NotAuthenticated
			:id				6
			:description	Wrong user name and password combination
";
		#endregion
	}
}