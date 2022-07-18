using System;

namespace Lexxys;

public static class ITokenScopeExtensions
{
	public static bool IsInScope(this ITokenScope scope, Token token)
		=> token.IsEmpty() || scope.Find(token.Id, token.Domain) == token;

	public static Token Token(this ITokenScope scope, Enum value, string? description = null, Token? domain = null)
	{
		int id = ((IConvertible)value).ToInt32(null);
		string name = ((IConvertible)value).ToString(null);
		return scope.Token(id, name, description, domain);
	}

	public static Token Token(this ITokenScope scope, Type value, string? description = null, Token? domain = null)
		=> scope.Token(value.GetTypeName(), description, domain);

	public static Token Token(this ITokenScope scope, Token token, Token domain)
		=> scope.Token(token.Id, token.Name, token.Description, domain);

	public static ITokenScope WithDomain(this ITokenScope scope, string domain)
		=> scope.WithDomain(scope.Token(domain));

	public static ITokenScope WithDomain(this ITokenScope scope, Type domain)
		=> scope.WithDomain(scope.Token(domain.GetTypeName()));

	public static ITokenScope WithDomain(this ITokenScope scope, params string[] path)
	{
		if (path == null)
			return scope;
		for (int i = 0; i < path.Length; ++i)
		{
			scope = scope.WithDomain(path[i]);
		}
		return scope;
	}
}
