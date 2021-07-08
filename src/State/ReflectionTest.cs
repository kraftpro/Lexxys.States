using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Reflect
{
	using Action = System.Action;
	public static class Test
	{
		public static int BatchSize = 10000;
		public static int TestsCount = 10000;

		public static void Go()
		{
			ConstructAndCallFunction().Invoke(new TestClass());
			TestResult[] rr = new[]
			{
				Run(nameof(DirectGetField), DirectGetField()),
				Run(nameof(DirectGetProperty), DirectGetProperty()),
				Run(nameof(DirectCallFunction), DirectCallFunction()),
				Run(nameof(ConstructAndCallFunction), ConstructAndCallFunction()),
			};
			foreach (var r in rr)
			{
				Console.WriteLine(r);
			}
			TestResult[] rr2 = new[]
			{
				Run(nameof(DirectGetField), DirectGetField()),
				Run(nameof(DirectGetProperty), DirectGetProperty()),
				Run(nameof(DirectCallFunction), DirectCallFunction()),
				Run(nameof(ConstructAndCallFunction), ConstructAndCallFunction()),
			};
			foreach (var r in rr2)
			{
				Console.WriteLine(r);
			}
			Console.ReadLine();
		}

		static Func<TestClass, int> DirectGetField()
		{
			return o => o.IntField;
		}

		static Func<TestClass, int> DirectGetProperty()
		{
			return o => o.IntProperty;
		}

		static Func<TestClass, int> DirectCallFunction()
		{
			return o => o.IntFunction();
		}

		static Func<TestClass, int> ConstructAndCallFunction()
		{
			var m = typeof(TestClass).GetMethod("IntFunction");
			var p = new object[0];
			return o =>
			{
				Factory.Invoke(o, m, p);
				return 1;
			};
		}

		static TestResult Run(string name, Func<TestClass, int> action)
		{
			var values = new double[TestsCount];
			for (int i = 0; i < TestsCount; ++i)
			{
				TestClass x = new TestClass();
				var t = WatchTimer.Start();
				for (int j = 0; j < BatchSize; ++j)
				{
					action(x);
				}
				values[i] = WatchTimer.ToSeconds(WatchTimer.Query(t));
			}
			return new TestResult(name, values);
		}

		public class TestResult
		{
			public string Name { get; }
			public TestResultValue Value { get; }

			public TestResult(string name, TestResultValue value)
			{
				Name = name;
				Value = value;
			}

			public TestResult(string name, IReadOnlyCollection<double> value)
			{
				Name = name;
				Value = new TestResultValue(value);
			}

			public override string ToString()
			{
				return $"{Name.PadRight(40)} {Value.Mean * 1000:0.000}ms (±{Value.StdErr * 1000:0.000}ms) of {Value.Count}";
			}
		}

		public class TestResultValue
		{
			public int Count { get; }
			public double Mean { get; }
			public double StdErr { get; }

			public TestResultValue(IReadOnlyCollection<double> values)
			{
				Count = values.Count;
				Mean = values.Sum() / Count;
				StdErr = values.Sum(o => Math.Abs(o - Mean)) / Count;
			}
		}

		public class TestClass
		{
			public int IntField;
			public int IntProperty { get; set; }

			public int IntFunction()
			{
				return 1;
			}

			public int IntAction(int value)
			{
				return value + 1;
			}
		}
	}
}
