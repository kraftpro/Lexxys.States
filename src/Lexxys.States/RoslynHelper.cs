using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Lexxys.States;

internal static class RoslynHelper
{
	public static IEnumerable<MetadataReference> GetReferences<T>() => TypedReferences<T>.References;

	public static IEnumerable<string> GetImports() => __imports;
	private static readonly string[] __imports = { "System", "System.Collections.Generic", "System.Linq", "System.Text" };

	private static readonly Dictionary<string, MetadataReference> BasicReferences = GetBasicReferences();

	private static Dictionary<string, MetadataReference> GetBasicReferences()
	{
		var map = new Dictionary<string, MetadataReference>();
		AddReference(typeof(System.Object).Assembly);
		AddReference(typeof(Lexxys.States.Statechart<>).Assembly);
		AddReference(typeof(System.Linq.Enumerable).Assembly);
		AddReference(typeof(System.Collections.Generic.List<>).Assembly);
		AddReference(typeof(System.Text.StringBuilder).Assembly);

#if NETSTANDARD
		var location = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		var standard = Path.Combine(location, "netstandard.dll");
		if (File.Exists(standard))
			AddReference(Assembly.LoadFile(standard));
#endif
		var entry = Assembly.GetEntryAssembly();
		if (entry is not null)
		{
			AddReference(entry);
			foreach (var name in entry.GetReferencedAssemblies())
			{
				if (!map.ContainsKey(name.FullName))
					map.Add(name.FullName, MetadataReference.CreateFromFile(Assembly.Load(name).Location));
			}
		}
		return map;

		void AddReference(Assembly assembly)
		{
			var name = assembly.FullName;
			var location = assembly.Location;
			if (name == null || string.IsNullOrEmpty(location))
				return;
			if (!map.ContainsKey(name))
				map.Add(name, MetadataReference.CreateFromFile(location));
		}
	}

	private static class TypedReferences<T>
	{
		public static readonly List<MetadataReference> References = GetTypedReferences();

		private static List<MetadataReference> GetTypedReferences()
		{
			var a = typeof(T).Assembly;
			var name = a.FullName;
			var location = a.Location;
			var l = BasicReferences.Values.ToList();
			if (name != null && !string.IsNullOrEmpty(location) && !BasicReferences.ContainsKey(name))
				l.Add(MetadataReference.CreateFromFile(location));
			return l;
		}
	}
}
