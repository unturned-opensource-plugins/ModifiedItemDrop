using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public sealed class V2ClothingExecutionAdapter
    {
        private const byte AnyInventoryPage = byte.MaxValue;

        public void Execute(
            Player player,
            IEnumerable<ClothingItemSnapshot> snapshots,
            DeathOutcomeExecutionPlan executionPlan,
            PendingRestore pending,
            Vector3 deathPosition)
        {
            if (player?.clothing == null || snapshots == null || executionPlan == null || pending == null)
            {
                return;
            }

            foreach (var snapshot in snapshots.Where(snapshot => snapshot?.Item != null && snapshot.Item.id != 0))
            {
                var assets = V2PlayerAssetRuntimeAdapter.ProjectClothingItem(snapshot);
                var parentAsset = assets.First(asset => !asset.IsClothingContent);
                var parentAction = executionPlan.ForAsset(parentAsset.Id);
                var keptContents = ExecuteContents(player, snapshot, parentAction, executionPlan, pending, deathPosition);

                ClothingOperationHelper.ClearClothingSlot(player.clothing, snapshot.SlotType);

                switch (parentAction.Kind)
                {
                    case DeathOutcomeExecutionActionKind.Drop:
                        UtilityHelper.DropWorldItem(snapshot.Item, deathPosition);
                        break;
                    case DeathOutcomeExecutionActionKind.KeepForRestore:
                        pending.ClothingItems.Add(new ClothingItemSnapshot(
                            snapshot.SlotType,
                            UtilityHelper.CloneItem(snapshot.Item),
                            keptContents));
                        break;
                    case DeathOutcomeExecutionActionKind.Delete:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported v2 clothing execution action '" + parentAction.Kind + "'.");
                }
            }
        }

        private static List<ClothingContentSnapshot> ExecuteContents(
            Player player,
            ClothingItemSnapshot snapshot,
            DeathOutcomeExecutionAction parentAction,
            DeathOutcomeExecutionPlan executionPlan,
            PendingRestore pending,
            Vector3 deathPosition)
        {
            var keptContents = new List<ClothingContentSnapshot>();
            if (snapshot.Contents == null || snapshot.Contents.Count == 0)
            {
                return keptContents;
            }

            var container = PlayerExtensions.GetClothingContainer(player, snapshot.SlotType);
            foreach (var content in snapshot.Contents
                .Where(content => content?.Item != null && content.Item.id != 0)
                .OrderByDescending(content => content.Index))
            {
                var contentAsset = PlayerAssetProjection.ClothingContent(
                    V2PlayerAssetRuntimeAdapter.ToDomainSlot(snapshot.SlotType),
                    parentAction.Asset.Id,
                    content.Index,
                    content.Item.id,
                    content.Item.amount,
                    content.Item.quality,
                    content.Item.state);
                var contentAction = executionPlan.ForAsset(contentAsset.Id);

                container?.removeItem(content.Index);

                switch (contentAction.Kind)
                {
                    case DeathOutcomeExecutionActionKind.Drop:
                        UtilityHelper.DropWorldItem(content.Item, deathPosition);
                        break;
                    case DeathOutcomeExecutionActionKind.KeepForRestore:
                        KeepContent(snapshot, parentAction, pending, keptContents, content);
                        break;
                    case DeathOutcomeExecutionActionKind.Delete:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported v2 clothing content execution action '" + contentAction.Kind + "'.");
                }
            }

            keptContents.Sort((a, b) => a.Index.CompareTo(b.Index));
            return keptContents;
        }

        private static void KeepContent(
            ClothingItemSnapshot snapshot,
            DeathOutcomeExecutionAction parentAction,
            PendingRestore pending,
            List<ClothingContentSnapshot> keptContents,
            ClothingContentSnapshot content)
        {
            var clone = UtilityHelper.CloneItem(content.Item);
            if (parentAction.Kind == DeathOutcomeExecutionActionKind.KeepForRestore)
            {
                if (!pending.ClothingContentsToRestore.TryGetValue(snapshot.SlotType, out var contents))
                {
                    contents = new List<Item>();
                    pending.ClothingContentsToRestore[snapshot.SlotType] = contents;
                }

                contents.Add(UtilityHelper.CloneItem(content.Item));
                keptContents.Add(new ClothingContentSnapshot(content.Index, clone));
                return;
            }

            pending.InventoryItems.Add(new PendingInventoryItem(clone, AnyInventoryPage));
        }
    }
}
