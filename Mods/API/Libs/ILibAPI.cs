using System;

namespace Nox.CCK.Mods.Libs {
	/// <summary>
	/// Provides native plugin folder paths for the current mod.
	/// KernelMods and ExternalMods (FolderMods) expose different paths:
	/// - KernelMod (build): Application.dataPath/Plugins/&lt;arch&gt;
	/// - ExternalMod: &lt;modFolder&gt;/Plugins/&lt;arch&gt;
	/// </summary>
	public interface ILibAPI {
		/// <summary>
		/// Returns an ordered list of directories where native plugins (.dll / .so / .dylib)
		/// for this mod can be found. The first directory that contains the requested file wins.
		/// </summary>
		string[] GetFolders();

		/// <summary>
		/// Returns the names of all native library files (without extension) discoverable
		/// in the folders returned by <see cref="GetFolders"/>.
		/// </summary>
		string[] GetLibraries();

		/// <summary>
		/// Returns the platform-specific native library file extension (e.g. ".dll", ".so", ".dylib").
		/// </summary>
		string GetExtension();

		/// <summary>
		/// Returns the prioritized list of compatible plugin subfolder names for the current
		/// platform and CPU architecture (e.g. ["win64", "x86_64", "x64"] on Windows x64).
		/// These are used to compose search paths like Plugins/{subfolder}.
		/// </summary>
		string[] GetSubFolders();

		/// <summary>
		/// Returns the full path to the native library file named <paramref name="name"/> (without extension),
		/// searching through <see cref="GetFolders"/> in order.
		/// Returns <c>null</c> if the file is not found in any folder.
		/// </summary>
		string ToPath(string name);

		/// <summary>
		/// Loads the native library <paramref name="name"/> (without extension) into the process.
		/// Uses <c>LoadLibrary</c> on Windows, <c>dlopen</c> on Linux/macOS.
		/// Searches the current mod's plugin folders first, then falls back to global paths.
		/// Reference-counted — safe to call multiple times from different mods.
		/// <para>
		/// After calling this, <c>[DllImport("name")]</c> will resolve from the loaded library.
		/// </para>
		/// <exception cref="DllNotFoundException">Thrown if the library is not found or fails to load.</exception>
		void Load(string name);

		/// <summary>
		/// Loads the native library <paramref name="name"/> and returns its native module handle
		/// (<c>IntPtr</c> as returned by <c>LoadLibrary</c>/<c>dlopen</c>).
		/// This is the only API that physically loads native libraries; callers must not use
		/// <c>DllImport</c> themselves.
		/// <exception cref="DllNotFoundException">Thrown if the library is not found or fails to load.</exception>
		IntPtr GetHandle(string name);

		/// <summary>
		/// Resolves the address of the native export <paramref name="symbol"/> from the library
		/// <paramref name="name"/> (loading it first if needed). Returns <c>IntPtr.Zero</c> if not found.
		/// </summary>
		IntPtr GetSymbol(string name, string symbol);

		/// <summary>
		/// Resolves the native export <paramref name="symbol"/> from the library <paramref name="name"/>
		/// and wraps it in a managed delegate of type <typeparamref name="T"/> (must be a delegate type).
		/// This is the DllImport-free equivalent of calling an extern P/Invoke entry point.
		/// <exception cref="EntryPointNotFoundException">Thrown if the symbol is not found.</exception>
		T GetDelegate<T>(string name, string symbol) where T : Delegate;

		/// <summary>
		/// Unloads (reference-counted decrement) the native library <paramref name="name"/>.
		/// The physical library is only unloaded when no mod references it anymore.
		/// </summary>
		void Unload(string name);			}
}
