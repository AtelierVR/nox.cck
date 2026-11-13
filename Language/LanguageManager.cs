using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Language {
	public class LanguageManager {
		public static readonly UnityEvent<string> OnLanguageChanged = new();
		public static readonly UnityEvent OnPackListUpdated = new();

		public const string FallbackLanguage = "en-US";

		public static string DefaultLanguage
			=> CultureInfo.CurrentCulture.IetfLanguageTag;

		private static string _currentLanguage = DefaultLanguage;

		public static string CurrentLanguage {
			get => _currentLanguage;
			set {
				if (value == _currentLanguage)
					return;
				_currentLanguage = value;
				Chains.Clear();
				UpdateTexts();
				OnLanguageChanged.Invoke(_currentLanguage);
			}
		}

		private static readonly List<LanguagePack> LanguagePacks = new();

		private static List<LanguagePack> _packs;


		static LanguageManager() {
			#if UNITY_EDITOR
			// The list below comes from the project (AssetDatabase, out of Play mode) and from the packs registered
			// by the running mods: it is cached, so it is dropped whenever one of the two changes.
			EditorApplication.projectChanged += InvalidatePacks;
			EditorApplication.playModeStateChanged += _ => InvalidatePacks();
			#endif
		}

		/// <summary>
		/// Packs the entries are resolved in: the ones registered by the mods, then, out of Play mode, the ones
		/// authored in the project. Cached, because a lookup happens for every text of every panel.
		/// </summary>
		private static List<LanguagePack> GetPacksList() {
			if (_packs != null)
				return _packs;

			var packs = new List<LanguagePack>();
			AddAlive(packs, LanguagePacks);
			#if UNITY_EDITOR
			// No mod registered anything out of Play mode: the project packs are the AssetDatabase ones. While
			// playing, each mod registers its own (see its main entrypoint), so nothing is scanned here.
			if (!Application.isPlaying)
				AddAlive(packs, EditorPacks());
			#endif
			return _packs = packs;
		}

		private static void AddAlive(List<LanguagePack> packs, List<LanguagePack> source) {
			foreach (var pack in source)
				if (pack && !packs.Contains(pack))
					packs.Add(pack);
		}

		private static void InvalidatePacks()
			=> _packs = null;

		#if UNITY_EDITOR
		/// <summary>Packs authored in the project, read from the AssetDatabase (editor, out of Play mode).</summary>
		private static List<LanguagePack> EditorPacks() {
			var packs = new List<LanguagePack>();
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(LanguagePack)}"))
				try {
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var pack = AssetDatabase.LoadAssetAtPath<LanguagePack>(path);
					if (pack && !packs.Contains(pack)) packs.Add(pack);
				} catch (Exception e) {
					Logger.LogError(e);
				}

			return packs;
		}
		#endif

		public static LanguagePack[] GetPacks()
			=> GetPacksList().ToArray();

		public static string[] GetAvailableLanguages() {
			// Every pack the texts are resolved in, so the editor (where mods may not have registered
			// anything) lists the languages of the project packs too.
			var languages = new List<string>();

			foreach (var pack in GetPacksList()) {
				if (!pack || pack.languages == null)
					continue;

				foreach (var language in pack.languages) {
					if (language == null || string.IsNullOrEmpty(language.IETF) || languages.Contains(language.IETF))
						continue;

					languages.Add(language.IETF);
				}
			}

			return languages.ToArray();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public static void UpdateTexts() {
			var texts = ComponentExtension.GetComponentsInChildren<TextLanguage>();
			foreach (var text in texts)
				text.UpdateText();
		}

		/// <summary>
		/// Builds the IETF resolution chain: e.g. "fr-FR" → ["fr-FR", "fr", "en-US", "en"].
		/// <para>
		/// Cached: every text lookup of every panel builds it, and the previous version allocated a
		/// <c>HashSet</c>, five <c>List</c> and their arrays on each call. The key is the pair
		/// (language, current language), the only two inputs that can vary.
		/// </para>
		/// </summary>
		public static string[] BuildChain(string lang) {
			var key = (lang ?? string.Empty, CurrentLanguage);
			if (Chains.TryGetValue(key, out var cached))
				return cached;

			var chain = new List<string>(8);
			AddChain(chain, lang);
			AddChain(chain, CurrentLanguage);
			AddChain(chain, DefaultLanguage);
			AddChain(chain, FallbackLanguage);

			return Chains[key] = chain.ToArray();
		}

		/// <summary>Chains already built, keyed by (language, current language).</summary>
		private static readonly Dictionary<(string, string), string[]> Chains = new();

		/// <summary>Appends a language then its root ("fr-CA" → "fr"), keeping the first occurrence only.</summary>
		private static void AddChain(List<string> chain, string lang) {
			if (string.IsNullOrEmpty(lang) || chain.Contains(lang))
				return;

			chain.Add(lang);

			var dash = lang.IndexOf('-');
			if (dash <= 0)
				return;

			var root = lang[..dash];
			if (!chain.Contains(root))
				chain.Add(root);
		}

		/// <summary>
		/// Resolves a key in a single pack through the fallback chain.
		/// Returns (value, resolvedLang) if found, null otherwise.
		/// </summary>
		public static (string value, string resolvedLang)? GetInPack(LanguagePack pack, string key, string language) {
			if (!pack || string.IsNullOrEmpty(key))
				return null;
			foreach (var lang in BuildChain(language))
				if (pack.TryGetLocalizedString(lang, key, out var value))
					return (value, lang);
			return null;
		}

		public static string Get(string key) {
			foreach (var lang in BuildChain(CurrentLanguage)) {
				var v = GetInPacks(lang, key);
				if (v != null)
					return v;
			}
			return $"[{key}]";
		}

		#if UNITY_EDITOR
		[UnityEditor.MenuItem("Nox/Tools/Reload LanguageTexts")]
		public static void ReloadLanguageTexts()
			=> UpdateTexts();
		#endif

        public static string Get(string language, string key)
			=> GetInPacks(language, key);

		public static string Get(string key, params object[] args) {
			var value = Get(key);
			try {
				return string.Format(value, args);
			} catch {
				// ignored
			}

			return value;
		}

		public static string Get(string language, string key, params object[] args) {
			var value = Get(language, key);
			try {
				return string.Format(value, args);
			} catch (Exception e) {
				Logger.LogError(e);
			}

			return value;
		}

		public static void AddPack(LanguagePack pack) {
			if (!pack) {
				Logger.LogWarning("Attempted to add a null language pack.");
				return;
			}
			if (LanguagePacks.Contains(pack))
				return;
			pack.languages ??= Array.Empty<LanguagePack.LanguageData>();
			pack.languages = pack.languages
				.Where(l => l != null && !string.IsNullOrEmpty(l.IETF) && l.entries != null)
				.ToArray();
			pack.InvalidateIndex();
			LanguagePacks.Add(pack);
			InvalidatePacks();
			OnPackListUpdated.Invoke();
		}

		public static void RemovePack(LanguagePack pack) {
			if (!LanguagePacks.Remove(pack))
				return;
			InvalidatePacks();
			OnPackListUpdated.Invoke();
		}


		public static string GetInPacks(string key, List<LanguagePack> packs = null)
			=> GetInPacks(CurrentLanguage, key, packs);

		public static string GetInPacks(string language, string key, List<LanguagePack> packs = null) {
			packs ??= GetPacksList();
			for (var i = 0; i < packs.Count; i++) {
				var pack = packs[i];
				if (!pack)
					continue;
				if (pack.TryGetLocalizedString(language, key, out var value))
					return value;
			}
			return null;
		}

		public static bool Has(string language, string key, List<LanguagePack> packs = null) {
			packs ??= GetPacksList();
			for (var i = 0; i < packs.Count; i++) {
				var pack = packs[i];
				if (pack && pack.HasLocalizationString(language, key))
					return true;
			}
			return false;
		}

		public static bool Has(string key, List<LanguagePack> packs = null)
			=> Has(CurrentLanguage, key, packs);
	}
}
