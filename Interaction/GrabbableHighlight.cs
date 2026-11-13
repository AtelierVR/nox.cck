using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Interaction {
	/// <summary>
	/// Hover glow of a grabbable, drawn by the <c>OutlineGlowFeature</c> renderer feature.
	/// <para>
	/// The feature renders the objects of a layer into a mask, blurs it and composites a glow and an
	/// outline. This component adds invisible copies of the object meshes on that layer while it is
	/// hovered (the real objects keep their layer, so grabbing keeps working), and removes them after.
	/// </para>
	/// <para>
	/// Several sources can ask for the same glow (a desktop ray and an XR ray hover the same prop):
	/// requests are counted, and the glow disappears when the last one stops asking.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Interaction/Grabbable Highlight")]
	[DisallowMultipleComponent]
	public class GrabbableHighlight : MonoBehaviour {
		/// <summary>
		/// Layer of the glow copies: the UI layer, the one no reflection renders (see the mirror settings), exactly
		/// like the nameplates and the mirror panels they sit next to.
		/// </summary>
		public const string GlowLayerName = Layers.Ui;

		/// <summary>
		/// Render queue of the glow copies (<c>Geometry+2</c>, as in <c>GlowMaskProxy.shader</c>): the feature draws
		/// its mask from that queue only, so sharing the UI layer does not make every UI mesh glow - the menus, the
		/// nameplates and the mirror panels stay out of the mask.
		/// </summary>
		public const int GlowQueue = 2002;

		private const string ProxyShaderName = "Hidden/Nox/GlowMaskProxy";

		/// <summary>Invisible material shared by every mask copy.</summary>
		private static Material _proxyMaterial;

		/// <summary>Mask copies currently alive.</summary>
		private readonly List<GameObject> _proxies = new List<GameObject>();

		/// <summary>Number of sources currently asking for the glow.</summary>
		private int _requests;

		/// <summary>Whether the mask copies were already built.</summary>
		private bool _built;

		/// <summary>
		/// Shows or hides the glow of a grabbable. Adding the component on demand keeps the feature
		/// opt-out free: an object does not need anything but its collider and a grabbable behaviour.
		/// </summary>
		public static void Set(Component target, bool visible) {
			if (!target)
				return;

			var highlight = target.GetComponent<GrabbableHighlight>();
			if (!highlight) {
				if (!visible)
					return;

				highlight = target.gameObject.AddComponent<GrabbableHighlight>();
			}

			highlight.Request(visible);
		}

		private void Request(bool visible) {
			_requests = Mathf.Max(0, _requests + (visible ? 1 : -1));

			if (_requests == 0) {
				Clear();
				return;
			}

			if (!_built)
				Build();
		}

		/// <summary>Creates one invisible mask copy per mesh renderer of the object, on the glow layer.</summary>
		private void Build() {
			if (_built)
				return;

			_built = true;

			var shader = Shader.Find(ProxyShaderName);
			if (!shader) {
				Logger.LogWarning(
					$"[{nameof(GrabbableHighlight)}] Shader '{ProxyShaderName}' not found: no glow will be drawn.",
					this, tag: nameof(GrabbableHighlight));
				return;
			}

			_proxyMaterial ??= new Material(shader) {
				name = "Glow Mask Proxy",
				hideFlags = HideFlags.DontSave,
				renderQueue = GlowQueue
			};

			var renderers = GetComponentsInChildren<MeshRenderer>(true);
			for (var i = 0; i < renderers.Length; i++) {
				var source = renderers[i];
				if (!source)
					continue;

				var filter = source.GetComponent<MeshFilter>();
				if (!filter || !filter.sharedMesh)
					continue;

				var node = new GameObject("Glow Proxy") { hideFlags = HideFlags.DontSave };
				node.transform.SetLayer(GlowLayerName);
				node.transform.SetParent(source.transform, false);

				var proxyFilter = node.AddComponent<MeshFilter>();
				proxyFilter.sharedMesh = filter.sharedMesh;

				var proxyRenderer = node.AddComponent<MeshRenderer>();
				proxyRenderer.sharedMaterial = _proxyMaterial;
				proxyRenderer.shadowCastingMode = ShadowCastingMode.Off;
				proxyRenderer.receiveShadows = false;
				proxyRenderer.lightProbeUsage = LightProbeUsage.Off;
				proxyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
				proxyRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

				_proxies.Add(node);
			}

			if (_proxies.Count > 0)
				GlowSources.Add();
		}

		/// <summary>Destroys the mask copies.</summary>
		private void Clear() {
			if (!_built)
				return;

			_built = false;

			if (_proxies.Count > 0)
				GlowSources.Remove();

			for (var i = 0; i < _proxies.Count; i++) {
				var proxy = _proxies[i];
				if (proxy)
					Remove(proxy);
			}

			_proxies.Clear();
		}

		/// <summary>
		/// Removes a runtime-only object: destroyed at the end of the frame while playing (so nothing is
		/// removed under the feet of a system using it this frame), immediately in the editor.
		/// </summary>
		private static void Remove(Object target) {
			if (!target)
				return;

			target.Destroy();
		}

		private void OnEnable() {
			if (_requests > 0 && !_built)
				Build();
		}

		private void OnDisable()
			=> Clear();
	}
}
