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

namespace Lexxys.States
{
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

			StateConfig.ValidateTokens(States);
		}

		public Statechart<T> Create<T>(ITokenScope? scope = null, Func<string, IStateAction<T>>? action = null, Func<string, IStateCondition<T>>? condition = null)
		{
			if (String.IsNullOrEmpty(Name))
				throw new ArgumentNullException(nameof(scope));
			if (scope == null)
				scope = TokenScope.Create("statechart");
			if (action == null)
				action = StateAction.CSharpScript<T>;
			if (condition == null)
				condition = StateCondition.CSharpScript<T>;
			var token = CreateToken(scope, Id, Name, Description);
			scope = scope.WithDomain(token);

			var states = States.Select(o => o.Create<T>(scope, action, condition)).ToList();
			var transitions = WithInitial().Select(o => o.Create<T>(scope, states, action, condition));
			var chart = new Statechart<T>(token, states, transitions);

			if (!String.IsNullOrEmpty(OnLoad))
				chart.OnLoad += action(OnLoad!);
			if (!String.IsNullOrEmpty(OnUpdate))
				chart.OnUpdate += action(OnUpdate!);
			if (!String.IsNullOrEmpty(ChartStart))
				chart.ChartStart += action(ChartStart!);
			if (!String.IsNullOrEmpty(ChartFinish))
				chart.ChartFinish += action(ChartFinish!);
			if (!String.IsNullOrEmpty(StateEnter))
				chart.StateEnter += action(StateEnter!);
			if (!String.IsNullOrEmpty(StateEntered))
				chart.StateEntered += action(StateEntered!);
			if (!String.IsNullOrEmpty(StatePassthrough))
				chart.StatePassthrough += action(StatePassthrough!);
			if (!String.IsNullOrEmpty(StateExit))
				chart.StateExit += action(StateExit!);
			return chart;

			static Token CreateToken(ITokenScope scope, int? id, string name, string? description)
				=> id == null ? scope.Token(name, description) : scope.Token(id.GetValueOrDefault(), name, description);
		}

		private IEnumerable<TransitionConfig> WithInitial()
		{
			if (States.Count == 0)
				return Transitions;

			var first = States.First();
			var initial = !String.IsNullOrEmpty(InitialState) ? InitialState: first.Id?.ToString() ?? first.Name;
			return Transitions.Prepend(new TransitionConfig(destination: initial!));
		}

		public void GenerateCode(TextWriter writer, string objName, string? methodPrefix = null, string? visibility = null, bool nullable = false, string? indent = null, string? tab = null)
		{
			if (indent == null)
				indent = "\t\t";
			if (tab == null)
				tab = "\t";
			if (visibility == null)
				visibility = "internal";
			string q = nullable ? "?": "";
			string methodName = (methodPrefix ?? "Create") + Name;
			var stateNames = new Dictionary<StateConfig, string>();
			var transitionNames = new Dictionary<TransitionConfig, string>();
			var chartMethods = States.SelectMany(o => o.Charts ?? Array.Empty<StatechartConfig>()).ToDictionary(o => o, o => methodName + "_" + o.Name);

			writer.WriteLine($"{indent}{visibility} static Statechart<{objName}> {methodName}(ITokenScope{q} root = null)");
			writer.WriteLine($"{indent}{{");
			var indent0 = indent;
			indent += tab;
			writer.WriteLine($"{indent}root ??= TokenScope.Create(\"statechart\");");
			writer.WriteLine($"{indent}var token = root.Token({S(Name)});");
			writer.WriteLine($"{indent}var s = root.WithDomain(token);");
			writer.WriteLine($"{indent}var t = s.WithTransitionDomain();");
			writer.WriteLine();

			int index = 0;
			foreach (var state in States)
			{
				string name = $"s{++index}";
				stateNames.Add(state, name);
				state.GenerateCode(writer, objName, name, chartMethods, indent);
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
						chart.GenerateCode(writer, objName, methodName + "_", "private", nullable, indent0, tab);
					}
				}
			}

			static string AA(IEnumerable<string> values) => values?.Any() == true ? "new[] { " + String.Join(", ", values) + " }": "null";
		}

		public Func<ITokenScope?, Statechart<T>> GenerateLambda<T>(string? methodPrefix = null, string? className = null, string? nameSpace = null, IEnumerable<string>? usings = null)
		{
			var text = new StringBuilder();
			className ??= "StatechartFactory";
			using (var writer = new StringWriter(text))
			{
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
					writer.WriteLine($"namespace {nameSpace} {{");
				writer.WriteLine($"public static partial class {className} {{");
				GenerateCode(writer, typeof(T).FullName!, methodPrefix, "public", true, indent: "  ", tab: "  ");
				writer.WriteLine("}");
				if (!String.IsNullOrEmpty(nameSpace))
					writer.WriteLine("}");
			}
			var code = text.ToString();
#if NET6_0_OR_GREATER
			var fw = "c";
#else
#if NETFRAMEWORK
			var fw = "a";
#else
			var fw = "b";
#endif
#endif

			var name = $"sc{fw}-{GetHash(code)}.dll";
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
						.Select(o => new Exception(o.ToString())));
				asm = Factory.TryLoadAssembly(filepath, true);
				if (asm == null)
					throw new InvalidOperationException($"Cannot create / load assembly {filepath}");
			}

			var factoryType = asm.GetType(className);
			if (factoryType == null)
				throw new InvalidOperationException($"Cannot find class {className}.");
			string methodName = (methodPrefix ?? "Create") + Name;
			var method = factoryType.GetMethod(methodName);
			if (method == null)
				throw new InvalidOperationException($"Cannot find method {methodName}.");

			var arg = Expression.Parameter(typeof(ITokenScope));
			var exp = Expression.Call(method, arg);
			var lambda = Expression.Lambda<Func<ITokenScope?, Statechart<T>>>(exp, arg).Compile();

			return lambda;

			static string GetHash(string text)
			{
				using var hasher = System.Security.Cryptography.SHA1.Create();
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
					{
						if (charts == null)
							charts = new List<StatechartConfig>();
						charts.Add(chart);
					}
				}
				var name = state["name"];
				if (name == null)
					throw new ArgumentOutOfRangeException(nameof(node), state, "Missing state name attribute.");
				var stateConfig = new StateConfig(
					id: state["id"].AsInt32(null),
					name: name,
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
}
