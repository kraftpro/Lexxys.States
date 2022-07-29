using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Lexxys;

public static class TokenScope
{
	private static readonly ITokenScope __defaultScope = Token.CreateScope();
	private static readonly ConcurrentDictionary<string, ITokenScope> __tokenScopes = new ConcurrentDictionary<string, ITokenScope>(StringComparer.OrdinalIgnoreCase){ [""] = __defaultScope };

	public static ITokenScope Default => __defaultScope;

	public static ITokenScope Create(string? name) => __tokenScopes.GetOrAdd(name ?? "", o => Token.CreateScope());

	public static ITokenScope Scope(Token? token)
	{
		#pragma warning disable CA1062 // Validate arguments of public methods
		if (token.IsEmpty())
			return Default;

		Token root = token!;
		while (!root.Domain.IsEmpty())
			root = root.Domain;

		var scope = __tokenScopes.Values.First(o => o.Contains(root));
		return scope.Scope(token!);
	}

	public static IEnumerable<KeyValuePair<string, ITokenScope>> Enumerate() => Enumerable.AsEnumerable(__tokenScopes);
}
