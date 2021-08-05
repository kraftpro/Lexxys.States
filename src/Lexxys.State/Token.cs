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

		public bool Contains(Token token) => !token.IsGlobal() && (this == token.Domain || Contains(token.Domain));

		public List<Token> GetPath()
		{
			var list = new List<Token>();
			var token = this;
			do
			{
				list.Add(token);
				token = token.Domain;
			} while (!token.IsEmpty());
			list.Reverse();
			return list;
		}

		public override string ToString()
		{
			if (Object.ReferenceEquals(this, Empty))
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
			private readonly ConcurrentDictionary<(Token Domain, int Id), Token> _issuedTokens ;

			public TokenScope(TokenScope? scope = null, Token? domain = null)
			{
				Domain = domain ?? Empty;
				_issuedTokens = scope?._issuedTokens ?? new ConcurrentDictionary<(Token Domain, int Id), Token>();
			}

			public Token Domain { get; }

			public ITokenScope WithDomain(Token domain)
				=> new TokenScope(this, domain);

			public Token Token(int id, string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				return _issuedTokens.GetOrAdd((domain ?? Domain, id), o => new Token(o.Id, name, description, o.Domain));
			}

			public Token Token(string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				if (domain == null)
					domain = Domain;

				int index = 0;
				foreach (var item in _issuedTokens.Where(o => o.Key.Domain == domain))
				{
					if (String.Equals(item.Value.Name, name, StringComparison.OrdinalIgnoreCase))
						return item.Value;
					if (index < item.Key.Id)
						index = item.Key.Id;
				}
				Token token;
				do
				{
					token = new Token(++index, name, description, domain);
				} while (!_issuedTokens.TryAdd((domain, index), token));
				return token;
			}

			public Token? Find(Token domain, int id)
				=> _issuedTokens.TryGetValue((domain, id), out var token) ? token: null;
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
		Token Domain { get; }
		ITokenScope WithDomain(Token domain);
		Token Token(int id, string name, string? description = null, Token? domain = null);
		Token Token(string name, string? description = null, Token? domain = null);
		Token? Find(Token domain, int id);
	}

	public static class TokenFactory
	{
		private static readonly ITokenScope __defaultFactory = Token.CreateScope();
		private static readonly ConcurrentDictionary<string, ITokenScope> __factories = new ConcurrentDictionary<string, ITokenScope>(StringComparer.OrdinalIgnoreCase);

		public static ITokenScope Default => __defaultFactory;

		public static ITokenScope Create(params string[] path)
			=> Create(path, path.Length);

		public static ITokenScope Create(ITokenScope parent, string name)
			=> Create(parent.Token(name));

		private static ITokenScope Create(Token domain)
		{
			var name = String.Join('.', domain.GetPath().Select(o => o.Name));
			return __factories.GetOrAdd(name, o => Token.CreateScope().WithDomain(domain));
		}

		private static ITokenScope Create(string[] path, int count)
		{
			if (count == 0)
				return Default;
			if (count == 1)
				return __factories.GetOrAdd(path[0], o => Token.CreateScope().WithDomain(Default.Token(o)));

			var pathName = String.Join('.', path, 0, count);
			if (__factories.TryGetValue(pathName, out var scope))
				return scope;

			scope = Create(path, count - 1);
			var token = scope.Token(path[count - 1]);
			return __factories.GetOrAdd(pathName, o => Token.CreateScope().WithDomain(token));
		}
	}

	public static class ITokenExtensjions
	{
		public static bool IsEmpty(this Token? token) => token == null || Object.ReferenceEquals(token, Token.Empty);
		public static bool IsGlobal(this Token? token) => token == null || Object.ReferenceEquals(token.Domain, Token.Empty);
	}

	public static class ITokenScopeExtensions
	{
		public static bool IsInScope(this ITokenScope scope, Token token)
			=> token.IsEmpty() || scope.Find(token.Domain, token.Id) == token;

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

		public static ITokenScope WithDomain(this ITokenScope scope, string domain)
			=> scope.WithDomain(scope.Token(domain));
	}
}
