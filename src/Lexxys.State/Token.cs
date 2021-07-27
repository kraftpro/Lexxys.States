
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

		private Token(int id, string name, string? description, Token domain)
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

		public static ITokenScope CreateScope() => new TokenScope();

		private class TokenScope: ITokenScope
		{
			private readonly ConcurrentDictionary<(Token Domain, int Id), Token> __issuedTokens = new();

			public Token Create(int id, string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				if (domain == null)
					domain = Empty;
				else if (!this.IsInScope(domain))
					throw new ArgumentOutOfRangeException(nameof(domain));
				return __issuedTokens.GetOrAdd((domain, id), o => new Token(o.Id, name, description, o.Domain));
			}

			public Token Create(string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				if (domain == null)
					domain = Empty;
				else if (!this.IsInScope(domain))
					throw new ArgumentOutOfRangeException(nameof(domain));
				int index = -1;
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
				=> __issuedTokens.TryGetValue((domain, id), out var token) ? null: token;
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

	public interface ITokenScope
	{
		Token Create(int id, string name, string? description = null, Token? domain = null);
		Token Create(string name, string? description = null, Token? domain = null);
		Token? Find(Token domain, int id);
	}

	public static class TokenFactory
	{
		private static readonly ITokenScope __defaultFactory = Token.CreateScope();
		private static readonly ConcurrentDictionary<string, ITokenScope> __factories = new ConcurrentDictionary<string, ITokenScope>();

		public static ITokenScope Default => __defaultFactory;

		public static ITokenScope GetFactory(string name) => __factories.GetOrAdd(name, o => Token.CreateScope());

		public static bool IsInScope(this ITokenScope scope, Token token)
				=> token is null || token == Token.Empty || scope.Find(token.Domain, token.Id) == token;
	}
}
