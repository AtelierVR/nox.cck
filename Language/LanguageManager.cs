using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

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
				UpdateTexts();
				OnLanguageChanged.Invoke(_currentLanguage);
			}
		}

		private static readonly List<LanguagePack> LanguagePacks = new();

		public static LanguagePack[] GetPacks() {
			#if UNITY_EDITOR
			if (!Application.isPlaying) {
				var guids = UnityEditor.AssetDatabase.FindAssets("t:LanguagePack");
				var packs = new List<LanguagePack>();
				foreach (var guid in guids)
					try {
						var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
						var pack = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguagePack>(path);
						if (pack) packs.Add(pack);
					} catch (Exception e) {
						Logger.LogError(e);
					}
				return packs.ToArray();
			}
			#endif
			return LanguagePacks.ToArray();
		}

		public static string[] GetAvailableLanguages() {
			var languages = new List<string>();
			foreach (var language in from pack in LanguagePacks
				from language in pack.languages
				where !languages.Contains(language.IETF)
				select language)
				languages.Add(language.IETF);
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
		/// </summary>
		public static string[] BuildChain(string lang) {
			var chain = new HashSet<string>();

			var t = Chain(lang);
			foreach (var l in t)
				chain.Add(l);
			
			t = Chain(CurrentLanguage);
			foreach (var l in t)
				chain.Add(l);
			
			t = Chain(DefaultLanguage);
			foreach (var l in t)
				chain.Add(l);
			
			t = Chain(FallbackLanguage);
			foreach (var l in t)
				chain.Add(l);
				
			return chain.ToArray();

            static string[] Chain(string l) {
				var c = new List<string> { l };
				var d = l.IndexOf('-');
				if (d <= 0)
					return c.ToArray();
				var b = l[..d];
				if (!c.Contains(b))
					c.Add(b);
				return c.ToArray();
			}
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
		[UnityEditor.MenuItem("Nox/Reload LanguageTexts")]
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
			OnPackListUpdated.Invoke();
		}

		public static void RemovePack(LanguagePack pack) {
			if (!LanguagePacks.Contains(pack)) {
				Logger.LogWarning("Attempted to remove a language pack that is not in the list.", pack);
				return;
			}
			LanguagePacks.Remove(pack);
			OnPackListUpdated.Invoke();
		}


		public static string GetInPacks(string key, List<LanguagePack> packs = null)
			=> GetInPacks(CurrentLanguage, key, packs);

		public static string GetInPacks(string language, string key, List<LanguagePack> packs = null) {
			packs ??= GetPacks().ToList();
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
			packs ??= GetPacks().ToList();
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