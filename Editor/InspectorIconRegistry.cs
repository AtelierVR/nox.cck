using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nox.CCK.Editor {
	/// <summary>
	/// Gives the components of the CCK editors their icon as soon as the project loads, so the Hierarchy shows it
	/// without the object ever being selected.
	/// <para>
	/// <see cref="InspectorEditor{T}.OnEnable"/> only runs once Unity creates the Editor of an object, i.e. when
	/// that object is inspected: the icons of a scene would otherwise stay the default script icons until each
	/// component is clicked. The icon of an editor is read here from a throwaway instance of it — the property
	/// does not need a target — then given to every object of its type, and again whenever the hierarchy changes.
	/// </para>
	/// </summary>
	[InitializeOnLoad]
	internal static class InspectorIconRegistry {
		/// <summary>Icons read once per domain reload: an editor is built to read each of them.</summary>
		private static List<(Type component, Texture2D icon)> _icons;

		static InspectorIconRegistry() {
			// Delayed: the assets of the mods (where the textures live) are not resolvable during initialization.
			EditorApplication.delayCall += Refresh;

			EditorApplication.hierarchyChanged -= Refresh;
			EditorApplication.hierarchyChanged += Refresh;
		}

		private static void Refresh() {
			try {
				_icons ??= ReadIcons();

				foreach (var (component, icon) in _icons)
					foreach (var target in Object.FindObjectsByType(component, FindObjectsInactive.Include))
						EditorGUIUtility.SetIconForObject(target, icon);
			} catch (Exception exception) {
				Debug.LogError("[Nox.CCK] Failed to set the icons of the inspectors: " + exception);
			}
		}

		/// <summary>
		/// The icon of every <see cref="InspectorEditor{T}"/>, read from an instance of it built for nothing:
		/// the editor is a <see cref="ScriptableObject"/>, and <c>Icon</c> does not look at the object it edits.
		/// </summary>
		private static List<(Type component, Texture2D icon)> ReadIcons() {
			var icons = new List<(Type, Texture2D)>();

			foreach (var type in TypeCache.GetTypesDerivedFrom<UnityEditor.Editor>()) {
				if (type.IsAbstract || type.IsGenericTypeDefinition)
					continue;

				var component = ComponentOf(type);
				if (component == null)
					continue;

				// An editor is a ScriptableObject: an instance of it can be built for nothing, and its `Icon`
				// does not look at the object it edits.
				var editor = ScriptableObject.CreateInstance(type) as UnityEditor.Editor;
				try {
					if (editor == null)
						continue;

					var source = SourceOf(editor, type);
					if (source == null)
						continue;

					var texture = InspectorIcon.Resolve(source);
					if (texture != null)
						icons.Add((component, texture));
				} finally {
					Object.DestroyImmediate(editor);
				}
			}

			return icons;
		}

		/// <summary>The component an editor is the custom editor of, or <c>null</c> when it is not one of ours.</summary>
		private static Type ComponentOf(Type editor) {
			for (var baseType = editor.BaseType; baseType != null; baseType = baseType.BaseType) {
				if (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(InspectorEditor<>))
					continue;

				return baseType.GetGenericArguments()[0];
			}

			return null;
		}

		/// <summary>The <c>Icon</c> of an editor instance, or <c>null</c> when it declares none.</summary>
		private static Nox.Search.ResourceTextureSource SourceOf(UnityEditor.Editor editor, Type type) {
			var property = type.GetProperty("Icon", BindingFlags.Instance | BindingFlags.NonPublic);
			if (property == null)
				return null;

			try {
				return property.GetValue(editor) as Nox.Search.ResourceTextureSource;
			} catch (Exception) {
				// An editor whose Icon throws without a target simply gets no icon here; the inspector still does.
				return null;
			}
		}
	}
}
