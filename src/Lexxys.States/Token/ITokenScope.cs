using System;
using System.Collections.Generic;

namespace Lexxys;

public interface ITokenScope: IEnumerable<Token>
{
	/// <summary>
	/// A <see cref="Token"/> identifying the current scope.  <see cref="Token.Empty"/> for a root scope.
	/// </summary>
	Token Domain { get; }
	/// <summary>
	/// Gets an existing by <paramref name="id"/> or creates a new <see cref="Token"/>.
	/// </summary>
	/// <param name="id">Token ID to find.</param>
	/// <param name="name">Token name for a new <see cref="Token"/>.</param>
	/// <param name="description">Token description for a new <see cref="Token"/>.</param>
	/// <returns></returns>
	Token Token(int id, string? name = null, string? description = null);
	/// <summary>
	/// Gets an existing by <paramref name="name"/> or creates a new <see cref="Token"/>.
	/// </summary>
	/// <param name="name">Token name to find or to create a new <see cref="Token"/>.</param>
	/// <param name="description">Token description for a new <see cref="Token"/>.</param>
	/// <returns></returns>
	Token Token(string name, string? description = null);
	/// <summary>
	/// Returns an existing of creates a new <see cref="ITokenScope"/>.
	/// </summary>
	/// <param name="domain">The scope identifier</param>
	/// <returns></returns>
	ITokenScope Scope(Token domain);
}
