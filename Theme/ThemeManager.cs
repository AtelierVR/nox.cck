using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Theme of the UI of the session, shared by every panel, like <see cref="Language.LanguageManager"/>.
	/// <para>
	/// One asset per theme (<see cref="ThemeAsset"/>): the themes registered by the mods, then the built-in
	/// ones, which are created in code. Only the panels marked with a <see cref="ThemedRoot"/> are touched, so
	/// the UI an avatar or a world brings with it keeps its own colours unless it marks itself as themed too.
	/// </para>
	/// </summary>
	public static class ThemeManager {
		/// <summary>Theme used until the settings say otherwise.</summary>
		public const string DEFAULT_THEME = "nox";

		/// <summary>Raised when the theme changes, with its id.</summary>
		public static readonly UnityEvent<string> OnThemeChanged = new();

		/// <summary>Themes registered by the mods, searched before the built-in ones.</summary>
		private static readonly List<ThemeAsset> _registered = new();

		private static ThemeAsset[] _builtin;

		/// <summary>Themes of every source, built when the registration changes.</summary>
		private static List<ThemeAsset> _resolved;

		private static string _current = DEFAULT_THEME;

		/// <summary>Theme that swaps nothing, used when no theme carries the current id.</summary>
		private static readonly ThemeAsset _untouched = CreateUntouched();

		/// <summary>Id of the theme in use. Changing it repaints every themed panel.</summary>
		public static string CurrentTheme {
			get => _current;
			set {
				var wanted = string.IsNullOrEmpty(value)
					? DEFAULT_THEME
					: value;
				if (wanted == _current)
					return;
				_current = wanted;
				ApplyAll();
				OnThemeChanged.Invoke(_current);
			}
		}

		/// <summary>
		/// Theme in use. Never <c>null</c>: an unknown id falls back to no swap at all, so a saved id that no
		/// theme defines leaves the UI as authored.
		/// </summary>
		public static ThemeAsset Current
			=> Find(_current) ?? _untouched;

		/// <summary>
		/// Themes every install has. Kept as one set so that the editor panel edits the very themes the
		/// manager reads, instead of copies that would be thrown away.
		/// </summary>
		public static ThemeAsset[] Builtin
			=> _builtin ??= ThemeAsset.Builtin();

		/// <summary>Theme of an id, or <c>null</c>.</summary>
		/// <param name="id">Id of the theme.</param>
		public static ThemeAsset Find(string id) {
			if (string.IsNullOrEmpty(id))
				return null;

			foreach (var theme in GetThemes())
				if (string.Equals(theme.id, id, System.StringComparison.OrdinalIgnoreCase))
					return theme;

			return null;
		}

		/// <summary>Every theme the manager can use: the registered ones, then the built-in ones.</summary>
		public static List<ThemeAsset> GetThemes() {
			if (_resolved != null)
				return _resolved;

			var themes = new List<ThemeAsset>(_registered.Count + Builtin.Length);
			foreach (var theme in _registered)
				if (theme)
					themes.Add(theme);

			foreach (var theme in Builtin)
				if (!themes.Contains(theme))
					themes.Add(theme);

			return _resolved = themes;
		}

		/// <summary>Registers a theme: it takes precedence over the built-in one with the same id.</summary>
		/// <param name="theme">Theme to read.</param>
		public static void Register(ThemeAsset theme) {
			if (!theme || _registered.Contains(theme))
				return;

			_registered.Add(theme);
			_resolved = null;
		}

		/// <summary>Forgets a theme, and repaints what it was theming.</summary>
		/// <param name="theme">Theme to forget.</param>
		public static void Unregister(ThemeAsset theme) {
			if (!theme || !_registered.Remove(theme))
				return;

			_resolved = null;
			ApplyAll();
		}

		/// <summary>
		/// Every theme available, with its id and the language key of its name, in the order the sources give
		/// them. The key is left unresolved, like the one of a language: the caller renders it with the packs
		/// of the moment, so the UI picks up a translation added or changed later.
		/// </summary>
		public static List<(string id, string key)> GetAvailableThemes() {
			var themes = new List<(string, string)>();
			foreach (var theme in GetThemes())
				if (!themes.Exists(t => t.Item1 == theme.id))
					themes.Add((theme.id, theme.LabelKey));

			return themes;
		}

		/// <summary>Repaints every <see cref="ThemedRoot"/> of the loaded scenes.</summary>
		public static void ApplyAll() {
			foreach (var root in ComponentExtension.GetComponentsInChildren<ThemedRoot>())
				if (root && root.isActiveAndEnabled)
					Apply(root.gameObject);
		}

		/// <summary>
		/// Repaints everything under <paramref name="root"/> with the current theme. The authored colours are
		/// kept on the way, so switching from a theme to another never compounds the swaps.
		/// </summary>
		/// <param name="root">Root of the subtree to repaint.</param>
		public static void Apply(GameObject root) {
			if (!root)
				return;

			var theme = Current;

			foreach (var background in root.GetComponentsInChildren<ThemedBackground>(true))
				if (background)
					background.Apply(theme);

			foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
				if (graphic)
					Element(graphic.gameObject).Apply(theme);

			foreach (var selectable in root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
				if (selectable)
					Element(selectable.gameObject).Apply(theme);
		}

		/// <summary>Puts the authored colours back on everything under <paramref name="root"/>.</summary>
		/// <param name="root">Root of the subtree to restore.</param>
		public static void Restore(GameObject root) {
			if (!root)
				return;

			foreach (var background in root.GetComponentsInChildren<ThemedBackground>(true))
				if (background)
					background.Restore();

			foreach (var element in root.GetComponentsInChildren<ThemedElement>(true))
				if (element)
					element.Restore();
		}

		private static ThemedElement Element(GameObject go)
			=> go.TryGetComponent<ThemedElement>(out var element)
				? element
				: go.AddComponent<ThemedElement>();

		private static ThemeAsset CreateUntouched() {
			var theme = ScriptableObject.CreateInstance<ThemeAsset>();
			theme.name = "[Theme_untouched]";
			theme.hideFlags = HideFlags.DontSave;
			theme.id = DEFAULT_THEME;
			return theme;
		}
	}
}
