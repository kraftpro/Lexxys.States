using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lexxys.States
{
	public class Token
	{
		public static readonly Token Empty = new Token();
		public const int MinDynamicIndex = 1_000_000_000;

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
			Id = id;
			Name = name;
			Description = description;
			Domain = domain;
		}

		/// <summary>
		/// Determines if the current token is a domain for the specified <paramref name="token"/>.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public bool Contains(Token token) => token.Domain != Empty && (token.Domain == this || Contains(token.Domain));

		/// <summary>
		/// Returns this token with all domains this token belongs to.
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Returns full name of this token.
		/// </summary>
		/// <returns></returns>
		public string FullName() => String.Join('.', GetPath().Select(o => o.Name));

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

		/// <summary>
		/// Creates new <see cref="ITokenFactory"/>.
		/// </summary>
		/// <returns></returns>
		public static ITokenFactory CreateScope() => new TokenScope();

		public static implicit operator int (Token token) => token.Id;

		class TokenScope: ITokenFactory
		{
			private readonly ConcurrentDictionary<Token, TokenCollection> _tokens;
			private volatile int _index = MinDynamicIndex;

			public TokenScope()
			{
				_tokens = new ConcurrentDictionary<Token, TokenCollection>();
			}

			public Token Domain => Empty;

			public bool Contains(Token? token)
				=> token.IsEmpty() || (_tokens.TryGetValue(token!.Domain, out var tokens) && Array.IndexOf(tokens.Items, token) >= 0);

			public ITokenFactory WithDomain(Token domain)
				=> domain.IsEmpty() ? this: new TokenScopeWithDomain(this, domain);

			public Token? Find(int id, Token? domain = null)
				=> _tokens.TryGetValue(domain ?? Domain, out var tokens) ? tokens.Items.FirstOrDefault(o => o.Id == id): null;

			public Token Token(int id, string? name, string? description = null, Token? domain = null)
			{
				if (id >= MinDynamicIndex)
					throw new ArgumentOutOfRangeException(nameof(id), id, null);
				if (name != null && (name = name.Trim()).Length == 0)
					name = null;
				if (domain == null)
					domain = Domain;
				var items = _tokens.GetOrAdd(domain, o
					=> name == null ?
						throw new ArgumentNullException(nameof(name)):
						new TokenCollection(new [] { new Token(id, name, description, domain) }) );
				return items.GetOrAdd(o => o.Id == id, ()
					=> new States.Token(id, name ?? throw new ArgumentNullException(nameof(name)), description, domain));
			}

			public Token Token(string name, string? description = null, Token? domain = null)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					throw new ArgumentNullException(nameof(name));
				if (domain == null)
					domain = Domain;
				return Token(o => String.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase), () => new Token(Interlocked.Increment(ref _index), name, description, domain), domain);
			}

			public Token Token(Func<Token, bool> predicate, Func<Token> constructor, Token domain)
			{
				var items = _tokens.GetOrAdd(domain, o => new TokenCollection(new[] { constructor() }));
				return items.GetOrAdd(predicate, constructor);
			}

			class TokenCollection
			{
				volatile public Token[] Items;

				public TokenCollection(Token[] items) => Items = items;

				public Token GetOrAdd(Func<Token, bool> predicate, Func<Token> constructor) 
				{
					Token? value = null;
					for (;;)
					{
						var items = Items;
						var t = items.FirstOrDefault(predicate);
						if (t != null)
							return t;
						var tmp = new Token[items.Length + 1];
						Array.Copy(items, tmp, items.Length);
						if (value == null)
							value = constructor();
						tmp[tmp.Length - 1] = value;
						if (Interlocked.CompareExchange(ref Items, tmp, items) == items)
							return value;
					}
				}
			}

			class TokenScopeWithDomain: ITokenFactory
			{
				private readonly ITokenFactory _scope;

				public TokenScopeWithDomain(ITokenFactory scope, Token domain)
				{
					_scope = scope ?? throw new ArgumentNullException(nameof(scope));
					Domain = domain ?? throw new ArgumentNullException(nameof(domain));
				}

				public Token Domain { get; }

				public Token? Find(int id, Token? domain = null)
					=> _scope.Find(id, domain ?? Domain);

				public Token Token(int id, string? name = null, string? description = null, Token? domain = null)
					=> _scope.Token(id, name, description, domain ?? Domain);

				public Token Token(string name, string? description = null, Token? domain = null)
					=> _scope.Token(name, description, domain ?? Domain);

				public ITokenFactory WithDomain(Token domain)
					=> domain.IsEmpty() ? _scope: domain == Domain ? this: new TokenScopeWithDomain(_scope, domain);
			}
		}
	}

	public interface ITokenFactory
	{
		Token Domain { get; }
		ITokenFactory WithDomain(Token domain);
		Token Token(int id, string? name = null, string? description = null, Token? domain = null);
		Token Token(string name, string? description = null, Token? domain = null);
		Token? Find(int id, Token? domain = null);
	}

	public static class TokenFactory
	{
		private static readonly ITokenFactory __defaultFactory = Token.CreateScope();
		private static readonly ConcurrentDictionary<string, ITokenFactory> __factories = new ConcurrentDictionary<string, ITokenFactory>(StringComparer.OrdinalIgnoreCase);

		public static ITokenFactory Default => __defaultFactory;

		public static ITokenFactory Create(string name)
		{
			if (name == null || name.Length <= 0)
				throw new ArgumentNullException(nameof(name));
			return __factories.GetOrAdd(name, o => Token.CreateScope());
		}

		public static ITokenFactory Create(string name, params string[] path)
		{
			if (name == null || name.Length <= 0)
				throw new ArgumentNullException(nameof(name));
			return Create(Create(name), path);
		}

		public static ITokenFactory Create(ITokenFactory scope, params string[] path)
		{
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));
			if (path == null)
				return scope;
			for (int i = 0; i < path.Length; ++i)
			{
				scope = scope.WithDomain(path[i]);
			}
			return scope;
		}
	}

	public static class TokenExtensions
	{
		public static bool IsEmpty(this Token? token) => token == null || Object.ReferenceEquals(token, Token.Empty);
		public static bool IsGlobal(this Token? token) => token == null || Object.ReferenceEquals(token.Domain, Token.Empty);
	}

	public static class ITokenFactoryExtensions
	{
		public static bool IsInScope(this ITokenFactory scope, Token token)
			=> token.IsEmpty() || scope.Find(token.Id, token.Domain) == token;

		public static Token Token(this ITokenFactory scope, Enum value, string? description = null, Token? domain = null)
		{
			int id = ((IConvertible)value).ToInt32(null);
			string name = ((IConvertible)value).ToString(null);
			return scope.Token(id, name, description, domain);
		}

		public static Token Token(this ITokenFactory scope, Type value, string? description = null, Token? domain = null)
			=> scope.Token(value.GetTypeName(), description, domain);

		public static Token Token(this ITokenFactory scope, Token token, Token domain)
			=> scope.Token(token.Id, token.Name, token.Description, domain);

		public static ITokenFactory WithDomain(this ITokenFactory scope, string domain)
			=> scope.WithDomain(scope.Token(domain));

		public static ITokenFactory WithDomain(this ITokenFactory scope, Type domain)
			=> scope.WithDomain(scope.Token(domain.GetTypeName()));

		public static ITokenFactory WithTransitionDomain(this ITokenFactory scope)
			=> scope.WithDomain(scope.Token(TransitionConfig.TokenDomain));
	}
}
