using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public List<string> Permissions => new List<string>();

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

            var target = ResolveTarget(caller, args, "preview");
            if (target == null)
            {
                return;
            }

            foreach (var line in InventoryInspector.BuildPreviewLines(target, dropService))
            {
                SendMessage(caller, line, Color.cyan);
            }
        }

        private static void HandleDump(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, "modifieditemdrop.preview"))
            {
                SendMessage(caller, "You do not have permission to dump inventory.", Color.red);
                return;
            }

            var target = ResolveTarget(caller, args, "dump");
            if (target == null)
            {
                return;
            }

            foreach (var line in InventoryInspector.BuildDumpLines(target))
            {
                SendMessage(caller, line, Color.cyan);
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

        private static UnturnedPlayer ResolveTarget(IRocketPlayer caller, string[] args, string subCommand)
        {
            if (args.Length == 0)
            {
                var target = caller as UnturnedPlayer;
                if (target == null)
                {
                    SendMessage(caller, $"Console must specify a player: /mid {subCommand} <player>", Color.yellow);
                }
                return target;
            }

            var search = string.Join(" ", args);
            var found = UnturnedPlayer.FromName(search);
            if (found == null)
            {
                SendMessage(caller, $"Player '{search}' not found.", Color.red);
            }
            return found;
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
    }
}
