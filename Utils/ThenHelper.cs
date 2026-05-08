using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Nox.CCK.Utils
{
	public static class ThenHelper
	{
		public static UniTask Then<T>(
			this UniTask<T> task,
			Action<T> onSuccess = null,
			Action<Exception> onError = null
		) => task
			.AsTask()
			.Then(onSuccess, onError)
			.AsUniTask();

		public static async Task Then<T>(
			this Task<T> task,
			Action<T> onSuccess = null,
			Action<Exception> onError = null
		)
		{
			if (task.IsCompletedSuccessfully)
			{
				onSuccess?.Invoke(task.Result);
				return;
			}

			if (task.IsFaulted)
			{
				onError?.Invoke(task.Exception);
				return;
			}

			try
			{
				var result = await task;
				onSuccess?.Invoke(result);
			}
			catch (Exception ex)
			{
				onError?.Invoke(ex);
			}
		}
	}
}