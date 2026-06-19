using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Models;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public sealed class V2DeathProcessingAdapter
    {
        private readonly DeathProcessingOrchestrator _orchestrator;

        public V2DeathProcessingAdapter()
            : this(new DeathProcessingOrchestrator())
        {
        }

        public V2DeathProcessingAdapter(DeathProcessingOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public DeathProcessingResult ProcessDeath(
            string sessionId,
            ulong steamId,
            IEnumerable<InventoryItemSnapshot> inventorySnapshots,
            IEnumerable<ClothingItemSnapshot> clothingSnapshots,
            IEnumerable<OutcomeRule> rules)
        {
            var assets = new List<PlayerAsset>();

            if (inventorySnapshots != null)
            {
                assets.AddRange(inventorySnapshots
                    .Where(snapshot => snapshot?.Jar?.item != null && snapshot.Jar.item.id != 0)
                    .Select(V2PlayerAssetRuntimeAdapter.ProjectInventoryItem));
            }

            if (clothingSnapshots != null)
            {
                foreach (var snapshot in clothingSnapshots)
                {
                    if (snapshot?.Item == null || snapshot.Item.id == 0)
                    {
                        continue;
                    }

                    assets.AddRange(V2PlayerAssetRuntimeAdapter.ProjectClothingItem(snapshot));
                }
            }

            return _orchestrator.ProcessDeath(sessionId, steamId, assets, rules);
        }
    }
}
