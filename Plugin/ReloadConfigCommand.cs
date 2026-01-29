using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Drop;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    public sealed class ReloadConfigCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "mid";

        public string Help => "ModifiedItemDrop command suite.";

        public string Syntax => "<reload|preview|dump|claim>";

        public List<string> Aliases => new List<string> { "modifieditemdrop" };

        public List<string> Permissions => new List<string>
        {
            "modifieditemdrop.reload",
            "modifieditemdrop.preview",
            "modifieditemdrop.claim"
        };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command == null || command.Length == 0)
            {
                SendUsage(caller);
                return;
            }

            var subCommand = command[0].ToLowerInvariant();
            var args = command.Skip(1).ToArray();

            switch (subCommand)
            {
                case "reload":
                    HandleReload(caller);
                    break;
                case "preview":
                    HandlePreview(caller, args);
                    break;
                case "dump":
                    HandleDump(caller, args);
                    break;
                case "claim":
                    HandleClaim(caller);
                    break;
                default:
                    SendUsage(caller);
                    break;
            }
        }

        private static void HandleReload(IRocketPlayer caller)
        {
            if (!HasPermission(caller, "modifieditemdrop.reload"))
            {
                SendMessage(caller, "You do not have permission to reload ModifiedItemDrop.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            if (plugin == null)
            {
                SendMessage(caller, "Plugin not initialised.", Color.red);
                return;
            }
            if (plugin.TryReloadConfiguration(out var summary, out var error))
            {
                var sb = new StringBuilder();
                sb.Append("Reloaded configuration.");
                if (summary != null)
                {
                    sb.Append($" Regions: {summary.RegionEntries} (discarded {summary.RegionDiscardedEntries}).");
                    sb.Append($" Custom items: {summary.CustomItemEntries} (discarded {summary.CustomItemDiscardedEntries}).");
                    sb.Append($" Clothing rules: {summary.ClothingEntries} (discarded {summary.ClothingDiscardedEntries}).");
                    sb.Append($" Debug={summary.DebugLoggingEnabled} ContentsDebug={summary.ClothingContentsDebugEnabled}.");
                    if (summary.UsedDefaults)
                    {
                        sb.Append(" (RuleSet missing in config, loaded defaults.)");
                    }
                    if (summary.HasWarnings)
                    {
                        sb.Append(" (Some entries were invalid and ignored.)");
                    }
                }

                SendMessage(caller, sb.ToString(), Color.green);
            }
            else
            {
                SendMessage(caller, $"Reload failed: {error}", Color.red);
            }
        }

        private static void HandlePreview(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, "modifieditemdrop.preview"))
            {
                SendMessage(caller, "You do not have permission to preview drop chances.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            var dropService = plugin?.DropService;
            if (plugin == null || dropService == null)
            {
                SendMessage(caller, "ModifiedItemDrop is not ready.", Color.red);
                return;
            }

            UnturnedPlayer target = null;
            if (args.Length == 0)
            {
                target = caller as UnturnedPlayer;
                if (target == null)
                {
                    SendMessage(caller, "Console must specify a player: /mid preview <player>", Color.yellow);
                    return;
                }
            }
            else
            {
                var search = string.Join(" ", args);
                target = UnturnedPlayer.FromName(search);
                if (target == null)
                {
                    SendMessage(caller, $"Player '{search}' not found.", Color.red);
                    return;
                }
            }

            var lines = BuildPreviewLines(target, dropService);
            foreach (var line in lines)
            {
                SendMessage(caller, line, Color.cyan);
            }
        }

        private static void SendUsage(IRocketPlayer caller)
        {
            SendMessage(caller, "Usage: /mid reload | /mid preview [player] | /mid dump [player] | /mid claim", Color.yellow);
        }

        private static void SendMessage(IRocketPlayer caller, string message, Color color)
        {
            if (caller is UnturnedPlayer player)
            {
                UnturnedChat.Say(player, message, color);
                return;
            }

            if (caller == null)
            {
                Logger.Log(message);
                return;
            }

            Logger.Log(message);
        }

        private static bool HasPermission(IRocketPlayer caller, string permission)
        {
            if (caller == null)
            {
                return true;
            }

            return caller.HasPermission(permission);
        }

        private static IEnumerable<string> BuildPreviewLines(UnturnedPlayer target, DropService dropService)
        {
            var lines = new List<string>
            {
                $"[Preview] {target.CharacterName} ({target.CSteamID})"
            };

            // Equipped (hand) item preview
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

        private static bool TryParseChance(string input, out double chance)
        {
            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out chance))
            {
                return false;
            }

            chance = Math.Max(0d, Math.Min(1d, chance));
            return true;
        }

        private static void HandleDump(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, "modifieditemdrop.preview"))
            {
                SendMessage(caller, "You do not have permission to dump inventory.", Color.red);
                return;
            }

            UnturnedPlayer target = null;
            if (args.Length == 0)
            {
                target = caller as UnturnedPlayer;
                if (target == null)
                {
                    SendMessage(caller, "Console must specify a player: /mid dump <player>", Color.yellow);
                    return;
                }
            }
            else
            {
                var search = string.Join(" ", args);
                target = UnturnedPlayer.FromName(search);
                if (target == null)
                {
                    SendMessage(caller, $"Player '{search}' not found.", Color.red);
                    return;
                }
            }

            var lines = BuildDumpLines(target);
            foreach (var line in lines)
            {
                SendMessage(caller, line, Color.cyan);
            }
        }

        private static IEnumerable<string> BuildDumpLines(UnturnedPlayer target)
        {
            var lines = new List<string>
            {
                $"[Dump] {target.CharacterName} ({target.CSteamID})"
            };

            // Equipped item
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

            // Inventory items by page (exclude clothing pages 3-6)
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

            // Clothing with contents
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

            // Summary
            var primaryCount = weaponItemCounts.ContainsKey(0) ? weaponItemCounts[0] : 0;
            var secondaryCount = weaponItemCounts.ContainsKey(1) ? weaponItemCounts[1] : 0;
            var handsCount = weaponItemCounts.ContainsKey(2) ? weaponItemCounts[2] : 0;
            var totalItems = primaryCount + secondaryCount + handsCount + totalClothingItems + totalClothingContents;
            lines.Add($"总计: {totalItems} 个物品 (主武器: {primaryCount}, 副武器: {secondaryCount}, 手持: {handsCount}, 衣物: {totalClothingItems}, 衣物内容: {totalClothingContents})");

            return lines;
        }

        private static string GetPageName(byte page)
        {
            // Unturned inventory pages:
            // 0 = Primary, 1 = Secondary, 2 = Hands
            // 3 = Backpack, 4 = Vest, 5 = Shirt, 6 = Pants
            // 7 = Storage (external)
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

        private static void HandleClaim(IRocketPlayer caller)
        {
            if (!HasPermission(caller, "modifieditemdrop.claim"))
            {
                SendMessage(caller, "You do not have permission to claim pending items.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            var dropService = plugin?.DropService;
            if (dropService == null)
            {
                SendMessage(caller, "ModifiedItemDrop is not ready.", Color.red);
                return;
            }

            if (!(caller is UnturnedPlayer player))
            {
                SendMessage(caller, "Console cannot claim items. Use in-game as a player.", Color.yellow);
                return;
            }

            dropService.ClaimPending(player);
        }
    }
}

