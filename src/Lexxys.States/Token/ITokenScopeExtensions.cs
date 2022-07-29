using System;

namespace Lexxys;

public static class ITokenScopeExtensions
{
	public static Token Token(this ITokenScope scope, Enum value, string? description = null)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		int id = ((IConvertible)value).ToInt32(null);
		string name = ((IConvertible)value).ToString(null);
		return scope.Token(id, name, description);
	}

	public static Token Token(this ITokenScope scope, Type value, string? description = null)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return scope.Token(value.GetTypeName(), description);
	}

	public static Token Token(this ITokenScope scope, Token token)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (token is null)
			throw new ArgumentNullException(nameof(token));

		return scope.Token(token.Id, token.Name, token.Description);
	}

	public static ITokenScope Scope(this ITokenScope scope, string domain, string? description = null)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (domain is null)
			throw new ArgumentNullException(nameof(domain));

		return scope.Scope(scope.Token(domain, description));
	}

	public static ITokenScope Scope(this ITokenScope scope, Type domain, string? description = null)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (domain is null)
			throw new ArgumentNullException(nameof(domain));

		return scope.Scope(scope.Token(domain.GetTypeName(), description));
	}
}
