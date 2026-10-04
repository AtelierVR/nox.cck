using Nox.CCK.Language;

namespace Nox.CCK.Convertors {
	/// <summary>Resolution of a <see cref="DictionnaryOrString"/> for display.</summary>
	public static class DictionnaryOrStringExtensions {
		/// <summary>
		/// Returns the best translation for <paramref name="language"/> (the current UI language by
		/// default), walking the fallback chain, or <c>null</c> when the value holds no text.
		/// </summary>
		public static string Resolve(this DictionnaryOrString value, string language = null) {
			if (value == null || value.Count == 0)
				return null;

			foreach (var lang in LanguageManager.BuildChain(language ?? LanguageManager.CurrentLanguage))
				if (value.TryGetValue(lang, out var text) && !string.IsNullOrEmpty(text))
					return text;

			foreach (var entry in value)
				if (!string.IsNullOrEmpty(entry.Value))
					return entry.Value;

			return null;
		}

		/// <summary>
		/// Wraps a plain text into a <see cref="TranslatedString"/> holding a single translation for
		/// <paramref name="language"/> (the fallback language by default), or <c>null</c> when the
		/// text is <c>null</c>. A single translation is written as a plain string on the wire.
		/// </summary>
		public static TranslatedString ToTranslated(this string value, string language = null)
			=> value == null
				? null
				: new TranslatedString { [language ?? LanguageManager.FallbackLanguage] = value };
	}
}
