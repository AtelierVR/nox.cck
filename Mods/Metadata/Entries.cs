using System.Collections.Generic;

namespace Nox.CCK.Mods.Metadata
{
    public interface IEntries
    {
        /// <summary>Check if a section exists (e.g. "main", "editor").</summary>
        bool Has(string id);

        /// <summary>Get parsed entrypoint elements for a section.</summary>
        EntryPointElement[] Get(string id);

        /// <summary>All entrypoints grouped by section (read-only).</summary>
        IReadOnlyDictionary<string, EntryPointElement[]> All { get; }
    }
}