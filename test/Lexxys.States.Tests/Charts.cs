using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests
{
	static class Charts
	{
		public static Statechart<Login> CreateLoginChart()
			=> LoginPattern<Login>(TokenFactory.Create("statecharts"), o => o.Success());

		public static Statechart<Inside<Login>> CreateLogin2Chart()
			=> InsidePattern(TokenFactory.Create("statecharts"),
				LoginPattern<Inside<Login>>(TokenFactory.Create("statecharts"), o => o.Item?.Success() == true));

		public static Statechart<T> LoginPattern<T>(ITokenScope root, Func<T, bool> success)
		{
			var scope = TokenFactory.Create(root, typeof(T).GetTypeName());
			var s = scope.WithDomain("stt");
			var t = scope.WithDomain("trn");

			var initialized = new State<T>(s.Token(LoginStates.Initialized, "Initial login state"));
			var nameEntered = new State<T>(s.Token(LoginStates.NameEntered, "Name has been entered"));
			var passwordEntered = new State<T>(s.Token(LoginStates.PasswordEntered, "Password has been entered"));
			var nameAndPasswordEntered = new State<T>(s.Token(LoginStates.NameAndPasswordEntered, "Both name and password are entered"));
			var authenticated = new State<T>(s.Token(LoginStates.Authenticated, "The user is authenticated"));
			var notAuthenticated = new State<T>(s.Token(LoginStates.NotAuthenticated, "Wrong user name and password combination"));

			var start = new Transition<T>(State<T>.Empty, initialized);
			var enterName1 = new Transition<T>(initialized, nameEntered, t.Token("Name"));
			var enterParrword1 = new Transition<T>(initialized, passwordEntered, t.Token("Password"));

			var enterName2 = new Transition<T>(passwordEntered, nameAndPasswordEntered, t.Token("Name"));
			var enterParrword2 = new Transition<T>(nameEntered, nameAndPasswordEntered, t.Token("Password"));

			var authenticate1 = new Transition<T>(nameAndPasswordEntered, authenticated, t.Token("Authenticate"),
				guard: StateCondition.Create<T>(success));
			var authenticate2 = new Transition<T>(nameAndPasswordEntered, notAuthenticated, t.Token("Authenticate"),
				guard: StateCondition.Create<T>(o => !success(o)));

			var reset1 = new Transition<T>(nameAndPasswordEntered, initialized, t.Token("Reset"));
			var reset2 = new Transition<T>(nameEntered, initialized, t.Token("Reset"));
			var reset3 = new Transition<T>(passwordEntered, initialized, t.Token("Reset"));

			var loginChart = new Statechart<T>(root.Token(typeof(T).Name),
				new[] { initialized, nameEntered, passwordEntered, nameAndPasswordEntered, authenticated, notAuthenticated },
				new[] { start, enterName1, enterName2, enterParrword1, enterParrword2, authenticate1, authenticate2, reset1, reset2, reset3 });
			return loginChart;
		}

		public static Statechart<T> InsidePattern<T>(ITokenScope root, Statechart<T> chart)
		{
			var scope = TokenFactory.Create(root, typeof(T).GetTypeName());
			var s = scope.WithDomain("stt");
			var t = scope.WithDomain("trn");

			var s1 = new State<T>(s.Token(InsideState.Desition, "Inside initial state"));
			var s2 = new State<T>(s.Token(InsideState.Action, "Inside action state"), charts: new[] { chart });
			var s3 = new State<T>(s.Token(InsideState.Done, "Inside final state"));

			var t0 = new Transition<T>(State<T>.Empty, s1);
			var t1 = new Transition<T>(s1, s2, t.Token("Inside"));
			var t2 = new Transition<T>(s1, s3, t.Token("Over"));
			var t3 = new Transition<T>(s2, s3, t.Token("Done"));
			var t4 = new Transition<T>(s2, s3, guard: State<T>.AllFinishedCondition); // auto transition when the subchart is done

			var result = new Statechart<T>(root.Token(typeof(T).Name), new[] {s1, s2, s3 }, new[] { t0, t1, t2, t3, t4 });
			return result;
		}

		public static Statechart<T> HoldPattern<T>(ITokenScope root, Statechart<T> chart)
		{
			var scope = TokenFactory.Create(root, typeof(T).GetTypeName());
			var s = scope.WithDomain("stt");
			var t = scope.WithDomain("trn");

			var s1 = new State<T>(s.Token(HoldState.Running, "Running state"), charts: new[] { chart });
			var s2 = new State<T>(s.Token(HoldState.Hold, "Hold state"));
			var s3 = new State<T>(s.Token(HoldState.Continues, "Continues state"));

			var t0 = new Transition<T>(State<T>.Empty, s1);
			var t1 = new Transition<T>(s1, s2, t.Token("Hold"));
			var t2 = new Transition<T>(s2, s1, t.Token("Resume"));
			var t3 = new Transition<T>(s1, s3, guard: State<T>.AllFinishedCondition);

			var result = new Statechart<T>(root.Token(typeof(T).Name), new[] { s1, s2, s3 }, new[] { t0, t1, t2, t3 });
			return result;
		}

		public static ITokenScope GetTokenFactory<T>(this Statechart<T> statechart)
			=> TokenFactory.Create("statecharts", typeof(T).GetTypeName());
	}

	public enum HoldState
	{
		Running = 201,
		Hold,
		Continues,
	}

	public enum InsideState
	{
		Desition = 101,
		Action,
		Done
	}

	public enum LoginStates
	{
		Initialized = 1,
		NameEntered,
		PasswordEntered,
		NameAndPasswordEntered,
		Authenticated,
		NotAuthenticated // WrongNameOrPawword,
	}

	class Inside<T>
	{
		public T? Item { get; set; }
		public InsideState State { get; set; }
	}

	public class Login
	{
		private readonly bool _success;

		public Login(bool success)
		{
			_success = success;
		}

		public LoginStates State { get; set; }

		public bool Success() => _success;
	}

	public static class TypeExtensions
	{
		public static string GetTypeName(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (type.HasElementType)
				return GetTypeName(type.GetElementType() ?? typeof(void)) + (type.IsArray ? "[]": type.IsByRef ? "^": "*");
			if (!type.IsGenericType)
				return __builtinTypes.TryGetValue(type, out var s) ? s: type.Name;
			var text = new StringBuilder();
			text.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
			char c = '<';
			foreach (var item in type.GetGenericArguments())
			{
				text.Append(c).Append(GetTypeName(item));
				c = ',';
			}
			text.Append('>');
			return text.ToString();
		}

		private static Dictionary<Type, string> __builtinTypes = new ()
		{
			{ typeof(void), "void" },
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(char), "char" },
			{ typeof(short), "short" },
			{ typeof(ushort), "ushort" },
			{ typeof(int), "int" },
			{ typeof(uint), "uint" },
			{ typeof(long), "long" },
			{ typeof(ulong), "ulong" },
			{ typeof(float), "float" },
			{ typeof(double), "double" },
			{ typeof(decimal), "decimal" },
			{ typeof(string), "string" },
		};
	}
}
