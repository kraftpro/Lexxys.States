using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

#nullable enable

namespace State.Test1
{
	public enum IdValueType
	{
		Default,
		Start,
		End,
		Special
	}

	public class CommandFactory : TokenFactory
	{
		public CommandFactory(IEnumerable<Token> items) : base(items)
		{
		}
	}

	public class StateFactory : TokenFactory
	{
		public StateFactory(IEnumerable<Token> items) : base(items)
		{
		}
	}

	public class TransitionFactory : TokenFactory
	{
		public TransitionFactory(IEnumerable<Token> items) : base(items)
		{
		}
	}

	public readonly struct IdValue : IEquatable<IdValue>, IComparable<IdValue>, IComparable
	{
		public int Value { get; }
		public IdValueType Type { get; }
		public bool HasValue => Type == IdValueType.Default;

		public IdValue(int value, IdValueType type)
		{
			Value = value;
			Type = type;
		}

		public IdValue(int value)
		{
			Value = value;
			Type = IdValueType.Default;
		}

		public IdValue(IdValueType type)
		{
			Value = 0;
			Type = type;
		}

		public override bool Equals(object? obj) => obj is IdValue other && Equals(other);

		public override int GetHashCode() => Lexxys.HashCode.Join(Value, (int)Type);

		public override string ToString() => Type switch
		{
			IdValueType.Default => Value.ToString(),
			IdValueType.Start => Value == 0 ? "(S)" : $"({Value}S)",
			IdValueType.End => Value == 0 ? "(E)" : $"({Value}E)",
			_ => Value == 0 ? "(+)" : $"({Value})"
		};

		public bool Equals(IdValue other) => Value == other.Value && Type == other.Type;

		public int CompareTo(IdValue other) => Type == other.Type ? Value.CompareTo(other.Value) : -Type.CompareTo(other.Type);

		public int CompareTo(object? obj) => obj == null ? 1 : obj is IdValue other ? CompareTo(other) : throw new ArgumentTypeException(nameof(obj), obj.GetType());

		public static explicit operator int(IdValue value) => value.Value;
		public static explicit operator IdValue(int value) => new IdValue(value);

		public static explicit operator int?(IdValue value) => value.HasValue ? value.Value : null;
		public static explicit operator IdValue(int? value) => new IdValue(value.GetValueOrDefault(), value.HasValue ? IdValueType.Default : IdValueType.Special);

		public static bool operator ==(IdValue left, IdValue right) => left.Equals(right);
		public static bool operator !=(IdValue left, IdValue right) => !left.Equals(right);
		public static bool operator >(IdValue left, IdValue right) => left.CompareTo(right) > 0;
		public static bool operator >=(IdValue left, IdValue right) => left.CompareTo(right) >= 0;
		public static bool operator <(IdValue left, IdValue right) => left.CompareTo(right) < 0;
		public static bool operator <=(IdValue left, IdValue right) => left.CompareTo(right) <= 0;
	}

	public interface IToken
	{
		IdValue Id { get; }
		string Name { get; }
		string? Description { get; }
	}

	public class Token : IToken
	{
		public IdValue Id { get; }
		public string Name { get; }
		public string Description { get; }

		public Token(IdValue id, string name, string description)
		{
			Id = id;
			Name = name;
			Description = description;
		}
	}

	public interface ITokenFactory
	{
		IReadOnlyList<IToken> Items { get; }
		IToken? TryCreate(IdValue id, string name) => Items.FirstOrDefault(o => o.Id == id && o.Name == name);
		IToken? TryCreate(IdValue id) => Items.FirstOrDefault(o => o.Id == id);
		IToken? TryCreate(string name) => Items.FirstOrDefault(o => o.Name == name);
		IToken Create(IdValue id, string name) => TryCreate(id, name) ?? throw new InvalidOperationException($"Item {id}, \"{name}\" not found");
		IToken Create(IdValue id) => TryCreate(id) ?? throw new InvalidOperationException($"Item {id} not found");
		IToken Create(string name) => TryCreate(name) ?? throw new InvalidOperationException($"Item \"{name}\" not found");
	}

	public class TokenFactory : ITokenFactory
	{
		private IReadOnlyList<Token> _items;

		public TokenFactory(IEnumerable<Token> items)
		{
			_items = ReadOnly.WrapCopy(items ?? throw new ArgumentNullException(nameof(items)));
		}

		public IReadOnlyList<IToken> Items => _items;
	}

	public interface IEnm<T>
	{
		T Value { get; }
	}

	public readonly struct Enm<T> : IEquatable<Enm<T>>
		where T : IEquatable<T>
	{
		T Value { get; }

		public Enm(T value) => Value = value;

		public bool Equals(Enm<T> other) => Value.Equals(other.Value);

		public override bool Equals(object? obj) => obj is Enm<T> other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode();

		public override string ToString() => $"{{{Value}}}";
	}

	public interface IEnmDefinition<T>
	{
		IEnm<T> Item { get; }
		string Name { get; }
		string Description { get; }
	}

	public interface IEnmType<T>
	{
		IReadOnlyList<IEnmDefinition<T>> Items { get; }
	}

	public static class EnmTypeExtensions
	{
		public static bool TryConvert<T>(this IEnmType<T> type, object value, out IEnm<T>? result)
		{
			if (value is IEnm<T> enm)
			{
				result = enm;
				return true;
			}
			if (value is string str)
				return type.TryParse(str, out result);
			if (value is T val)
			{
				foreach (var item in type.Items)
				{
					if (Object.Equals(item.Item.Value, val))
					{
						result = item.Item;
						return true;
					}
				}
			}
			result = default;
			return false;
		}

		public static bool TryParse<T>(this IEnmType<T> type, string value, out IEnm<T>? result) => TryParse(type, value, false, out result);

		public static bool TryParse<T>(this IEnmType<T> type, string value, bool ignoreCase, out IEnm<T>? result)
		{
			var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			foreach (var item in type.Items)
			{
				if (String.Equals(item.Name, value, comparison))
				{
					result = item.Item;
					return true;
				}
			}
			result = default;
			return false;
		}
	}
}
