using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Lexxys.Logging;

using Microsoft.Extensions.Logging;

namespace Lexxys.States.Con
{
	class Program
	{
		static void Main(string[] args)
		{
			//Console.WriteLine("Hello World!");
			FactoryTest();

			StateUsage.TestConsole();
		}

		private static Dictionary<string, object> __d = new Dictionary<string, object>();
		public static bool TryGetValue<T>(string value, [MaybeNullWhen(false)] out T result)
		{
			if (value.Length > 10)
			{
				Dictionary<string, T> d = new Dictionary<string, T>();
				bool success = d.TryGetValue(value, out var temp);
				result = success ? temp : default;
				return success;
			}
			else
			{
				if (__d.TryGetValue(value, out var t))
				{
					result = (T)t;
					return true;
				}
				result = default;
				bool success = __d.TryGetValue(value, out var temp);
				result = success ? (T)temp! : default;
				return success;
			}
		}

		private static void FactoryTest()
		{
			string a = "System.Nullable<Dictionary<System.String, System.Int32[]>**[][,][,,]>";
			var t = typeof(Tuple<byte, short?, Statechart<int>>);
			a = t.FullName!;
			a = "Tuple<byte, short?, Statechart<int>>";
			var r = Factory.TypeNameParser.ParseType(a);
			var s1 = r?.BaseName(true);
			var s2 = r?.BaseName(false);
			var r1 = r == null ? null : Factory.TypeNameParser.ParseType(s1);
			var s11 = r1?.BaseName(true);
			var s12 = r1?.BaseName(false);
			var r2 = r == null ? null : Factory.TypeNameParser.ParseType(s2);
			var s21 = r2?.BaseName(true);
			var s22 = r2?.BaseName(false);

			var t1 = r?.MakeType();
			var x = Factory.GetType("Logger<Program>");
			var y = Factory.GetType("Lexxys.Logger`1[[Lexxys.States.Con.Program]]");
			var z = Factory.GetType("Microsoft.Extensions.Logging.Logger<Program>");
		}

		private static void Write(FormattableString s)
		{
			Console.WriteLine($"ArgumentCount: {s.ArgumentCount}");
			Console.WriteLine($"Format:        {s.Format}");
			var args = s.GetArguments();
			for (int i = 0; i < args.Length; i++)
			{
				Console.WriteLine($"args[{i}]:       {args[i]}");
			}
		}
	}
}
