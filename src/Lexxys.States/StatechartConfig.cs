﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Lexxys.States;

#pragma warning disable CA1819

public class StatechartConfig
{
	public int? Id { get; init; }
	public string Name { get; init; }
	public string? Description { get; init; }
	public string? OnLoad { get; init; }
	public string? OnUpdate { get; init; }
	public string? ChartStart { get; init; }
	public string? ChartFinish { get; init; }
	public string? StateEnter { get; init; }
	public string? StateEntered { get; init; }
	public string? StatePassthrough { get; init; }
	public string? StateExit { get; init; }
	public string? InitialState { get; init; }
	public StateConfig[] State { get; init; }
	public string? Reference { get; init; }

	public StatechartConfig()
	{
		Name = String.Empty;
		State = Array.Empty<StateConfig>(); // new List<StateConfig>();
	}

	public StatechartConfig(string name, int? id = null, string? description = null, string? onLoad = null, string? onUpdate = null, string? chartStart = null, string? chartFinish = null, string? stateEnter = null, string? stateEntered = null, string? statePassthrough = null, string? stateExit = null, string? initialState = null, IReadOnlyCollection<StateConfig>? state = null, string? reference = null)
	{
		if (name is null || (name = name.Trim()).Length == 0)
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
		State = state?.ToArray() ?? Array.Empty<StateConfig>();

		ValidateTokens(State);
		Reference = reference;
	}

	private static void ValidateTokens(IReadOnlyCollection<StateConfig> states)
	{
		var dupId = states.Where(o => o.Id.HasValue).GroupBy(o => o.Id).FirstOrDefault(o => o.Count() > 1);
		if (dupId is not null)
			throw new InvalidOperationException($"State ID value is not unique ({dupId.Key})");
		var dupName = states.Where(o => o.Name is not null).GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(o => o.Count() > 1);
		if (dupName is not null)
			throw new InvalidOperationException($"State Name value is not unique ({dupName.Key})");
	}

	private List<TransitionConfig> GetTransitions()
	{
		var transitions = new List<TransitionConfig>();
		foreach (var item in State)
		{
			transitions.AddRange(item.GetTransitions());
		}
		return transitions;
	}

	public Statechart<T> Create<T>(ITokenScope? scope = null, Func<string, IStateAction<T>?>? actionBuilder = null, Func<string, IStateCondition<T>?>? conditionBuilder = null, Func<string, StatechartConfig>? referenceResolver = null)
	{
		scope ??= TokenScope.Create("statechart");
		actionBuilder ??= StateAction.CSharpScript<T>;
		conditionBuilder ??= StateCondition.CSharpScript<T>;
		var token = CreateToken(scope, Id, Name, Description);
		scope = scope.Scope(token);

		var reference = Reference is null ? null:
			referenceResolver is not null ? referenceResolver(Reference):
			throw new ArgumentNullException(nameof(referenceResolver));

		var states = State.Select(o => o.Create(scope, actionBuilder, conditionBuilder, referenceResolver)).ToList();
		List<string?> exclude = new List<string?>(states.Select(o => o.Name));
		if (reference?.State.Length > 0)
			states.AddRange(reference.State.Where(o => !exclude.Contains(o.Name)).Select(o => o.Create(scope, actionBuilder, conditionBuilder, referenceResolver)));
		var transitions = WithInitial(states.Select(o => ((int?)o.Id, o.Name)), reference, exclude).Select(o => o.Create(scope, states, actionBuilder, conditionBuilder));
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
			=> id is null ? scope.Token(name, description) : scope.Token(id.GetValueOrDefault(), name, description);
	}

	private IEnumerable<TransitionConfig> WithInitial(IEnumerable<(int? Id, string Name)> states, StatechartConfig? reference, List<string?> exclude)
	{
		var transitions = GetTransitions();
		var refers = reference?.GetTransitions();
		if (refers?.Count > 0)
			transitions.AddRange(refers.Where(o => !exclude.Contains(o.Source)));

		string initial;
		if (!String.IsNullOrEmpty(InitialState))
		{
			initial = InitialState!;
		}
		else if (!String.IsNullOrEmpty(reference?.InitialState))
		{
			initial = reference!.InitialState!;
		}
		else
		{
			var nip = states.Where(o => !transitions.Any(t => t.Destination == o.Name || int.TryParse(t.Destination, out var n) && n == o.Id)).ToList();
			if (nip.Count == 0)
				throw new InvalidOperationException($"Cannot find the initial state for statechart {Name}.");
			if (nip.Count > 1)
				throw new InvalidOperationException($"Multiple initial states found for statechart {Name}.");
			initial = nip[0].Id?.ToString(CultureInfo.InvariantCulture) ?? nip[0].Name;
		}
		transitions.Insert(0, new TransitionConfig(destination: initial));
		return transitions;
	}

	public void GenerateCode(TextWriter writer, string objName, string? methodPrefix = null, string? visibility = null, string? indent = null, string? tab = null, bool nullable = false, Func<string, StatechartConfig>? referenceResolver = null)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		var reference = Reference is null ? null:
			referenceResolver is not null ? referenceResolver(Reference):
			throw new ArgumentNullException(nameof(referenceResolver));

		indent ??= "\t";
		tab ??= "\t";
		visibility ??= "internal";
		string q = nullable ? "?": "";
		string methodName = GetMethodName(Name, methodPrefix ?? "Create");

		var stateNames = new Dictionary<StateConfig, string>();
		var transitionNames = new Dictionary<TransitionConfig, string>();
		var states = State.ToList();
		var exclude = new List<string?>(states.Select(o => o.Name));
		if (reference?.State.Length > 0)
			states.AddRange(reference.State.Where(o => !exclude.Contains(o.Name)));
		var chartMethods = states.SelectMany(o => o.Statechart).ToDictionary(o => o, o => GetMethodName(o.Name, methodName + "_"));

		writer.WriteLine($"{indent}{visibility} static Statechart<{objName}> {methodName}(ITokenScope{q} root = null)");
		writer.WriteLine($"{indent}{{");
		var indent0 = indent;
		indent += tab;
		writer.WriteLine($"{indent}root ??= TokenScope.Create(\"statechart\");");
		writer.WriteLine($"{indent}var token = root.Token({S(Name)});");
		writer.WriteLine($"{indent}var s = root.Scope(token);");
		writer.WriteLine($"{indent}var t = s.TransitionScope();");
		if (states.Any(o => o.Statechart.Length > 0))
			writer.WriteLine($"{indent}Token x;");
		writer.WriteLine();

		int index = 0;
		foreach (var state in states)
		{
			string name = $"s{++index}";
			stateNames.Add(state, name);
			state.GenerateCode(writer, objName, name, "s", "x", chartMethods, indent);
		}

		index = 0;
		foreach (var transition in WithInitial(states.Select(o => (o.Id, o.Name)), reference, exclude))
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

		foreach (var s in State)
		{
			foreach (var chart in s.Statechart)
			{
				writer.WriteLine();
				chart.GenerateCode(writer, objName, methodName + "_", "private", indent0, tab, nullable, referenceResolver);
			}
		}

		static string AA(IEnumerable<string>? values)
		{
			if (values is null)
				return "null";
			var s = String.Join(", ", values);
			return s.Length == 0 ? "null": "new[] { " + s + " }";
		}
	}

	private static string GetMethodName(string name, string namePrefix) => namePrefix + Regex.Replace(Strings.ToPascalCase(name)!, @"[_\W]+", "_");


	public Func<ITokenScope?, Statechart<T>> GenerateLambda<T>(string? methodPrefix = null, string? className = null, string? nameSpace = null, IEnumerable<string>? usings = null, Func<string, StatechartConfig>? referenceResolver = null)
	{
		var text = new StringBuilder();
		className ??= "StatechartFactory";
		using (var writer = new StringWriter(text))
		{
			string tab = "  ";
			string indent = "";
			writer.WriteLine("#nullable enable");
			writer.WriteLine("using System;");
			writer.WriteLine("using System.Collections.Generic;");
			writer.WriteLine("using System.Linq;");
			writer.WriteLine("using System.Text;");
			writer.WriteLine("using Lexxys;");
			writer.WriteLine("using Lexxys.States;");
			writer.WriteLine($"using {typeof(T).Namespace};");
			if (usings is not null)
			{
				foreach (var item in usings)
				{
					writer.WriteLine($"using {item};");
				}
			}
			writer.WriteLine();
			if (!String.IsNullOrEmpty(nameSpace))
			{
				writer.WriteLine($"namespace {nameSpace} {{");
				writer.WriteLine("{");
				indent += tab;
			}
			writer.WriteLine($"{indent}public static partial class {className}");
			writer.WriteLine($"{indent}{{");
			GenerateCode(writer, typeof(T).GetTypeName(true), methodPrefix, "public", indent: indent + tab, tab: tab, nullable: true, referenceResolver);
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
		if (asm is null)
		{
			var compilation = CSharpCompilation.Create(name)
				.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddReferences(RoslynHelper.GetReferences<T>())
				.AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

			EmitResult emitResult;
			using (var stream = new FileStream(filepath, FileMode.Create))
			{
				emitResult = compilation.Emit(stream);
			}
			if (!emitResult.Success)
				throw new AggregateException("Compilation Error", emitResult.Diagnostics
					.Select(o => new InvalidOperationException(o.ToString())));

			asm = Factory.LoadAssembly(filepath);
		}

		var factoryType = asm.GetType(className) ?? throw new InvalidOperationException($"Cannot find class {className}.");
		string methodName = GetMethodName(Name, methodPrefix ?? "Create");
		var method = factoryType.GetMethod(methodName);
		if (method is null)
			throw new InvalidOperationException($"Cannot find method {methodName}.");

		var arg = Expression.Parameter(typeof(ITokenScope));
		var exp = Expression.Call(method, arg);
		var lambda = Expression.Lambda<Func<ITokenScope?, Statechart<T>>>(exp, arg).Compile();

		return lambda;

		static string GetHash(string text)
		{
			var name = Assembly.GetEntryAssembly()?.FullName ?? "<>";
			var n1 = Encoding.UTF8.GetByteCount(name);
			var n2 = Encoding.UTF8.GetByteCount(text);
			var bytes = new byte[n1 + n2];
			Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 0);
			Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, n1);
			using var hasher = System.Security.Cryptography.SHA256.Create();
			var hash = hasher.ComputeHash(bytes, 0, bytes.Length);
			return SixBitsCoder.Encode5(hash);
		}
	}

	internal static (int? Id, string Name) FixName(int? id, string name) => StateConfig.FixName(id, name);

	private static string A(string value, string objName) => StateConfig.A(value, objName);
	private static string S(string value) => StateConfig.S(value);
}
