using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFEmqo.ModifiedItemDrop.Domain;
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

        public string Syntax => "<config|rules|inventory|claims|diagnostics>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>
        {
            MidCommandPermissionPolicy.ConfigReload,
            MidCommandPermissionPolicy.RulesPreview,
            MidCommandPermissionPolicy.RulesExplain,
            MidCommandPermissionPolicy.InventoryDump,
            MidCommandPermissionPolicy.ClaimsList,
            MidCommandPermissionPolicy.ClaimsRecover,
            MidCommandPermissionPolicy.DiagnosticsStatus,
            MidCommandPermissionPolicy.DiagnosticsExport
        };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command == null || command.Length == 0)
            {
                SendUsage(caller);
                return;
            }

            var route = MidCommandRouter.Parse(command);
            if (!route.Accepted)
            {
                SendMessage(caller, route.Message, Color.yellow);
                return;
            }

            switch (route.Kind)
            {
                case MidCommandRouteKind.ConfigReload:
                    HandleReload(caller);
                    break;
                case MidCommandRouteKind.RulesPreview:
                    HandlePreview(caller, route.Arguments.ToArray());
                    break;
                case MidCommandRouteKind.RulesExplain:
                    HandleRulesExplain(caller, route.Arguments.ToArray());
                    break;
                case MidCommandRouteKind.InventoryDump:
                    HandleDump(caller, route.Arguments.ToArray());
                    break;
                case MidCommandRouteKind.ClaimsList:
                    HandleClaimsList(caller, route.Arguments.ToArray());
                    break;
                case MidCommandRouteKind.ClaimsRecover:
                    HandleClaim(caller, route.Arguments.ToArray());
                    break;
                case MidCommandRouteKind.DiagnosticsStatus:
                    HandleStatus(caller);
                    break;
                case MidCommandRouteKind.DiagnosticsExport:
                    HandleDiagnosticsExport(caller);
                    break;
                default:
                    SendUsage(caller);
                    break;
            }
        }

        private static void HandleReload(IRocketPlayer caller)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.ConfigReload))
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
                    sb.Append(summary.DeathProcessingEnabled ? " DeathProcessing=enabled." : $" DeathProcessing=disabled: {summary.SafeModeReason}");
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
            if (!HasPermission(caller, MidCommandPermissionPolicy.RulesPreview))
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

        private static void HandleRulesExplain(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.RulesExplain))
            {
                SendMessage(caller, "You do not have permission to explain Outcome Rules.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            var dropService = plugin?.DropService;
            if (plugin == null || dropService == null)
            {
                SendMessage(caller, "ModifiedItemDrop is not ready.", Color.red);
                return;
            }

            foreach (var line in dropService.ExplainOutcomeRuleTarget(args))
            {
                SendMessage(caller, line, Color.cyan);
            }
        }

        private static void HandleDump(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.InventoryDump))
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

        private static void HandleClaimsList(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.ClaimsList))
            {
                SendMessage(caller, "You do not have permission to list pending Claims.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            var recovery = plugin?.V2ClaimRecoveryService;
            if (plugin == null || recovery == null)
            {
                SendMessage(caller, "ModifiedItemDrop is not ready.", Color.red);
                return;
            }

            if (!recovery.RecoveryEnabled)
            {
                SendMessage(caller, "Claim Recovery is disabled by Claim storage degraded mode: " + recovery.DisabledReason, Color.red);
                return;
            }

            var target = ResolveTarget(caller, args, "claims list");
            if (target == null)
            {
                return;
            }

            var claims = recovery.ListClaims((ulong)target.CSteamID);
            if (claims.Count == 0)
            {
                SendMessage(caller, $"No v2 Durable Claims found for {target.CharacterName}.", Color.cyan);
                return;
            }

            var assetCount = claims.Sum(claim => claim.Assets.Count);
            SendMessage(caller, $"{target.CharacterName} has {claims.Count} v2 Durable Claim(s), {assetCount} Player Asset(s).", Color.cyan);
            foreach (var claim in claims.Take(5))
            {
                SendMessage(caller, $" - claim={claim.Id} assets={claim.Assets.Count}", Color.cyan);
            }
        }

        private static void HandleClaim(IRocketPlayer caller, string[] args)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.ClaimsRecover))
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

            var mode = args != null && args.Length > 0 ? args[0].ToLowerInvariant() : "oldest";
            if (mode == "all")
            {
                dropService.ClaimAllPending(player);
                return;
            }

            dropService.ClaimPending(player);
        }

        private static void HandleStatus(IRocketPlayer caller)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.DiagnosticsStatus) && !HasPermission(caller, MidCommandPermissionPolicy.ConfigReload))
            {
                SendMessage(caller, "You do not have permission to view ModifiedItemDrop status.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            var loader = plugin?.ConfigurationLoader;
            var dropService = plugin?.DropService;
            if (plugin == null || loader == null || dropService == null)
            {
                SendMessage(caller, "ModifiedItemDrop is not ready.", Color.red);
                return;
            }

            SendMessage(caller, loader.IsDeathProcessingEnabled
                ? "Outcome Rules: valid; death processing enabled."
                : $"Outcome Rules: invalid; safe mode active. {loader.SafeModeReason}", loader.IsDeathProcessingEnabled ? Color.green : Color.yellow);

            SendMessage(caller, dropService.IsClaimStorageDeathProcessingEnabled
                ? "Claim storage: healthy for death processing."
                : $"Claim storage: degraded; death processing disabled. {dropService.ClaimStorageDisabledReason}", dropService.IsClaimStorageDeathProcessingEnabled ? Color.green : Color.red);

            SendMessage(caller, dropService.IsV2ClaimRecoveryEnabled
                ? "Claim Recovery: enabled."
                : "Claim Recovery: disabled by Claim storage degraded mode.", dropService.IsV2ClaimRecoveryEnabled ? Color.green : Color.red);
        }

        private static void HandleDiagnosticsExport(IRocketPlayer caller)
        {
            if (!HasPermission(caller, MidCommandPermissionPolicy.DiagnosticsExport) && !HasPermission(caller, MidCommandPermissionPolicy.DiagnosticsStatus))
            {
                SendMessage(caller, "You do not have permission to export ModifiedItemDrop diagnostics.", Color.red);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            var loader = plugin?.ConfigurationLoader;
            var dropService = plugin?.DropService;
            if (plugin == null || loader == null || dropService == null)
            {
                SendMessage(caller, "ModifiedItemDrop is not ready.", Color.red);
                return;
            }

            SendMessage(caller, "[ModifiedItemDrop diagnostics export]", Color.cyan);
            SendMessage(caller, loader.IsDeathProcessingEnabled
                ? "Outcome Rules: valid; death processing enabled."
                : $"Outcome Rules: invalid; safe mode active. {loader.SafeModeReason}", Color.cyan);
            SendMessage(caller, dropService.IsClaimStorageDeathProcessingEnabled
                ? "Claim storage: healthy for death processing."
                : $"Claim storage: degraded; death processing disabled. {dropService.ClaimStorageDisabledReason}", Color.cyan);

            var paths = plugin.V2ClaimStoragePaths;
            if (paths != null)
            {
                SendMessage(caller, $"V2 Claim primary: {paths.PrimaryPath}", Color.cyan);
                SendMessage(caller, $"V2 Claim backup: {paths.BackupPath}", Color.cyan);
                SendMessage(caller, $"V2 Claim corrupt directory: {paths.CorruptDirectory}", Color.cyan);
            }
        }

        private static UnturnedPlayer ResolveTarget(IRocketPlayer caller, string[] args, string subCommand)
        {
            if (args.Length == 0)
            {
                var target = caller as UnturnedPlayer;
                if (target == null)
                {
                    SendMessage(caller, $"Console must specify a player for /mid {subCommand}.", Color.yellow);
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
            SendMessage(caller, MidCommandRouter.Parse(command: null).Message, Color.yellow);
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
