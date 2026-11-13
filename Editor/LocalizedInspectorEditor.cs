using Nox.CCK.Language;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Editor {
	/// <summary>
	/// <see cref="InspectorEditor{T}"/> whose markup stays localized: every element carrying a <c>lang-key</c>
	/// (see <see cref="ILocalizedElement"/> and the localized elements of the CCK) shows the entry of that key,
	/// formatted with its <c>userData</c> as arguments, and is refreshed when the language changes. The header
	/// title is localized too, through <see cref="TitleKey"/>.
	/// </summary>
	public abstract class LocalizedInspectorEditor<T> : InspectorEditor<T> where T : UnityEngine.Object {
		/// <summary>
		/// Language entry shown as the header title, or <c>null</c> to keep <see cref="InspectorEditor{T}.Title"/>.
		/// </summary>
		protected virtual string TitleKey => null;

        /// <summary>
        /// Title of the panel and of the header of the inspector window: the entry of <see cref="TitleKey"/>
        /// when there is one, so the window header is localized too.
        /// </summary>
        protected override string Title
                => TitleKey == null ? base.Title : LanguageManager.Get(TitleKey);

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();

			// The header belongs to the shared template: give it its key once the base has built the root.
			if (TitleKey != null && Root?.Q<LocalizedLabel>("header-label") is { } header)
				header.LangKey = TitleKey;

			return root;
		}

		protected override void OnAttachToPanel(AttachToPanelEvent evt) {
			base.OnAttachToPanel(evt);

			LocalizeTexts();
			LanguageManager.OnLanguageChanged.AddListener(OnLanguageChanged);
		}

		protected override void OnDetachFromPanel(DetachFromPanelEvent evt) {
			base.OnDetachFromPanel(evt);

			LanguageManager.OnLanguageChanged.RemoveListener(OnLanguageChanged);
		}

		private void OnLanguageChanged(string language) {
			LocalizeTexts();

			// The header of the inspector window is drawn by IMGUI: it has to be repainted to pick the new title up.
			UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
		}

		/// <summary>
		/// Applies the <c>lang-key</c> of every element of the panel — header included — with the arguments
		/// currently held by their <c>userData</c>. Call it after assigning new arguments; the language change
		/// calls it already. Override it to localize what is not a localized element (a field label).
		/// </summary>
		protected virtual void LocalizeTexts() {
			// The header is not part of Content: walk the whole root so it follows the language too.
			var panel = Root ?? Content;
			if (panel != null)
				Localize(panel);
		}

		private static void Localize(VisualElement element) {
			if (element is ILocalizedElement localized)
				localized.RefreshText();

			foreach (var child in element.Children())
				Localize(child);
		}
	}
}
