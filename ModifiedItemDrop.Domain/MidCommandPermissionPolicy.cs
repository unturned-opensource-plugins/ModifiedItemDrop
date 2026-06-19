using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class MidCommandPermissionPolicy
    {
        public const string ConfigReload = "modifieditemdrop.config.reload";
        public const string RulesPreview = "modifieditemdrop.rules.preview";
        public const string RulesExplain = "modifieditemdrop.rules.explain";
        public const string InventoryDump = "modifieditemdrop.inventory.dump";
        public const string ClaimsRecover = "modifieditemdrop.claims.recover";
        public const string DiagnosticsStatus = "modifieditemdrop.diagnostics.status";
        public const string DiagnosticsExport = "modifieditemdrop.diagnostics.export";

        public static string RequiredPermissionFor(MidCommandRouteKind kind)
        {
            switch (kind)
            {
                case MidCommandRouteKind.ConfigReload:
                    return ConfigReload;
                case MidCommandRouteKind.RulesPreview:
                    return RulesPreview;
                case MidCommandRouteKind.RulesExplain:
                    return RulesExplain;
                case MidCommandRouteKind.InventoryDump:
                    return InventoryDump;
                case MidCommandRouteKind.ClaimsRecover:
                    return ClaimsRecover;
                case MidCommandRouteKind.DiagnosticsStatus:
                    return DiagnosticsStatus;
                case MidCommandRouteKind.DiagnosticsExport:
                    return DiagnosticsExport;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Route kind has no command permission.");
            }
        }
    }
}
