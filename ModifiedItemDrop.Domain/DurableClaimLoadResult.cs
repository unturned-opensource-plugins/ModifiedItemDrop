using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimLoadResult
    {
        public DurableClaimLoadResult(IEnumerable<DurableClaimRecord> claims)
            : this(claims, recoveredFromBackup: false, preservedCorruptPath: null)
        {
        }

        public DurableClaimLoadResult(
            IEnumerable<DurableClaimRecord> claims,
            bool recoveredFromBackup,
            string? preservedCorruptPath)
        {
            Claims = claims.ToList().AsReadOnly();
            RecoveredFromBackup = recoveredFromBackup;
            PreservedCorruptPath = preservedCorruptPath;
        }

        public IReadOnlyList<DurableClaimRecord> Claims { get; }

        public bool RecoveredFromBackup { get; }

        public string? PreservedCorruptPath { get; }
    }
}
