using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Lexxys.States;

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using Lexxys;

using Xml;

public class StatechartConfig
{
	public int? Id { get; }
	public string Name { get; }
	public string? Description { get; }
	public string? OnLoad { get; }
	public string? OnUpdate { get; }
	public string? ChartStart { get; }
	public string? ChartFinish { get; }
	public string? StateEnter { get; }
	public string? StateEntered { get; }
	public string? StatePassthrough { get; }
	public string? StateExit { get; }
	public string? InitialState { get; }
	public IReadOnlyCollection<StateConfig> States { get; }
	public IReadOnlyCollection<TransitionConfig> Transitions { get; }

	public StatechartConfig(int? id, string name, string? description, string? onLoad, string? onUpdate, string? chartStart, string? chartFinish, string? stateEnter, string? stateEntered, string? statePassthrough, string? stateExit, string? initialState, IReadOnlyCollection<StateConfig>? states, IReadOnlyCollection<TransitionConfig>? transitions)
	{
		if (name == null || (name = name.Trim()).Length == 0)
			throw new ArgumentNullException(nameof(name));

		(Id, Name) = FixName(id, name);
		Description = description;
		OnLoad = onLoad;
		OnUpdate = onUpdate;
		ChartStart = chartStart;
		ChartFinish = chartFinish;
		StateEnter = stateEnter;
		StateEntered = stateEntered;
		StatePassthrough = statePassthrough;
		StateExit = stateExit;
		InitialState = initialState;
		States = states ?? Array.Empty<StateConfig>();
		Transitions = transitions ?? Array.Empty<TransitionConfig>();

		ValidateTokens(States);
	}

	private static void ValidateTokens(IReadOnlyCollection<StateConfig> states)
	{
		var dupId = states.Where(o => o.Id.HasValue).GroupBy(o => o.Id).FirstOrDefault(o => o.Count() > 1);
		if (dupId != null)
			throw new InvalidOperationException($"State ID value is not unique ({dupId.Key})");
		var dupName = states.Where(o => o.Name != null).GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(o => o.Count() > 1);
		if (dupName != null)
			throw new InvalidOperationException($"State Name value is not unique ({dupName.Key})");
	}

	public Statechart<T> Create<T>(ITokenScope? scope = null, Func<string, IStateAction<T>?>? actionBuilder = null, Func<string, IStateCondition<T>?>? conditionBuilder = null)
	{
		scope ??= TokenScope.Create("statechart");
		actionBuilder ??= StateAction.CSharpScript<T>;
		conditionBuilder ??= StateCondition.CSharpScript<T>;
		var token = CreateToken(scope, Id, Name, Description);
		scope = scope.WithDomain(token);

		var states = States.Select(o => o.Create<T>(scope, actionBuilder, conditionBuilder)).ToList();
		var transitions = WithInitial().Select(o => o.Create<T>(scope, states, actionBuilder, conditionBuilder));
		var chart = new Statechart<T>(token, states, transitions);

		if (!String.IsNullOrEmpty(OnLoad))
			chart.OnLoad += actionBuilder(OnLoad!);
		if (!String.IsNullOrEmpty(OnUpdate))
			chart.OnUpdate += actionBuilder(OnUpdate!);
		if (!String.IsNullOrEmpty(ChartStart))
			chart.ChartStart += actionBuilder(ChartStart!);
		if (!String.IsNullOrEmpty(ChartFinish))
			chart.ChartFinish += actionBuilder(ChartFinish!);
		if (!String.IsNullOrEmpty(StateEnter))
			chart.StateEnter += actionBuilder(StateEnter!);
		if (!String.IsNullOrEmpty(StateEntered))
			chart.StateEntered += actionBuilder(StateEntered!);
		if (!String.IsNullOrEmpty(StatePassthrough))
			chart.StatePassthrough += actionBuilder(StatePassthrough!);
		if (!String.IsNullOrEmpty(StateExit))
			chart.StateExit += actionBuilder(StateExit!);
		return chart;

		static Token CreateToken(ITokenScope scope, int? id, string name, string? description)
			=> id == null ? scope.Token(name, description) : scope.Token(id.GetValueOrDefault(), name, description);
	}

	private IEnumerable<TransitionConfig> WithInitial()
	{
		if (States.Count == 0)
			return Transitions;

		var first = States.First();
		var initial = !String.IsNullOrEmpty(InitialState) ? InitialState: first.Id?.ToString(CultureInfo.InvariantCulture) ?? first.Name;
		return Transitions.Prepend(new TransitionConfig(destination: initial!));
	}

	public void GenerateCode(TextWriter writer, string objName, string? methodPrefix = null, string? visibility = null, string? indent = null, string? tab = null, bool nullable = false)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		indent ??= "\t";
		tab ??= "\t";
		visibility ??= "internal";
		string q = nullable ? "?": "";
		string methodName = GetMethodName(Name, methodPrefix ?? "Create");
		var stateNames = new Dictionary<StateConfig, string>();
		var transitionNames = new Dictionary<TransitionConfig, string>();
		var chartMethods = States.SelectMany(o => o.Charts ?? Array.Empty<StatechartConfig>()).ToDictionary(o => o, o => GetMethodName(o.Name, methodName + "_"));

		writer.WriteLine($"{indent}{visibility} static Statechart<{objName}> {methodName}(ITokenScope{q} root = null)");
		writer.WriteLine($"{indent}{{");
		var indent0 = indent;
		indent += tab;
		writer.WriteLine($"{indent}root ??= TokenScope.Create(\"statechart\");");
		writer.WriteLine($"{indent}var token = root.Token({S(Name)});");
		writer.WriteLine($"{indent}var s = root.WithDomain(token);");
		writer.WriteLine($"{indent}var t = s.WithTransitionDomain();");
		if (States.Any(o => o.Charts?.Count > 0))
			writer.WriteLine($"{indent}Token tk;");
		writer.WriteLine();

		int index = 0;
		foreach (var state in States)
		{
			string name = $"s{++index}";
			stateNames.Add(state, name);
			state.GenerateCode(writer, objName, name, "s", "tk", chartMethods, indent);
		}

		index = 0;
		foreach (var transition in WithInitial())
		{
			string name = $"t{++index}";
			transition.GenerateCode(writer, objName, name, stateNames, indent);
			transitionNames.Add(transition, name);
		}

		writer.WriteLine($"{indent}var statechart = new Statechart<{objName}>(token, {AA(stateNames.Values)}, {AA(transitionNames.Values)});");
		if (!String.IsNullOrEmpty(OnLoad))
			writer.WriteLine($"{indent}statechart.OnLoad += {A(OnLoad!, objName)};");
		if (!String.IsNullOrEmpty(OnUpdate))
			writer.WriteLine($"{indent}statechart.OnUpdate += {A(OnUpdate!, objName)};");
		if (!String.IsNullOrEmpty(ChartStart))
			writer.WriteLine($"{indent}statechart.ChartStart += {A(ChartStart!, objName)};");
		if (!String.IsNullOrEmpty(ChartFinish))
			writer.WriteLine($"{indent}statechart.ChartFinish += {A(ChartFinish!, objName)};");
		if (!String.IsNullOrEmpty(StateEnter))
			writer.WriteLine($"{indent}statechart.StateEnter += {A(StateEnter!, objName)};");
		if (!String.IsNullOrEmpty(StatePassthrough))
			writer.WriteLine($"{indent}statechart.StatePassthrough += {A(StatePassthrough!, objName)};");
		if (!String.IsNullOrEmpty(StateEntered))
			writer.WriteLine($"{indent}statechart.StateEntered += {A(StateEntered!, objName)};");
		if (!String.IsNullOrEmpty(StateExit))
			writer.WriteLine($"{indent}statechart.StateExit += {A(StateExit!, objName)};");

		writer.WriteLine($"{indent}return statechart;");
		writer.WriteLine($"{indent0}}}");

		foreach (var s in States)
		{
			if (s.Charts?.Count > 0)
			{
				foreach (var chart in s.Charts)
				{
					writer.WriteLine();
					chart.GenerateCode(writer, objName, methodName + "_", "private", indent0, tab, nullable);
				}
			}
		}

		static string AA(IEnumerable<string> values) => values?.Any() == true ? "new[] { " + String.Join(", ", values) + " }": "null";
	}

	private static string GetMethodName(string name, string namePrefix) => namePrefix + Regex.Replace(Strings.ToPascalCase(name), @"[_\W]+", "_");


	public Func<ITokenScope?, Statechart<T>> GenerateLambda<T>(string? methodPrefix = null, string? className = null, string? nameSpace = null, IEnumerable<string>? usings = null)
	{
		var text = new StringBuilder();
		className ??= "StatechartFactory";
		using (var writer = new StringWriter(text))
		{
			string tab = "  ";
			string indent = "";
			writer.WriteLine("#nullable enable");
			writer.WriteLine("using Lexxys;");
			writer.WriteLine("using Lexxys.States;");
			writer.WriteLine($"using {typeof(T).Namespace};");
			if (usings != null)
			{
				foreach (var item in usings)
				{
					writer.WriteLine($"using {item};");
				}
			}
			if (!String.IsNullOrEmpty(nameSpace))
			{
				writer.WriteLine($"namespace {nameSpace} {{");
				writer.WriteLine("{");
				indent += tab;
			}
			writer.WriteLine($"{indent}public static partial class {className}");
			writer.WriteLine($"{indent}{{");
			GenerateCode(writer, typeof(T).GetTypeName(true), methodPrefix, "public", indent: indent + tab, tab: tab, nullable: true);
			writer.WriteLine($"{indent}}}");
			if (!String.IsNullOrEmpty(nameSpace))
				writer.WriteLine("}");
		}
		var code = text.ToString();
#if NETFRAMEWORK
		const string FW = "f";
#endif
#if NETSTANDARD
		const string FW = "s";
#endif
#if NET6_0_OR_GREATER
		const string FW = "n";
#endif
		var name = $"sc{FW}-{GetHash(code)}.dll";
		var filepath = Path.Combine(Path.GetTempPath(), name);
		var asm = Factory.TryLoadAssembly(filepath, false);
		if (asm == null)
		{
			var references = new List<MetadataReference>
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Statechart<>).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
			};
#if NETSTANDARD
			var location = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
			var netstandard = Path.Combine(location, "netstandard.dll");
			if (File.Exists(netstandard))
				references.Add(MetadataReference.CreateFromFile(netstandard));
#endif
			var entry = Assembly.GetEntryAssembly();
			if (entry != null)
				references.AddRange(
					entry.GetReferencedAssemblies()
						.Select(o => MetadataReference.CreateFromFile(Assembly.Load(o).Location))
					);
			var compilation = CSharpCompilation.Create(name)
				.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddReferences(references)
				.AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

			EmitResult emitResult;
			using (var stream = new FileStream(filepath, FileMode.Create))
			{
				emitResult = compilation.Emit(stream);
			}
			if (!emitResult.Success)
				throw new AggregateException("Compilation Error", emitResult.Diagnostics
					.Select(o => new InvalidOperationException(o.ToString())));
			asm = Factory.TryLoadAssembly(filepath, true);
			if (asm == null)
				throw new InvalidOperationException($"Cannot create / load assembly {filepath}");
		}

		var factoryType = asm.GetType(className);
		if (factoryType == null)
			throw new InvalidOperationException($"Cannot find class {className}.");
		string methodName = GetMethodName(Name, methodPrefix ?? "Create");
		var method = factoryType.GetMethod(methodName);
		if (method == null)
			throw new InvalidOperationException($"Cannot find method {methodName}.");

		var arg = Expression.Parameter(typeof(ITokenScope));
		var exp = Expression.Call(method, arg);
		var lambda = Expression.Lambda<Func<ITokenScope?, Statechart<T>>>(exp, arg).Compile();

		return lambda;

		static string GetHash(string text)
		{
			using var hasher = System.Security.Cryptography.SHA256.Create();
			byte[] bytes = Encoding.Unicode.GetBytes(text);
			var hash = hasher.ComputeHash(bytes, 0, bytes.Length);
			return Strings.ToHexString(hash);
		}
	}

	internal static (int? Id, string Name) FixName(int? id, string name) => StateConfig.FixName(id, name);

	private static string A(string value, string objName) => StateConfig.A(value, objName);
	private static string S(string value) => StateConfig.S(value);

	public static StatechartConfig? FromXml(XmlLiteNode node)
	{
		if (node == null || node.IsEmpty)
			return null;

		List<StateConfig> states = new List<StateConfig>();
		List<TransitionConfig> transitions = new List<TransitionConfig>();

		foreach (var state in node.Where("state"))
		{
			List<StatechartConfig>? charts = null;
			foreach (var xml in state.Where("statechart"))
			{
				var chart = FromXml(xml);
				if (chart != null)
					(charts ??= new List<StatechartConfig>()).Add(chart);
			}
			var name = state["name"];
			if (name == null)
				throw new ArgumentOutOfRangeException(nameof(node), state, "Missing state name attribute.");
			var stateConfig = new StateConfig(
				name: name,
				id: state["id"].AsInt32(null),
				description: state["description"],
				guard: state["guard"],
				stateEnter: state["stateEnter"],
				statePassthrough: state["statePassthrough"],
				stateEntered: state["stateEntered"],
				stateExit: state["stateExit"],
				roles: state["role"]?.Split(',').Select(o => o.Trim()).Where(o => !String.IsNullOrEmpty(o)).ToList(),
				charts: charts
				);
			states.Add(stateConfig);

			foreach (var transition in state.Where("transition"))
			{
				if (String.IsNullOrEmpty(transition["destination"]))
					continue;
				var destination = transition["destination"] ?? transition["target"];
				if (destination == null)
					throw new ArgumentOutOfRangeException(nameof(node), transition, "Missing destination of the transition.");
				var transitionConfig = new TransitionConfig(
					id: transition["id"].AsInt32(null),
					name: transition["name"] ?? transition["event"],
					description: transition["description"],
					source: state["name"] ?? state["id"],
					destination: destination,
					guard: transition["guard"],
					action: transition["action"],
					continues: transition["continues"].AsBoolean(false),
					roles: transition["role"]?.Split(',').Select(o => o.Trim()).Where(o => !String.IsNullOrEmpty(o)).ToList()
					);
				transitions.Add(transitionConfig);
			}
		}

		return new StatechartConfig(
			id: node["id"].AsInt32(null),
			name: node["name"] ?? node.Name,
			description: node["description"],
			onLoad: node["onLoad"],
			onUpdate: node["onUpdate"],
			chartStart: node["chartStart"],
			chartFinish: node["chartFinish"],
			stateEnter: node["stateEnter"],
			stateEntered: node["stateEntered"],
			statePassthrough: node["statePassthrough"],
			stateExit: node["stateExit"],
			initialState: node["initialState"],
			states: states,
			transitions: transitions);
	}
}
