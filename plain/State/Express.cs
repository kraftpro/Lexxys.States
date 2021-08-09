#if false
using ExpressionEvaluator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ExpressionEvaluator.Parser;
using Lexxys;
using State.Eval;
using ExpressionCompiler = ExpressionEvaluator.ExpressionCompiler;

namespace State
{
	public class Express: ExpressionCompiler
	{
		public Express()
		{
			Parser = new AntlrParser();
		}

		public Express(string expression)
		{
			Parser = new AntlrParser(expression);
		}

		public object Global
		{
			set { Parser.Global = value; }
		}

		private IEnumerable<ParameterExpression> GetParameters(IReadOnlyDictionary<string, Type> args)
		{
			if (args == null || args.Count == 0)
				return new ParameterExpression[0];
			if (TypeRegistry == null)
				TypeRegistry = new TypeRegistry();
			List<ParameterExpression> parameters = args.Select(o => Expression.Parameter(o.Value, o.Key)).ToList();
			foreach (var param in parameters)
			{
				TypeRegistry.RegisterSymbol(param.Name, param);
			}
			return parameters;
		}

		// Func<T1, T2, ..., TR>
		public Delegate CompileFunction(IReadOnlyDictionary<string, Type> args = null)
		{
			var parms = GetParameters(args);
			Expression = BuildTree();
			return Expression.Lambda(Expression, parms).Compile();
		}

		// Action<T1, T2, ...>
		public Delegate CompileAction(IReadOnlyDictionary<string, Type> args = null)
		{
			var parms = GetParameters(args);
			Expression = BuildTree(null, true);
			return Expression.Lambda(Expression, parms).Compile();
		}

		// Func<T0, T1, T2, ..., TR>
		public Delegate ScopeCompileFunction(Type scopeType, IReadOnlyDictionary<string, Type> args = null)
		{
			if (scopeType == null)
				throw new ArgumentNullException("scopeType");
			var scope = Expression.Parameter(scopeType, "scope");
			var parms = new List<ParameterExpression> { scope };
			parms.AddRange(GetParameters(args));
			Expression = BuildTree(scope);
			return Expression.Lambda(Expression, parms).Compile();
		}

		// Action<T>
		public Delegate ScopeCompileAction(Type scopeType, IReadOnlyDictionary<string, Type> args = null)
		{
			if (scopeType == null)
				throw new ArgumentNullException("scopeType");
			var scope = Expression.Parameter(scopeType, "scope");
			var parms = new List<ParameterExpression> { scope };
			parms.AddRange(GetParameters(args));
			Expression = BuildTree(scope, true);
			return Expression.Lambda(Expression, parms).Compile();
		}

		public Func<T1, TR> ScopeCompileFunction<T1, TR>()
		{
			return (Func<T1, TR>)ScopeCompileFunction(typeof(T1));
		}

		public Action<T> ScopeCompileAction<T>()
		{
			return (Action<T>)ScopeCompileAction(typeof(T));
		}

		protected override void ClearCompiledMethod()
		{
		}
	}
}
#endif