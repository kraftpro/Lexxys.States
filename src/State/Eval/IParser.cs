using System.Collections.Generic;
using System.Linq;
using Lexxys.Tokenizer;

namespace State.Eval
{
	public interface IParser
	{
		int At { get; }
		PushScanner Scanner { get; }

		EtExpression ParseExpression();
		EtParameter ParseParameter();
		void AddItemParser(IItemParser item);
		void Rewind(int at);
		void Push(EtExpression value);

		//EtExpression ParseSimple();
		//EtMapExpression ParseMap();
		//List<EtCallParameter> ParseParameters(bool isMap, int endItem);
		//EtSequenseExpression ParseSequence();
	}

	public static class ParserExtension
	{
		public static void Queue(this IParser parser, IEnumerable<EtExpression> expressions)
		{
			foreach (var expression in expressions.Reverse())
			{
				parser.Push(expression);
			}
		}
	}

	public interface IItemParser
	{
		string Name { get; }
		EtExpression Parse(EtIdentifier itemId, IParser parser);
	}
}