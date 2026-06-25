using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Utils {
    public static class SceneExtensions {
        public const int DontDestroyOnLoadId = -1;


        public static Scene DontDestroyOnLoad {
            get {
                var o = new GameObject("DontDestroyOnLoad");
                o.DontDestroyOnLoad();
                var scene = o.scene;
                o.DestroyImmediate();
                return scene;
            }
        }

        public static Scene Get(int index) {
            if (index == DontDestroyOnLoadId) 
                return DontDestroyOnLoad;

            if (index < 0 || index >= SceneManager.sceneCount) 
                throw new ArgumentOutOfRangeException(nameof(index), $"Scene index {index} is out of range. Valid range is 0 to {SceneManager.sceneCount - 1}.");
            
            return SceneManager.GetSceneAt(index);
        }

        public static Scene Get(string name) {
            if (name == nameof(DontDestroyOnLoad)) 
                return Get(DontDestroyOnLoadId);

            for (var i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == name)
                    return scene;
            }

            throw new ArgumentException($"Scene with name '{name}' not found.", nameof(name));
        }
    }
}