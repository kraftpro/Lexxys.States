using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Eval
{
	public interface IFn
	{
		dynamic Value { get; }
		ISet<Type> ValueType { get; }
		void Evaluate();
	}

	public class FnConst: IFn
	{
		public FnConst(object value)
		{
			Value = value;
			ValueType = ReadOnly.Wrap(new SortedSet<Type> {value?.GetType() ?? typeof(void)});
		}

		public object Value { get; }

		public ISet<Type> ValueType { get; }

		public void Evaluate()
		{
		}
	}

	public class FnBinary: IFn
	{
		private readonly Func<object> _action;
		private bool _evaluated;
		private object _value;

		public FnBinary(BinaryOperation operation, IFn left, IFn right)
		{
			var types = new SortedSet<Type>(left.ValueType);
			types.UnionWith(right.ValueType);
			ValueType = ReadOnly.Wrap(types);

			switch (operation)
			{
				case BinaryOperation.Noop:
					_action = () => null;
					break;
				case BinaryOperation.Dot:
					break;
				case BinaryOperation.Colon:
					break;
				case BinaryOperation.Plus:
					_action = () => left.Value + right.Value;
					break;
				case BinaryOperation.Minus:
					_action = () => left.Value - right.Value;
					break;
				case BinaryOperation.Mult:
					_action = () => left.Value * right.Value;
					break;
				case BinaryOperation.Div:
					_action = () => left.Value / right.Value;
					break;
				case BinaryOperation.Mod:
					_action = () => left.Value % right.Value;
					break;
				case BinaryOperation.And:
					break;
				case BinaryOperation.Or:
					break;
				case BinaryOperation.AndAlso:
					break;
				case BinaryOperation.OrElse:
					break;
				case BinaryOperation.Eq:
					break;
				case BinaryOperation.Lt:
					break;
				case BinaryOperation.Gt:
					break;
				case BinaryOperation.Le:
					break;
				case BinaryOperation.Ge:
					break;
				case BinaryOperation.Ne:
					break;
				case BinaryOperation.Equal:
					break;
				case BinaryOperation.PlusEqual:
					break;
				case BinaryOperation.MinusEqual:
					break;
				case BinaryOperation.MultEqual:
					break;
				case BinaryOperation.DivEqual:
					break;
				case BinaryOperation.ModEqual:
					break;
				case BinaryOperation.AndEqual:
					break;
				case BinaryOperation.OrEqual:
					break;
				default:
					break;
			}
		}

		public object Value
		{
			get
			{
				Evaluate();
				return _value;
			}
		}

		public ISet<Type> ValueType { get; }

		public void Evaluate()
		{
			if (!_evaluated)
			{
				_evaluated = true;
				_value = _action();
			}
		}
	}

	public class FnCall
	{

	}
}
