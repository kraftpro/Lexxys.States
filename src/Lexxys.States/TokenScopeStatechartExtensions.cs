using System;

namespace Lexxys.States;

public static class TokenScopeStatechartExtensions
{
	public static ITokenScope TransitionScope(this ITokenScope scope)
		=> (scope ?? throw new ArgumentNullException(nameof(scope))).Scope(scope.Token(TransitionConfig.TokenDomain));
}
