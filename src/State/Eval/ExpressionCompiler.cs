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
    class ExpressionCompiler: Visitor
    {
        private Expression _result;

        public override void Visit(EtCallExpression element)
        {
            var xx = new List<KeyValuePair<string, Expression>>();
            foreach (var item in element.Parameters)
            {
                item.Accept(this);
                xx.Add(KeyValue.Create(item.Name, _result));
            }
            
            throw new NotImplementedException();
        }

        public override void Visit(EtParameter element)
        {
            element.Value.Accept(this);
        }

        public override void Visit(EtUnaryExpression element)
        {
            switch (element.Operation)
            {
                case UnaryOperation.Plus:
                    element.Operand.Accept(this);
                    _result = Expression.UnaryPlus(_result);
                    break;
                case UnaryOperation.Minus:
                    element.Operand.Accept(this);
                    _result = Expression.Negate(_result);
                    break;
                case UnaryOperation.Noop:
                default:
                    _result = Expression.Empty();
                    break;
            }
        }

        public override void Visit(EtMap element)
        {
            if (element.Items.Count == 0)
            {
                _result = Expression.New(typeof(Hashtable));
                return;
            }

            var add = typeof(Hashtable).GetMethod("Add");
            var xx = new List<Expression>();
            var x = Expression.Variable(typeof(Hashtable));

            xx.Add(Expression.Assign(x, Expression.New(typeof(Hashtable))));
            foreach (var item in element.Items)
            {
                item.Key.Accept(this);
                var key = _result;
                item.Value.Accept(this);
                var value = _result;
                xx.Add(Expression.Call(x, add, key, _result));
            }
            xx.Add(x);
            _result = Expression.Block(xx);
        }

        public override void Visit(EtKeyValue element)
        {
            element.Key.Accept(this);
            var key = _result;
            element.Value.Accept(this);
            var value = _result;
            _result = Expression.New(typeof(KeyValuePair<object, object>).GetConstructor(new[] { typeof(object), typeof(object) }), key, value);
        }

        public override void Visit(EtSequense element)
        {
            if (element.Items.Count < 2)
            {
                if (element.Items.Count == 1)
                    element.Items[0].Accept(this);
                else
                    _result = Expression.Empty();
                return;
            }

            var xx = new List<Expression>();
            foreach (var item in element.Items)
            {
                item.Accept(this);
                xx.Add(_result);
            }
            _result = Expression.Block(xx);
        }

        public override void Visit(EtBinaryExpression element)
        {
            element.Left.Accept(this);
            var left = _result;
            element.Right.Accept(this);
            var right = _result;
            switch (element.Operation)
            {
                case BinaryOperation.Dot:
                    {
                        
                    }
                    break;
                case BinaryOperation.Colon:
                    {
                        
                    }
                    break;
                case BinaryOperation.Plus:
                    _result = Expression.Add(left, right);
                    break;
                case BinaryOperation.Minus:
                    _result = Expression.Subtract(left, right);
                    break;
                case BinaryOperation.Mult:
                    _result = Expression.Multiply(left, right);
                    break;
                case BinaryOperation.Div:
                    _result = Expression.Divide(left, right);
                    break;
                case BinaryOperation.Mod:
                    _result = Expression.Modulo(left, right);
                    break;
                case BinaryOperation.And:
                    _result = Expression.And(left, right);
                    break;
                case BinaryOperation.Or:
                    _result = Expression.Or(left, right);
                    break;
                case BinaryOperation.AndAlso:
                    _result = Expression.AndAlso(left, right);
                    break;
                case BinaryOperation.OrElse:
                    _result = Expression.OrElse(left, right);
                    break;
                case BinaryOperation.Eq:
                    _result = Expression.Equal(left, right);
                    break;
                case BinaryOperation.Lt:
                    _result = Expression.LessThan(left, right);
                    break;
                case BinaryOperation.Gt:
                    _result = Expression.GreaterThan(left, right);
                    break;
                case BinaryOperation.Le:
                    _result = Expression.LessThanOrEqual(left, right);
                    break;
                case BinaryOperation.Ge:
                    _result = Expression.GreaterThanOrEqual(left, right);
                    break;
                case BinaryOperation.Ne:
                    _result = Expression.NotEqual(left, right);
                    break;
                case BinaryOperation.Equal:
                    _result = Expression.Assign(left, right);
                    break;
                case BinaryOperation.PlusEqual:
                    _result = Expression.AddAssign(left, right);
                    break;
                case BinaryOperation.MinusEqual:
                    _result = Expression.SubtractAssign(left, right);
                    break;
                case BinaryOperation.MultEqual:
                    _result = Expression.MultiplyAssign(left, right);
                    break;
                case BinaryOperation.DivEqual:
                    _result = Expression.DivideAssign(left, right);
                    break;
                case BinaryOperation.ModEqual:
                    _result = Expression.ModuloAssign(left, right);
                    break;
                case BinaryOperation.AndEqual:
                    _result = Expression.AndAssign(left, right);
                    break;
                case BinaryOperation.OrEqual:
                    _result = Expression.OrAssign(left, right);
                    break;
                default:
                //case BinaryOperation.Noop:
                    break;
            }
            throw new NotImplementedException();
        }

        public override void Visit(EtConstant element)
        {
            _result = Expression.Constant(element.Value);
        }

        public override void Visit(EtSyntaxError element)
        {
            _result = Expression.Throw(Expression.Constant(EX.InvalidOperation(element.Message)));
        }

        public override void Visit(EtIdentifier element)
        {
            throw new NotImplementedException();
        }
    }
}
