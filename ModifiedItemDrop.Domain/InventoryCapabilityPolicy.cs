using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class InventoryCapabilityPolicy
    {
        public static HandsSlotCapabilityDecision SelectHandsSlotRule(
            IEnumerable<HandsSlotCapabilityRule> rules,
            Func<string, bool> hasPermission)
        {
            var ruleList = (rules ?? Array.Empty<HandsSlotCapabilityRule>())
                .Where(rule => rule != null)
                .ToList();
            if (ruleList.Count == 0)
            {
                return new HandsSlotCapabilityDecision(false, string.Empty, 0, 0, "No hands slot Inventory Capability rules configured.");
            }

            var permissionEvaluator = hasPermission ?? (_ => false);
            HandsSlotCapabilityRule? selected = null;
            var usedFallback = false;

            for (var index = ruleList.Count - 1; index >= 0; index--)
            {
                var rule = ruleList[index];
                if (rule.PermissionName.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    if (selected == null)
                    {
                        selected = rule;
                        usedFallback = true;
                    }

                    continue;
                }

                if (permissionEvaluator(rule.Permission))
                {
                    selected = rule;
                    usedFallback = false;
                    break;
                }
            }

            if (selected == null)
            {
                return new HandsSlotCapabilityDecision(false, string.Empty, 0, 0, "No matching hands slot Inventory Capability rule and no default fallback rule configured.");
            }

            var width = Clamp(selected.Width);
            var height = Clamp(selected.Height);
            var diagnostic = "Inventory Capability hands slot rule '" + selected.PermissionName + "' applied";
            if (usedFallback)
            {
                diagnostic += " as default fallback";
            }

            if (width != selected.Width || height != selected.Height)
            {
                diagnostic += "; dimensions clamped to " + width + "x" + height;
            }
            else
            {
                diagnostic += " with dimensions " + width + "x" + height;
            }

            return new HandsSlotCapabilityDecision(true, selected.PermissionName, width, height, diagnostic + ".");
        }

        private static int Clamp(int value)
        {
            if (value < 1)
            {
                return 1;
            }

            if (value > 12)
            {
                return 12;
            }

            return value;
        }
    }
}
