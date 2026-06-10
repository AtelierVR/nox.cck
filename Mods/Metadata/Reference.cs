namespace Nox.CCK.Mods.Metadata
{
    public interface IReference
    {
        string         GetNamespace();
        string         GetFile();
        IEngine         GetEngine();
        /// <summary>
        /// Tags (e.g. ["platform:windows", "arch:x86_64", "engine:unity:6000.4"]).
        /// Empty or null means compatible everywhere.
        /// </summary>
        string[]       GetTags();
        bool           IsCompatible();
    }
}