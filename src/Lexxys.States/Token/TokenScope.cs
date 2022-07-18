using System;
using System.Collections.Concurrent;

namespace Lexxys;

public static class TokenScope
{
	private static readonly ITokenScope __defaultScope = Token.CreateScope();
	private static readonly ConcurrentDictionary<string, ITokenScope> __tokenScopes = new ConcurrentDictionary<string, ITokenScope>(StringComparer.OrdinalIgnoreCase){ [""] = __defaultScope };

	public static ITokenScope Default => __defaultScope;

	public static ITokenScope Create(string name) => __tokenScopes.GetOrAdd(name ?? "", o => Token.CreateScope());

	public static ITokenScope Create(string name, params string[] path) => Create(name).WithDomain(path);
}
