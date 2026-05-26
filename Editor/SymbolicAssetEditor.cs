using Nox.CCK.Mods.Assets;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Editor
{
	[CustomEditor(typeof(SymbolicAsset))]
	public class SymbolicAssetEditor : UnityEditor.Editor
	{
		private Object _resolved;
		private string _lastTarget;

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var asset = (SymbolicAsset)target;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

			if (string.IsNullOrWhiteSpace(asset.Target))
			{
				EditorGUILayout.HelpBox("Target is empty.", MessageType.Info);
				return;
			}

			// Re-resolve only when target changes
			if (asset.Target != _lastTarget)
			{
				_lastTarget = asset.Target;
				_resolved = EditorGlobalAsset.GetAsset<Object>(asset.Target);
			}

			using (new EditorGUI.DisabledScope(true))
				EditorGUILayout.ObjectField("Resolved", _resolved, typeof(Object), false);

			if (_resolved == null)
			{
				EditorGUILayout.HelpBox(
						$"No asset found for \"{asset.Target}\".\nMake sure the mod is loaded in the editor.",
						MessageType.Warning);
				return;
			}

			// Large preview
			EditorGUILayout.Space();
			var rect = GUILayoutUtility.GetAspectRect(1f);
			if (_resolved is Texture2D tex)
			{
				// Draw the raw texture with alpha transparency
				EditorGUI.DrawTextureTransparent(rect, tex, ScaleMode.ScaleToFit);
				return;
			}

			var preview = AssetPreview.GetAssetPreview(_resolved)
				?? AssetPreview.GetMiniThumbnail(_resolved);

			if (AssetPreview.IsLoadingAssetPreview(_resolved.GetEntityId()))
				Repaint();

			if (preview != null)
				EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit);
		}
	}
}
