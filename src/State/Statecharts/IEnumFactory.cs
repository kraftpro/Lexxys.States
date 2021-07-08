using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace State.Statecharts
{
	public interface IEnumClass
	{
		IReadOnlyList<IEnum> GetItems();
	}

	public static class IEnumFactoryExtensions
	{
		public static bool TryParse(this IEnumClass factory, string value, out IEnum resutlt)
			=> TryParse(factory, value, false, out resutlt);

		public static bool TryParse(this IEnumClass factory, string value, bool ignoreCase, out IEnum resutlt)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
			resutlt = factory.GetItems().FirstOrDefault(o => String.Equals(value, o.Name, comparison));
			return resutlt != null;
		}
	}

	public class EnumFactory<TEnum, TValue>
		where TEnum: struct, IEnum<TValue>
		where TValue: IEquatable<TValue>
	{
		private TEnum[] _items;

		public EnumFactory(IEnumerable<TEnum> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			_items = items.ToArray();
		}

		public TEnum Create(TValue value) => _items.FirstOrDefault(o => Object.Equals(o.Value, value));
		public TEnum Create(string name, bool ignoreCase = false) => _items.FirstOrDefault(o => String.Equals(o.Name, name, ignoreCase ? StringComparison.Ordinal: StringComparison.OrdinalIgnoreCase));
	}

	public class EnumFactory
	{
		private readonly IReadOnlyList<IEnum> _items;

		public EnumFactory(IEnumerable<IEnum> items) => _items = ReadOnly.WrapCopy(items ?? throw new ArgumentNullException(nameof(items)));

		public IEnum Create(int value) => _items.FirstOrDefault(o => o.Value == value);
		public IEnum Create(string name, bool ignoreCase = false) => _items.FirstOrDefault(o => String.Equals(o.Name, name, ignoreCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
	}
}
