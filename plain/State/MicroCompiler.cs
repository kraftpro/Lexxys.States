using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Lexxys;
using Lexxys.Tokenizer;
using Lexxys.Xml;

namespace State
{
	public class MicroCompiler
	{
/*
			private string _expression;
			private Delegate _function;
			private object _value;
			private Type _type;
			private ISet<string> _externals;
			private bool _oprimization;
			private bool _hasValue;

			private MicroCompiler()
			{
				_externals = ReadOnly.EmptySet<string>.Items;
			}

			public MicroCompiler(object value)
			{
				_value = value;
				_hasValue = true;
				_externals = ReadOnly.EmptySet<string>.Items;

				if (value == null)
				{
					_type = typeof(object);
				}
				else
				{
					_type = value.GetType();
					_expression = value.ToString();
				}
			}

			public MicroCompiler(string expression, Type type, ObjectContext externals = null, bool optimize = false)
			{
				_type = type ?? typeof(object);
				_expression = expression;
				_oprimization = optimize;

				if (expression == null)
				{
					_externals = ReadOnly.EmptySet<string>.Items;
				}
				else
				{
					try
					{
						Compile(expression, externals);
						if (_externals.Count == 0)
							SetValue();
					}
					catch (Exception e)
					{
						throw e.Add("expression", expression).Add("type", type);
					}
				}
			}

			public bool IsEmpty
			{
				get { return _expression == null; }
			}

			public Delegate Function
			{
				get { return _function; }
			}

			public object Value
			{
				get { return _hasValue || _function == null ? _value: _function.DynamicInvoke(); }
			}

			public bool HasValue
			{
				get { return _hasValue; }
			}

			public void SetValue()
			{
				_value = _function == null ? null: _function.DynamicInvoke();
				_hasValue = true;
			}

			public void ResetValue()
			{
				_hasValue = false;
			}

			public string Display
			{
				get { return _expression; }
			}

			public bool HasExternal()
			{
				return _externals.Count > 0;
			}

			public bool HasExternal(string name)
			{
				return _externals.Contains(name);
			}

			public bool HasExternal(string[] name)
			{
				return name.Any(o => _externals.Contains(o));
			}

			public static MicroCompiler DefaultValue(Type type)
			{
				return type.IsClass ? Empty: __defaults.GetOrAdd(type, x => new MicroCompiler(Factory.Construct(x)));
			}
			private static readonly ConcurrentDictionary<Type, MicroCompiler> __defaults = new ConcurrentDictionary<Type, MicroCompiler>();

			public static ObjectContext Externals()
			{
				return new ObjectContext();
			}

			public static ObjectContext Externals(string configurationNode)
			{
				IList<XmlLiteNode> nodes = Config.GetList<XmlLiteNode>(configurationNode, true);
				ObjectContext methods = new ObjectContext();
				foreach (var node in nodes)
				{
					foreach (var item in node.Descendant)
					{
						string method = item["method"];
						if (method != null)
							methods.Add(method, item["name"]);
					}
				}
				return methods;
			}

			public static ObjectContext Externals(params KeyValuePair<string, MethodInfo>[] methods)
			{
				if (methods == null || methods.Length == 0)
					throw new ArgumentNullException("methods");

				return new ObjectContext(methods);
			}

			public static KeyValuePair<string, MethodInfo> Method(string name, Delegate function)
			{
				if (name == null || name.Length == 0)
					throw new ArgumentNullException("name");
				if (function == null)
					throw new ArgumentNullException("function");
				return new KeyValuePair<string, MethodInfo>(name, function.Method);
			}

			public static KeyValuePair<string, MethodInfo> Method(Delegate function)
			{
				if (function == null)
					throw new ArgumentNullException("function");

				return Method(function.Method);
			}

			public static KeyValuePair<string, MethodInfo> Method(string name, MethodInfo method)
			{
				if (name == null || name.Length == 0)
					throw new ArgumentNullException("name");
				return new KeyValuePair<string, MethodInfo>(name, method);
			}

			public static KeyValuePair<string, MethodInfo> Method(MethodInfo method)
			{
				if (method == null)
					throw new ArgumentNullException("method");

				string name = method.Name;
				if (name.StartsWith("get_", StringComparison.Ordinal))
					name = name.Substring(4);
				return new KeyValuePair<string, MethodInfo>(name, method);
			}
*/

		private Type _type;
		private bool _oprimization;
		private string _expression;
		private ISet<string> _externals;
		private readonly Delegate _delegate;

		public MicroCompiler(string expression, Type type, ObjectContext context = null, bool optimize = false)
		{
			_type = type ?? typeof(object);
			_expression = expression;
			_oprimization = optimize;

			if (expression == null)
			{
				_externals = ReadOnly.EmptySet<string>();
			}
			else
			{
				try
				{
					_delegate = Compile(context);
				}
				catch (Exception e)
				{
					throw e.Add("expression", expression).Add("type", type);
				}
			}
		}

		private static bool TestCastDirection(Type source, Type target)
		{
			if (target.IsAssignableFrom(source))
				return true;

			TypeCode tt = Type.GetTypeCode(target);
			if (tt < TypeCode.SByte && tt > TypeCode.Decimal)
				return false;
			TypeCode st = Type.GetTypeCode(source);
			if (st < TypeCode.SByte && st > TypeCode.Decimal)
				return false;
			return (tt == TypeCode.Double || (st != TypeCode.Double && tt >= st));
		}

		private static void Cast(ref Expression left, ref Expression right)
		{
			if (TestCastDirection(right.Type, left.Type))
				right = Cast(right, left.Type);
			else
				left = Cast(left, right.Type);
		}

		private static Expression Cast(Expression expression, Type type)
		{
			if (expression.Type == type)
				return expression;
			ConstantExpression ce = expression as ConstantExpression;
			if (ce != null)
				return Expression.Constant(Convert.ChangeType(ce.Value, type), type);
			return Expression.Convert(expression, type);
		}

		#region Parser

		private static readonly LexicalTokenType OPERATION = new LexicalTokenType(21);
		private const int PLUS		= 1;
		private const int MINUS		= 2;
		private const int MULT		= 3;
		private const int DIV		= 4;
		private const int MOD		= 5;

		private const int AND		= 11;
		private const int OR		= 12;
		private const int XOR		= 13;
		private const int ANDAND	= 14;
		private const int OROR		= 15;

		private static readonly LexicalTokenType COMPARE = new LexicalTokenType(22);
		private const int EQ		= 21;
		private const int LT		= 22;
		private const int GT		= 23;
		private const int NE		= 24;
		private const int LE		= 25;
		private const int GE		= 26;
	
		private static readonly LexicalTokenType ASSIGN = new LexicalTokenType(23);
		private const int EQUAL		= 30;
		private const int PLUSEQ	= 31;
		private const int MINUSEQ	= 32;
		private const int MULEQ		= 33;
		private const int DIVEQ		= 34;
		private const int MODEQ		= 35;

		private static readonly LexicalTokenType BRACES = new LexicalTokenType(24);
		private const int BRCBEG	= 41;
		private const int BRCEND	= 42;
		private const int IDXBEG	= 43;
		private const int IDXEND	= 44;
		private const int BEG		= 45;
		private const int END		= 46;

		private const int COMMA		= 45;

		private Delegate Compile(ObjectContext context)
		{
			var scanner = new TokenScanner5(new CharStream(_expression),
				new CppCommentsTokenRule(LexicalTokenType.IGNORE),
				new SequenceTokenRule()
					.Add(PLUS, "+")
					.Add(MINUS, "-")
					.Add(MULT, "*")
					.Add(DIV, "/")
					.Add(MOD, "%")
					.Add(AND, "&")
					.Add(OR, "|")
					.Add(XOR, "^")
					.Add(ANDAND, "&&")
					.Add(OROR, "||")
					.Add(EQ, "==")
					.Add(LT, "<")
					.Add(GT, ">")
					.Add(NE, "!=")
					.Add(LE, "<=")
					.Add(GE, ">=")
					.Add(EQUAL, "=")
					.Add(PLUSEQ, "+=")
					.Add(MINUSEQ, "-=")
					.Add(MULEQ, "*=")
					.Add(DIVEQ, "/=")
					.Add(MODEQ, "%=")
					.Add(BRCBEG, "(")
					.Add(BRCEND, ")")
					.Add(IDXBEG, "[")
					.Add(IDXEND, "]")
					.Add(BEG, "{")
					.Add(END, "}")
					.Add(COMMA, ","),
				new IdentifierTokenRule("true", "false", "if", "else", "null"),
				new StringTokenRule('\0'),
				new NumericTokenRule());
			Expression exp = CompilePlus(scanner, context);
			if (exp == null)
				return null;
			if (exp.Type != _type)
			{
				Type t = Array.Find(_type.GetInterfaces(), o => o.IsGenericType && o.GetGenericTypeDefinition() == typeof(IValue<>));
				if (t != null)
					exp = Expression.Convert(exp, t.GetGenericArguments()[0]);
				exp = Expression.Convert(exp, _type);
			}
			return Expression.Lambda(typeof(Func<>).MakeGenericType(_type), exp).Compile();
		}

		class TokenScanner5: TokenScanner
		{
			private OneBackFilter _push;
			public TokenScanner5(CharStream stream, params LexicalTokenRule[] rules)
				:base(stream, rules)
			{
				_push = new OneBackFilter();
				SetFilter(_push);
			}

			public void Back()
			{
				_push.Back();
			}
		}

		private Expression CompilePlus(TokenScanner5 scanner, ObjectContext context)
		{
			Expression left = CompileMult(scanner, context);
			if (left == null)
				return null;
			LexicalToken token;
			while ((token = scanner.Next()).Is(LexicalTokenType.SEQUENCE, PLUS, MINUS))
			{
				Expression right = CompileMult(scanner, context);
				if (right == null)
					return null;
				if (left.Type != right.Type)
					Cast(ref left, ref right);
				Expression temp = token.TokenType.Item == PLUS ? Expression.Add(left, right): Expression.Subtract(left, right);
				if (_oprimization && left is ConstantExpression && right is ConstantExpression)
				{
					Delegate function = Expression.Lambda(typeof(Func<>).MakeGenericType(left.Type), temp).Compile();
					temp = Expression.Constant(function.DynamicInvoke());
				}
				left = temp;
			}
			scanner.Back();
			return left;
		}

		private Expression CompileMult(TokenScanner5 scanner, ObjectContext context)
		{
			Expression left = CompileSimple(scanner, context);
			if (left == null)
				return null;
			LexicalToken token;
			while ((token = scanner.Next()).Is(LexicalTokenType.SEQUENCE, MULT, DIV, MOD))
			{
				Expression right = CompileSimple(scanner, context);
				if (right == null)
					return null;
				if (left.Type != right.Type)
					Cast(ref left, ref right);
				Expression temp =
					token.TokenType.Item == MULT ? Expression.Multiply(left, right): 
					token.TokenType.Item == DIV ? Expression.Divide(left, right): Expression.Modulo(left, right);
				if (_oprimization && left is ConstantExpression && right is ConstantExpression)
				{
					Delegate function = Expression.Lambda(typeof(Func<>).MakeGenericType(left.Type), temp).Compile();
					temp = Expression.Constant(function.DynamicInvoke());
				}
				left = temp;
			}
			scanner.Back();
			return left;
		}

		private Expression CompileSimple(TokenScanner5 scanner, ObjectContext context)
		{
			LexicalToken token = scanner.Next();
			if (token.Is(LexicalTokenType.EOF))
				return null;

			bool negative = false;
			if (token.Is(LexicalTokenType.SEQUENCE, MINUS))
			{
				negative = true;
				token = scanner.Next();
			}
			if (token.Is(LexicalTokenType.STRING))
			{
				return negative ? null: Expression.Constant(token.Text);
			}
			if (token.Is(LexicalTokenType.NUMERIC))
			{
				if (!negative)
					return Expression.Constant(token.Value);

				Type type = token.Value.GetType();
				if (type == typeof(int))
					return Expression.Constant(-(int)token.Value);
				if (type == typeof(long))
					return Expression.Constant(-(long)token.Value);
				if (type == typeof(decimal))
					return Expression.Constant(-(decimal)token.Value);
				if (type == typeof(double))
					return Expression.Constant(-(double)token.Value);
				return Expression.Negate(Expression.Constant(token.Value));
			}
			if (token.Is(LexicalTokenType.IDENTIFIER))
			{
				string identifier = token.Text;
				token = scanner.Next();
				Expression[] arguments = null;
				if (token.Is(LexicalTokenType.SEQUENCE, BRCBEG))
				{
					arguments = ParseArguments(scanner, context);
					if (arguments == null)
						return null;
				}
				else
				{
					scanner.Back();
				}
				MethodInfo method = context.Find(identifier, arguments);
				if (method == null)
					return null;
				if (_externals == null)
					_externals = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
				_externals.Add(identifier);
				return negative ?
					Expression.Negate(Expression.Call(method, arguments)):
					(Expression)Expression.Call(method, arguments);
			}
			if (token.Is(LexicalTokenType.KEYWORD))
			{
				return negative ? null: Expression.Constant(token.Item == 1);
			}
			if (token.Is(LexicalTokenType.SEQUENCE, BRCBEG))
			{
				Expression exp = CompilePlus(scanner, context);
				return exp != null && scanner.Next().Is(LexicalTokenType.SEQUENCE, BRCEND) ? (negative ? Expression.Negate(exp): exp): null;
			}
			return null;
		}

		private Expression[] ParseArguments(TokenScanner5 scanner, ObjectContext context)
		{
			List<Expression> result = new List<Expression>();
			do
			{
				Expression item = CompilePlus(scanner, context);
				if (item == null)
					if (scanner.Current.Is(LexicalTokenType.SEQUENCE, BRCEND))
						break;
					else
						return null;
				result.Add(item);
			} while (scanner.MoveNext() && scanner.Current.Is(LexicalTokenType.SEQUENCE, COMMA));
			return scanner.Current.Is(LexicalTokenType.SEQUENCE, BRCEND) ? result.ToArray(): null;
		}
		#endregion
	}

	public class ObjectContext
	{
		private KeyValuePair<string, MethodInfo>[] _methods;

		public ObjectContext(KeyValuePair<string, MethodInfo>[] methods)
		{
			_methods = methods;
		}

		public ObjectContext()
		{
		}

		public void Add(string method, string p)
		{
		}

		public MethodInfo Find(string identifier, Expression[] arguments)
		{
			return null;
		}
	}
}
