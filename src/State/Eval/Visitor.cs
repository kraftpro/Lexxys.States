using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace State.Eval
{
	public abstract class Visitor
	{
		public void Visit(EtExpression element)
		{
			//switch (element)
			//{
			//	case EtCallExpression call:
			//		Visit(call);
			//		break;
			//	case EtConstant constant:
			//		Visit(constant);
			//		break;
			//	case EtBinaryExpression binary:
			//		Visit(binary);
			//		break;
			//	case EtUnaryExpression unary:
			//		Visit(unary);
			//		break;
			//	case EtSequense sequence:
			//		Visit(sequence);
			//		break;
			//	case EtMap map:
			//		Visit(map);
			//		break;
			//	case EtParameter parameter:
			//		Visit(parameter);
			//		break;
			//	case EtKeyValue keyValue:
			//		Visit(keyValue);
			//		break;
			//	case EtIdentifier identifier:
			//		Visit(identifier);
			//		break;
			//	case EtSyntaxError syntaxError:
			//		Visit(syntaxError);
			//		break;
			//	default:
			//		throw EX.ArgumentOutOfRange("element.Type", element.Type);
			//}
			switch (element.Type)
			{
				case EtExpressionType.Call:
					Visit(element as EtCallExpression);
					break;
				case EtExpressionType.Const:
					Visit(element as EtConstant);
					break;
				case EtExpressionType.Binary:
					Visit(element as EtBinaryExpression);
					break;
				case EtExpressionType.Unary:
					Visit(element as EtUnaryExpression);
					break;
				case EtExpressionType.Sequence:
					Visit(element as EtSequense);
					break;
				case EtExpressionType.Map:
					Visit(element as EtMap);
					break;
				case EtExpressionType.Parameter:
					Visit(element as EtParameter);
					break;
				case EtExpressionType.KeyValue:
					Visit(element as EtKeyValue);
					break;
				case EtExpressionType.Identifier:
					Visit(element as EtIdentifier);
					break;
				case EtExpressionType.SyntaxError:
					Visit(element as EtSyntaxError);
					break;
				default:
					throw EX.ArgumentOutOfRange("element.Type", element.Type);
			}
		}

		public abstract void Visit(EtIdentifier element);
		public abstract void Visit(EtSyntaxError element);
		public abstract void Visit(EtCallExpression element);
		public abstract void Visit(EtConstant element);
		public abstract void Visit(EtParameter element);
		public abstract void Visit(EtBinaryExpression element);
		public abstract void Visit(EtUnaryExpression element);
		public abstract void Visit(EtSequense element);
		public abstract void Visit(EtMap element);
		public abstract void Visit(EtKeyValue element);
	}
}
