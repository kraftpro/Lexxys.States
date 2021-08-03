using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lexxys.States
{
	public class Token
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

		internal Token(int id, string name, string? description, Token domain)
		{
			Id = id;
			Name = name;
			Description = description;
			Domain = domain;
		}

		public bool IsEmpty => Object.ReferenceEquals(this, Empty);

		public bool IsGlobal => Object.ReferenceEquals(this.Domain, Empty);

		public bool Contains(Token token) => !token.IsGlobal && (this == token.Domain || Contains(token.Domain));

		public IEnumerable<Token> GetPath() => new PathCollection(this);

		public override string ToString()
		{
			if (IsEmpty)
				return "(empty)";
			var text = new StringBuilder();
			if (Id != 0)
				text.Append(Id).Append('.');
			text.Append(Name);
			if (Description != null)
				text.Append(" - ").Append(Description);
			return text.ToString();
		}

		public static ITokenScope CreateScope() => new TokenScope();

		public static implicit operator int (Token token) => token.Id;

		private class TokenScope: ITokenScope
		{
			private readonly ConcurrentDictionary<(Token Domain, int Id), Token> __issuedTokens = new();

			public Token Token(int id, string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				if (domain == null)
					domain = Empty;
				//else if (!this.IsInScope(domain))
				//	throw new ArgumentOutOfRangeException(nameof(domain));
				return __issuedTokens.GetOrAdd((domain, id), o => new Token(o.Id, name, description, o.Domain));
			}

			public Token Token(string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				if (domain == null)
					domain = Empty;

				int index = 0;
				foreach (var item in __issuedTokens.Where(o => o.Key.Domain == domain))
				{
					if (item.Value.Name == name)
						return item.Value;
					if (index < item.Key.Id)
						index = item.Key.Id;
				}
				Token token;
				do
				{
					token = new Token(++index, name, description, domain);
				} while (!__issuedTokens.TryAdd((domain, index), token));
				return token;
			}

			public Token? Find(Token domain, int id)
				=> __issuedTokens.TryGetValue((domain, id), out var token) ? token: null;
		}

		private class PathCollection: IEnumerable<Token>
		{
			private readonly Token _token;

			public PathCollection(Token token) => _token = token ?? Empty;

			public IEnumerator<Token> GetEnumerator() => new Enumerator(_token);

			IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_token);

			private struct Enumerator: IEnumerator<Token>
			{
				private readonly Token _token;
				private Token? _part;

				public Enumerator(Token token)
				{
					_token = token ?? Empty;
					_part = null;
				}

				public Token Current => _part ?? throw new InvalidOperationException();
				object IEnumerator.Current => _part ?? throw new InvalidOperationException();

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					if (_part == null)
						_part = _token;
					else
						_part = _part.Domain;
					return !_part.IsEmpty;
				}

				public void Reset()
				{
					_part = null;
				}
			}
		}
	}

	//public class TokenType: Token
	//{
	//	private readonly ConcurrentDictionary<(int Id, string Name), Token> _tokens;

	//	private TokenType((int Id, string Name) value, string? description, TokenType? type)
	//		: base(value.Id, value.Name, description, type ?? States.Token.Empty)
	//		=> _tokens = new ConcurrentDictionary<(int Id, string Name), Token>();

	//	public TokenType(string name, string? description = null, TokenType? type = null)
	//		: this((0, name), description, type)
	//	{
	//	}

	//	public TokenType(int id, string name, string? description = null, TokenType? type = null)
	//		: this((id, name), description, type)
	//	{
	//	}

	//	public TokenType(Enum value, string? description = null, TokenType? type = null)
	//		: this(ExtractEnum(value), description, type)
	//	{
	//	}

	//	public ICollection<Token> Items => _tokens.Values;

	//	public Token Token(int id, string name, string? description)
	//		=> _tokens.GetOrAdd((id, name), o => new Token(o.Id, o.Name, description, this));

	//	private static (int Id, string Name) ExtractEnum(Enum value)
	//		=> (((IConvertible)value).ToInt32(null), ((IConvertible)value).ToString(null));
	//}

	public interface ITokenScope
	{
		Token Token(int id, string name, string? description = null, Token? domain = null);
		Token Token(string name, string? description = null, Token? domain = null);
		Token? Find(Token domain, int id);
	}

	public static class TokenFactory
	{
		private static readonly ITokenScope __defaultFactory = Token.CreateScope();
		private static readonly ConcurrentDictionary<string, ITokenScope> __factories = new ConcurrentDictionary<string, ITokenScope>();

		public static ITokenScope Default => __defaultFactory;

		public static ITokenScope Create(Token domain)
		{
			var name = String.Join(',', domain.GetPath().Select(o => o.Name));
			return __factories.GetOrAdd(name, o => new TokenScopeWithDomain(Token.CreateScope(), domain));
		}

		public static ITokenScope Create(params string[] path)
		{
			//var scope = Default;
			//var text = new StringBuilder();
			//foreach (var name in path)
			//{
			//	text.Append(name);
			//	var token = scope.Token(name);
			//	scope = __factories.GetOrAdd(text.ToString(), o => new TokenScopeWithDomain(Token.CreateScope(), token));
			//	text.Append('.');
			//}
			//return scope;
			return Create(path, path.Length);
		}

		private static ITokenScope Create(string[] path, int count)
		{
			if (count == 0)
				return Default;
			var pathName = String.Join('.', path, 0, count);
			if (__factories.TryGetValue(pathName, out var scope))
				return scope;

			scope = Create(path, count - 1);
			var token = scope.Token(path[count - 1]);
			return __factories.GetOrAdd(pathName, o => new TokenScopeWithDomain(Token.CreateScope(), token));
		}

		public static ITokenScope Create(ITokenScope parent, string name)
			=> Create(parent.Token(name));

		public static ITokenScope Create(string? domain = null)
			=> __factories[Guid.NewGuid().ToString("N")] = domain == null ? Token.CreateScope() : new TokenScopeWithDomain(Token.CreateScope(), Default.Token(domain));

		//public static ITokenScope GetFactory(string name)
		//	=> __factories.GetOrAdd(name, o => Token.CreateScope());

		//public static ITokenScope GetFactory(Token domain)
		//	=> __factories.GetOrAdd(domain.Name, o => new TokenScopeWithDomain(Token.CreateScope(), domain));

		//public static ITokenScope GetFactory(string name, Token domain)
		//	=> __factories.GetOrAdd(name, o => new TokenScopeWithDomain(Token.CreateScope(), domain));

		//public static ITokenScope GetFactory(string name, string domain)
		//	=> __factories.GetOrAdd(name, o => new TokenScopeWithDomain(Token.CreateScope(), Default.Token(domain)));

		class TokenScopeWithDomain : ITokenScope
		{
			public TokenScopeWithDomain(ITokenScope scope, Token domain)
			{
				Scope = scope;
				Domain = domain;
			}

			public ITokenScope Scope { get; }
			public Token Domain { get; }

			public Token Token(int id, string name, string? description = null, Token? domain = null)
				=> Scope.Token(id, name, description, domain ?? Domain);

			public Token Token(string name, string? description = null, Token? domain = null)
				=> Scope.Token(name, description, domain ?? Domain);

			public Token? Find(Token domain, int id)
				=> Scope.Find(domain, id);
		}
	}

	public static class ITokenScopeExtensions
	{
		public static bool IsInScope(this ITokenScope scope, Token token)
			=> token is null || token == States.Token.Empty || scope.Find(token.Domain, token.Id) == token;

		public static Token Token(this ITokenScope scope, Enum value, string? description = null, Token? domain = null)
		{
			int id = ((IConvertible)value).ToInt32(null);
			string name = ((IConvertible)value).ToString(null);
			return scope.Token(id, name, description, domain);
		}

		public static Token Token(this ITokenScope scope, Token token, Token domain)
		{
			return scope.Token(token.Id, token.Name, token.Description, domain);
		}

		public static ITokenScope WithDomain(this ITokenScope scope, Token domain)
			=> new TokenScopeWithDomain(scope, domain);

		class TokenScopeWithDomain: ITokenScope
		{
			public TokenScopeWithDomain(ITokenScope scope, Token domain)
			{
				Scope = scope ?? throw new ArgumentNullException(nameof(scope));
				Domain = domain ?? throw new ArgumentNullException(nameof(domain));
			}

			public ITokenScope Scope { get; }
			public Token Domain { get; }

			public Token Token(int id, string name, string? description = null, Token? domain = null)
				=> Scope.Token(id, name, description, domain ?? Domain);

			public Token Token(string name, string? description = null, Token? domain = null)
				=> Scope.Token(name, description, domain ?? Domain);

			public Token? Find(Token domain, int id)
				=> Scope.Find(domain, id);
		}
	}
}
