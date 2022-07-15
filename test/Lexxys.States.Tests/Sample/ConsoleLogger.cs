using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Lexxys.States.Tests.Sample;


class ConsoleLogger : ILogging
{
	private string _indent;
	public ConsoleLogger(string? source = null)
	{
		_indent = "";
		Source = source ?? "";
	}

	public string Source { get; }

	public IDisposable? Enter(LogType logType, string? section, IDictionary? args)
	{
		section ??= "";
		Console.Write(_indent);
		Console.WriteLine(TypeName(logType) + "Begin " + section);
		_indent += "  ";
		if (args != null)
		{
			foreach (IDictionaryEnumerator? item in args)
			{
				if (item is null)
					continue;
				Console.Write(_indent);
				Console.Write(item.Key);
				Console.Write(": ");
				Console.WriteLine(item.Value);
			}
		}
		return new Section(section, this);
	}

	private static string TypeName(LogType type) => type switch
	{
		LogType.Output => "[OUT] ",
		LogType.Debug => "[DBG] ",
		LogType.Trace => "[TRC] ",
		LogType.Information => "[INF] ",
		LogType.Warning => "[WRN] ",
		LogType.Error => "[ERR] ",
		_ => "[***] "
	};

	public bool IsEnabled(LogType logType) => true;

	public void Log(LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args)
	{
		Console.Write(_indent);
		Console.Write(TypeName(logType));
		Console.Write(source ?? Source);
		if (message != null)
		{
			Console.Write(": ");
			Console.Write(message);
		}
		if (exception != null)
		{
			Console.Write(" ");
			Console.Write(exception);
		}
		Console.WriteLine();
	}

	public IDisposable? Timing(LogType logType, string? section, TimeSpan threshold)
	{
		return Enter(logType, section, null);
	}

	void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
	=> LoggingTools.Log(this, logLevel, eventId, state, exception, formatter);

	bool ILogger.IsEnabled(LogLevel logLevel)
		=> LoggingTools.IsEnabled(this, logLevel);

	IDisposable ILogger.BeginScope<TState>(TState state)
		=> LoggingTools.BeginScope(this, state);

	sealed class Section : IDisposable
	{
		public Section(string name, ConsoleLogger logger)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Logger = logger;
		}

		public string Name { get; }
		public ConsoleLogger Logger { get; }

		public void Dispose()
		{
			Logger._indent = Logger._indent.Substring(0, Logger._indent.Length - 2);
			Console.Write(Logger._indent);
			Console.WriteLine("[---] End " + Name);
		}
	}
}

class ConsoleLogger<T> : ConsoleLogger, ILogging<T>
{
	public ConsoleLogger() : base(typeof(T).GetTypeName())
	{
	}
}

class ConsoleLoggerFactory : StaticServices.IFactory
{
	public static readonly StaticServices.IFactory Instance = new ConsoleLoggerFactory();

	private ConsoleLoggerFactory()
	{
	}

	public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
	private readonly Type[] _supportedTypes = new Type[] { typeof(ILogger), typeof(ILogger<>), typeof(ILogging), typeof(ILogging<>) };

	public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
	{
		if (type.IsAssignableFrom(typeof(ConsoleLogger)))
		{
			result = new ConsoleLogger(arguments?.Length > 0 ? arguments[0]?.ToString() : null);
			return true;
		}
		if (!type.IsGenericType)
		{
			result = null!;
			return false;
		}
		var generic = type.GetGenericTypeDefinition();
		result = generic == typeof(ILogging<>) || generic == typeof(ILogger<>) ?
			Activator.CreateInstance(typeof(ConsoleLogger<>).MakeGenericType(type.GetGenericArguments()))! : null!;
		return result != null;
	}
}
