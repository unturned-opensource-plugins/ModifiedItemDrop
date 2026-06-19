using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimLoadResult
    {
        public DurableClaimLoadResult(IEnumerable<DurableClaimRecord> claims)
            : this(claims, recoveredFromBackup: false, preservedCorruptPath: null, warnings: null)
        {
        }

        public DurableClaimLoadResult(
            IEnumerable<DurableClaimRecord> claims,
            bool recoveredFromBackup,
            string? preservedCorruptPath)
            : this(claims, recoveredFromBackup, preservedCorruptPath, warnings: null)
        {
        }

        public DurableClaimLoadResult(
            IEnumerable<DurableClaimRecord> claims,
            bool recoveredFromBackup,
            string? preservedCorruptPath,
            IEnumerable<string>? warnings)
        {
            Claims = claims.ToList().AsReadOnly();
            RecoveredFromBackup = recoveredFromBackup;
            PreservedCorruptPath = preservedCorruptPath;
            Warnings = (warnings ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
        }

        public IReadOnlyList<DurableClaimRecord> Claims { get; }

        public bool RecoveredFromBackup { get; }

        public string? PreservedCorruptPath { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
