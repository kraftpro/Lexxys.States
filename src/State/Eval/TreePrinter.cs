using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

using Lexxys.Tokenizer;

namespace State.Eval
{

	public sealed class TreePrinter: Visitor, IDisposable
	{
		private readonly int _indentSize;
		private readonly TextWriter _w;
		private int _indent;
		private bool _disposed;

		public TreePrinter(TextWriter writer, int indentSize = 2)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			_w = writer;
			_indentSize = indentSize;
		}

		private string Indent => new string(' ', _indent * _indentSize);

		private void Header(EtExpression element)
		{
			if (_indentSize == 0)
				_w.Write("{0} ", element.Type);
			else
				_w.Write($"{element.Type}({element.Position}) ");
		}

		public static StringBuilder Dump(StringBuilder text, int indent, IEnumerable<EtExpression> expressionList)
		{
			using (var writer = new StringWriter(text))
			{
				Print(writer, indent, expressionList);
			}
			return text;
		}

		public static void Print(TextWriter writer, int indent, IEnumerable<EtExpression> expressionList)
		{
			using (var printer = new TreePrinter(writer))
			{
				bool writeLine = false;
				foreach (var item in expressionList)
				{
					if (indent == 0)
						if (writeLine)
							writer.WriteLine();
						else
							writeLine = true;
					item.Accept(printer);
				}
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_w.Dispose();
			}
		}

		public override void Visit(EtBinaryExpression element)
		{
			if (_indentSize == 0)
			{
				_w.Write("({0} ", element.Operation);
				element.Left.Accept(this);
				_w.Write(' ');
				element.Left.Accept(this);
				_w.Write(')');
			}
			else
			{
				Header(element);
				_w.WriteLine(element.Operation);
				++_indent;
				_w.Write(Indent);
				_w.Write("Left: ");
				element.Left.Accept(this);
				_w.Write(Indent);
				_w.Write("Right: ");
				element.Right.Accept(this);
				--_indent;
			}
		}

		public override void Visit(EtCallExpression element)
		{
			if (_indentSize == 0)
			{
				_w.Write("({0}", element.Name);
				foreach (var item in element.Parameters)
				{
					_w.Write(' ');
					item.Accept(this);
				}
				_w.Write(')');
			}
			else
			{
				Header(element);
				_w.WriteLine(element.Name);
				++_indent;
				int i = 0;
				foreach (var item in element.Parameters)
				{
					_w.Write(Indent);
					_w.Write("{0}. ", ++i);
					if (item != null)
						item.Accept(this);
					else
						_w.WriteLine();
				}
				--_indent;
			}
		}

		public override void Visit(EtConstant element)
		{
			if (_indentSize == 0)
			{
				_w.Write(element.Value ?? "<null>");
			}
			else
			{
				Header(element);
				_w.WriteLine(element.Value ?? "<null>");
			}
		}

		public override void Visit(EtIdentifier element)
		{
			if (_indentSize == 0)
			{
				_w.Write(element.Name);
			}
			else
			{
				Header(element);
				_w.WriteLine(element.Name);
			}
		}

		public override void Visit(EtMap element)
		{
			if (_indentSize == 0)
			{
				_w.Write("{");
				string pad = "";
				foreach (var item in element.Items)
				{
					_w.Write(pad);
					item.Accept(this);
					pad = " ";
				}
				_w.Write(')');
			}
			else
			{
				Header(element);
				_w.WriteLine();
				++_indent;
				int i = 0;
				foreach (var item in element.Items)
				{
					_w.Write(Indent);
					_w.Write("{0}. ", ++i);
					item.Accept(this);
				}
				--_indent;
			}
		}

		public override void Visit(EtParameter element)
		{
			if (_indentSize == 0)
			{
				_w.Write("{0}:", element.Name);
				element.Value.Accept(this);
			}
			else
			{
				Header(element);
				_w.Write("{0}: ", element.Name);
				element.Value.Accept(this);
			}
		}

		public override void Visit(EtSequense element)
		{
			if (_indentSize == 0)
			{
				_w.Write("[");
				string pad = "";
				foreach (var item in element.Items)
				{
					_w.Write(pad);
					item.Accept(this);
					pad = " ";
				}
				_w.Write(']');
			}
			else
			{
				Header(element);
				_w.WriteLine();
				++_indent;
				int i = 0;
				foreach (var item in element.Items)
				{
					_w.Write(Indent);
					_w.Write("{0}. ", ++i);
					item.Accept(this);
				}
				--_indent;
			}
		}

		public override void Visit(EtSyntaxError element)
		{
			_w.Write($"Syntax error at ({element.Position}) {element.Message}");
			if (_indentSize != 0)
				_w.WriteLine();
		}

		public override void Visit(EtUnaryExpression element)
		{
			if (_indentSize == 0)
			{
				_w.Write("({0} ", element.Operation);
				element.Operand.Accept(this);
				_w.Write(')');
			}
			else
			{
				Header(element);
				_w.WriteLine(element.Operation);
				++_indent;
				_w.Write(Indent);
				element.Operand.Accept(this);
				--_indent;
			}
		}

		public override void Visit(EtKeyValue element)
		{
			if (_indentSize == 0)
			{
				_w.Write("<");
				element.Key.Accept(this);
				_w.Write(",");
				element.Value.Accept(this);
				_w.Write(">");
			}
			else
			{
				Header(element);
				_w.WriteLine();
				++_indent;
				_w.Write(Indent);
				_w.Write("Key: ");
				element.Key.Accept(this);
				_w.Write(Indent);
				_w.Write("Value: ");
				element.Value.Accept(this);
				--_indent;
			}
		}
	}
}