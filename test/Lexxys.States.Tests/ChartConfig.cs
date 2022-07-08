using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Configuration;

namespace Lexxys.States.Tests
{
	static class ChartConfig
	{
		public static StatechartConfig LoadLoginConfig()
		{
			var config = Config.Current.GetValue<StatechartConfig>($"statecharts.Login");
			return config.Value;
		}

		public static Statechart<Login> LoadLoginChart()
		{
			var config = LoadLoginConfig();
			return config.Create<Login>(TokenScope.Create("statechart"), SimpleAction<Login>, o => SimpleCondition<Login>(x => x.Contains("true"), o));
		}

		public static Statechart<Login> LoadLoginChartScScript()
		{
			var config = LoadLoginConfig();
			return config.Create<Login>(TokenScope.Create("statechart"));
		}

		public static void RegisterConfiguration(string value)
		{
			Registered.GetOrAdd(value, o =>
			{
				Config.AddConfiguration(new Uri("string:[txt]/?" + o), null);
				return true;
			});
		}
		private static readonly ConcurrentDictionary<string, bool> Registered = new ConcurrentDictionary<string, bool>();

		public static string LoginChartConfigText(string name, string authenticated, string notAuthenticated)
		{
			return LoginTextConfig
				.Replace("{Name}", name)
				.Replace("{Authenticated}", authenticated)
				.Replace("{NotAuthenticated}", notAuthenticated);
		}

		public static string Login2ChartConfigText(string name)
		{
			return Login2TextConfig
				.Replace("{Name}", name);
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
		:initialState	Initialized
		state Initialized
			:id				1
			:description	Initial login state

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

		static readonly string Login2TextConfig = @"
statecharts
	%{Name} name
	%{Name}(**)/state				name
	%{Name}(**)/state/statechart	name
	%{Name}(**)/state/transition	name destination
	{Name}
		:description	Sample {Name} statechart
		state Initialized
			:id				1
			:description	Initial login state

			transition Name => NameEntered
			transition Password => PasswordEntered

		state NameEntered
			:id				2
			:description	Name has been entered

			transition	Password => NameAndPasswordEntered
			transition	ClearName => Initialized
			transition	Reset => Initialized

		state PasswordEntered
			:id				3
			:description	Password has been entered

			transition		Name => NameAndPasswordEntered
			transition		ClearPassword => Initialized
			transition		Reset => Initialized

		state NameAndPasswordEntered
			:id				4
			:description	Both name and password are entered

			transition		ClearName => PasswordEntered
			transition		ClearPassword => NameEntered
			transition		Reset => Initialized
			transition		Authenticate => Authenticate

		state Authenticate
			:id				5
			:description	Authenticate user
			:onEnter		obj.VerifyCredentials();

			transition		=> Initialized
				:guard		!obj.CredentialsVerified
			transition		Reset => Initialized

			statechart TextVerification

				state Text
					:stateEnter		obj.SendToken();

					transition		TextEntered => VerifyText

				state VerifyText
					:stateEnter		obj.VerifyToken();

					transition		=> Authenticated
						:guard		obj.TokenVerified
					transition		=> Text
						:guard		!obj.TokenVerified

				state Authenticated

		state Authenticated
			:id				99
			:description	The user is authenticated
";
	}
}
