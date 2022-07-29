using System;
using System.Diagnostics.CodeAnalysis;

namespace Lexxys;

public static class TokenExtensions
{
	public static bool IsEmpty([NotNullWhen(false)] this Token? token) => token == null || ReferenceEquals(token, Token.Empty);
	public static bool IsGlobal([NotNullWhen(false)] this Token? token) => token == null || ReferenceEquals(token.Domain, Token.Empty);
}
