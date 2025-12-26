namespace Nox.CCK.Mods.Metadata
{
    public interface IRelation
    {
        string GetId();
        Utils.VersionMatching GetVersion();
        RelationType GetRelationType();
    }
}