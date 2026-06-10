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
		/// Unloads (reference-counted decrement) the native library <paramref name="name"/>.
		/// The physical library is only unloaded when no mod references it anymore.
		/// </summary>
		void Unload(string name);			}
}
