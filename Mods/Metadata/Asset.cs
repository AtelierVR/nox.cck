namespace Nox.CCK.Mods.Metadata
{
    public interface IAsset
    {
        string   GetName();
        string   GetFile();
        string   GetHash();
        string[] GetAssets();
        string[] GetScenes();
    }
}
