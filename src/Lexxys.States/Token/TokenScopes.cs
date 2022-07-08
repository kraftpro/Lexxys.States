using System;
using System.Collections.Concurrent;

namespace Lexxys
{
	public static class TokenScope
	{
		private static readonly ITokenScope __defaultFactory = Token.CreateScope();
		private static readonly ConcurrentDictionary<string, ITokenScope> __factories = new ConcurrentDictionary<string, ITokenScope>(StringComparer.OrdinalIgnoreCase);

		public static ITokenScope Default => __defaultFactory;

		public static ITokenScope Create(string name)
		{
			if (name == null || name.Length <= 0)
				throw new ArgumentNullException(nameof(name));
			return __factories.GetOrAdd(name, o => Token.CreateScope());
		}

		public static ITokenScope Create(string name, params string[] path)
		{
			if (name == null || name.Length <= 0)
				throw new ArgumentNullException(nameof(name));
			return Create(name).WithDomain(path);
		}
	}
}
