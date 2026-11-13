using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.CCK.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Utils {
	public class Config : ISavableContainer {
		/// <summary>
		/// Raised by <c>Set</c>/<c>Remove</c> with the key, the new and the old value. Listeners read the
		/// new value back with <see cref="Get{T}(string,T)"/>.
		/// </summary>
		public static readonly NoxEvent<string[], object, object> OnChanged = new();

		/// <summary>Raised when a config is reloaded from disk: every value may have changed.</summary>
		public static readonly UnityEvent OnReloaded = new();

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


		/// <summary>
		/// Loads the config, reading the file on the calling thread. Settings are then served from memory
		/// by <see cref="Get{T}(string[],T)"/>, so the readers (property getters polled every frame) stay
		/// synchronous.
		/// </summary>
		public static Config Load(bool force = false) {
			if (Current != null && !force)
				return Current;
			var path = GetPath();
			if (!File.Exists(path))
				return new Config { _path = path }.Save();
			return Apply(path, File.ReadAllText(path), force);
		}

		/// <summary>Same as <see cref="Load"/>, with the file read moved off the main thread.</summary>
		public static async UniTask<Config> LoadAsync(bool force = false) {
			if (Current != null && !force)
				return Current;
			var path = GetPath();
			if (!File.Exists(path))
				return new Config { _path = path }.Save();
			var jsonString = await UniTask.RunOnThreadPool(() => File.ReadAllText(path));
			return Apply(path, jsonString, force);
		}

		/// <summary>Publishes the json of <paramref name="path"/> as the current config.</summary>
		private static Config Apply(string path, string jsonString, bool force) {
			try {
				var config = new Config { _jsonObject = JObject.Parse(jsonString), _path = path };
				Current = config;

#if UNITY_EDITOR
				try {
					_lastWriteTime = File.GetLastWriteTimeUtc(path);
				} catch (Exception) {
					// ignored
				}

				WatchFile(path);
#endif

				if (force)
					NotifyReloaded();

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

			if (force)
				NotifyReloaded();

			return config;
		}

		// La position d'un sous-menu dans "Nox/" est dérivée des priorités de ses enfants.
		// Pour rester collé aux autres sous-menus Nox (priorité par défaut 1000), TOUTES
		// les priorités d'ici doivent tenir dans [990, 1010] - sans quoi Nox/Config sort
		// du bloc. Un écart de 11 (> 10) suffit à créer le séparateur entre les deux
		// sections, tout en restant dans cette fenêtre.
		private const int ConfigPriority = 994;  // Edit Config, Reveal Config, Reload Config
		private const int EditorConfigPriority = 1007; // Edit/Reveal/Reload Editor Config

		[UnityEditor.MenuItem("Nox/Config/Edit Config", false, ConfigPriority)]
		private static void EditConfig() {
			if (File.Exists(GetPath()))
				UnityEditor.EditorUtility.OpenWithDefaultApp(GetPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reveal Config", false, ConfigPriority + 1)]
		private static void OpenConfigFolder() {
			if (File.Exists(GetPath()))
				UnityEditor.EditorUtility.RevealInFinder(GetPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reload Config", false, ConfigPriority + 2)]
		private static void ReloadConfig() {
			Load(force: true);
			UnityEditor.EditorUtility.DisplayDialog("Nox Config", "Config reloaded.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Edit Editor Config", false, EditorConfigPriority)]
		private static void EditEditorConfig() {
			if (File.Exists(GetEditorPath()))
				UnityEditor.EditorUtility.OpenWithDefaultApp(GetEditorPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reveal Editor Config", false, EditorConfigPriority + 1)]
		private static void OpenEditorConfigFolder() {
			if (File.Exists(GetEditorPath()))
				UnityEditor.EditorUtility.RevealInFinder(GetEditorPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reload Editor Config", false, EditorConfigPriority + 2)]
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

			if (current is not { Type: JTokenType.Object })
				return null;

			return current[propertyPathName[^1]];
		}

		public JObject Get()
			=> _jsonObject;

		public T Get<T>(string propertyName, T defaultValue = default)
			=> Get(propertyName.Split('.'), defaultValue);

		public T Get<T>(string[] propertyPathName, T defaultValue = default) {
			var token = Get(propertyPathName);
			if (token == null)
				return defaultValue;

			if (token is JValue value) {
				if (value.Value is T typed)
					return typed;
				if ((typeof(T).IsPrimitive || typeof(T) == typeof(string))
					&& (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
					return value.Value.To<T>();
			}

			return token.ToObject<T>();
		}

		public void Set<T>(string propertyName, T value)
			=> Set(propertyName.Split('.'), value);

		public void Set<T>(string[] propertyPathName, T value) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				var child = current[propertyPathName[i]];
				if (child == null || child.Type != JTokenType.Object) {
					var obj = new JObject();
					current[propertyPathName[i]] = obj;
					current = obj;
				} else current = (JObject)child;
			}

			var key = propertyPathName[^1];
			var previous = current[key];
			var next = value == null
				? null
				: JToken.FromObject(value);

			if (JToken.DeepEquals(previous, next))
				return;

			if (next == null)
				current.Remove(key);
			else current[key] = next;

			OnChanged.Invoke(propertyPathName, value, previous?.ToObject<object>());
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

			var key = propertyPathName[^1];
			var previous = current[key];

			if (previous == null)
				return;

			current.Remove(key);
			OnChanged.Invoke(propertyPathName, null, previous.ToObject<object>());
		}


		// IDataContainer / ISavableContainer


		/// <summary>The container event is the config one: every instance notifies the same listeners.</summary>
		NoxEvent<string[], object, object> IDataContainer.OnChanged
			=> OnChanged;

		void IDataContainer.Delete(string key)
			=> Remove(key);

		void IDataContainer.Delete(string[] key)
			=> Remove(key);

		void IDisposable.Dispose() {
			// Nothing unmanaged to release: the editor file watcher is a static hook that outlives the
			// instance.
		}

		UniTask ISavableContainer.Load()
			=> LoadAsync();

		async UniTask ISavableContainer.Save()
			=> await SaveAsync();

		/// <summary>Notifies the listeners that the config was reloaded from disk.</summary>
		private static void NotifyReloaded() {
			try {
				OnReloaded.Invoke();
			} catch (Exception exception) {
				Logger.LogError(exception);
			}
		}

		/// <summary>Writes the config to disk on the calling thread (the change is persisted when this returns).</summary>
		public Config Save() {
			File.WriteAllText(_path, _jsonObject.ToString(Formatting.Indented));
			return Saved();
		}

		/// <summary>Same as <see cref="Save"/>, with the write moved off the main thread.</summary>
		public async UniTask<Config> SaveAsync() {
			var json = _jsonObject.ToString(Formatting.Indented);
			await UniTask.RunOnThreadPool(() => File.WriteAllText(_path, json));
			return Saved();
		}

		/// <summary>Remembers the write time, so the editor watcher ignores our own write.</summary>
		private Config Saved() {
#if UNITY_EDITOR
			try {
				_lastWriteTime = File.GetLastWriteTimeUtc(_path);
				_lastSaveTime = _lastWriteTime;
			} catch (Exception) {
				// ignored
			}
#endif

			return this;
		}

#if UNITY_EDITOR
		private static DateTime _lastWriteTime;
		private static DateTime _lastSaveTime;
		private static string _watchedPath;
		private static bool _watching;

		/// <summary>Smallest interval between two file stats of the watched config, in seconds.</summary>
		private const double PollInterval = 0.5;

		/// <summary>Next allowed poll (<c>EditorApplication.timeSinceStartup</c>).</summary>
		private static double _nextPoll;

		/// <summary>Reloads the config when the file is edited outside Unity (editor only).</summary>
		private static void WatchFile(string path) {
			if (_watching || string.IsNullOrEmpty(path))
				return;

			_watching = true;
			_watchedPath = path;
			UnityEditor.EditorApplication.update += PollFile;
		}

		private static void PollFile() {
			var now = UnityEditor.EditorApplication.timeSinceStartup;
			if (now < _nextPoll)
				return;

			_nextPoll = now + PollInterval;

			DateTime time;

			try {
				time = File.GetLastWriteTimeUtc(_watchedPath);
			} catch (Exception) {
				return;
			}

			if (time == _lastWriteTime)
				return;

			_lastWriteTime = time;

			if (time == _lastSaveTime)
				return;

			Logger.Log("Config changed on disk, reloading.", nameof(Config));
			Load(force: true);
		}
#endif
	}
}
