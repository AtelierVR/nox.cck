using UnityEngine;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Marks a panel whose UI follows the theme of the session.
	/// <para>
	/// Nothing is themed without it: the UI of an avatar or of a world keeps its own colours unless it puts
	/// this component on its root, so a theme of the player never repaints someone else's panel.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Theme/Themed Root")]
	[DisallowMultipleComponent]
	public class ThemedRoot : MonoBehaviour {
		private void OnEnable() {
			ThemeManager.OnThemeChanged.AddListener(OnThemeChanged);
			ThemeManager.Apply(gameObject);
		}

		private void OnDisable()
			=> ThemeManager.OnThemeChanged.RemoveListener(OnThemeChanged);

		/// <summary>Repaints the panel again, for the content built after it was enabled.</summary>
		public void Refresh()
			=> ThemeManager.Apply(gameObject);

		private void OnThemeChanged(string _)
			=> Refresh();
	}
}
