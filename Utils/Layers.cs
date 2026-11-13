using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Utils {
	/// <summary>
	/// Utility class for managing Unity layers,
	/// including checking for existence,
	/// creating layers,
	/// building masks from layer names,
	/// and setting/getting layers on transforms.
	/// </summary>
	public static class Layers {
		/// <summary>
		/// Built-in layer the player-facing UI sits on: menus, nameplates, mirror panels and the hover glow.
		/// Nothing a reflection should show, so the mirror settings never render it.
		/// </summary>
		public const string Ui = "UI";

		/// <summary>
		/// Checks if a layer with the specified name exists in the project.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool LayerExists(string name)
			=> !string.IsNullOrEmpty(name) && LayerMask.NameToLayer(name) != -1;

		/// <summary>
		/// Checks if layers with the specified names exist in the project.
		/// </summary>
		/// <param name="names"></param>
		/// <returns></returns>
		public static bool LayersExists(string[] names)
			=> names != null && names.All(LayerExists);

		/// <summary>
		/// Creates layers with the specified names
		/// if they don't already exist.
		/// </summary>
		/// <param name="names"></param>
		#if UNITY_EDITOR
		public static void CreateLayers(string[] names) {
			var created = false;
			var li      = GetLayers();
			var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			var layers  = manager.FindProperty("layers");
			foreach (var name in names) {
				if (li.ContainsKey(name))
					continue;
				for (var i = 0; i < 31; i++) {
					var el = layers.GetArrayElementAtIndex(i);
					if (!string.IsNullOrEmpty(el.stringValue) || i < 6)
						continue;
					el.stringValue = name;
					created        = true;
					break;
				}
			}

			if (!created)
				return;

			manager.ApplyModifiedProperties();
			Logger.LogDebug($"{names.Length} layers created: {string.Join(", ", names)}");
		}
		#else
		public static void CreateLayers(string[] names)
			=> throw new System.NotSupportedException("Creating layers is only supported in the Unity Editor.");
		#endif

		/// <summary>
		/// Creates a layer with the specified name
		/// if it doesn't already exist.
		/// </summary>
		/// <param name="name"></param>
		public static void CreateLayer(string name)
			=> CreateLayers(new[] { name });

		/// <summary>
		/// Builds a layer mask from layer names.
		/// <para>
		/// A name that does not exist in the project is ignored instead of throwing, so a mask keeps working
		/// on a build target or a project where an optional layer was never created.
		/// </para>
		/// </summary>
		/// <param name="names">Layer names to include.</param>
		/// <returns>The mask, 0 when no name matches.</returns>
		public static LayerMask Mask(params string[] names)
			=> MaskBits(names);

		/// <summary>Bits of the mask built from layer names (see <see cref="Mask(string[])"/>).</summary>
		/// <param name="names">Layer names to include.</param>
		/// <returns>The mask bits, 0 when no name matches.</returns>
		public static int MaskBits(params string[] names) {
			var mask = 0;
			if (names == null)
				return mask;

			for (var i = 0; i < names.Length; i++) {
				var layer = LayerMask.NameToLayer(names[i]);
				if (layer >= 0)
					mask |= 1 << layer;
			}

			return mask;
		}

		/// <summary>Whether a layer index is part of a mask.</summary>
		/// <param name="mask">Mask to inspect.</param>
		/// <param name="layer">Layer index.</param>
		public static bool Contains(this LayerMask mask, int layer)
			=> layer >= 0 && layer < 32 && (mask.value & (1 << layer)) != 0;

		/// <summary>Whether a named layer is part of a mask (false when the layer does not exist).</summary>
		/// <param name="mask">Mask to inspect.</param>
		/// <param name="name">Layer name.</param>
		public static bool Contains(this LayerMask mask, string name)
			=> mask.Contains(LayerMask.NameToLayer(name));

		/// <summary>Whether a layer index is part of a mask given as bits.</summary>
		/// <param name="mask">Mask bits to inspect.</param>
		/// <param name="layer">Layer index.</param>
		public static bool Contains(int mask, int layer)
			=> layer >= 0 && layer < 32 && (mask & (1 << layer)) != 0;

		/// <summary>
		/// Gets a dictionary of all defined layers in the project,
		/// mapping layer names to their corresponding indices.
		/// </summary>
		/// <returns></returns>
		#if UNITY_EDITOR
		public static Dictionary<string, int> GetLayers() {
			var results = new Dictionary<string, int>();
			var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			var layers  = manager.FindProperty("layers");
			var size    = layers.arraySize;

			for (var i = 0; i < size; i++) {
				var el   = layers.GetArrayElementAtIndex(i);
				var name = el.stringValue;
				if (!string.IsNullOrEmpty(name))
					results.Add(name, i);
			}

			return results;
		}
		#else
		public static Dictionary<string, int> GetLayers() {
			var results = new Dictionary<string, int>();

			for (var i = 0; i < 32; i++) {
				var name = LayerMask.LayerToName(i);
				if (!string.IsNullOrEmpty(name))
					results.Add(name, i);
			}

			return results;
		}
		#endif

		/// <summary>
		/// Layers the engine keeps for itself. They hold nothing a player would choose to show or hide, so they
		/// are left out of the layer lists offered to the user (see <see cref="GetVisibleLayers"/>).
		/// </summary>
		public static readonly string[] HiddenLayers = {
			"TransparentFX", "Ignore Raycast", "Water"
		};

		/// <summary>
		/// Gets the defined layers of the project without <see cref="HiddenLayers"/>,
		/// in layer index order: what a layer list should offer.
		/// </summary>
		/// <returns>Layer names mapped to their corresponding indices.</returns>
		public static Dictionary<string, int> GetVisibleLayers()
			=> GetLayers()
				.Where(pair => !HiddenLayers.Contains(pair.Key))
				.OrderBy(pair => pair.Value)
				.ToDictionary(pair => pair.Key, pair => pair.Value);

		/// <summary>
		/// Sets the layer of a transform and optionally all its children.
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="layerName"></param>
		/// <param name="includeChildren"></param>
		public static void SetLayer(this Transform transform, string layerName, bool includeChildren = false) {
			if (!LayerExists(layerName))
				throw new ArgumentException($"Layer '{layerName}' does not exist. Create it first using {typeof(Layers).FullName}.{nameof(CreateLayer)}(\"{layerName}\").", nameof(layerName));
			transform.gameObject.layer = LayerMask.NameToLayer(layerName);
			if (!includeChildren)
				return;
			foreach (Transform child in transform)
				child.SetLayer(layerName, true);
		}

		/// <summary>
		/// Gets the layer name of a transform.
		/// </summary>
		/// <param name="transform"></param>
		/// <returns></returns>
		public static string GetLayer(this Transform transform)
			=> LayerMask.LayerToName(transform.gameObject.layer);
	}
}
