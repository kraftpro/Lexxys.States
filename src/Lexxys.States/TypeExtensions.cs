using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States
{
	static class TypeExtensions
	{
		public static string GetTypeName(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (type.HasElementType)
				return GetTypeName(type.GetElementType() ?? typeof(void)) + (type.IsArray ? "[]" : type.IsByRef ? "^" : "*");
			if (!type.IsGenericType)
				return __builtinTypes.TryGetValue(type, out var s) ? s : type.Name;
			var text = new StringBuilder();
			text.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
			char c = '<';
			foreach (var item in type.GetGenericArguments())
			{
				text.Append(c).Append(GetTypeName(item));
				c = ',';
			}
			text.Append('>');
			return text.ToString();
		}

		private static Dictionary<Type, string> __builtinTypes = new()
		{
			{ typeof(void), "void" },
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(char), "char" },
			{ typeof(short), "short" },
			{ typeof(ushort), "ushort" },
			{ typeof(int), "int" },
			{ typeof(uint), "uint" },
			{ typeof(long), "long" },
			{ typeof(ulong), "ulong" },
			{ typeof(float), "float" },
			{ typeof(double), "double" },
			{ typeof(decimal), "decimal" },
			{ typeof(string), "string" },
		};
	}
}
