namespace Nox.CCK.Editor {
	/// <summary>
	/// A UIElements element whose text comes from a language entry (see
	/// <see cref="Nox.CCK.Language.LanguageManager"/>), the way <c>TextLanguage</c> does it for the runtime UI:
	/// the markup names the entry with a <c>lang-key</c> and the element keeps its text in sync.
	/// <para>
	/// The arguments formatted into the entry are the element's <c>userData</c>, so a value that changes (a
	/// count, a name) only has to be assigned to it.
	/// </para>
	/// </summary>
	public interface ILocalizedElement {
		/// <summary>Key of the language entry (the UXML <c>lang-key</c> attribute), or <c>null</c> for none.</summary>
		string LangKey { get; set; }

		/// <summary>Arguments formatted into the entry, stored in the element's <c>userData</c>.</summary>
		object[] LangArgs { get; set; }

		/// <summary>
		/// Re-applies the text: the language entry when the packs know it, the text authored in the UXML
		/// otherwise (so a missing translation never breaks the panel).
		/// </summary>
		void RefreshText();
	}
}
