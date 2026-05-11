using Nox.CCK.Mods.Libs;

namespace Nox.CCK.Mods.Cores
{
    /// <summary>
    /// Core API for editor mods, providing access to editor-specific APIs.
    /// </summary>
    public interface IEditorModCoreAPI : IModCoreAPI
    {
        /// <summary>
        /// Gets the libraries API for the editor.
        /// </summary>
        public EditorLibsAPI LibsAPI { get; }
    }
}