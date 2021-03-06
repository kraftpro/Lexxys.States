using System;

using Lexxys;

namespace Lexxys;

public static class ITokenScopeExtensions
{
	public static bool IsInScope(this ITokenScope scope, Token token)
	{
		if (token.IsEmpty())
			return true;
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (token is null)
			throw new ArgumentNullException(nameof(token));

		return scope.Find(token.Id, token.Domain) == token;
	}

	public static Token Token(this ITokenScope scope, Enum value, string? description = null, Token? domain = null)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		int id = ((IConvertible)value).ToInt32(null);
		string name = ((IConvertible)value).ToString(null);
		return scope.Token(id, name, description, domain);
	}

	public static Token Token(this ITokenScope scope, Type value, string? description = null, Token? domain = null)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return scope.Token(value.GetTypeName(), description, domain);
	}

	public static Token Token(this ITokenScope scope, Token token, Token domain)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (token is null)
			throw new ArgumentNullException(nameof(token));

		return scope.Token(token.Id, token.Name, token.Description, domain);
	}

	public static ITokenScope WithDomain(this ITokenScope scope, string domain)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (domain is null)
			throw new ArgumentNullException(nameof(domain));

		return scope.WithDomain(scope.Token(domain));
	}

	public static ITokenScope WithDomain(this ITokenScope scope, Type domain)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (domain is null)
			throw new ArgumentNullException(nameof(domain));

		return scope.WithDomain(scope.Token(domain.GetTypeName()));
	}

	public static ITokenScope WithDomain(this ITokenScope scope, params string[]? path)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));

		if (path == null)
			return scope;
		for (int i = 0; i < path.Length; ++i)
		{
			scope = scope.WithDomain(path[i]);
		}
		return scope;
	}
}
