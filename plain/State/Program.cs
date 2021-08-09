// using ExpressionEvaluator;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lexxys;
using Lexxys.Data;

namespace State
{
	using Statecharts;

	static class Program
	{
		static void Main(string[] args)
		{
			//Statecharts.StatechartBuilderTest.Go();
			//Reflect.Test.Go();
			TestDcNow();
			//TestParser();
			TestStatechart();
			//TestEval();
		}

		static void TestParser()
		{
			Eval.Test.Go();
		}

		static void TestStatechart()
		{
			Test.Console(StatechartFactory.Find<Expense>("statechartsCollection", "Expense"), new Expense());
		}

		static void TestDcNow()
		{
			do
			{
				using (var dc = new DataContext(new ConnectionStringInfo("Server=db1.qa1.fs.local;Database=CharityPlanner;user=sa;password=post+Office2")))
				{
					DateTime n0 = dc.Now;
					DateTime n1 = dc.GetValue<DateTime>("select sysdatetime()");
					DateTime n2 = dc.Now;
					DateTime n3 = DateTime.Now;
					Console.WriteLine($@"
Dc.Now      = {n0:HH':'mm':'ss'.'fffffff}
sysdatetime = {n1:HH':'mm':'ss'.'fffffff} diff = {n1 - n0:s'.'fffffff}
Dc.Now      = {n2:HH':'mm':'ss'.'fffffff} diff = {n2 - n1:s'.'fffffff}
DateTime.Now= {n3:HH':'mm':'ss'.'fffffff} diff = {n3 - n2:s'.'fffffff}");
				}
			} while (Console.ReadLine() != "x");
		}

		//private static void TestEval()
		//{
		//	var le = Expression.Parameter(typeof(int), "x");
		//	var re = Expression.Constant(100);
		//	var ex = Expression.Add(le, re);
		//	{
		//		var exp = new CompiledExpression { StringToParse = "2+2*2" };
		//		var f1 = exp.Compile();
		//		var result = f1();
		//		Console.WriteLine("result: {0}", result);
		//		exp.StringToParse = "S = \"A\"";
		//		var f2 = exp.ScopeCompile<AnObject>();
		//		var a = new AnObject { I = 10, S = "88" };
		//		result = f2.Invoke(a);
		//		Console.WriteLine("result: {0}", result);
		//	}
		//	{
		//		var exp = new Express { StringToParse = "x+2+2*2" };
		//		var f1 = exp.CompileFunction(new OrderedDictionary<string, Type> { { "x", typeof(int) } });
		//		var result = f1.DynamicInvoke(5);
		//		Console.WriteLine("result: {0}", result);
		//		exp.StringToParse = "S = \"A\"";
		//		var f2 = exp.ScopeCompileFunction(typeof(AnObject));
		//		var a = new AnObject { I = 10, S = "88" };
		//		result = f2.DynamicInvoke(a);
		//		Console.WriteLine("result: {0}", result);
		//	}
		//}
	}

	class AnObject
	{
		public int I { get; set; }
		public string S { get; set; }

		public void Act()
		{
			Console.WriteLine("Action");
		}
	}
}
