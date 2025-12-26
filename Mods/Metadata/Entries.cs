using System.Collections.Generic;

namespace Nox.CCK.Mods.Metadata
{
    public interface IEntries
    {
        string[] Get(string id);
        bool Has(string id);
        Dictionary<string, string[]> GetAll();
    }
}