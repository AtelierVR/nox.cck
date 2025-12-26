namespace Nox.CCK.Mods.Metadata
{
    public interface IEngine
    {
        Utils.Engine GetName();
        Utils.VersionMatching GetVersion();
    }
}