using System.Collections.Generic;
using UnityEngine;

namespace Nox.CCK.Language {
	[CreateAssetMenu(fileName = "LanguagePack", menuName = "Nox/Language Pack", order = 1)]
	public class LanguagePack : ScriptableObject {
		[System.Serializable]
		public class LanguageData {
			public string IETF;

			public List<LanguageEntry> entries = new();
		}

		[System.Serializable]
		public class LanguageEntry {
			public string key;
			public string value;
		}

		public LanguageData[] languages;

		// IETF -> key -> value
		private Dictionary<string, Dictionary<string, string>> _index;

		private void BuildIndex() {
			_index = new Dictionary<string, Dictionary<string, string>>();
			if (languages == null) return;
			foreach (var lang in languages) {
				if (lang?.entries == null || string.IsNullOrEmpty(lang.IETF)) continue;
				var dict = new Dictionary<string, string>(lang.entries.Count);
				foreach (var e in lang.entries)
					if (e != null && e.key != null)
						dict.TryAdd(e.key, e.value);
				_index[lang.IETF] = dict;
			}
		}

		public void InvalidateIndex() => _index = null;

		private Dictionary<string, Dictionary<string, string>> EnsureIndex() {
			if (_index == null) BuildIndex();
			return _index;
		}

		public string GetLocalizedString(string key, string language) {
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language)) 
				return null;
			var idx = EnsureIndex();
			if (!idx.TryGetValue(language, out var dict)) return null;
			return dict.TryGetValue(key, out var value) ? value : null;
		}

		internal bool TryGetLocalizedString(string language, string key, out string value) {
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language)) { 
				value = null; 
				return false;
			}
			var idx = EnsureIndex();
			if (!idx.TryGetValue(language, out var dict)) { value = null; return false; }
			return dict.TryGetValue(key, out value) && value != null;
		}

		public bool HasLocalizationString(string language, string key) {
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language)) 
				return false;
			var idx = EnsureIndex();
			return idx.TryGetValue(language, out var dict) && dict.ContainsKey(key);
		}
	}
}