// Lexxys Infrastructural library.
// file: ConcreteLogWriters.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Lexxys;
using Lexxys.Logging;
using Lexxys.Xml;

namespace State.Logging
{
	public class TextFileLogWriter: LogWriter
	{
		private TimeSpan _timeout = DefaultTimeout;
		private string _file;
		private bool _truncate;
		private bool _errorLogged;

		public const string DefaultLogFileMask = @"{YMD}.log";
		public const int DefaultBatchSize = 16;

		public static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 0, 2);
		public static readonly TimeSpan MaxTimeout = new TimeSpan(0, 0, 10);
		public static readonly TimeSpan MinTimeout = new TimeSpan(0, 0, 0, 100);

		public override string Target => _file;

		/// <summary>
		/// Initialize the LogWriter from XML.
		/// </summary>
		/// <param name="name">Name of the <see cref="LogWriter"/> for future reference.</param>
		/// <param name="config">
		///	Initialization XML containing 'parameters' element with attributes:
		///		format setting <see cref="TextFormatSetting.Join(XmlLiteNode)"/>
		///		file		- mask of file name. default: %TEMP%\Logs\{YMD}-LL.log
		///		timeout		- timeout while opening file in milliseconds. default: 2000
		/// </param>
		protected override void Initialize(string name, XmlLiteNode config)
		{
			base.Initialize(name, config);
			if (config == null)
				config = XmlLiteNode.Empty;
			_file = XmlTools.GetString(config["file"], DefaultLogFileMask);
			_timeout = XmlTools.GetTimeSpan(config["timeout"], DefaultTimeout, MinTimeout, MaxTimeout);
			XmlLiteNode overwrite = config.FirstOrDefault("overwrite");
			_truncate = overwrite != null && overwrite.Value.AsBoolean(true);
			if (BatchSize < 0)
				BatchSize = DefaultBatchSize;
		}

		public override void Write(LogRecord record)
		{
			if (record == null)
				return;

			using (StreamWriter o = OpenLog())
			{
				if (o != null)
				{
					Format(o, record).WriteLine();
					return;
				}
			}
			if (!_errorLogged)
			{
				WriteErrorMessage("TextFileLogWriter", $"--{_file}--", null);
				_errorLogged = true;
			}
			WriteEventLogMessage(record);
		}

		public override void Write(IEnumerable<LogRecord> records)
		{
			if (records == null)
				return;

			using (StreamWriter o = OpenLog())
			{
				if (o != null)
				{
					foreach (var record in records)
					{
						if (record != null)
							Format(o, record).WriteLine();
					}
					return;
				}
			}

			if (!_errorLogged)
			{
				WriteErrorMessage("TextFileLogWriter", $"--{_file}--", null);
				_errorLogged = true;
			}
			foreach (var record in records)
			{
				WriteEventLogMessage(record);
			}
		}

		private StreamWriter OpenLog()
		{
			try
			{
				StreamWriter o = OpenLogStream(FileMaskToName(Target), _timeout, _truncate);
				for (int i = 0; o == null && i < 5; ++i)
				{
					string name = FileMaskToName(Target);
					int k = name.LastIndexOf('.');
					name = k < 0 ?
						name + "." + SixBitsCoder.Thirty((ulong)(WatchTimer.Query(0) % WatchTimer.TicksPerMinute)).PadLeft(6, '0') + ".":
						name.Substring(0, k) + "." + SixBitsCoder.Thirty((ulong)(WatchTimer.Query(0) % WatchTimer.TicksPerMinute)).PadLeft(6, '0') + name.Substring(k);
					o = OpenLogStream(name, TimeSpan.Zero, _truncate);
				}
				_truncate = false;
				return o;
			}
			catch
			{
				_truncate = false;
				return null;
			}
		}

		private static string FileMaskToName(string logFileMask)
		{
			if (logFileMask == null)
				return null;

			if (logFileMask.IndexOf('{') < 0)
				return logFileMask;

			DateTime tm = DateTime.Now;

			return __fileMaskRe.Replace(logFileMask, match =>
			{
				string s = match.Value;
				var r = new StringBuilder();
				for (int i = 1; i < s.Length-1; ++i)
				{
					switch (s[i])
					{
						case 'Y':
							r.Append(tm.Year.ToString(CultureInfo.InvariantCulture));
							break;
						case 'y':
							r.Append((tm.Year % 100).ToString("D2", CultureInfo.InvariantCulture));
							break;
						case 'M':
							r.Append(tm.Month.ToString("D2", CultureInfo.InvariantCulture));
							break;
						case 'D':
							r.Append(tm.Day.ToString("D2", CultureInfo.InvariantCulture));
							break;
						case 'd':
							r.Append(tm.DayOfYear.ToString("D3", CultureInfo.InvariantCulture));
							break;
						case 'H':
							r.Append(tm.Hour.ToString("D2", CultureInfo.InvariantCulture));
							break;
						case 'm':
							r.Append(tm.Minute.ToString("D2", CultureInfo.InvariantCulture));
							break;
						default:
							r.Append(s[i]);
							break;
					}
				}
				return r.ToString();
			});
		}
		private static readonly Regex __fileMaskRe = new Regex(@"\{[^\}]*\}");

		private static StreamWriter OpenLogStream(string fileName, TimeSpan timeout, bool truncate)
		{
			if (fileName == null || fileName.Length == 0)
				return null;

			const int ErrorSharingViolation = 32;
			const int ErrorLockViolation = 33;

			fileName = Environment.ExpandEnvironmentVariables(fileName);
			int i = fileName.LastIndexOf('\\');
			if (i > 0)
			{
				string dir = fileName.Substring(0, i);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			Random r = null;
			TimeSpan delay = TimeSpan.Zero;
			int bound = 32;
			StreamWriter o = null;
			do
			{
				try
				{
					o = new StreamWriter(fileName, !truncate);
				}
				catch (IOException e)
				{
					int errorId = Marshal.GetHRForException(e) & 0xFFFF;
					if (errorId != ErrorSharingViolation && errorId != ErrorLockViolation)
						return null;
					if (delay >= timeout)
						return null;
					if (r == null)
						r = new Random();
					int sleep = r.Next(bound);
					delay += TimeSpan.FromMilliseconds(sleep);
					bound += bound;
					System.Threading.Thread.Sleep(sleep);
				}
			} while (o == null);

			return o;
		}
	}


	public class NullLogWriter: LogWriter
	{

		public override string Target => "Null";

		public override void Write(LogRecord record)
		{
		}
	}

	public class ConsoleLogWriter: LogWriter
	{
		private static readonly TextFormatSetting Defaults = new TextFormatSetting("  ", ". ",
			"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff} {IndentMark}{Source}: {Message}");

		protected override TextFormatSetting FormattingDefaults => Defaults;

		public override string Target => "Console";

		public override void Write(LogRecord record)
		{
			Format(Console.Error, record).WriteLine();
		}
	}


	public class TraceLogWriter: LogWriter
	{
		private static readonly TextFormatSetting Defaults = new TextFormatSetting("  ", ". ",
			"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff} {IndentMark}{Source}: {Message}");

		protected override TextFormatSetting FormattingDefaults => Defaults;

		public override string Target => "Trace";

		public override void Write(LogRecord record)
		{
			Trace.WriteLine(Format(record));
		}
	}


	public class DebuggerLogWriter: LogWriter
	{
		private static readonly TextFormatSetting Defaults = new TextFormatSetting("  ", ". ",
			"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff} {IndentMark}{Source}: {Message}");

		protected override TextFormatSetting FormattingDefaults => Defaults;

		public override string Target => "Debugger";

		public override void Write(LogRecord record)
		{
			if (Debugger.IsLogging())
			{
				string message = Format(record);
				Debugger.Log(record.Priority, record.Source, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message: message + Environment.NewLine);
			}
		}
	}


	public class EventLogLogWriter: LogWriter
	{
		private string _eventSource;

		private static readonly TextFormatSetting Defaults = new TextFormatSetting("", "",
			"{MachineName} {ProcessID:X4}\nthread {ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyy-MM-ddTHH:mm:ss.fffff}\n{Source}: {Message}");

		protected override TextFormatSetting FormattingDefaults => Defaults;

		public override string Target => "EventLog";

		/// <summary>
		/// Initialize the LogWriter from XML.
		/// </summary>
		/// <param name="name">Name of the <see cref="LogWriter"/> for future reference.</param>
		/// <param name="config">
		///	Initialization XML contaning 'parameters' element with attributes:
		///		format setting <see cref="TextFormatSetting.Join(XmlLiteNode)"/>
		///		source		- EventLog source (default: "Lexxys")
		/// </param>
		protected override void Initialize(string name, XmlLiteNode config)
		{
			if (config == null)
				config = XmlLiteNode.Empty;
			base.Initialize(name, config);

			_eventSource = XmlTools.GetString(config["eventSource"], LogEventSource);
			if (_eventSource.Length > 254)
				_eventSource = _eventSource.Substring(0, 254);
		}

		public override void Write(LogRecord record)
		{
			try
			{
				#pragma warning disable CA1416 // Validate platform compatibility
				var entryType = record.LogType switch
				{
					LogType.Output or LogType.Error => EventLogEntryType.Error,
					LogType.Warning => EventLogEntryType.Warning,
					_ => EventLogEntryType.Information,
				};
				string message = Format(record);
				if (message.Length > 32765)
					message = message.Substring(0, 32765);
				EventLog.WriteEntry(_eventSource, message, entryType);
				#pragma warning restore CA1416 // Validate platform compatibility
			}
			#pragma warning disable CA1031 // Do not catch general exception types
			catch
			{
				// ignored
			}
		}
	}
}
