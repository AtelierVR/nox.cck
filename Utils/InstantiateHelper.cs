using System;
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
			var instance = (await Object.InstantiateAsync(prefab, parent).ToUniTask(
				progress: progress,
				cancellationToken: cancellationToken
			)).FirstOrDefault();
			if (!instance)
				throw new Exception($"Failed to instantiate prefab {prefab.name}.");
			FixInstantiate(instance, prefab, parent);
			OnInstantiate.Invoke(instance);
			// Yield so callers resume on the next frame, preventing all post-instantiation
			// setup from piling into the same frame as IntegrateInMainThread.
			await UniTask.Yield(cancellationToken: cancellationToken);
			return instance;
		}

		private static void FixInstantiate(GameObject instance, GameObject prefab, Transform parent) {
			if (parent)
				instance.transform.SetParent(parent);
			else instance.DontDestroyOnLoad();
			instance.transform.position   = prefab.transform.position;
			instance.transform.rotation   = prefab.transform.rotation;
			instance.transform.localScale = prefab.transform.localScale;
			if (!prefab.TryGetComponent<RectTransform>(out var pRect) || !instance.TryGetComponent<RectTransform>(out var iRect))
				return;
			iRect.sizeDelta        = pRect.sizeDelta;
			iRect.anchoredPosition = pRect.anchoredPosition;
		}
	}
}