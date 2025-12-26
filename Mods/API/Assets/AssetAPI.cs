using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Mods.Assets
{
    /// <summary>
    /// API for managing assets and worlds (scenes) from mods, with support for namespaces and overrides.
    /// </summary>
    public interface IAssetAPI
    {
        /// <summary>
        /// Checks if an asset exists in the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>True if the asset exists; otherwise, false.</returns>
        public bool HasAsset<T>(ResourceIdentifier path)
            where T : Object;

        /// <summary>
        /// Gets an asset from the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>The asset, or null if not found.</returns>
        public T GetAsset<T>(ResourceIdentifier path)
            where T : Object;

        /// <summary>
        /// Checks if an override asset exists in the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>True if the override asset exists; otherwise, false.</returns>
        public bool HasInternalAsset<T>(ResourceIdentifier path) where T : Object;
        
        /// <summary>
        /// Gets an override asset from the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>The override asset, or null if not found.</returns>
        public T GetInternalAsset<T>(ResourceIdentifier path) where T : Object;

        /// <summary>
        /// Asynchronously checks if an asset exists in the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>A task with a boolean indicating if the asset exists.</returns>
        public UniTask<bool> HasAssetAsync<T>(ResourceIdentifier path)
            where T : Object;

        /// <summary>
        /// Asynchronously gets an asset from the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>A task with the asset, or null if not found.</returns>
        public UniTask<T> GetAssetAsync<T>(ResourceIdentifier path)
            where T : Object;

        /// <summary>
        /// Asynchronously checks if an override asset exists in the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>A task with a boolean indicating if the override asset exists.</returns>
        public UniTask<bool> HasInternalAssetAsync<T>(ResourceIdentifier path) where T : Object;
        
        /// <summary>
        /// Asynchronously gets an override asset from the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="path">The resource identifier.</param>
        /// <returns>A task with the override asset, or null if not found.</returns>
        public UniTask<T> GetInternalAssetAsync<T>(ResourceIdentifier path) where T : Object;
        
        /// <summary>
        /// Checks if a world (scene) exists in the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>True if the world exists; otherwise, false.</returns>
        public bool HasWorld(ResourceIdentifier path);
        
        /// <summary>
        /// Checks if a world (scene) from the specified namespace is currently loaded.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>True if the world is loaded; otherwise, false.</returns>
        public bool IsLoadedWorld(ResourceIdentifier path);
        
        /// <summary>
        /// Gets a loaded world (scene) from the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>The scene object.</returns>
        public Scene GetWorld(ResourceIdentifier path);
        
        /// <summary>
        /// Asynchronously loads a world (scene) from the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <param name="mode">The load scene mode.</param>
        /// <returns>A task with the loaded scene.</returns>
        public UniTask<Scene> LoadWorld(ResourceIdentifier path, LoadSceneMode mode = LoadSceneMode.Single);
        
        /// <summary>
        /// Asynchronously unloads a world (scene) from the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask UnloadWorld(ResourceIdentifier path);

        /// <summary>
        /// Checks if an override world (scene) exists in the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>True if the override world exists; otherwise, false.</returns>
        public bool HasInternalWorld(ResourceIdentifier path);
        
        /// <summary>
        /// Checks if an override world (scene) from the specified namespace is currently loaded.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>True if the override world is loaded; otherwise, false.</returns>
        public bool IsLoadedInternalWorld(ResourceIdentifier path);
        
        /// <summary>
        /// Gets a loaded override world (scene) from the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>The scene object.</returns>
        public Scene GetInternalWorld(ResourceIdentifier path);
        
        /// <summary>
        /// Asynchronously loads an override world (scene) from the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <param name="mode">The load scene mode.</param>
        /// <returns>A task with the loaded scene.</returns>
        public UniTask<Scene> LoadInternalWorld(ResourceIdentifier path, LoadSceneMode mode = LoadSceneMode.Single);
        
        /// <summary>
        /// Asynchronously unloads an override world (scene) from the specified namespace.
        /// </summary>
        /// <param name="path">The resource identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask UnloadInternalWorld(ResourceIdentifier path);
    }
}