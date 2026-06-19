using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimLoadResult
    {
        public DurableClaimLoadResult(IEnumerable<DurableClaimRecord> claims)
        {
            Claims = claims.ToList().AsReadOnly();
        }

        public IReadOnlyList<DurableClaimRecord> Claims { get; }
    }
}
