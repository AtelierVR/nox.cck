using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Filet de sécurité runtime contre les caméras « étrangères » (provenant d'un monde, d'une
	/// session ou d'un avatar). Elle empêche n'importe quelle caméra qui n'appartient pas au joueur
	/// (rig, mains, contrôleurs) de rendre dans les <b>yeux XR</b> ou de prendre le dessus sur
	/// l'<b>écran bureau</b>.
	/// <para>
	/// Une caméra neutralisée <b>reste active</b> : on ne touche qu'à sa capacité à rendre dans le
	/// casque et sur l'écran.
	/// </para>
	/// <list type="bullet">
	/// <item><c>stereoTargetEye = None</c> : elle ne peut plus écrire dans les textures d'yeux.</item>
	/// <item><c>targetTexture = &lt;offscreen&gt;</c> (uniquement si elle rendait directement à
	/// l'écran) : elle ne peut plus recouvrir le bureau ; la taille de la cible correspond à l'écran
	/// pour ne pas casser les caméras utilisées comme <c>eventCamera</c> de Canvas.</item>
	/// <item>le tag <c>MainCamera</c> lui est retiré pour que <c>Camera.main</c> ne la renvoie jamais.</item>
	/// </list>
	/// Les caméras de type <c>SceneView</c>/<c>Preview</c>/<c>Reflection</c> et les caméras rendant
	/// vers une <see cref="RenderTexture"/> sont ignorées (elles ne peuvent pas affecter le casque ni
	/// l'écran).
	/// </summary>
	public static class CameraGuard {
		private static readonly List<Camera>    _allowed     = new();
		private static readonly List<Transform> _roots       = new();
		private static readonly HashSet<Camera> _neutralized = new();

		/// <summary>
		/// Caméras appartenant à un contrôleur : autorisées en <b>permanence</b>, elles ne sont jamais
		/// neutralisées ni « détaguées », même quand un autre contrôleur devient courant (sinon le
		/// retour XR/bureau casserait le rendu). Voir <see cref="Protect"/>.
		/// </summary>
		private static readonly List<Camera> _protected = new();

		/// <summary>État d'une caméra avant neutralisation, pour pouvoir la restaurer.</summary>
		private static readonly Dictionary<Camera, CameraState> _originals = new();

		/// <summary>État initial d'une caméra, mémorisé avant neutralisation.</summary>
		private struct CameraState {
			public RenderTexture       TargetTexture;
			public StereoTargetEyeMask StereoTargetEye;
			public string              Tag;
		}

		private static RenderTexture _offscreen;
		private static bool          _installed;
		private static float         _nextScan;
		private static int           _lastCameraCount = -1;

		/// <summary>Intervalle minimal (secondes) entre deux balayages de sécurité.</summary>
		private const float ScanInterval = 1f;

		/// <summary>Coupe entièrement le garde-fou (diagnostic). Le garde-fou reste installé.</summary>
		public static bool Enabled { get; set; } = true;

		/// <summary>
		/// Tag posé sur les caméras gérées par le garde-fou, à la place de <c>Untagged</c>, pour
		/// pouvoir les identifier a posteriori. Doit exister dans le TagManager de l'application.
		/// </summary>
		public const string NeutralizedTag = "CameraGuard";

		private static bool HasPlayers
			=> _allowed.Count > 0 || _roots.Count > 0 || _protected.Count > 0;

		// ── Cycle de vie ─────────────────────────────────────────────────────────

		public static void Install() {
			if (_installed)
				return;

			_installed = true;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			Application.onBeforeRender                    += OnBeforeRender;
			Logger.LogDebug("Camera guard installed", tag: nameof(CameraGuard));
		}

		public static void Uninstall() {
			if (!_installed)
				return;

			_installed = false;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			Application.onBeforeRender                    -= OnBeforeRender;
			Clear();
			RestoreAll();
			_protected.Clear();
			ReleaseOffscreen();
			Logger.LogDebug("Camera guard uninstalled", tag: nameof(CameraGuard));
		}

		/// <summary>
		/// Vide la whitelist liée au contrôleur courant (le contrôleur courant change/part).
		/// Les caméras <see cref="Protect">protégées</see> sont conservées : ce sont des caméras de
		/// contrôleur qui doivent rester autorisées en permanence.
		/// </summary>
		public static void Clear() {
			_allowed.Clear();
			_roots.Clear();
		}

		// ── Whitelist ────────────────────────────────────────────────────────────

		/// <summary>Autorise explicitement une caméra (ex. la caméra du contrôleur courant).</summary>
		public static void Allow(Camera cam) {
			if (!cam || _allowed.Contains(cam))
				return;
			_allowed.Add(cam);
			RestoreAllowed();
			Logger.LogDebug($"Camera '{cam.name}' allowed", tag: nameof(CameraGuard));
		}

		/// <summary>Retire une caméra de la whitelist.</summary>
		public static void Disallow(Camera cam) {
			if (cam)
				_allowed.Remove(cam);
		}

		/// <summary>
		/// Autorise toutes les caméras descendantes de <paramref name="root"/> (ex. la racine du rig
		/// joueur : couvre la tête et d'éventuelles caméras de main/contrôleur).
		/// </summary>
		public static void AddRoot(Transform root) {
			if (!root || _roots.Contains(root))
				return;
			_roots.Add(root);
			RestoreAllowed();
		}

		public static bool IsAllowed(Camera cam) {
			if (!cam)
				return true;

			if (_allowed.Contains(cam) || _protected.Contains(cam))
				return true;

			var t = cam.transform;
			for (var i = 0; i < _roots.Count; i++) {
				var root = _roots[i];
				if (root && t.IsChildOf(root))
					return true;
			}

			return false;
		}
		// ── API publique (éditeur + runtime) ─────────────────────────────

		/// <summary>Vrai si la caméra a été marquée par le garde-fou.</summary>
		public static bool IsNeutralized(Camera camera)
			=> camera && camera.CompareTag(NeutralizedTag);

		/// <summary>
		/// Marque une caméra comme appartenant à un contrôleur : elle reste autorisée en permanence et
		/// n'est donc jamais neutralisée / « détaguée », même quand un autre contrôleur devient courant.
		/// C'est ce qui évite de casser le rendu du XRController/DesktopController au changement de
		/// contrôleur. La protection survit à <see cref="Clear"/>.
		/// </summary>
		public static void Protect(Camera camera) {
			if (!camera || _protected.Contains(camera))
				return;

			_protected.Add(camera);
			RestoreAllowed();
			Logger.LogDebug($"Camera '{camera.name}' protected", tag: nameof(CameraGuard));
		}

		/// <summary>Retire la protection d'une caméra de contrôleur.</summary>
		public static void Unprotect(Camera camera) {
			if (camera)
				_protected.Remove(camera);
		}

		/// <summary>
		/// Restaure l'état d'origine d'une caméra neutralisée par le garde-fou (tag, cible de rendu,
		/// <c>stereoTargetEye</c>).
		/// </summary>
		/// <returns><c>true</c> si la caméra a été restaurée.</returns>
		public static bool Restore(Camera camera) {
			if (!camera || !_originals.TryGetValue(camera, out var state))
				return false;

			_originals.Remove(camera);
			_neutralized.Remove(camera);

			if (camera.targetTexture == _offscreen)
				camera.targetTexture = state.TargetTexture;

			camera.stereoTargetEye = state.StereoTargetEye;

			if (state.Tag != null && camera.CompareTag(NeutralizedTag))
				camera.tag = state.Tag;

			return true;
		}

		/// <summary>
		/// Empêche une caméra d'être « principale » sans la supprimer : retire le tag
		/// <c>MainCamera</c> (remplacé par <see cref="NeutralizedTag"/>) et la sort du rendu XR.
		/// La caméra reste active. Côté éditeur, penser à <c>Undo.RecordObject</c> avant l'appel.
		/// </summary>
		/// <returns><c>true</c> si la caméra a été modifiée.</returns>
		public static bool Demote(Camera camera) {
			if (!camera)
				return false;

			var changed = false;

			if (camera.CompareTag("MainCamera") || camera.CompareTag("Untagged")) {
				ApplyNeutralizedTag(camera);
				changed = true;
			}

			if (camera.stereoTargetEye != StereoTargetEyeMask.None) {
				camera.stereoTargetEye = StereoTargetEyeMask.None;
				changed = true;
			}

			return changed;
		}

		/// <summary>Caméras descendantes de <paramref name="root"/> (inactives incluses).</summary>
		public static Camera[] GetCameras(GameObject root)
			=> root ? root.GetComponentsInChildren<Camera>(true) : Array.Empty<Camera>();

		/// <summary>Caméras d'une scène chargée (toutes ses racines).</summary>
		public static Camera[] GetCameras(Scene scene) {
			if (!scene.IsValid() || !scene.isLoaded)
				return Array.Empty<Camera>();

			var cameras = new List<Camera>();
			foreach (var root in scene.GetRootGameObjects())
				cameras.AddRange(root.GetComponentsInChildren<Camera>(true));

			return cameras.ToArray();
		}

		private static void ApplyNeutralizedTag(Camera camera) {
			try {
				camera.tag = NeutralizedTag;
			} catch {
				// Tag absent du TagManager : on retombe sur « Untagged » plutôt que de planter.
				camera.tag = "Untagged";
			}
		}
		// ── Enforcement ──────────────────────────────────────────────────────────

		private static void OnBeforeRender() {
			if (!Enabled || !HasPlayers)
				return;

			RestoreAllowed();

			// On rescanne immédiatement quand le nombre de caméras actives change (apparition d'une
			// caméra étrangère), sinon au plus une fois par <see cref="ScanInterval"/>.
			var count = Camera.allCamerasCount;
			var now   = Time.unscaledTime;
			if (count == _lastCameraCount && now < _nextScan)
				return;
			_lastCameraCount = count;
			_nextScan        = now + ScanInterval;

			var cameras = Camera.allCameras;
			for (var i = 0; i < cameras.Length; i++)
				Neutralize(cameras[i]);
		}

		private static void OnBeginCameraRendering(ScriptableRenderContext _, Camera cam)
			=> Neutralize(cam);

		private static void Neutralize(Camera cam) {
			if (!Enabled || !HasPlayers || !cam)
				return;

			// Une caméra qui n'est pas une caméra de jeu (SceneView, Preview, Reflection) ne participe
			// ni aux yeux XR ni à l'écran bureau.
			if (cam.cameraType != CameraType.Game)
				return;

			if (IsAllowed(cam))
				return;

			// Rendre vers une RenderTexture est inoffensif : la caméra n'écrit ni dans le casque ni à
			// l'écran. On la laisse intacte (miroirs, caméras CCTV d'un monde, etc.).
			if (cam.targetTexture != null)
				return;

			// Mémorise l'état d'origine (une seule fois) pour pouvoir restaurer la caméra si elle
			// redevient légitime (ex. un contrôleur redevient courant).
			if (!_originals.ContainsKey(cam))
				_originals[cam] = new CameraState {
					TargetTexture   = cam.targetTexture,
					StereoTargetEye = cam.stereoTargetEye,
					Tag             = cam.tag
				};

			// 1) Plus jamais dans les yeux XR.
			if (cam.stereoTargetEye != StereoTargetEyeMask.None)
				cam.stereoTargetEye = StereoTargetEyeMask.None;

			// 2) Plus jamais directement sur l'écran : on redirige la sortie vers une cible hors
			//    écran (la caméra reste active). La taille suit l'écran pour préserver le mapping
			//    écran->rayon des EventSystem/GraphicRaycaster.
			cam.targetTexture = GetOffscreen();

			// 3) Plus jamais « caméra principale » : on la marque comme gérée par le garde-fou
			//    (sans écraser un éventuel tag personnalisé).
			if (cam.CompareTag("MainCamera") || cam.CompareTag("Untagged"))
				ApplyNeutralizedTag(cam);

			if (_neutralized.Add(cam))
				Logger.LogWarning(
					$"Camera '{cam.name}' is not part of the player rig: it will not render to the XR eyes nor to the desktop screen.",
					tag: nameof(CameraGuard)
				);
		}

		// ── Cible hors écran partagée ────────────────────────────────────────────

		private static RenderTexture GetOffscreen() {
			if (_offscreen)
				return _offscreen;

			var w = Mathf.Max(2, Screen.width);
			var h = Mathf.Max(2, Screen.height);

			_offscreen = new RenderTexture(w, h, 16, RenderTextureFormat.Default) {
				name      = "CameraGuard (offscreen)",
				hideFlags = HideFlags.HideAndDontSave
			};
			_offscreen.Create();

			return _offscreen;
		}

		/// <summary>
		/// Restaure toutes les caméras neutralisées (à appeler avant de détruire la cible hors écran,
		/// sinon elles pointeraient vers une <see cref="RenderTexture"/> détruite).
		/// </summary>
		private static void RestoreAll() {
			foreach (var cam in new List<Camera>(_originals.Keys))
				if (cam)
					Restore(cam);

			_originals.Clear();
			_neutralized.Clear();
		}

		/// <summary>
		/// Restaure les caméras neutralisées qui sont (re)devenues autorisées. Sans ça, une caméra de
		/// contrôleur neutralisée pendant qu'un autre contrôleur était courant resterait « cassée »
		/// (rendu hors écran, tag CameraGuard) au retour.
		/// </summary>
		private static void RestoreAllowed() {
			if (_originals.Count > 0)
				foreach (var cam in new List<Camera>(_originals.Keys))
					if (!cam || IsAllowed(cam))
						Restore(cam);

			Prune();
		}

		/// <summary>Retire les références détruites (caméras/racines).</summary>
		private static void Prune() {
			_allowed.RemoveAll(cam => !cam);
			_protected.RemoveAll(cam => !cam);
			_roots.RemoveAll(root => !root);
			_neutralized.RemoveWhere(cam => !cam);

			if (_originals.Count > 0)
				foreach (var cam in new List<Camera>(_originals.Keys))
					if (!cam)
						_originals.Remove(cam);
		}

		private static void ReleaseOffscreen() {
			if (!_offscreen)
				return;

			_offscreen.Release();
			if (Application.isPlaying)
				Object.Destroy(_offscreen);
			else
				Object.DestroyImmediate(_offscreen);
			_offscreen = null;
		}
	}
}
