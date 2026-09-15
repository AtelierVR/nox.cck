using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Helper class for instantiating GameObjects with additional functionality.
	/// </summary>
	public static class InstantiateHelper {
		/// <summary>
		/// Event triggered when a GameObject is instantiated using this helper.
		/// </summary>
		public static readonly UnityEvent<GameObject> OnInstantiate = new();

		/// <summary>
		/// Asynchronous instantiations started through <see cref="Object.InstantiateAsync"/> that have
		/// not been integrated on the main thread yet.
		/// <para>
		/// Unity owns native state for these operations: if the domain is torn down while one is still
		/// pending (script recompile in Play mode, entering/exiting Play mode), the next domain tries
		/// to integrate objects whose scripting handles belong to the previous one — the editor logs
		/// « Resolve of invalid GC handle. The handle is from a previous domain. » and crashes in
		/// <c>AsyncInstantiateOperation::IntegrateInMainThread</c>. Keeping a handle on them lets us
		/// flush them via <see cref="CompletePendingInstantiations"/> right before the reload.
		/// </para>
		/// </summary>
		private static readonly HashSet<AsyncInstantiateOperation> PendingInstantiations = new();

		/// <summary>
		/// Number of asynchronous instantiations that are still pending (not integrated yet).
		/// </summary>
		public static int PendingInstantiationCount
			=> PendingInstantiations.Count;

		/// <summary>
		/// Completes (integrates) every pending asynchronous instantiation, so no native operation
		/// survives the current domain. Must be called <b>before</b> a domain reload and before the
		/// prefabs/scripts it instantiates are destroyed.
		/// </summary>
		/// <param name="reason">Optional reason, used for logging.</param>
		/// <returns>The number of operations that were awaited.</returns>
		public static int CompletePendingInstantiations(string reason = null) {
			var pending = PendingInstantiations.ToArray();
			if (pending.Length == 0)
				return 0;

			Logger.LogDebug(
				$"Integrating {pending.Length} pending async instantiation(s){(reason == null ? string.Empty : $" ({reason})")} before the domain goes away...",
				tag: nameof(InstantiateHelper)
			);

			var completed = 0;
			foreach (var operation in pending) {
				if (operation == null)
					continue;
				try {
					// WaitForCompletion() integrates the objects now (Awake/OnEnable included) while
					// the current domain — and the scripting handles of its prefabs — are still valid.
					if (!operation.isDone)
						operation.WaitForCompletion();
					completed++;
				} catch (Exception e) {
					Logger.LogException(e, tag: nameof(InstantiateHelper));
				}
			}

			PendingInstantiations.Clear();
			return completed;
		}

		/// <summary>
		/// Démarre une instantiation asynchrone <b>suivie</b> : toute opération lancée par ce helper est
		/// enregistrée puis intégrée automatiquement avant un reload de domaine
		/// (voir <see cref="CompletePendingInstantiations"/>). Rien à faire côté appelant.
		/// </summary>
		private static AsyncInstantiateOperation<GameObject> StartInstantiateAsync(GameObject prefab)
			=> Object.InstantiateAsync(prefab).TrackPendingInstantiation();

		/// <summary>
		/// Enregistre une opération créée <b>en dehors</b> de ce helper (appel direct à
		/// <see cref="Object.InstantiateAsync"/>), pour qu'elle soit intégrée elle aussi avant un reload
		/// de domaine. Inutile pour les instantiations passées par
		/// <see cref="InstantiateAsync(GameObject, Transform, IProgress{float}, CancellationToken)"/> :
		/// le suivi y est automatique.
		/// </summary>
		/// <param name="operation">L'opération à suivre.</param>
		/// <typeparam name="T">Type de l'objet instancié.</typeparam>
		/// <returns>La même opération.</returns>
		public static AsyncInstantiateOperation<T> TrackPendingInstantiation<T>(this AsyncInstantiateOperation<T> operation)
			where T : Object {
			PendingInstantiations.Add(operation);
			operation.completed += _ => PendingInstantiations.Remove(operation);
			return operation;
		}

		/// <summary>
		/// Instantiates a prefab and returns the specified component type from the instantiated GameObject.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T Instantiate<T>(this GameObject prefab, Transform parent = null) where T : Component {
			var instance  = prefab.Instantiate(parent);
			var component = instance.GetComponent<T>();
			if (component)
				return component;
			instance.Destroy();
			throw new Exception($"Component of type {typeof(T).Name} not found on instantiated prefab {prefab.name}.");
		}

		/// <summary>
		/// Instantiates a prefab and returns the instantiated GameObject.
		/// If a parent transform is provided,
		/// the instantiated GameObject will be set as a child of the parent
		/// and marked to not be destroyed on load.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="Exception"></exception>
		public static GameObject Instantiate(this GameObject prefab, Transform parent = null) {
			if (!prefab)
				throw new ArgumentNullException(nameof(prefab), "Prefab cannot be null.");
			var instance = Object.Instantiate(prefab, parent);
			if (!instance)
				throw new Exception($"Failed to instantiate prefab {prefab.name}.");
			FixInstantiate(instance, prefab, parent);
			OnInstantiate.Invoke(instance);
			return instance;
		}

		/// <summary>
		/// Asynchronously instantiates a prefab and returns the specified component type from the instantiated GameObject.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static async UniTask<T> InstantiateAsync<T>(
			this UniTask<GameObject> prefab,
			Transform                parent            = null,
			IProgress<float>         progress          = null,
			CancellationToken        cancellationToken = default
		) where T : Component
			=> await (await prefab).InstantiateAsync<T>(
				parent,
				progress: progress,
				cancellationToken: cancellationToken
			);

		/// <summary>
		/// Asynchronously instantiates a prefab and returns the instantiated GameObject.
		/// If a parent transform is provided,
		/// the instantiated GameObject will be set as a child of the parent
		/// and marked to not be destroyed on load.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async UniTask<GameObject> InstantiateAsync(
			this UniTask<GameObject> prefab,
			Transform                parent            = null,
			IProgress<float>         progress          = null,
			CancellationToken        cancellationToken = default
		)
			=> await (await prefab).InstantiateAsync(
				parent,
				progress: progress,
				cancellationToken: cancellationToken
			);

		/// <summary>
		/// Asynchronously instantiates a prefab and returns the specified component type from the instantiated GameObject.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static async UniTask<T> InstantiateAsync<T>(
			this GameObject   prefab,
			Transform         parent            = null,
			IProgress<float>  progress          = null,
			CancellationToken cancellationToken = default
		) where T : Component {
			var instance  = await prefab.InstantiateAsync(parent, progress: progress, cancellationToken: cancellationToken);
			var component = instance.GetComponent<T>();
			if (component)
				return component;
			instance.Destroy();
			throw new Exception($"Component of type {typeof(T).Name} not found on instantiated prefab {prefab.name}.");
		}

		/// <summary>
		/// Asynchronously instantiates a prefab and returns the instantiated GameObject.
		/// If a parent transform is provided,
		/// the instantiated GameObject will be set as a child of the parent
		/// and marked to not be destroyed on load.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="Exception"></exception>
		public static async UniTask<GameObject> InstantiateAsync(
			this GameObject   prefab,
			Transform         parent            = null,
			IProgress<float>  progress          = null,
			CancellationToken cancellationToken = default
		) {
			if (!prefab)
				throw new ArgumentNullException(nameof(prefab), "Prefab cannot be null.");

			await UniTask.Yield(cancellationToken: cancellationToken);

			// Do NOT pass `parent` here: Object.InstantiateAsync would call
			// SetParent(parent, worldPositionStays: true), which recalculates
			// RectTransform local positions and introduces non-zero Z offsets.
			// FixInstantiate handles parenting via SetParent(parent, false).
			// StartInstantiateAsync suit aussi l'opération : elle ne doit jamais rester en vol au
			// moment d'un reload de domaine (voir CompletePendingInstantiations).
			var operation = StartInstantiateAsync(prefab);

			var instance = (await operation.ToUniTask(
				progress: progress,
				cancellationToken: cancellationToken
			)).FirstOrDefault();

			if (!instance)
				throw new Exception($"Failed to instantiate prefab {prefab.name}.");
				
			FixInstantiate(instance, prefab, parent);
			OnInstantiate.Invoke(instance);
			return instance;
		}

		private static void FixInstantiate(GameObject instance, GameObject prefab, Transform parent) {
			// Handle UI rect transforms before touching world-space transforms:
			// copying world position/rotation breaks layout placement for RectTransforms.
			if (prefab.TryGetComponent<RectTransform>(out var pRect) && instance.TryGetComponent<RectTransform>(out var iRect)) {
				if (parent)
					iRect.SetParent(parent, false);
				else instance.DontDestroyOnLoad();
				iRect.localRotation   = pRect.localRotation;
				iRect.localScale      = pRect.localScale;
				iRect.anchorMin       = pRect.anchorMin;
				iRect.anchorMax       = pRect.anchorMax;
				iRect.pivot           = pRect.pivot;
				iRect.anchoredPosition = pRect.anchoredPosition;
				iRect.sizeDelta       = pRect.sizeDelta;
				return;
			}

			if (parent)
				instance.transform.SetParent(parent);
			else instance.DontDestroyOnLoad();
			instance.transform.SetLocalPositionAndRotation(
				prefab.transform.localPosition,
				prefab.transform.localRotation
			);
			instance.transform.localScale = prefab.transform.localScale;
		}
	}
}