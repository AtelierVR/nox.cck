using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.CCK.Build {
	public class Compiler {
		private readonly ICompilable[] _compilables;
		private readonly object[]    _contexts;

		public Compiler(IEnumerable<ICompilable> compilables, params object[] contexts) {
			_compilables = compilables.ToArray();
			_contexts    = contexts;
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
					foreach (var compilable in a) {
						if (cancellationToken.IsCancellationRequested)
							return false;

						switch (await compilable.CompileAsync(_contexts)) {
							case CompilationResult.Failed:
								return false;
							case CompilationResult.NeedRepass:
								need.Add(compilable);
								break;
							case CompilationResult.Done:
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					if (need.Count == 0)
						break;

					a = need.ToArray();
					await UniTask.Yield();
				}
			} catch (Exception e) {
				Logger.LogException(new Exception("Compilation exception", e));
				return false;
			}

			return true;
		}
	}
}