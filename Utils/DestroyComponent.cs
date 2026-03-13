using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Utils {
	public class DestroyComponent : MonoBehaviour {
		public readonly UnityEvent Destroyed  = new();
		private void OnDestroy() 
			=> Destroyed.Invoke();
	}
}