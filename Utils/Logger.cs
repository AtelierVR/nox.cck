using ULogger = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using ILogger = UnityEngine.ILogger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Nox.CCK.Utils {
	public class Logger : ILogger {
		public static readonly List<LogEntry> History = new();

		public const int MaxLogLines = 1000;
		public const long MaxLogSize = 1024 * 1024 * 10; // 10 MB

		public static string LogDir
			=> Path.Combine(Constants.AppPath, "logs");

		public static string LogFile
			=> Path.Combine(LogDir, "latest.log");

		public static byte LogID { get; private set; } = 0;
		public static bool IsInitialized { get; private set; } = false;

		private static readonly object FileLock = new();
		// Persistent writer – kept open to avoid OS file-handle open/close overhead (~1-2ms each).
		private static StreamWriter _logWriter;


		public ILogHandler logHandler { get; set; } = ULogger.unityLogger.logHandler;
		public bool logEnabled { get; set; } = true;
		public UnityEngine.LogType filterLogType { get; set; } = UnityEngine.LogType.Log;

		private Logger() {
			// Constructor privé pour le singleton
		}

		#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void EditorInit() {
			Init();
			IsInitialized = true;
		}

		[MenuItem("Nox/Logger/Open Latest Log")]
		private static void OpenLatestLog() {
			if (File.Exists(LogFile))
				EditorUtility.OpenWithDefaultApp(LogFile);
			else
				OpenDialog("Nox Logger", "No log file found.", "OK");
		}

		[MenuItem("Nox/Logger/Reveal Latest Log")]
		private static void RevealLatestLog() {
			if (File.Exists(LogFile))
				EditorUtility.RevealInFinder(LogFile);
			else
				OpenDialog("Nox Logger", "No log file found.", "OK");
		}

		[MenuItem("Nox/Logger/Clear Logs")]
		private static void ClearLog() {
			var files = Directory.GetFiles(LogDir);
			foreach (var file in files)
				File.Delete(file);
			Init();
		}

		[MenuItem("Nox/Logger/Open Editor Log")]
		private static void OpenUnityLog() {
			var logPath = Application.consoleLogPath;
			if (File.Exists(logPath))
				EditorUtility.OpenWithDefaultApp(logPath);
			else
				OpenDialog("Nox Logger", "No Unity log file found.", "OK");
		}

		[MenuItem("Nox/Logger/Open Runtime Log")]
		private static void OpenRuntimeLog() {
			var logPath = Application.persistentDataPath + "/Player.log";
			if (File.Exists(logPath))
				EditorUtility.OpenWithDefaultApp(logPath);
			else
				OpenDialog("Nox Logger", "No runtime log file found.", "OK");
		}

		/// <summary>
		/// Opens a dialog in the Editor with a title, message, and buttons.
		/// If cancel is null, it will only have an OK button.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="ok"></param>
		/// <param name="cancel"></param>
		/// <returns></returns>
		public static bool OpenDialog(string title, string message, string ok, string cancel = null)
			=> string.IsNullOrEmpty(cancel)
				? EditorUtility.DisplayDialog(title, message, ok)
				: EditorUtility.DisplayDialog(title, message, ok, cancel);

		/// <summary>
		/// Displays a progress bar in the Editor.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="progress"></param>
		public static void ShowProgress(string title, string message, float progress) {
			EditorUtility.DisplayProgressBar(title, message, progress);
			OnProgress.Invoke(true, title, message, progress);
		}

		/// <summary>
		/// Clears the progress bar in the Editor.
		/// </summary>
		public static void ClearProgress() {
			EditorUtility.ClearProgressBar();
			OnProgress.Invoke(false, null, null, -1f);
		}

		#else
		public static void ShowProgress(string title, string message, float progress)
			=> OnProgress.Invoke(true, title, message, progress);
		
		public static void ClearProgress()
			=> OnProgress.Invoke(false, null, null, -1f);

		#endif

		public static readonly UnityEvent<bool, string, string, float> OnProgress = new();
		public static readonly UnityEvent<LogType, string, string, Object> OnLog = new();

		public static void Init() {
			lock (FileLock) {
				// Close any existing writer before rotating the file
				_logWriter?.Close();
				_logWriter = null;

				if (!Directory.Exists(LogDir))
					Directory.CreateDirectory(LogDir);

				if (File.Exists(LogFile)) {
					var    creationTime = File.GetCreationTime(LogFile);
					var    i            = 0;
					string newFileName;
					do {
						newFileName = Path.Combine(LogDir, $"log_{creationTime:yyyy-MM-dd_HH-mm-ss}_{i++}.log");
					} while (File.Exists(newFileName));

					try {
						File.Move(LogFile, newFileName);
					} catch (IOException) {
						// The file is held open by another process (e.g. a second game instance or the editor).
						// Skip rotation and fall through to truncate+overwrite with FileMode.Create below.
					}
				}

				// Open a persistent writer (FileMode.Create after the Move above).
				// FileShare.Delete allows another process to rotate (File.Move) this file while we hold it open.
				_logWriter = new StreamWriter(
					new FileStream(LogFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)
				) { AutoFlush = true };

				LogID = (byte)new System.Random().Next(byte.MinValue, byte.MaxValue);
				_logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] Logger initialized.");
			}
		}

		// == Log methods with various overloads for convenience ==

		public static void Log(object message, string tag = null)
			=> Print(LogType.Log, message, tag: tag);

		public static void LogWarning(object message, string tag = null)
			=> Print(LogType.Warning, message, tag: tag);

		public static void LogError(object message, string tag = null)
			=> Print(LogType.Error, message, tag: tag);

		public static void LogException(Exception exception, string tag = null)
			=> Print(LogType.Exception, exception, tag: tag);

		public static void LogDebug(object message, string tag = null)
			=> Print(LogType.Debug, message, tag: tag);


		// == Log methods with context ==

		public static void Log(object message, Object context, string tag = null)
			=> Print(LogType.Log, message, context, tag: tag);

		public static void LogWarning(object message, Object context, string tag = null)
			=> Print(LogType.Warning, message, context, tag: tag);

		public static void LogError(object message, Object context, string tag = null)
			=> Print(LogType.Exception, message, context, tag: tag);

		public static void LogException(Exception exception, Object context, string tag = null)
			=> Print(LogType.Exception, exception, context, tag);

		public static void LogDebug(object message, Object context, string tag = null)
			=> Print(LogType.Debug, message, context, tag: tag);

		// ILogger interface methods
		public void LogFormat(UnityEngine.LogType logType, string format, params object[] args) {
			if (!logEnabled)
				return;
			var message = string.Format(format, args);
			Print(ConvertLogType(logType), message);
		}

		public void LogFormat(UnityEngine.LogType logType, Object context, string format, params object[] args) {
			if (!logEnabled)
				return;
			var message = string.Format(format, args);
			Print(ConvertLogType(logType), message, context);
		}

		public bool IsLogTypeAllowed(UnityEngine.LogType logType) {
			if (!logEnabled)
				return false;
			return (int)filterLogType >= (int)logType;
		}

		public void Log(UnityEngine.LogType logType, object message) {
			if (!logEnabled)
				return;
			Print(ConvertLogType(logType), message);
		}

		public void Log(UnityEngine.LogType logType, object message, Object context) {
			if (!logEnabled)
				return;
			Print(ConvertLogType(logType), message, context);
		}

		public void Log(UnityEngine.LogType logType, string tag, object message) {
			if (!logEnabled)
				return;
			Print(ConvertLogType(logType), message, tag: tag);
		}

		public void Log(UnityEngine.LogType logType, string tag, object message, Object context) {
			if (!logEnabled)
				return;
			Print(ConvertLogType(logType), message, context, tag: tag);
		}

		public void Log(object message) {
			if (!logEnabled)
				return;
			Print(LogType.Log, message);
		}

		public void Log(string tag, object message) {
			if (!logEnabled)
				return;
			Print(LogType.Log, message, tag: tag);
		}

		public void Log(string tag, object message, Object context) {
			if (!logEnabled)
				return;
			Print(LogType.Log, message, context, tag: tag);
		}



		public void LogWarning(string tag, object message) {
			if (!logEnabled)
				return;
			Print(LogType.Warning, message, tag: tag);
		}

		public void LogWarning(string tag, object message, Object context) {
			if (!logEnabled)
				return;
			Print(LogType.Warning, message, context, tag: tag);
		}

		public void LogError(string tag, object message) {
			if (!logEnabled)
				return;
			Print(LogType.Error, message, tag: tag);
		}

		public void LogError(string tag, object message, Object context) {
			if (!logEnabled)
				return;
			Print(LogType.Error, message, context, tag: tag);
		}

		public void LogException(Exception exception, Object context) {
			if (!logEnabled)
				return;
			Print(LogType.Exception, exception, context);
		}

		public void LogException(Exception exception) {
			if (!logEnabled)
				return;
			Print(LogType.Exception, exception);
		}


		private static LogType ConvertLogType(UnityEngine.LogType type)
			=> type switch {
				UnityEngine.LogType.Log       => LogType.Log,
				UnityEngine.LogType.Warning   => LogType.Warning,
				UnityEngine.LogType.Assert    => LogType.Assert,
				UnityEngine.LogType.Exception => LogType.Exception,
				UnityEngine.LogType.Error     => LogType.Error,
				_                             => LogType.Debug
			};


		public static readonly string[] IgnoreStack = {
			"AsyncUniTask",
			"AwaiterActions",
			"CompletionSource",
			"InvokableCall.Invoke",
			"AsyncUniTask`1.Run",
			"AsyncUniTask`2.Run",
			"AsyncUniTaskMethodBuilder.Start",
			"UnityEvent.Invoke",
			"UnityWebRequestAsyncOperationConfiguredSource.MoveNext",
			"UnityWebRequestAsyncOperationConfiguredSource.Continuation",
			"AsyncOperation.InvokeCompletionEvent",
			"AsyncOperation.InvokeCompletionEvent",
			"PlayerLoopRunner.RunCore",
			"ContinuationQueue.RunCore",
			"ContinuationQueue.Update",
			"ContinuationQueue.Run",
			"PlayerLoopHelper.ForceEditorPlayerLoopUpdate"
		};

		private static bool? _debugLoggingCache;
		private static bool? _debugStackTraceCache;

		public static bool DebugLogging {
			get {
				if (_debugLoggingCache.HasValue) return _debugLoggingCache.Value;
				_debugLoggingCache = Config.Load().Get("debug.logging", Application.isEditor);
				return _debugLoggingCache.Value;
			}
			set {
				_debugLoggingCache = value;
				var config = Config.Load();
				config.Set("debug.logging", value);
				config.Save();
			}
		}

		public static bool DebugStackTrace {
			get {
				if (_debugStackTraceCache.HasValue) return _debugStackTraceCache.Value;
				_debugStackTraceCache = Config.Load().Get("debug.stacktrace", false);
				return _debugStackTraceCache.Value;
			}
			set {
				_debugStackTraceCache = value;
				var config = Config.Load();
				config.Set("debug.stacktrace", value);
				config.Save();
			}
		}

		#if UNITY_EDITOR
		[MenuItem("Nox/Logger/Toggle Debug Logging")]
		private static void ToggleDebugLogging() {
			DebugLogging = !DebugLogging;
			ULogger.Log($"[Logger] debug.logging = {DebugLogging}");
		}

		[MenuItem("Nox/Logger/Toggle Debug Logging", true)]
		private static bool ToggleDebugLoggingValidate() {
			UnityEditor.Menu.SetChecked("Nox/Logger/Toggle Debug Logging", DebugLogging);
			return true;
		}

		[MenuItem("Nox/Logger/Toggle Stack Trace")]
		private static void ToggleDebugStackTrace() {
			DebugStackTrace = !DebugStackTrace;
			ULogger.Log($"[Logger] debug.stacktrace = {DebugStackTrace}");
		}

		[MenuItem("Nox/Logger/Toggle Stack Trace", true)]
		private static bool ToggleDebugStackTraceValidate() {
			UnityEditor.Menu.SetChecked("Nox/Logger/Toggle Stack Trace", DebugStackTrace);
			return true;
		}
		#endif

		// ReSharper disable Unity.PerformanceAnalysis
		public static void Print(LogType type, object message, Object context = null, string tag = null) {
			message ??= "<null>";
			var logMessage = message.ToString();
			OnLog.Invoke(type, tag, logMessage, context);

			if (type == LogType.Debug && !DebugLogging)
				return;

			try {
				// File info (line numbers) is expensive — only capture it for types that write the stack trace
				var needsStackTrace = DebugStackTrace || type is LogType.Error or LogType.Warning or LogType.Exception;
				// Avoid StackTrace entirely for Debug logs when stack trace is not needed —
				// walking a deep async call stack (UniTask state machines) can take several ms.
				System.Diagnostics.StackFrame[] frames = null;
				string stackTraceStr = null;
				if (needsStackTrace) {
					var stackTrace = new System.Diagnostics.StackTrace(2, needsStackTrace);
					frames         = stackTrace.GetFrames();
					stackTraceStr  = stackTrace.ToString();
				}
				var timestamp = DateTime.Now;

				// Write to file in a separate thread
				UniTask.Post(() => {
					try {
						var methodName = "<UnknownMethod>";
						var className  = "<UnknownClass>";

						if (frames is { Length: > 0 }) {
							var old    = 0;
							var method = frames[old].GetMethod();
							methodName = method?.Name ?? methodName;
							className  = method?.DeclaringType?.Name ?? className;
							while (ShouldSkipFrame(className, methodName) && frames.Length > ++old) {
								method     = frames[old].GetMethod();
								methodName = method?.Name ?? methodName;
								className  = method?.DeclaringType?.Name ?? className;
							}
						}

						lock (FileLock) {
							if (!IsInitialized) {
								Init();
								IsInitialized = true;
							} else if (_logWriter == null || !File.Exists(LogFile) || new FileInfo(LogFile).Length > MaxLogSize) {
								Init();
								// Note: Avoid recursive call here, just log directly
								_logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] [Log] [Logger.OnLog] Log file exceeded maximum size and was rotated.");
							}

							// Use the persistent writer — avoids per-call FileStream open/close overhead
							_logWriter.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] [{type}] [{(string.IsNullOrEmpty(tag) ? "" : tag + ":")}{className}.{methodName}] {logMessage}");

							if (stackTraceStr != null)
								_logWriter.WriteLine(stackTraceStr);
						}
					} catch (Exception e) {
						// Can't use custom logging here to avoid infinite recursion
						ULogger.LogException(e);
					}
				});

				if (History.Count > MaxLogLines)
					History.RemoveAt(0);

				var entry = new LogEntry {
					Type      = type,
					Tag       = tag,
					Message   = logMessage,
					Context   = context,
					Timestamp = timestamp
				};
				History.Add(entry);

				var parsed = ParseToString(entry);

				switch (type) {
					case LogType.Log:
						ULogger.Log(parsed, context);
						break;
					case LogType.Warning:
						ULogger.LogWarning(parsed, context);
						break;
					case LogType.Error:
						ULogger.LogError(parsed, context);
						break;
					case LogType.Debug:
						ULogger.Log(parsed, context);
						break;
					#if UNITY_EDITOR
					case LogType.Editor:
						ULogger.Log(parsed, context);
						break;
					#endif
					case LogType.Assert:
						ULogger.LogAssertion(parsed, context);
						break;
					case LogType.Exception:
						ULogger.LogError(
							message is Exception o
								? new Exception(parsed, o)
								: new Exception(parsed),
							context);
						break;
					default:
						ULogger.Log(parsed, context);
						break;
				}
			} catch (Exception e) {
				ULogger.LogException(e);
			}
		}

		private static bool ShouldSkipFrame(string className, string methodName) {
			if (className.StartsWith("<"))
				return true;
			var fullName = $"{className}.{methodName}";
			for (var i = 0; i < IgnoreStack.Length; i++)
				if (fullName.Contains(IgnoreStack[i]))
					return true;
			return false;
		}

		private static string ParseToString(LogEntry entry) {
			var tagPart = string.IsNullOrEmpty(entry.Tag) ? "" : $" [<color=#{ColorFromTag(entry.Tag):X6}>{entry.Tag}</color>]";
			return $"<b>[<color=#{ColorFromType(entry.Type):X6}>{entry.Type.ToString().ToUpperInvariant()}</color>]{tagPart}</b> {entry.Message}";
		}

		private static int ColorFromTag(string tag) {
			if (string.IsNullOrEmpty(tag))
				return 0xFFFFFF;

			var hash = Hash.CRC32(tag);
			var r    = (hash & 0xFF0000) >> 16;
			var g    = (hash & 0x00FF00) >> 8;
			var b    = (hash & 0x0000FF);
			return (r << 16) | (g << 8) | b;
		}

		private static int ColorFromType(LogType type) {
			return type switch {
				LogType.Error     => 0xFF0000,
				LogType.Warning   => 0xFFFF00,
				LogType.Log       => 0xFFFFFF,
				LogType.Debug     => 0x00FFFF,
				LogType.Assert    => 0xFF00FF,
				LogType.Exception => 0xFF8000,
				#if UNITY_EDITOR
				LogType.Editor => 0x00FF00,
				#endif
				_ => 0xFFFFFF
			};
		}
	}

	public enum LogType {
		Debug,
		Log,
		Warning,
		Error,
		Assert,
		Exception,
		#if UNITY_EDITOR
		Editor,
		#endif
	}

	public class LogEntry {
		public LogType Type;
		public string Tag;
		public string Message;
		public Object Context;
		public DateTime Timestamp;
	}
}