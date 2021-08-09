using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Tokenizer;

namespace State.Eval
{
	class Parser: IParser
	{
		private readonly PushScanner _scanner;
		private readonly Stack<EtExpression> _push;

		private Parser(CharStream text)
		{
			_push = new Stack<EtExpression>();
			_scanner = new PushScanner(text,
				new WhiteSpaceTokenRule(false, true),
				new CppCommentsTokenRule(LexicalTokenType.IGNORE),
				new SequenceTokenRule()
					.Add(PLUS, "+")
					.Add(MINUS, "-")
					.Add(MULT, "*")
					.Add(DIV, "/")
					.Add(MOD, "%")
					.Add(AND, "&")
					.Add(OR, "|")
//					.Add(XOR, "^")
					.Add(ANDALSO, "&&")
					.Add(ORELSE, "||")
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
					.Add(ANDEQ, "&=")
					.Add(OREQ, "|=")
//					.Add(XOREQ, "^=")
					.Add(BRCBEG, "(")
					.Add(BRCEND, ")")
					.Add(IDXBEG, "[")
					.Add(IDXEND, "]")
					.Add(MAPBEG, "{")
					.Add(MAPEND, "}")
					.Add(COMMA, ",")
					.Add(DOT, ".")
					.Add(COLON, ":"),
				new StringTokenRule(),
				new NumericTokenRule(),
				new IdentifierTokenRule());
		}

		public int At => _scanner.At;

		public PushScanner Scanner => _scanner;

		public static Et Parse(CharStream text, IEnumerable<IItemParser> items)
		{
			var p = new Parser(text);
			if (items != null)
			{
				foreach (IItemParser item in items)
				{
					p.AddItemParser(item);
				}
			}

			var t = new Et();
			EtExpression x;
			while ((x = p.ParseExpression()) != null)
			{
				t.Statements.Add(x);
			}
			if (!p.Scanner.EOF)
				t.Statements.Add(new EtSyntaxError(p.Scanner.Current, "Unexpected token: {0}", p.Scanner.Current));
			return t;
		}

		public void Rewind(int at)
		{
			_scanner.Stream.Move(at);
		}

		public void Push(EtExpression value)
		{
			if (value != null)
				_push.Push(value);
		}

		public EtExpression ParseExpression()
		{
			if (_push.Count > 0)
				return _push.Pop();
			return ParseAssignExpression();
		}

		private EtExpression ParseAssignExpression()
		{
			var lv = ParseCommaSeparatedList();
			if (lv == null || !IsLValue(lv))
				return lv;

			var tok = Scanner.Next();
			if (!tok.Is(LexicalTokenType.SEQUENCE, AssignOps) )
			{
				Scanner.Push(tok);
				return lv;
			}
			var at = Scanner.At;
			var rv = ParseCommaSeparatedList() ?? new EtSyntaxError(at, "Missing r-value");
			return new EtBinaryExpression(tok.Item, lv, rv);
		}
		private static readonly int[] AssignOps = new[] { EQUAL, PLUSEQ, MINUSEQ, MULEQ, DIVEQ, MODEQ, ANDEQ, OREQ/*, XOREQ*/ };

		private static bool IsLValue(EtExpression value)
		{
			return value.Type == EtExpressionType.Identifier || value.Type == EtExpressionType.Sequence;
		}

		private EtExpression ParseCommaSeparatedList()
		{
			var items = new List<EtExpression>();
			for (;;)
			{
				var x = ParseBlankSeparatedList();
				if (!Scanner.Next().Is(LexicalTokenType.SEQUENCE, COMMA))
				{
					Scanner.Push(Scanner.Current);
					if (items.Count == 0)
						return x;
					items.Add(x);
					break;
				}
				items.Add(x ?? EtExpression.Empty());
			}
			return new EtSequense(items[0].Position, items);
		}

		private EtExpression ParseBlankSeparatedList()
		{
			var items = new List<EtExpression>();
			for (;;)
			{
				var x = ParseKeyValue();
				if (x == null)
					break;
				items.Add(x);
			}
			return items.Count == 0 ? null :
				items.Count == 1 ? items[0] :
				new EtSequense(items[0].Position, items);
		}

		public EtParameter ParseParameter()
		{
			LexicalToken token = _scanner.Next();
			EtIdentifier name = null;
			if (token.Is(LexicalTokenType.IDENTIFIER) && token.Text.IndexOf('.') < 0)
			{
				LexicalToken colon = _scanner.Next();
				if (colon.Text != ":")
				{
					_scanner.Push(colon);
					return new EtParameter(new EtIdentifier(token.Position, token.Text));
				}
				name = new EtIdentifier(token.Position, token.Text);
			}
			else
			{
				_scanner.Push(token);
			}
			var value = ParseExpression();
			return value == null ? null : new EtParameter(name, value);
		}

		private EtExpression ParseKeyValue()
		{
			var key = ParseBinary(Prec.Table);
			if (key == null || key.Type == EtExpressionType.SyntaxError)
				return key;
			var token = Scanner.Next();
			if (!token.Is(LexicalTokenType.SEQUENCE, COLON))
			{
				Scanner.Push(token);
				return key;
			}
			var value = ParseBinary(Prec.Table);
			return new EtKeyValue(key.Position, key, value);
		}

		private EtExpression ParseBinary(Prec precedance)
		{
			if (precedance == null)
				return ParseSimple();
			EtExpression left = ParseBinary(precedance.High);
			if (left == null || left.Type == EtExpressionType.SyntaxError)
				return left;
			LexicalToken token;
			while ((token = _scanner.Next()).Is(LexicalTokenType.SEQUENCE, precedance.Ops))
			{
				EtExpression right = ParseBinary(precedance.High);
				if (right == null)
				{
					_scanner.Stream.Move(token.Position);
					return left;
				}
				if (right.Type == EtExpressionType.SyntaxError)
				{
					Push(right);
					return left;
				}
				left = new EtBinaryExpression(token.TokenType.Item, left, right);
			}
			_scanner.Push(token);
			return left;
		}

		private EtExpression ParseSimple()
		{
			LexicalToken token = _scanner.Next();
			if (token.Is(LexicalTokenType.EOF))
				return null;

			if (token.Is(LexicalTokenType.STRING))
				return new EtConstant(token.Position, token.Text, token.Value);
			if (token.Is(LexicalTokenType.NUMERIC))
				return new EtConstant(token.Position, token.Text, token.Value);

			IItemParser item = MapItem(token.Text);
			if (item != null)
				return item.Parse(new EtIdentifier(token.Position, token.Text), this);

			if (token.Is(LexicalTokenType.SEQUENCE, BRCBEG))
				return ParseSequence();
			if (token.Is(LexicalTokenType.SEQUENCE, MAPBEG))
				return ParseMap();
			if (token.Is(LexicalTokenType.IDENTIFIER))
				return new EtIdentifier(token.Position, token.Text);
			if (token.Is(LexicalTokenType.NEWLINE) || token.Is(LexicalTokenType.EOF))
				return null;
			Scanner.Push(token);
			return null;
		}

		public void AddItemParser(IItemParser item)
		{
			_itemsMap[item.Name] = item;
		}

		private IItemParser MapItem(string name)
		{
			IItemParser item;
			_itemsMap.TryGetValue(name, out item);
			return item;
		}
		private readonly Dictionary<string, IItemParser> _itemsMap = new Dictionary<string, IItemParser>();

		private EtExpression ParseSequence()
		{
			List<EtExpression> items = new List<EtExpression>();
			int at = At;
			//bool map = false;
			for (; ;)
			{
				LexicalToken a = _scanner.Next();
				if (a.Is(LexicalTokenType.SEQUENCE, BRCEND))
					break;
				if (a.Text != ",")
					_scanner.Push(a);
				EtExpression value = ParseExpression();
				//if (items.Count == 0 && value is EtKeyValue)
				//	map = true;
				//if (map && !(value.Type == ExpressionType.KeyValue || value.Type == ExpressionType.SyntaxError))
				//	value = new EtSyntaxError(a.At, "Key-value expression is expected.");

				if (value == null || value.Type == EtExpressionType.SyntaxError)
				{
					if (value == null)
						value = new EtSyntaxError(a.Position, "Missing closing brace.");
					while (Scanner.MoveNext() && !Scanner.Current.Is(LexicalTokenType.SEQUENCE, BRCEND))
					{
					}
					if (items.Count == 0)
						return value;
					Push(value);
					break;
				}
				items.Add(value);
			}
			return new EtSequense(at, items);
		}

		private EtMap ParseMap()
		{
			List<EtKeyValue> items = new List<EtKeyValue>();
			int at = At;
			for (; ;)
			{
				LexicalToken a = _scanner.Next();
				if (a.Is(LexicalTokenType.SEQUENCE, MAPEND))
					break;
				if (a.Text != ",")
					_scanner.Push(a);
				EtExpression x = ParseExpression();
				EtKeyValue kv = x as EtKeyValue;
				if (kv == null)
				{
					if (x == null || x.Type != EtExpressionType.SyntaxError)
						x = new EtSyntaxError(x?.Position ?? a.Position, "Key-value expression is expected");
					Push(x);
					while (Scanner.MoveNext() && !Scanner.Current.Is(LexicalTokenType.SEQUENCE, MAPEND))
					{
					}
					break;
				}
				items.Add(kv);
			}
			return new EtMap(at, items);
		}

		//public List<EtParameter> ParseParameters(bool isMap, int endItem)
		//{
		//	List<EtParameter> parameters = new List<EtParameter>();
		//	for (; ;)
		//	{
		//		LexicalToken a = _scanner.Next();
		//		if (a.Is(LexicalTokenType.SEQUENCE, endItem))
		//			return parameters;

		//		string name = null;
		//		if (a.Is(LexicalTokenType.IDENTIFIER) && a.Text.IndexOf('.') < 0)
		//		{
		//			LexicalToken b = _scanner.Next();
		//			if (b.Text == ":")
		//			{
		//				name = a.Text;
		//			}
		//			else
		//			{
		//				_scanner.Push(a);
		//				_scanner.Push(b);
		//			}
		//		}
		//		else
		//		{
		//			_scanner.Push(a);
		//		}
		//		if (isMap && name == null)
		//			throw _scanner.SyntaxException("name is required", a.At);
		//		EtExpression x = ParseExpression();
		//		if (x == null)
		//		{
		//			if (name != null)
		//				throw _scanner.SyntaxException("expression is required");
		//			return parameters;
		//		}
		//		parameters.Add(new EtParameter(a.At, name, x));
		//	}
		//}

		#region Tokens

		private const int DOT = (int)BinaryOperation.Dot;
		private const int COLON = (int)BinaryOperation.Colon;

		private const int PLUS = (int)BinaryOperation.Plus;
		private const int MINUS = (int)BinaryOperation.Minus;
		private const int MULT = (int)BinaryOperation.Mult;
		private const int DIV = (int)BinaryOperation.Div;
		private const int MOD = (int)BinaryOperation.Mod;

		private const int AND = (int)BinaryOperation.And;
		private const int OR = (int)BinaryOperation.Or;
		private const int ANDALSO = (int)BinaryOperation.AndAlso;
		private const int ORELSE = (int)BinaryOperation.OrElse;

		private const int EQ = (int)BinaryOperation.Eq;
		private const int LT = (int)BinaryOperation.Lt;
		private const int GT = (int)BinaryOperation.Gt;
		private const int NE = (int)BinaryOperation.Ne;
		private const int LE = (int)BinaryOperation.Le;
		private const int GE = (int)BinaryOperation.Ge;

		private const int EQUAL = (int)BinaryOperation.Equal;
		private const int PLUSEQ = (int)BinaryOperation.PlusEqual;
		private const int MINUSEQ = (int)BinaryOperation.MinusEqual;
		private const int MULEQ = (int)BinaryOperation.MultEqual;
		private const int DIVEQ = (int)BinaryOperation.DivEqual;
		private const int MODEQ = (int)BinaryOperation.ModEqual;
		private const int ANDEQ = (int)BinaryOperation.AndEqual;
		private const int OREQ = (int)BinaryOperation.OrEqual;
//		private const int XOREQ = (int)BinaryOperation.XorEqual;

		private const int BRCBEG = 41;	//	(
		private const int BRCEND = 42;	//	)
		private const int IDXBEG = 43;	//	[
		private const int IDXEND = 44;	//	]
		private const int MAPBEG = 45;	//	{
		private const int MAPEND = 46;	//	}

		private const int COMMA = 47;

		#endregion

		#region Table of precedance

		class Prec
		{
			public readonly int[] Ops;
			public readonly Prec High;

			private Prec(int[] ops, Prec high = null)
			{
				Ops = ops;
				High = high;
			}

			private static Prec P(int[] ops, Prec high = null)
			{
				return new Prec(ops, high);
			}

			public static readonly Prec Table = P(
				new [] { ORELSE }, P(
					new[] { ANDALSO }, P(
						new[] { OR }, P(
							new[] { AND }, P(
								new[] { EQ, NE }, P(
									new[] { LT, GT, LE, GE }, P(
										new[] { PLUS, MINUS }, P(
											new[] { MULT, DIV, MOD })
				)))))));
		}

		#endregion
	}

	public class PushScanner: TokenScanner
	{
		private readonly PushFilter _push;

		public PushScanner(CharStream stream, params LexicalTokenRule[] rules)
			: base(stream, rules)
		{
			_push = new PushFilter();
			SetFilter(_push);
		}

		public PushScanner(CharStream stream, PushScanner scanner)
			: base(CharStream.Empty, scanner, false)
		{
			_push = new PushFilter();
			SetFilter(_push);
		}

		public void Push(LexicalToken token)
		{
			_push.Push(token);
		}
	}
}
