using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests
{
	[TestClass()]
	public class StatechartGenerateTests
	{
		[TestInitialize]
		public void Initialize()
		{
			ChartConfig.RegisterConfiguration(ChartConfig.LoginChartConfigText("Login", "return true;", "return false;"));
			ChartConfig.RegisterConfiguration(ChartConfig.Login2ChartConfigText("Login2"));
		}

		[TestMethod()]
		public void SimpleCodeTest()
		{
			var chart = ChartConfig.LoadLoginConfig();
			var text = new StringBuilder();
			using (var writer = new StringWriter(text))
			{
				writer.WriteLine("\t\t// Generated SipleCodeTest");
				chart.GenerateCode(writer, "Login", "CreateStatechart", "public", true);
			}
			var code = text.ToString();
			Assert.IsTrue(code.Length > 0);
		}

		[TestMethod()]
		public void SubchatsCodeTest()
		{
			var chart = Config.GetValue<StatechartConfig>($"statecharts.Login2");
			var text = new StringBuilder();
			using (var writer = new StringWriter(text))
			{
				writer.WriteLine("\t\t// Generated SubchatsCodeTest");
				chart.GenerateCode(writer, "Login2", "CreateStatechart", "public", true);
			}
			var code = text.ToString();
			Assert.IsTrue(code.Length > 0);
		}

		// Generated SipleCodeTest
		public static Statechart<Login> CreateStatechartLogin(ITokenScope root)
		{
			var token = root.Token("Login");
			var s = root.WithDomain(token);
			var t = s.WithTransitionDomain();

			var s1 = new State<Login>(s.Token(1, "Initialized", "Initial login state"));
			var s2 = new State<Login>(s.Token(2, "NameEntered", "Name has been entered"));
			var s3 = new State<Login>(s.Token(3, "PasswordEntered", "Password has been entered"));
			var s4 = new State<Login>(s.Token(4, "NameAndPasswordEntered", "Both name and password are entered"));
			var s5 = new State<Login>(s.Token(5, "Authenticated", "The user is authenticated"));
			var s6 = new State<Login>(s.Token(6, "NotAuthenticated", "Wrong user name and password combination"));
			var t1 = new Transition<Login>(State<Login>.Empty, s1);
			var t2 = new Transition<Login>(s1, s2, t.Token("Name"));
			var t3 = new Transition<Login>(s1, s3, t.Token("Password"));
			var t4 = new Transition<Login>(s2, s4, t.Token("Password"));
			var t5 = new Transition<Login>(s2, s1, t.Token("Reset"));
			var t6 = new Transition<Login>(s3, s4, t.Token("Name"));
			var t7 = new Transition<Login>(s3, s1, t.Token("Reset"));
			var t8 = new Transition<Login>(s4, s5, t.Token("Authenticate"), false, null, StateCondition.Create<Login>((obj, chart, state, transition) => true));
			var t9 = new Transition<Login>(s4, s6, t.Token("Authenticate"), false, null, StateCondition.Create<Login>((obj, chart, state, transition) => false));
			var t10 = new Transition<Login>(s4, s1, t.Token("Reset"));
			var statechart = new Statechart<Login>(token, new[] { s1, s2, s3, s4, s5, s6 }, new[] { t1, t2, t3, t4, t5, t6, t7, t8, t9, t10 });
			return statechart;
		}

		// Generated SubchatsCodeTest
		public static Statechart<Login2> CreateStatechartLogin2(ITokenScope? root = null)
		{
			if (root == null)
				root = TokenFactory.Create("statechart");
			var token = root.Token("Login2");
			var s = root.WithDomain(token);
			var t = s.WithTransitionDomain();

			var s1 = new State<Login2>(s.Token(1, "Initialized", "Initial login state"));
			var s2 = new State<Login2>(s.Token(2, "NameEntered", "Name has been entered"));
			var s3 = new State<Login2>(s.Token(3, "PasswordEntered", "Password has been entered"));
			var s4 = new State<Login2>(s.Token(4, "NameAndPasswordEntered", "Both name and password are entered"));
			var s5 = new State<Login2>(s.Token(5, "Authenticate", "Authenticate user"), new[] { CreateStatechartLogin2_TextVerification(s) });
			var s6 = new State<Login2>(s.Token(99, "Authenticated", "The user is authenticated"));
			var t1 = new Transition<Login2>(State<Login2>.Empty, s1);
			var t2 = new Transition<Login2>(s1, s2, t.Token("Name"));
			var t3 = new Transition<Login2>(s1, s3, t.Token("Password"));
			var t4 = new Transition<Login2>(s2, s4, t.Token("Password"));
			var t5 = new Transition<Login2>(s2, s1, t.Token("ClearName"));
			var t6 = new Transition<Login2>(s2, s1, t.Token("Reset"));
			var t7 = new Transition<Login2>(s3, s4, t.Token("Name"));
			var t8 = new Transition<Login2>(s3, s1, t.Token("ClearPassword"));
			var t9 = new Transition<Login2>(s3, s1, t.Token("Reset"));
			var t10 = new Transition<Login2>(s4, s3, t.Token("ClearName"));
			var t11 = new Transition<Login2>(s4, s2, t.Token("ClearPassword"));
			var t12 = new Transition<Login2>(s4, s1, t.Token("Reset"));
			var t13 = new Transition<Login2>(s4, s5, t.Token("Authenticate"));
			var t14 = new Transition<Login2>(s5, s1, null, false, null, StateCondition.Create<Login2>((obj, chart, state, transition) => !obj.CredentialsVerified));
			var t15 = new Transition<Login2>(s5, s1, t.Token("Reset"));
			var statechart = new Statechart<Login2>(token, new[] { s1, s2, s3, s4, s5, s6 }, new[] { t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15 });
			return statechart;
		}

		private static Statechart<Login2> CreateStatechartLogin2_TextVerification(ITokenScope root)
		{
			var token = root.Token("TextVerification");
			var s = root.WithDomain(token);
			var t = s.WithTransitionDomain();

			var s1 = new State<Login2>(s.Token("Text"));
			s1.StateEnter += StateAction.Create<Login2>((obj, chart, state, transition) => obj.SendToken());
			var s2 = new State<Login2>(s.Token("VerifyText"));
			s2.StateEnter += StateAction.Create<Login2>((obj, chart, state, transition) => obj.VerifyToken());
			var s3 = new State<Login2>(s.Token("Authenticated"));
			var t1 = new Transition<Login2>(State<Login2>.Empty, s1);
			var t2 = new Transition<Login2>(s1, s2, t.Token("TextEntered"));
			var t3 = new Transition<Login2>(s2, s3, null, false, null, StateCondition.Create<Login2>((obj, chart, state, transition) => obj.TokenVerified));
			var t4 = new Transition<Login2>(s2, s1, null, false, null, StateCondition.Create<Login2>((obj, chart, state, transition) => !obj.TokenVerified));
			var statechart = new Statechart<Login2>(token, new[] { s1, s2, s3 }, new[] { t1, t2, t3, t4 });
			return statechart;
		}
	}
}