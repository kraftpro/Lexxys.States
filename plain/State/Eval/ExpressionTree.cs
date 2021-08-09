using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

using Lexxys.Tokenizer;

namespace State.Eval
{
    public class Et
    {
        public readonly List<EtExpression> Statements;

        public Et()
        {
            Statements = new List<EtExpression>();
        }

        public void Accept(Visitor visitor)
        {
            foreach (var item in Statements)
            {
                item.Accept(visitor);
            }
        }

        public void Print(TextWriter writer, int indent = 2)
        {
            TreePrinter.Print(writer, indent, Statements);
        }

        public StringBuilder Dump(StringBuilder text, int indent = 2)
        {
            return TreePrinter.Dump(text, indent, Statements);
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int indent)
        {
            return Dump(new StringBuilder(), indent).ToString();
        }
    }

    #region Enumerators

    public enum EtExpressionType
    {
        Call,
        Const,
        Binary,
        Unary,
        Sequence,
        Map,
        Parameter,
        KeyValue,
        Identifier,
        SyntaxError,
    }

    public enum BinaryOperation
    {
        Noop,

        Dot,
        Colon,

        Plus,
        Minus,
        Mult,
        Div,
        Mod,

        And,
        Or,
//		Xor,
        AndAlso,
        OrElse,

        Eq,
        Lt,
        Gt,
        Le,
        Ge,
        Ne,

        Equal,
        PlusEqual,
        MinusEqual,
        MultEqual,
        DivEqual,
        ModEqual,
        AndEqual,
        OrEqual,
//		XorEqual,

        LastOperatorIndex = OrEqual
    }

    public enum UnaryOperation
    {
        Noop,
        Plus,
        Minus,
    }

    #endregion

    public abstract class EtExpression
    {
        public readonly EtExpressionType Type;
        public readonly int Position;

        protected EtExpression(EtExpressionType type, int position = default)
        {
            Type = type;
            Position = position;
        }

        public abstract void Accept(Visitor visitor);

        public virtual Expression Generate()
        {
            return null;
        }

        public void Dump(TextWriter writer, int indent = 2)
        {
            TreePrinter.Print(writer, indent, new[] { this });
        }

        public StringBuilder Dump(StringBuilder text, int indent = 2)
        {
            using (var writer = new StringWriter(text))
            {
                TreePrinter.Print(writer, indent, new[] { this });
            }
            return text;
        }

        public string ToString(int indent)
        {
            return Dump(new StringBuilder(), indent).ToString();
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public static EtExpression Empty()
        {
            return new EtConstant(null, null);
        }
    }

    public class EtIdentifier: EtExpression
    {
        public readonly string Name;

        public EtIdentifier(int at, string name)
            : base(EtExpressionType.Identifier, at)
        {
            Name = name;
        }

        public EtIdentifier(string name)
            :this(default, name)
        {
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtSyntaxError: EtExpression
    {
        public readonly string Message;
        public readonly LexicalToken Token;

        public EtSyntaxError(int at, string message, params object[] parameters)
            : base(EtExpressionType.SyntaxError, at)
        {
            Message = parameters == null || parameters.Length == 0 ? message : String.Format(message, parameters);
        }

        public EtSyntaxError(string message, params object[] parameters)
            : base(EtExpressionType.SyntaxError)
        {
            Message = parameters == null || parameters.Length == 0 ? message : String.Format(message, parameters);
        }

        public EtSyntaxError(LexicalToken token, string message, params object[] parameters)
            : base(EtExpressionType.SyntaxError, token == null ? default : token.Position)
        {
            Message = parameters == null || parameters.Length == 0 ? message : String.Format(message, parameters);
            Token = token;
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtCallExpression: EtExpression
    {
        public readonly string Name;
        public readonly List<EtParameter> Parameters;

        public EtCallExpression(int at, string name, List<EtParameter> parameters)
            : base(EtExpressionType.Call, at)
        {
            Name = name;
            Parameters = parameters;
        }

        public EtCallExpression(string name, List<EtParameter> parameters)
            : this(default, name, parameters)
        {
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtConstant: EtExpression
    {
        public readonly string Text;
        public readonly object Value;

        public EtConstant(int at, string text, object value)
            : base(EtExpressionType.Const, at)
        {
            Text = text;
            Value = value;
        }

        public EtConstant(string text, object value)
            : this(default, text, value)
        {
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtParameter: EtExpression
    {
        public readonly string Name;
        public readonly EtExpression Value;

        public EtParameter(int at, string name, EtExpression value)
            : base(EtExpressionType.Parameter, at)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
            Name = name;
        }

        public EtParameter(string name, EtExpression value)
            : this(value.Position, name, value)
        {
        }

        public EtParameter(EtIdentifier name, EtExpression value)
            : this(name?.Position ?? value.Position, name?.Name, value)
        {
        }

        public EtParameter(EtExpression value)
            : base(EtExpressionType.Parameter, value.Position)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Value = value;
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }

        public EtExpression ToExpression()
        {
            return Name == null ? Value : new EtKeyValue(Position, new EtIdentifier(Position, Name), Value);
        }
    }

    public class EtBinaryExpression: EtExpression
    {
        public readonly BinaryOperation Operation;
        public readonly EtExpression Left;
        public readonly EtExpression Right;

        public EtBinaryExpression(BinaryOperation operation, EtExpression left, EtExpression right)
            : base(EtExpressionType.Binary, left.Position)
        {
            Operation = operation;
            Left = left;
            Right = right;
        }

        public EtBinaryExpression(int operation, EtExpression left, EtExpression right)
            : this((BinaryOperation)operation, left, right)
        {
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtUnaryExpression: EtExpression
    {
        public readonly UnaryOperation Operation;
        public readonly EtExpression Operand;

        public EtUnaryExpression(int at, UnaryOperation operation, EtExpression operand)
            : base(EtExpressionType.Unary, at)
        {
            Operation = operation;
            Operand = operand;
        }

        public EtUnaryExpression(int at, int operation, EtExpression operand)
            : this(at, (UnaryOperation)operation, operand)
        {
        }

        public EtUnaryExpression(UnaryOperation operation, EtExpression operand)
            : this(default, operation, operand)
        {
        }

        public EtUnaryExpression(int operation, EtExpression operand)
            : this(default, (UnaryOperation)operation, operand)
        {
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtSequense: EtExpression
    {
        public readonly List<EtExpression> Items;

        public EtSequense(int at, List<EtExpression> items)
            : base(EtExpressionType.Sequence, at)
        {
            Items = items;
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtMap: EtExpression
    {
        public readonly List<EtKeyValue> Items;

        public EtMap(int position, List<EtKeyValue> items)
            : base(EtExpressionType.Map, position)
        {
            Items = items;
        }

        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class EtKeyValue: EtExpression
    {
        public readonly EtExpression Key;
        public readonly EtExpression Value;

        public EtKeyValue(int at, EtExpression key, EtExpression value)
            : base(EtExpressionType.KeyValue, at)
        {
            Key = key;
            Value = value;
        }
        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }

        internal string GetName()
        {
            return Key == null ? null :
                Key.Type == EtExpressionType.Identifier ? ((EtIdentifier)Key).Name :
                Key.Type == EtExpressionType.Const ? ((EtConstant)Key).Value as string : null;
        }
    }
}
