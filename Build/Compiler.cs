using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Nox.CCK.Build {
	public class Compiler {
		private readonly ICompilable[] _compilables;
		private readonly object[] _contexts;

		public readonly UnityEvent<ICompilable, int, int> OnCompilableCompiled = new();

		public Compiler(IEnumerable<ICompilable> compilables, params object[] contexts) {
			_compilables = compilables.ToArray();
			_contexts = contexts;
		}

		public ICompilable[] GetCompilables()
			=> _compilables;

		public object[] GetContexts()
			=> _contexts;

		public async UniTask<bool> Compile(CancellationToken cancellationToken = default) {
			var a = _compilables
				.OrderBy(c => c.CompileOrder)
				.ToArray();

			try {
				while (true) {
					List<ICompilable> need = new();
					for (var i = 0; i < a.Length; i++) {
						var compilable = a[i];

						if (cancellationToken.IsCancellationRequested)
							return false;

						switch (await compilable.CompileAsync(_contexts)) {
							case CompilationResult.Failed:
								Logger.LogWarning($"Compilation failed for {compilable.GetType().Name}", compilable as Object, tag: nameof(Compiler));
								return false;
							case CompilationResult.NeedRepass:
								need.Add(compilable);
								break;
							case CompilationResult.Done:
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						OnCompilableCompiled.Invoke(compilable, i, a.Length);
					}

					if (need.Count == 0)
						break;

					a = need.ToArray();
					await UniTask.Yield();
				}
			} catch (Exception e) {
				Logger.LogError(new Exception("Compilation exception", e));
				return false;
			}

			return true;
		}
	}
}