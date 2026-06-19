using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathSession
    {
        public DeathSession(string id, ulong steamId, IEnumerable<PlayerAssetOutcome> outcomes)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Death Session id must be provided.", nameof(id));
            }

            Id = id;
            SteamId = steamId;
            Outcomes = (outcomes ?? throw new ArgumentNullException(nameof(outcomes))).ToList().AsReadOnly();
        }

        public string Id { get; }

        public ulong SteamId { get; }

        public IReadOnlyList<PlayerAssetOutcome> Outcomes { get; }

        public IReadOnlyList<PlayerAssetOutcome> KeptOutcomes
        {
            get { return Outcomes.Where(outcome => outcome.Kind == PlayerAssetOutcomeKind.Keep).ToList().AsReadOnly(); }
        }
    }
}
