using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Drop;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Unturned.Player;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    /// <summary>
    /// Builds preview and dump output lines for player inventory inspection.
    /// </summary>
    internal static class InventoryInspector
    {
        public static IEnumerable<string> BuildPreviewLines(UnturnedPlayer target, DropService dropService)
        {
            var lines = new List<string>
            {
                $"[Preview] {target.CharacterName} ({target.CSteamID})"
            };

            try
            {
                var eq = target.Player?.equipment;
                var asset = eq?.asset;
                if (eq != null && asset != null && asset.id != 0)
                {
                    var page = eq.equippedPage;
                    var slotType = UtilityHelper.GetSlotTypeForPage(page);
                    var chance = dropService.PeekChance(slotType, asset.id, out var source);
                    lines.Add($"Equipped: [{slotType}] id={asset.id} chance={chance:P1} source={source}");
                }
            }
            catch
            {
                // ignore preview errors
            }

            var inventory = target.CaptureInventory();
            if (inventory.Count == 0)
            {
                lines.Add("Inventory: (empty)");
            }
            else
            {
                lines.Add("Inventory items:");
                foreach (var snapshot in inventory.OrderByDescending(s => s.Page).ThenByDescending(s => s.Index))
                {
                    var jar = snapshot.Jar;
                    if (jar?.item == null || jar.item.id == 0)
                    {
                        continue;
                    }

                    var slotType = UtilityHelper.GetSlotTypeForPage(snapshot.Page);
                    var chance = dropService.PeekChance(slotType, jar.item.id, out var source);
                    lines.Add($" - [{slotType}] id={jar.item.id} chance={chance:P1} source={source}");
                }
            }

            var clothing = target.CaptureClothing();
            if (clothing.Count == 0)
            {
                lines.Add("Clothing: (none)");
            }
            else
            {
                lines.Add("Clothing slots:");
                foreach (var snapshot in clothing)
                {
                    var rule = dropService.ResolveClothingRule(snapshot.SlotType);
                    lines.Add($" - {snapshot.SlotType}: slotChance={rule.SlotDropChance:P1} contentsChance={rule.ContentsDropChance:P1} items={snapshot.Contents?.Count ?? 0}");
                }
            }

            var regionOverrides = dropService.RegionOverrides;
            var itemOverrides = dropService.ItemOverrides;
            if (regionOverrides.Count > 0 || itemOverrides.Count > 0)
            {
                lines.Add("Active overrides:");
                if (regionOverrides.Count > 0)
                {
                    lines.Add($" - Regions: {string.Join(", ", regionOverrides.Select(kvp => $"{kvp.Key}={kvp.Value:P1}"))}");
                }
                if (itemOverrides.Count > 0)
                {
                    lines.Add($" - Items: {string.Join(", ", itemOverrides.Select(kvp => $"{kvp.Key}={kvp.Value:P1}"))}");
                }
            }

            return lines;
        }

        public static IEnumerable<string> BuildDumpLines(UnturnedPlayer target)
        {
            var lines = new List<string>
            {
                $"[Dump] {target.CharacterName} ({target.CSteamID})"
            };

            try
            {
                var eq = target.Player?.equipment;
                var asset = eq?.asset;
                if (eq != null && asset != null && asset.id != 0)
                {
                    var assetName = asset.itemName ?? asset.name ?? "Unknown";
                    lines.Add($"  装备中: id={asset.id} name={assetName}");
                }
            }
            catch
            {
                // ignore
            }

            var inventory = target.CaptureInventory();
            var weaponInventory = inventory.Where(s => s.Page < 3).GroupBy(s => s.Page).OrderBy(g => g.Key);

            var weaponItemCounts = new Dictionary<byte, int>();
            foreach (var group in weaponInventory)
            {
                var pageName = GetPageName(group.Key);
                var itemsInPage = group.Where(s => s.Jar?.item != null && s.Jar.item.id != 0).ToList();

                if (itemsInPage.Count > 0)
                {
                    lines.Add($"{pageName}:");
                    weaponItemCounts[group.Key] = itemsInPage.Count;

                    foreach (var snapshot in itemsInPage.OrderBy(s => s.Index))
                    {
                        var jar = snapshot.Jar;
                        var itemAsset = SDG.Unturned.Assets.find(SDG.Unturned.EAssetType.ITEM, jar.item.id) as SDG.Unturned.ItemAsset;
                        var itemName = itemAsset?.itemName ?? itemAsset?.name ?? "Unknown";
                        lines.Add($"  [{snapshot.Index}] id={jar.item.id} name={itemName} qty={jar.item.amount} quality={jar.item.quality}%");
                    }
                }
            }

            var clothing = target.CaptureClothing();
            var totalClothingItems = 0;
            var totalClothingContents = 0;

            if (clothing.Count > 0)
            {
                lines.Add("衣物:");
                foreach (var snapshot in clothing)
                {
                    var itemAsset = SDG.Unturned.Assets.find(SDG.Unturned.EAssetType.ITEM, snapshot.Item.id) as SDG.Unturned.ItemAsset;
                    var itemName = itemAsset?.itemName ?? itemAsset?.name ?? "Unknown";
                    lines.Add($"  {snapshot.SlotType}: id={snapshot.Item.id} name={itemName} quality={snapshot.Item.quality}%");
                    totalClothingItems++;

                    if (snapshot.Contents != null && snapshot.Contents.Count > 0)
                    {
                        foreach (var content in snapshot.Contents)
                        {
                            var contentAsset = SDG.Unturned.Assets.find(SDG.Unturned.EAssetType.ITEM, content.Item.id) as SDG.Unturned.ItemAsset;
                            var contentName = contentAsset?.itemName ?? contentAsset?.name ?? "Unknown";
                            lines.Add($"    - id={content.Item.id} name={contentName} qty={content.Item.amount}");
                            totalClothingContents++;
                        }
                    }
                }
            }

            var primaryCount = weaponItemCounts.ContainsKey(0) ? weaponItemCounts[0] : 0;
            var secondaryCount = weaponItemCounts.ContainsKey(1) ? weaponItemCounts[1] : 0;
            var handsCount = weaponItemCounts.ContainsKey(2) ? weaponItemCounts[2] : 0;
            var totalItems = primaryCount + secondaryCount + handsCount + totalClothingItems + totalClothingContents;
            lines.Add($"总计: {totalItems} 个物品 (主武器: {primaryCount}, 副武器: {secondaryCount}, 手持: {handsCount}, 衣物: {totalClothingItems}, 衣物内容: {totalClothingContents})");

            return lines;
        }

        internal static string GetPageName(byte page)
        {
            switch (page)
            {
                case 0: return "主武器";
                case 1: return "副武器";
                case 2: return "手持";
                case 3: return "背包";
                case 4: return "马甲";
                case 5: return "衬衫";
                case 6: return "裤子";
                case 7: return "外部存储";
                default: return $"Page {page}";
            }
        }
    }
}
