using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Nox.CCK.Events {
	public class NoxEventAsync {
		private readonly HashSet<Func<UniTask>> listeners = new();

		public void AddListener(Func<UniTask> listener)
			=> listeners.Add(listener);

		public void RemoveListener(Func<UniTask> listener)
			=> listeners.Remove(listener);

		public async UniTask InvokeAsync() {
			foreach (var listener in listeners)
				await listener();
		}

		public void RemoveAllListeners()
			=> listeners.Clear();
	}

	public class NoxEventAsync<T0> {
		private readonly HashSet<Func<T0, UniTask>> listeners = new();

		public void AddListener(Func<T0, UniTask> listener)
			=> listeners.Add(listener);

		public void RemoveListener(Func<T0, UniTask> listener)
			=> listeners.Remove(listener);

		public async UniTask InvokeAsync(T0 arg0) {
			foreach (var listener in listeners)
				await listener(arg0);
		}

		public void RemoveAllListeners()
			=> listeners.Clear();
	}

	public class NoxEventAsync<T0, T1> {
		private readonly HashSet<Func<T0, T1, UniTask>> listeners = new();

		public void AddListener(Func<T0, T1, UniTask> listener)
			=> listeners.Add(listener);

		public void RemoveListener(Func<T0, T1, UniTask> listener)
			=> listeners.Remove(listener);

		public async UniTask InvokeAsync(T0 arg0, T1 arg1) {
			foreach (var listener in listeners)
				await listener(arg0, arg1);
		}

		public void RemoveAllListeners()
			=> listeners.Clear();
	}

	public class NoxEventAsync<T0, T1, T2> {
		private readonly HashSet<Func<T0, T1, T2, UniTask>> listeners = new();

		public void AddListener(Func<T0, T1, T2, UniTask> listener)
			=> listeners.Add(listener);

		public void RemoveListener(Func<T0, T1, T2, UniTask> listener)
			=> listeners.Remove(listener);

		public async UniTask InvokeAsync(T0 arg0, T1 arg1, T2 arg2) {
			foreach (var listener in listeners)
				await listener(arg0, arg1, arg2);
		}

		public void RemoveAllListeners()
			=> listeners.Clear();
	}

	public class NoxEventAsync<T0, T1, T2, T3> {
		private readonly HashSet<Func<T0, T1, T2, T3, UniTask>> listeners = new();

		public void AddListener(Func<T0, T1, T2, T3, UniTask> listener)
			=> listeners.Add(listener);

		public void RemoveListener(Func<T0, T1, T2, T3, UniTask> listener)
			=> listeners.Remove(listener);

		public async UniTask InvokeAsync(T0 arg0, T1 arg1, T2 arg2, T3 arg3) {
			foreach (var listener in listeners)
				await listener(arg0, arg1, arg2, arg3);
		}

		public void RemoveAllListeners()
			=> listeners.Clear();
	}
}