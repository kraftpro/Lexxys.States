using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Lexxys;
using Lexxys.Tokenizer;

namespace State.Eval
{
	public class Function : IItemParser
	{
		private readonly Parameter[] _parameters;

		public Function(string name, Type returnType, params Parameter[] parameters)
		{
			Name = name;
			ReturnType = returnType;
			_parameters = parameters;
			Parameters = ReadOnly.Wrap(_parameters);
		}

		public string Name { get; }
		public Type ReturnType { get; }
		public IReadOnlyList<Parameter> Parameters { get; }

		public EtExpression Parse(EtIdentifier itemId, IParser parser)
		{
			if (Parameters.Count == 0)
				return new EtCallExpression(itemId.Position, itemId.Name, new List<EtParameter>());

			var p0 = parser.ParseExpression();
			int n = _parameters.Length;
			if (!Parameters[0].IsCollection && p0.Type == EtExpressionType.Sequence)
			{
				var pp = p0 as EtSequense;
				Contract.Assert(pp != null);
				if (n < pp.Items.Count)
				{
					parser.Push(p0);
					return new EtSyntaxError(p0.Position, "Too many parameters ({1}) passed to function '{0}'", Name, pp.Items.Count);
				}
				n = pp.Items.Count;
				parser.Queue(pp.Items);
			}
			else
			{
				parser.Push(p0);
			}

			var parameters = new EtParameter[_parameters.Length];
			for (int i = 0; i < n; ++i)
			{
				//Parameter p = i < Parameters.Count ? Parameters[i] : Parameters[Parameters.Count - 1];
				Parameter p = _parameters[i];
				EtExpression x = parser.ParseExpression();
				if (x == null)
					break;
				int pat = x.Position;
				string name = null;
				var y = x as EtKeyValue;
				if (y != null)
				{
					name = y.GetName();
					if (name != null)
						x = y.Value;
				}
				if (name != null)
				{
					i = _parameters.FindIndex(o => o.Name == name);
					if (i < 0)
					{
						parser.Push(y);
						break;
					}
				}
				else if (p.ByNameOnly)
				{
					parser.Push(x);
					if (p.IsOptional)
						break;
					return new EtSyntaxError(x.Position, "Function '{0}' requires explicit parameter name for parameter '{1}'.", Name, p.Name);
				}
				parameters[i] = new EtParameter(pat, p.Name, x);
			}
			for (int j = 0; j < parameters.Length; j++)
			{
				var p = parameters[j];
				var q = _parameters[j];
				if (p == null && !q.IsOptional)
					return new EtSyntaxError(itemId.Position, "Missing required parameter '{1}' in function '{0}'.", Name, q.Name);
			}
			return new EtCallExpression(itemId.Position, Name, parameters.ToList());
		}

		public Expression Generate(EtExpression value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			var call = value as EtCallExpression;
			if (call == null)
				throw EX.ArgumentOutOfRange("value.Type", value.Type);
			if (call.Name != Name)
				throw EX.ArgumentOutOfRange("value.Name", call.Name);
			return null;

		}
	}

	public class Parameter
	{
		public string Name;
		public Type Type = typeof(object);
		public bool ByNameOnly;
		public bool IsOptional;
		public bool IsSequence;
		public EtConstant DefaultValue;

		public bool IsCollection => Type?.GetInterface("IEnumerable") != null;
	}
}
