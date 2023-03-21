using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lexxys;

/// <summary>
/// Represents a token in the hierarchical structure.
/// </summary>
public sealed class Token
{
	public static readonly Token Empty = new Token();

	public int Id { get; }
	public string Name { get; }
	public string? Description { get; }
	public Token Domain { get; }

	private Token()
	{
		Name = "*";
		Domain = this;
	}

	/// <summary>
	/// Creates a new <see cref="Token" />
	/// </summary>
	/// <param name="id">Token ID</param>
	/// <param name="name">Token name</param>
	/// <param name="description">Optional description</param>
	/// <param name="domain">Parent Token</param>
	private Token(int id, string name, string? description, Token domain)
	{
		if (id == 0)
			throw new ArgumentOutOfRangeException(nameof(id), id, null);
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		if (domain is null)
			throw new ArgumentNullException(nameof(domain));

		Id = id;
		Name = name;
		Description = description;
		Domain = domain;
	}

	public bool IsEmpty => Id == 0;

	/// <summary>
	/// Determines if this token is a domain for the specified <paramref name="token"/>.
	/// </summary>
	/// <param name="token"></param>
	/// <returns></returns>
	public bool Contains(Token? token) => token is not null && (token.Domain == this || (token.Domain != Empty && Contains(token.Domain)));

	/// <summary>
	/// Returns this token with all domains this token belongs to.
	/// </summary>
	/// <returns></returns>
	public IReadOnlyList<Token> GetPath()
	{
		var list = new List<Token>();
		var token = this;
		do
		{
			list.Add(token);
			token = token.Domain;
		} while (!token.IsEmpty);
		list.Reverse();
		return list;
	}

	/// <summary>
	/// Returns full name of this token.
	/// </summary>
	/// <returns></returns>
	public string FullName() => String.Join(".", GetPath().Select(o => o.Name));

	public override string ToString() => ToString(true);

	public string ToString(bool includeDescription)
	{
		if (Object.ReferenceEquals(this, Empty))
			return "(empty)";
		var text = new StringBuilder();
		if (Id > 0)
			text.Append(Id).Append('.');
		text.Append(Name);
		if (includeDescription && Description is not null)
			text.Append(" - ").Append(Description);
		return text.ToString();
	}

	public int ToInt32() => Id;

	/// <summary>
	/// Creates new <see cref="ITokenScope"/>.
	/// </summary>
	/// <returns></returns>
	internal static ITokenScope CreateScope() => new TokenScope();

	public static implicit operator int (Token token) => token?.Id ?? 0;

	class TokenScope: ITokenScope
	{
		private readonly ConcurrentDictionary<Token, TokenCollection> _tokens;
		private volatile int _index;

		public TokenScope()
		{
			_tokens = new ConcurrentDictionary<Token, TokenCollection>();
		}

		public Token Domain => Empty;

		public ITokenScope Scope(Token domain) =>
			domain is null ?
				throw new ArgumentNullException(nameof(domain)):
			domain.IsEmpty ?
				this:
			Contains(domain) ?
				new ScopedTokenScope(this, domain):
				throw new ArgumentOutOfRangeException(nameof(domain), domain, null);

		public Token Token(int id, string? name = null, string? description = null) => CreateToken(id, name, description, Domain);

		public Token Token(string name, string? description = null) => CreateToken(name, description, Domain);

		private bool Contains(Token token) => _tokens.TryGetValue(token.Domain, out var tokens) && tokens.Contains(token);

		private Token CreateToken(int id, string? name, string? description, Token domain)
		{
			if (id <= 0)
				throw new ArgumentOutOfRangeException(nameof(id), id, null);
			if (name is not null && (name = name.Trim()).Length == 0)
				name = null;
			return CreateToken(
				o => o.Id == id,
				() => new Token(id, name ?? throw new ArgumentNullException(nameof(name)), description, domain),
				domain);
		}

		private Token CreateToken(string name, string? description, Token domain)
		{
			if (name is null || (name = name.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(name));
			return CreateToken(
				o => String.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase),
				() => new Token(Interlocked.Decrement(ref _index), name, description, domain),
				domain);
		}

		private Token CreateToken(Func<Token, bool> predicate, Func<Token> constructor, Token domain)
		{
			Token? item = null;
			var items = _tokens.GetOrAdd(domain, o => new (new[] { item = constructor() }));
			#pragma warning disable CA1508 // Avoid dead conditional code
			return item ?? items.GetOrAdd(predicate, constructor);
			#pragma warning restore CA1508 // Avoid dead conditional code
		}

		public IEnumerator<Token> GetEnumerator() => _tokens.TryGetValue(Domain, out var items) ? items.GetEnumerator(): Enumerable.Empty<Token>().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		class TokenCollection: IEnumerable<Token>
		{
			volatile private Token[] _items;

			public TokenCollection(Token[] items) => _items = items;

            public bool Contains(Token token) => Array.IndexOf(_items, token) >= 0;

            public Token GetOrAdd(Func<Token, bool> predicate, Func<Token> constructor) 
			{
				Token? value = null;
				for (;;)
				{
					var items = _items;
					var t = items.FirstOrDefault(predicate);
					if (t is not null)
						return t;
					var tmp = new Token[items.Length + 1];
					Array.Copy(items, tmp, items.Length);
					value ??= constructor();
					tmp[items.Length] = value;
					if (Interlocked.CompareExchange(ref _items, tmp, items) == items)
						return value;
				}
			}

			public IEnumerator<Token> GetEnumerator() => ((IEnumerable<Token>)_items).GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
		}

		class ScopedTokenScope: ITokenScope, IEquatable<ScopedTokenScope>
		{
			private readonly TokenScope _scope;

			public ScopedTokenScope (TokenScope scope, Token domain)
			{
				_scope = scope ?? throw new ArgumentNullException(nameof(scope));
				Domain = domain ?? throw new ArgumentNullException(nameof(domain));
			}

			public Token Domain { get; }

			public Token Token(int id, string? name = null, string? description = null) => _scope.CreateToken(id, name, description, Domain);

			public Token Token(string name, string? description = null) => _scope.CreateToken(name, description, Domain);

			public ITokenScope Scope(Token domain)
				=> domain == Domain ? this:
				Domain.Contains(domain) ? new ScopedTokenScope (_scope, domain):
				throw new ArgumentOutOfRangeException(nameof(domain));

			public IEnumerator<Token> GetEnumerator() => _scope._tokens[Domain].GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _scope._tokens[Domain].GetEnumerator();

			public override bool Equals(object? obj) => obj is ScopedTokenScope scope && Equals(scope);

			public bool Equals(ScopedTokenScope? other) => other is not null && _scope == other._scope && Domain == other.Domain;

			public override int GetHashCode() => HashCode.Join(_scope.GetHashCode(), Domain.GetHashCode());
		}
	}
}
