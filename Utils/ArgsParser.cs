using System;
using System.Collections.Generic;
using System.Linq;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Parses command-line arguments in a yargs-like fashion.
	/// Supports:
	///   --key value        named argument
	///   --key=value        named argument (inline)
	///   --flag             boolean flag (true)
	///   --no-flag          boolean flag (false)
	///   -k value           short named argument
	///   -k=value           short named argument (inline)
	///   -abc               multiple boolean short flags
	///   positional         positional arguments (no dash prefix)
	///   --key v1 --key v2  repeated key → string array
	/// </summary>
	public static class ArgsParser {
		public sealed class ParsedArgs {
			private readonly Dictionary<string, List<string>> _named;
			private readonly List<string> _positional;

			public const char DEFAULT_SEPARATOR = ',';

			internal ParsedArgs(Dictionary<string, List<string>> named, List<string> positional) {
				_named = named;
				_positional = positional;
			}

			// ── Has ──────────────────────────────────────────────────────────

			public bool Has(string key) 
				=> _named.ContainsKey(Normalize(key));

			// ── Get single value ─────────────────────────────────────────────

			public string Get(string key, string defaultValue = null) {
				var k = Normalize(key);
				return _named.TryGetValue(k, out var list) && list.Count > 0 ? list[^1] : defaultValue;
			}

			public T Get<T>(string key, T defaultValue = default) {
				var raw = Get(key);
				if (raw == null) return defaultValue;
				try { return (T)Convert.ChangeType(raw, typeof(T)); }
				catch { return defaultValue; }
			}

			// ── Get boolean ──────────────────────────────────────────────────

			/// <summary>
			/// Returns true if --key is present (and not overridden by --no-key).
			/// Returns false if --no-key is present.
			/// Returns defaultValue if neither is present.
			/// </summary>
			public bool GetBool(string key, bool defaultValue = false) {
				var k = Normalize(key);
				var noKey = "no-" + k;
				if (_named.ContainsKey(noKey)) return false;
				if (!_named.TryGetValue(k, out var list) || list.Count == 0) return defaultValue;
				var last = list[^1];
				// bare flag → stored as empty string
				if (string.IsNullOrEmpty(last)) return true;
				return last is "1" or "true" or "yes";
			}

			// ── Get array ────────────────────────────────────────────────────

			public string[] GetArray(string key, char separator = '\0') {
				if (!_named.TryGetValue(Normalize(key), out var list))
					return Array.Empty<string>();
				if (separator == '\0')
					return list.ToArray();
				var result = new List<string>();
				foreach (var raw in list)
					if (raw.Contains(separator))
						result.AddRange(raw.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
					else
						result.Add(raw);
				return result.ToArray();
			}

			public T[] GetArray<T>(string key, char separator = '\0') {
				return GetArray(key, separator)
					.Select(v => {
						try { return ((T)Convert.ChangeType(v, typeof(T)), true); }
						catch { return (default, false); }
					})
					.Where(t => t.Item2)
					.Select(t => t.Item1)
					.ToArray();
			}

			// ── Get dictionary ──────────────────────────────────────────────

			/// <summary>
			/// Parses repeated --key k=v entries into a dictionary.
			/// Example: --noxDep "mod=1.0" --noxDep "lib=2.0" → {{"mod","1.0"},{"lib","2.0"}}
			/// If a value has no '=', it's used as both key and value.
			/// If separator is specified, also splits each value on that separator.
			/// Example: --noxOutput "Win=path;Linux=path" with separator ';'
			/// </summary>
			public Dictionary<string, string> GetDictionary(string key, char separator = DEFAULT_SEPARATOR) {
				var result = new Dictionary<string, string>();
				void AddEntry(string entry) {
					var eq = entry.IndexOf('=');
					var k = eq >= 0 ? entry[..eq].Trim() : entry.Trim();
					var v = eq >= 0 ? entry[(eq + 1)..].Trim() : entry.Trim();
					if (!string.IsNullOrEmpty(k)) result[k] = v;
				}
				foreach (var raw in GetArray(key))
					if (separator != '\0' && raw.Contains(separator)) {
						foreach (var part in raw.Split(separator, StringSplitOptions.RemoveEmptyEntries))
							AddEntry(part.Trim());
					} else AddEntry(raw);
				return result;
			}

			// ── Positional ───────────────────────────────────────────────────

			public string[] Positional 
				=> _positional.ToArray();

			public string GetPositional(int index, string defaultValue = null)
				=> index >= 0 && index < _positional.Count ? _positional[index] : defaultValue;

			// ── Raw ──────────────────────────────────────────────────────────

			public IReadOnlyDictionary<string, List<string>> Raw => _named;

			// ── Helpers ──────────────────────────────────────────────────────

			private static string Normalize(string key) {
				var k = key.TrimStart('-');
				// Short flags (-n, -N) are case-sensitive; long flags (--name) are lowercase
				return k.Length == 1 ? k : k.ToLowerInvariant();
			}

			public override string ToString() {
				var parts = new List<string>();
				foreach (var kv in _named)
					foreach (var v in kv.Value)
						parts.Add(string.IsNullOrEmpty(v) ? $"--{kv.Key}" : $"--{kv.Key}={v}");
				parts.AddRange(_positional);
				return string.Join(" ", parts);
			}
		}

		// ── Parse overloads ──────────────────────────────────────────────────

		/// <summary>Parses System.Environment.GetCommandLineArgs() (skips index 0 = executable).</summary>
		public static ParsedArgs Parse() => Parse(Environment.GetCommandLineArgs(), skip: 1);

		/// <summary>Parses a pre-split array of tokens.</summary>
		public static ParsedArgs Parse(string[] args, int skip = 0) {
			var named = new Dictionary<string, List<string>>();
			var positional = new List<string>();

			var tokens = args.Skip(skip).ToArray();

			void AddNamed(string key, string value, bool isLong = false) {
				var k = isLong ? key.ToLowerInvariant() : key;
				if (!named.TryGetValue(k, out var list)) named[k] = list = new List<string>();
				list.Add(value);
			}

			for (var i = 0; i < tokens.Length; i++) {
				var token = tokens[i];

				// Long flag: --key, --key=value, --no-key
				if (token.StartsWith("--")) {
					var body = token.Substring(2);
					var eq = body.IndexOf('=');
					if (eq >= 0) {
						AddNamed(body.Substring(0, eq), body.Substring(eq + 1).Trim('"'), isLong: true);
					} else {
						var nextIsValue = i + 1 < tokens.Length && !tokens[i + 1].StartsWith("-");
						if (nextIsValue)
							AddNamed(body, tokens[++i], isLong: true);
						else
							AddNamed(body, string.Empty, isLong: true);
					}
					continue;
				}

				// Short flag: -k, -k=value, -k value, -Kvalue
				if (token.StartsWith("-") && token.Length > 1) {
					var body = token.Substring(1);
					var eq = body.IndexOf('=');
					if (eq >= 0) {
						// -k=value
						AddNamed(body.Substring(0, eq), body.Substring(eq + 1).Trim('"'));
					} else if (body.Length == 1) {
						// -k [value]
						var nextIsValue = i + 1 < tokens.Length && !tokens[i + 1].StartsWith("-");
						if (nextIsValue)
							AddNamed(body, tokens[++i]);
						else
							AddNamed(body, string.Empty);
					} else {
						// -abc → multi boolean flags: -a -b -c
						foreach (var c in body)
							AddNamed(c.ToString(), string.Empty);
					}
					continue;
				}

				// Positional
				positional.Add(token);
			}

			return new ParsedArgs(named, positional);
		}

		/// <summary>Parses a single command-line string by splitting on spaces (respects quoted strings).</summary>
		public static ParsedArgs Parse(string commandLine) 
			=> Parse(SplitCommandLine(commandLine));

		// ── Utility ──────────────────────────────────────────────────────────

		public static string[] SplitCommandLine(string commandLine) {
			var args = new List<string>();
			var current = new System.Text.StringBuilder();
			var inQuotes = false;
			var quoteChar = '"';

			foreach (var c in commandLine) 
				if (c == '"' || c == '\'') {
					if (inQuotes && c == quoteChar) { inQuotes = false; }
					else if (!inQuotes) { inQuotes = true; quoteChar = c; }
					else current.Append(c);
				} else if (c == ' ' && !inQuotes) {
					if (current.Length > 0) { args.Add(current.ToString()); current.Clear(); }
				} else {
					current.Append(c);
				}

			if (current.Length > 0) args.Add(current.ToString());
			return args.ToArray();
		}
	}
}
