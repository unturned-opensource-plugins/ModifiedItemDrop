using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class MidCommandRouter
    {
        private static readonly Dictionary<string, string> RemovedV1Commands =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "reload", "/mid config reload" },
                { "preview", "/mid rules preview [player]" },
                { "dump", "/mid inventory dump [player]" },
                { "claim", "/mid claims recover oldest" },
                { "status", "/mid diagnostics status" }
            };

        public static MidCommandRouteResult Parse(IEnumerable<string> command)
        {
            var parts = (command ?? Array.Empty<string>())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            if (parts.Count == 0)
            {
                return Usage();
            }

            var root = parts[0];
            if (RemovedV1Commands.TryGetValue(root, out var replacement))
            {
                return new MidCommandRouteResult(
                    accepted: false,
                    kind: MidCommandRouteKind.RemovedV1Command,
                    message: "This v1 flat command was removed in v2. Use " + replacement + ".",
                    arguments: Array.Empty<string>());
            }

            if (parts.Count >= 2 && root.Equals("config", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("reload", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.ConfigReload, parts.Skip(2));
            }

            if (parts.Count >= 2 && root.Equals("rules", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("preview", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.RulesPreview, parts.Skip(2));
            }

            if (parts.Count >= 2 && root.Equals("rules", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("explain", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.RulesExplain, parts.Skip(2));
            }

            if (parts.Count >= 2 && root.Equals("inventory", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("dump", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.InventoryDump, parts.Skip(2));
            }

            if (parts.Count >= 2 && root.Equals("claims", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("recover", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.ClaimsRecover, parts.Skip(2));
            }

            if (parts.Count >= 2 && root.Equals("diagnostics", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.DiagnosticsStatus, parts.Skip(2));
            }

            if (parts.Count >= 2 && root.Equals("diagnostics", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("export", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(MidCommandRouteKind.DiagnosticsExport, parts.Skip(2));
            }

            return Usage();
        }

        private static MidCommandRouteResult Accepted(MidCommandRouteKind kind, IEnumerable<string> arguments)
        {
            return new MidCommandRouteResult(
                accepted: true,
                kind: kind,
                message: string.Empty,
                arguments: arguments);
        }

        private static MidCommandRouteResult Usage()
        {
            return new MidCommandRouteResult(
                accepted: false,
                kind: MidCommandRouteKind.Usage,
                message: "Usage: /mid config reload | /mid rules preview [player] | /mid rules explain <slot|item> <target> | /mid inventory dump [player] | /mid claims recover [oldest|all] | /mid diagnostics status | /mid diagnostics export",
                arguments: Array.Empty<string>());
        }
    }
}
