#if false
using System;
using System.Collections.Generic;

using Lexxys;
using Lexxys.Tokenizer;
using System.Reflection;

namespace State.Eval
{
    /// <summary>
    /// 
    /// 
    /// 
    /// </summary>

    class Gramm
    {
        public static readonly LexicalTokenType BRACES = new LexicalTokenType(20, 0, "braces");
        TokenScanner context = new TokenScanner
            (
                new WhiteSpaceTokenRule(false, true),
                new CppCommentsTokenRule(),
                new IdentifierTokenRule(),
                new StringTokenRule(),
                new NumericTokenRule(),
                new SequenceTokenRule(";", ",", ":"),
                new SequenceTokenRule(BRACES, "(", ")", "[", "]", "{", "}")
            );

        public static void Go()
        {
            Gramm g = new Gramm();
            g._definitions = Defs();
            g.Parse(@"
print min 1 3, max 5 9;
if 123 print 22 :else print 55;
                ");
        }

        private List<Function> _definitions;

        #region Parser

        private const int PLUS = (int)BinaryOperation.Plus;
        private const int MINUS = (int)BinaryOperation.Minus;
        private const int MULT = (int)BinaryOperation.Mult;
        private const int DIV = (int)BinaryOperation.Div;
        private const int MOD = (int)BinaryOperation.Mod;

        private const int AND = (int)BinaryOperation.And;
        private const int OR = (int)BinaryOperation.Or;
        private const int XOR = (int)BinaryOperation.Xor;
        private const int ANDAND = (int)BinaryOperation.AndAnd;
        private const int OROR = (int)BinaryOperation.OrOr;

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
        private const int XOREQ = (int)BinaryOperation.XorEqual;

        private const int BRCBEG = 41;
        private const int BRCEND = 42;
        private const int IDXBEG = 43;
        private const int IDXEND = 44;
        private const int MAPBEG = 45;
        private const int MAPEND = 46;

        private const int COMMA = 45;

        #region Table of precedance

        class Prec
        {
            public int[] Ops;
            public Prec High;
        }

        private static readonly Prec Precedance = new Prec
        {
            Ops = new[] { OROR },
            High = new Prec
            {
                Ops = new[] { ANDAND },
                High = new Prec
                {
                    Ops = new[] { OR },
                    High = new Prec
                    {
                        Ops = new[] { XOR },
                        High = new Prec
                        {
                            Ops = new[] { AND },
                            High = new Prec
                            {
                                Ops = new[] { EQ, NE },
                                High = new Prec
                                {
                                    Ops = new[] { LT, GT, LE, GE },
                                    High = new Prec
                                    {
                                        Ops = new[] { PLUS, MINUS },
                                        High = new Prec
                                        {
                                            Ops = new[] { MULT, DIV, MOD },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        #endregion

        class TokenScanner5: TokenScanner
        {
            private PushFilter _push;
            public TokenScanner5(CharStream stream, params LexicalTokenRule[] rules)
                : base(stream, rules)
            {
                _push = new PushFilter();
                SetFilter(_push);
            }

            public void Push(LexicalToken token)
            {
                _push.Push(token);
            }
        }
        private TokenScanner5 _scanner;

        public void Parse(string text)
        {
            TokenScanner5 _scanner = new TokenScanner5(new CharStream(text),
                new WhiteSpaceTokenRule(true, false),
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
                    .Add(ANDEQ, "&=")
                    .Add(OREQ, "|=")
                    .Add(XOREQ, "^=")
                    .Add(BRCBEG, "(")
                    .Add(BRCEND, ")")
                    .Add(IDXBEG, "[")
                    .Add(IDXEND, "]")
                    .Add(MAPBEG, "{")
                    .Add(MAPEND, "}")
                    .Add(COMMA, ","),
                new StringTokenRule('\0'),
                new NumericTokenRule());
            Et p = new Et { Statements = new List<EtExpression>() };
            EtExpression x;
            while ((x = CompileExpression()) != null)
            {
                p.Statements.Add(x);
            }
        }

        EtExpression CompileExpression()
        {
            return CompileBinary(Precedance);
        }


        private EtExpression CompileBinary(Prec precedance)
        {
            if (precedance == null)
                return CompileSimple();
            EtExpression left = CompileBinary(precedance.High);
            if (left == null)
                return null;
            LexicalToken token;
            while ((token = _scanner.Next()).Is(LexicalTokenType.SEQUENCE, precedance.Ops))
            {
                EtExpression right = CompileBinary(precedance.High);

                if (right == null)
                    return null;
                left = new EtBinaryExpression
                {
                    Operation = (BinaryOperation)token.TokenType.Item,
                    Left = left,
                    Right = right,
                };
            }
            _scanner.Push(token);
            return left;
        }

        private EtExpression CompileSimple()
        {
            LexicalToken token = _scanner.Next();
            if (token.Is(LexicalTokenType.EOF))
                return null;

            bool negative = false;
            if (token.Is(LexicalTokenType.SEQUENCE, MINUS))
            {
                negative = true;
                token = _scanner.Next();
            }
            if (token.Is(LexicalTokenType.SEQUENCE, BRCBEG))
                return ParseSequence();
            if (token.Is(LexicalTokenType.SEQUENCE, MAPBEG))
                return ParseMap();

            if (token.Is(LexicalTokenType.NUMERIC) || token.Is(LexicalTokenType.STRING))
            {
                // if (!negative)
                return new EtConstant(token.At, token.Text, token.Value);
            }
            if (token.Is(LexicalTokenType.IDENTIFIER))
            {
                string identifier = token.Text;
                token = _scanner.Next();
                List<EtParameter> arguments = null;
                if (token.Is(LexicalTokenType.SEQUENCE, BRCBEG))
                {
                    arguments = ParseParameters(false, BRCEND);
                    if (arguments == null)
                        return null;
                }
                else
                {
                    _scanner.Push(token);
                }
                MethodInfo method = context.Find(identifier, arguments);
                if (method == null)
                    return null;
                if (_externals == null)
                    _externals = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                _externals.Add(identifier);
                return negative ?
                    EtExpression.Negate(EtExpression.Call(method, arguments)) :
                    (EtExpression)EtExpression.Call(method, arguments);
            }
            if (token.Is(LexicalTokenType.KEYWORD))
            {
                return negative ? null : EtExpression.Constant(token.Item == 1);
            }
            if (token.Is(LexicalTokenType.SEQUENCE, BRCBEG))
            {
                EtExpression exp = CompilePlus(_scanner, context);
                return _scanner.Next().Is(LexicalTokenType.SEQUENCE, BRCEND) ? (negative ? EtExpression.Negate(exp) : exp) : null;
            }
            return null;
        }

        private EtSequenseExpression ParseSequence()
        {
            List<EtExpression> items = new List<EtExpression>();
            CharPosition at = _scanner.At;
            for (; ;)
            {
                LexicalToken a = _scanner.Next();
                if (a.Is(LexicalTokenType.SEQUENCE, BRCEND))
                    return new EtSequenseExpression(at, items);
                if (a.Text != ",")
                    _scanner.Push(a);
                EtExpression value = CompileExpression();
                if (value == null)
                    throw _scanner.SyntaxException("expression is expected");
                items.Add(value);
            }
        }

        private EtMapExpression ParseMap()
        {
            List<EtMapItem> items = new List<EtMapItem>();
            CharPosition at = _scanner.At;
            for (; ;)
            {
                LexicalToken a = _scanner.Next();
                if (a.Is(LexicalTokenType.SEQUENCE, MAPEND))
                    return new EtMapExpression(at, items);
                if (a.Text != ",")
                    _scanner.Push(a);
                EtExpression key = CompileExpression();
                if (key == null)
                    throw _scanner.SyntaxException("key expression is expected");
                if (_scanner.Next().Text != ":")
                    throw _scanner.SyntaxException("colon is expected");
                EtExpression value = CompileExpression();
                if (key == null)
                    throw _scanner.SyntaxException("value expression is expected");
                items.Add(new EtMapItem { Key = key, Value = value });
            }
        }

        private List<EtParameter> ParseParameters(bool isMap, int endItem)
        {
            List<EtParameter> parameters = new List<EtParameter>();
            for(;;)
            {
                LexicalToken a = _scanner.Next();
                if (a.Is(LexicalTokenType.SEQUENCE, endItem))
                    return parameters;

                string name = null;
                if (a.Is(LexicalTokenType.IDENTIFIER) && a.Text.IndexOf('.') < 0)
                {
                    LexicalToken b = _scanner.Next();
                    if (b.Text == ":")
                    {
                        name = a.Text;
                    }
                    else
                    {
                        _scanner.Push(a);
                        _scanner.Push(b);
                    }
                }
                else
                {
                    _scanner.Push(a);
                }
                if (isMap && name == null)
                    throw _scanner.SyntaxException("name is required", a.At);
                EtExpression x = CompileExpression();
                if (x == null)
                {
                    if (name != null)
                        throw _scanner.SyntaxException("expression is required");
                    return parameters;
                }
                parameters.Add(new EtParameter(a.At, name == null ? null : new EtIdentifier(a.At, name), x));
            }
        }
        #endregion

        public static List<Function> Defs()
        {
            return new List<Function> {
                new Function("min",
                    new[]
                    {
                        new Parameter { Name = "left" },
                        new Parameter { Name = "right" },
                    }
                ),
                new Function("max",
                    new[]
                    {
                        new Parameter { Name = "left" },
                        new Parameter { Name = "right" },
                    }
                ),
                new Function("print",
                    new[]
                    {
                        new Parameter { Name = "value", IsSequence = true },
                    }
                ),
                new Function("if",
                    new[]
                    {
                        new Parameter { Name = "condition" },
                        new Parameter { Name = "then" },
                        new Parameter { Name = "else", IsOptional = true, ByNameOnly = true },
                    }
                ),
            };
        }
    }

    public class Foo<T>: Lazy<T>
    {
    }

    public class Bar<T>
    {
        public string Name { get; set; }
        public List<Bar<T>> Args { get; set; }
        public Foo<T> Body { get; set; }
    }

    public static class TypeExtension
    {
    }
}
#endif
