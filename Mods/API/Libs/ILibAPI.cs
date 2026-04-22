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
		string[] GetNativePluginFolders();

		/// <summary>
		/// Returns the names of all native library files (without extension) discoverable
		/// in the folders returned by <see cref="GetNativePluginFolders"/>.
		/// </summary>
		string[] GetLibraries();

		/// <summary>
		/// Returns the platform-specific native library file extension (e.g. ".dll", ".so", ".dylib").
		/// </summary>
		string GetExtension();

		/// <summary>
		/// Returns the current CPU architecture subfolder name used by Unity's native plugin layout
		/// (e.g. "x86_64", "ARM64"), or <c>null</c> if the architecture is not recognised.
		/// </summary>
		string GetArch();

		/// <summary>
		/// Returns the full path to the native library file named <paramref name="name"/> (without extension),
		/// searching through <see cref="GetNativePluginFolders"/> in order.
		/// Returns <c>null</c> if the file is not found in any folder.
		/// </summary>
		string ToPath(string name);
	}
}
