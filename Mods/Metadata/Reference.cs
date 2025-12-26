namespace Nox.CCK.Mods.Metadata
{
    public interface Reference
    {
        string         GetNamespace();
        string         GetFile();
        IEngine         GetEngine();
        Utils.Platform GetPlatform();
        bool           IsCompatible();
    }
}