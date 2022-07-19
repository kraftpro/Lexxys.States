using System;

namespace Lexxys.States;

public static class TokenScopeStatechartExtensions
{
	public static ITokenScope WithTransitionDomain(this ITokenScope scope)
		=> (scope ?? throw new ArgumentNullException(nameof(scope))).WithDomain(scope.Token(TransitionConfig.TokenDomain));
}
