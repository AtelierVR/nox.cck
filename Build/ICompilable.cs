using Cysharp.Threading.Tasks;

namespace Nox.CCK.Build {
	/// <summary>
	/// Interface for components that can be compiled during the build process.
	/// </summary>
	public interface ICompilable {
		/// <summary>
		/// Gets the compile order priority (lower values compile first).
		/// Default value is 1000.
		/// </summary>
		int CompileOrder
			=> 1000;

		/// <summary>
		/// Synchronously compiles the component (optional).
		/// </summary>
		internal CompilationResult Compile(params object[] args)
			=> CompilationResult.Done;

		/// <summary>
		/// Asynchronously compiles the component (optional).
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		internal UniTask<CompilationResult> CompileAsync(params object[] args)
			=> UniTask.FromResult(Compile(args));
	}
}