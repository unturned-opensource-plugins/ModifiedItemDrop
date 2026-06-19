using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public sealed class V2QuickSlotExecutionAdapter
    {
        public void Execute(
            PlayerInventory inventory,
            IEnumerable<InventoryItemSnapshot> snapshots,
            DeathOutcomeExecutionPlan executionPlan,
            PendingRestore pending,
            Vector3 deathPosition)
        {
            if (inventory == null || snapshots == null || executionPlan == null || pending == null)
            {
                return;
            }

            var orderedSnapshots = snapshots
                .Where(snapshot => snapshot?.Jar?.item != null && snapshot.Page <= 2)
                .OrderByDescending(snapshot => snapshot.Page)
                .ThenByDescending(snapshot => snapshot.Index)
                .ToList();

            foreach (var snapshot in orderedSnapshots)
            {
                var item = snapshot.Jar.item;
                if (item == null || item.id == 0)
                {
                    continue;
                }

                var asset = V2PlayerAssetRuntimeAdapter.ProjectInventoryItem(snapshot);
                var action = executionPlan.ForAsset(asset.Id);
                inventory.removeItem(snapshot.Page, snapshot.Index);

                switch (action.Kind)
                {
                    case DeathOutcomeExecutionActionKind.Drop:
                        UtilityHelper.DropWorldItem(item, deathPosition);
                        break;
                    case DeathOutcomeExecutionActionKind.KeepForRestore:
                        pending.InventoryItems.Add(new PendingInventoryItem(UtilityHelper.CloneItem(item), snapshot.Page));
                        break;
                    case DeathOutcomeExecutionActionKind.Delete:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported v2 death execution action '" + action.Kind + "'.");
                }
            }
        }
    }
}
