using System;
using Cysharp.Threading.Tasks;

namespace Nox.CCK.Utils {
	public static class ThenHelper {
		public static async UniTask Then<T>(
			this UniTask<T>   task,
			Action<T>         onSuccess = null,
			Action<Exception> onError   = null
		) {
			try {
				var result = await task;
				onSuccess?.Invoke(result);
			} catch (Exception ex) {
				onError?.Invoke(ex);
			}
		}
	}
}