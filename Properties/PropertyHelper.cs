using UnityEngine;

namespace Nox.CCK.Properties {
	public static class PropertyHelper {
		public static int StringToKey(string str)
			=> Animator.StringToHash(str);
	}
}