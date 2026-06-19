using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathSessionRespawnGrantResult
    {
        public DeathSessionRespawnGrantResult(IEnumerable<RespawnGrantOutcome> grants, bool sessionMarkedRespawnGrantConsumed)
        {
            Grants = grants.ToList().AsReadOnly();
            SessionMarkedRespawnGrantConsumed = sessionMarkedRespawnGrantConsumed;
        }

        public IReadOnlyList<RespawnGrantOutcome> Grants { get; }

        public bool SessionMarkedRespawnGrantConsumed { get; }
    }
}
