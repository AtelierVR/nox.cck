using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Utils {
	public class Config {
		public static string GetPath() {
			#if UNITY_EDITOR
			var editorConfig = LoadEditor().Get<string>("global.config");
			if (!string.IsNullOrEmpty(editorConfig))
				return editorConfig;
			#endif

			var parsed = ArgsParser.Parse();
			var configArg = parsed.Get("config");
			if (!string.IsNullOrEmpty(configArg))
				return configArg;

			return Path.Combine(Constants.AppPath, "config.json");
		}

		public static Config Current;

		private JObject _jsonObject = new();
		private string _path;


		public static Config Load(bool force = false) {
			if (Current != null && !force) return Current;
			var path = GetPath();
			if (!File.Exists(path))
				return new Config { _path = path }.Save();
			var jsonString = File.ReadAllText(path);
			try {
				var config = new Config { _jsonObject = JObject.Parse(jsonString), _path = path };
				Current = config;
				return config;
			} catch (JsonReaderException) {
				var backupPath = path + ".bak";
				if (File.Exists(backupPath))
					File.Delete(backupPath);
				File.Move(path, backupPath);
				Logger.LogWarning($"Corrupted config file backed up to {backupPath}, creating fresh default.", nameof(Config));
				return new Config { _path = path }.Save();
			}
		}

		#if UNITY_EDITOR
		public static string GetEditorPath() {
			var parsed = ArgsParser.Parse();
			var editorConfigArg = parsed.Get("editor-config");
			if (!string.IsNullOrEmpty(editorConfigArg))
				return editorConfigArg;

			return Path.Combine(Application.dataPath, "..", "Library", "NoxEditorConfig.json");
		}

		public static Config CurrentEditor;

		public static Config LoadEditor(bool force = false) {
			if (CurrentEditor != null && !force) return CurrentEditor;
			var path = GetEditorPath();
			if (!File.Exists(path))
				return new Config { _path = path }.Save();
			var jsonString = File.ReadAllText(path);
			var config = new Config { _jsonObject = JObject.Parse(jsonString), _path = path };
			CurrentEditor = config;
			return config;
		}

		[UnityEditor.MenuItem("Nox/Config/Edit Config")]
		private static void EditConfig() {
			if (File.Exists(GetPath()))
				UnityEditor.EditorUtility.OpenWithDefaultApp(GetPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reveal Config")]
		private static void OpenConfigFolder() {
			if (File.Exists(GetPath()))
				UnityEditor.EditorUtility.RevealInFinder(GetPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Edit Editor Config")]
		private static void EditEditorConfig() {
			if (File.Exists(GetEditorPath()))
				UnityEditor.EditorUtility.OpenWithDefaultApp(GetEditorPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reveal Editor Config")]
		private static void OpenEditorConfigFolder() {
			if (File.Exists(GetEditorPath()))
				UnityEditor.EditorUtility.RevealInFinder(GetEditorPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reload Config")]
		private static void ReloadConfig() {
			Load(force: true);
			UnityEditor.EditorUtility.DisplayDialog("Nox Config", "Config reloaded.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reload Editor Config")]
		private static void ReloadEditorConfig() {
			LoadEditor(force: true);
			UnityEditor.EditorUtility.DisplayDialog("Nox Config", "Editor config reloaded.", "OK");
		}
		#endif

		public bool Has(string propertyName)
			=> Has(propertyName.Split('.'));

		public bool Has(string[] propertyPathName) {
			JToken current = _jsonObject;
			
			for (var i = 0; i < propertyPathName.Length; i++) {
				if (current is not { Type: JTokenType.Object })
					return false;
				current = current[propertyPathName[i]];
				if (current == null)
					return false;
			}
			
			return true;
		}

		public JToken Get(string propertyName)
			=> Get(propertyName.Split('.'));

		public JToken Get(string[] propertyPathName) {
			JToken current = _jsonObject;
			
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				if (current is not { Type: JTokenType.Object })
					return null;
				current = current[propertyPathName[i]];
			}
			
			return current?[propertyPathName[^1]];
		}

		public JObject Get()
			=> _jsonObject;

		public T Get<T>(string propertyName, T defaultValue = default)
			=> Get(propertyName.Split('.'), defaultValue);

		public T Get<T>(string[] propertyPathName, T defaultValue = default) {
			var token = Get(propertyPathName);
			return token == null ? defaultValue : token.ToObject<T>();
		}

		public void Set<T>(string propertyName, T value)
			=> Set(propertyName.Split('.'), value);

		public void Set<T>(string[] propertyPathName, T value) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				// Ensure the intermediate node is an object. If it doesn't exist or isn't an object, replace it with a new JObject.
				var child = current[propertyPathName[i]];
				if (child == null || child.Type != JTokenType.Object) {
					var obj = new JObject();
					current[propertyPathName[i]] = obj;
					current = obj;
				}
				else current = (JObject)child;
			}

			if (value == null)
				current.Remove(propertyPathName[^1]);
			else current[propertyPathName[^1]] = JToken.FromObject(value);
		}

		public void Remove(string propertyName)
			=> Remove(propertyName.Split('.'));

		public void Remove(string[] propertyPathName) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				var child = current[propertyPathName[i]];
				if (child is not { Type: JTokenType.Object })
					return;
				current = (JObject)child;
			}

			current.Remove(propertyPathName[^1]);
		}

		public Config Save() {
			File.WriteAllText(_path, _jsonObject.ToString(Formatting.Indented));
			return this;
		}
	}
}