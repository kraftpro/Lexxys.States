using System;

namespace Lexxys;

public static class TokenExtensions
{
	public static bool IsEmpty(this Token? token) => token == null || Object.ReferenceEquals(token, Token.Empty);
	public static bool IsGlobal(this Token? token) => token == null || Object.ReferenceEquals(token.Domain, Token.Empty);
}
