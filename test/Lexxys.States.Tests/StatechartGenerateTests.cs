using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
		public void GenerateSimpleCodeTest()
		{
			var chart = ChartConfig.LoadLoginConfig();
			var text = new StringBuilder();
			using (var writer = new StringWriter(text))
			{
				writer.WriteLine("\t\t// Generated SimpleCodeTest");
				chart.GenerateCode(writer, "Login", "CreateStatechart", "public", indent: "\t\t", nullable: true);
			}
			var code = text.ToString();
			Assert.IsTrue(code.Length > 0);
		}

		[TestMethod()]
		public void GenerateSubchartsCodeTest()
		{
			var chart = Config.Current.GetValue<StatechartConfig>($"statecharts.statechart[@name=Login2]").Value;
			var text = new StringBuilder();
			using (var writer = new StringWriter(text))
			{
				writer.WriteLine("\t\t// Generated SubchartsCodeTest");
				chart.GenerateCode(writer, "Login2", "CreateStatechart", "public", indent: "\t\t", nullable: true);
			}
			var code = text.ToString();
			Assert.IsTrue(code.Length > 0);
		}

		[TestMethod]
		public void CanCompileGeneratedCode()
		{
			var chart = Config.Current.GetValue<StatechartConfig>($"statecharts.statechart[@name=Login2]").Value;
			var text = new StringBuilder();
			using (var writer = new StringWriter(text))
			{
				writer.WriteLine("using Lexxys;");
				writer.WriteLine("using Lexxys.States;");
				writer.WriteLine("using Lexxys.States.Tests;");
				writer.WriteLine("#nullable enable");
				//writer.WriteLine("namespace Lexxys.States.Tests");
				//writer.WriteLine("{");
				writer.WriteLine("public static partial class StateChartFactory");
				writer.WriteLine("{");
				chart.GenerateCode(writer, "Login2", "CreateStatechart", "public", indent: "  ", tab: "  ", nullable: true);
				writer.WriteLine("}");
				//writer.WriteLine("}");
			}
			var code = text.ToString();

			var references = new List<MetadataReference>
				{
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Statechart<>).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Login).Assembly.Location),
				};
			var entry = Assembly.GetEntryAssembly();
			if (entry != null)
				references.AddRange(
					entry.GetReferencedAssemblies()
						.Select(o => MetadataReference.CreateFromFile(Assembly.Load(o).Location))
					);
			var location = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
			var netstandard = Path.Combine(location, "netstandard.dll");
			if (File.Exists(netstandard))
				references.Add(MetadataReference.CreateFromFile(netstandard));

			var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString() + ".dll")
				.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddReferences(references)
				.AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

			using var stream = new MemoryStream();
			var result = compilation.Emit(stream);

			Assert.IsTrue(result.Success);

			var asm = Assembly.Load(stream.ToArray());
			Assert.IsNotNull(asm);

			var types = asm.GetTypes();
			Assert.IsNotNull(types);
			Assert.IsTrue(types.Any());

			var factoryType = asm.GetType("StateChartFactory");
			Assert.IsNotNull(factoryType);

			var method = factoryType.GetMethod("CreateStatechartLogin2");
			Assert.IsNotNull(method);
			Assert.AreEqual("CreateStatechartLogin2", method.Name);
			Assert.AreEqual(typeof(Statechart<Login2>), method.ReturnType);
		}

		[TestMethod]
		public void CanGenerateLambda()
		{
			var config = Config.Current.GetValue<StatechartConfig>($"statecharts.statechart[@name=Login2]").Value;
			var constructor = config.GenerateLambda<Login2>();
			Assert.IsNotNull(constructor);

			var statechart = constructor(null);
			Assert.IsNotNull(statechart);
			Assert.AreEqual(typeof(Statechart<Login2>), statechart.GetType());
		}

		#region Generated code

		// Generated SimpleCodeTest
		public static Statechart<Login> CreateStatechartLogin(ITokenScope? root = null)
		{
			root ??= TokenScope.Create("statechart");
			var token = root.Token("Login");
			var s = root.Scope(token);
			var t = s.TransitionScope();

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
			root ??= TokenScope.Create("statechart");
			var token = root.Token("Login2");
			var s = root.Scope(token);
			var t = s.TransitionScope();
			Token tk;

			var s1 = new State<Login2>(s.Token(1, "Initialized", "Initial login state"));
			var s2 = new State<Login2>(s.Token(2, "NameEntered", "Name has been entered"));
			var s3 = new State<Login2>(s.Token(3, "PasswordEntered", "Password has been entered"));
			var s4 = new State<Login2>(s.Token(4, "NameAndPasswordEntered", "Both name and password are entered"));
			var s5 = new State<Login2>(tk = s.Token(5, "Authenticate", "Authenticate user"), new[] { CreateStatechartLogin2_TextVerification(s.Scope(tk)) });
			var s6 = new State<Login2>(s.Token(99, "Authenticated", "The user is authenticated"));
			var t1 = new Transition<Login2>(State<Login2>.Empty, s6);
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

		private static Statechart<Login2> CreateStatechartLogin2_TextVerification(ITokenScope? root = null)
		{
			root ??= TokenScope.Create("statechart");
			var token = root.Token("TextVerification");
			var s = root.Scope(token);
			var t = s.TransitionScope();

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

		#endregion
	}
}