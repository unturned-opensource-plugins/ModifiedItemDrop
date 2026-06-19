using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathOutcomePlan
    {
        private readonly Dictionary<string, PlayerAssetOutcome> _outcomesByAssetId;

        public DeathOutcomePlan(IEnumerable<PlayerAssetOutcome> outcomes)
        {
            if (outcomes == null)
            {
                throw new ArgumentNullException(nameof(outcomes));
            }

            Outcomes = outcomes.ToList().AsReadOnly();
            _outcomesByAssetId = Outcomes.ToDictionary(outcome => outcome.Asset.Id, outcome => outcome);
        }

        public IReadOnlyList<PlayerAssetOutcome> Outcomes { get; }

        public PlayerAssetOutcome ForAsset(string assetId)
        {
            if (!_outcomesByAssetId.TryGetValue(assetId, out var outcome))
            {
                throw new KeyNotFoundException("No outcome exists for asset id '" + assetId + "'.");
            }

            return outcome;
        }
    }
}
