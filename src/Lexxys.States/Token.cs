using Lexxys;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
			private readonly ConcurrentDictionary<Token, TokenCollection> _tokens;
			private volatile int _index = MinDynamicIndex;

			public TokenScope(TokenScope? scope = null, Token? domain = null)
			{
				Domain = domain ?? Empty;
				_tokens = scope?._tokens ?? new ConcurrentDictionary<Token, TokenCollection>();
			}

			public Token Domain { get; }

			public ITokenScope WithDomain(Token domain)
				=> new TokenScope(this, domain);

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
		}
	}

	public interface ITokenScope
	{
		Token Domain { get; }
		ITokenScope WithDomain(Token domain);
		Token Token(int id, string? name = null, string? description = null, Token? domain = null);
		Token Token(string name, string? description = null, Token? domain = null);
		Token? Find(int id, Token? domain = null);
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

	public static class ITokenExtensions
	{
		public static bool IsEmpty(this Token? token) => token == null || Object.ReferenceEquals(token, Token.Empty);
		public static bool IsGlobal(this Token? token) => token == null || Object.ReferenceEquals(token.Domain, Token.Empty);
	}

	public static class ITokenScopeExtensions
	{
		public static bool IsInScope(this ITokenScope scope, Token token)
			=> token.IsEmpty() || scope.Find(token.Id, token.Domain) == token;

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
