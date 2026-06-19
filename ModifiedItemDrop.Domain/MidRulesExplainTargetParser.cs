using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class MidRulesExplainTargetParser
    {
        public static MidRulesExplainTarget Parse(IEnumerable<string> args)
        {
            var parts = (args ?? Array.Empty<string>()).Where(part => !string.IsNullOrWhiteSpace(part)).ToList();
            if (parts.Count < 2)
            {
                return Rejected("Usage: /mid rules explain slot <PlayerAssetSlot> | /mid rules explain item <itemId>");
            }

            if (parts[0].Equals("slot", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse(parts[1], ignoreCase: true, out PlayerAssetSlot slot))
                {
                    return Rejected("Unknown Player Asset slot '" + parts[1] + "'.");
                }

                return Accepted(new PlayerAsset("explain:slot:" + slot, slot, itemId: 0));
            }

            if (parts[0].Equals("item", StringComparison.OrdinalIgnoreCase))
            {
                if (!ushort.TryParse(parts[1], out var itemId))
                {
                    return Rejected("Item target must be an unsigned 16-bit item id.");
                }

                return Accepted(new PlayerAsset("explain:item:" + itemId, PlayerAssetSlot.Hands, itemId));
            }

            return Rejected("Usage: /mid rules explain slot <PlayerAssetSlot> | /mid rules explain item <itemId>");
        }

        private static MidRulesExplainTarget Accepted(PlayerAsset asset)
        {
            return new MidRulesExplainTarget(true, asset, string.Empty);
        }

        private static MidRulesExplainTarget Rejected(string message)
        {
            return new MidRulesExplainTarget(false, null, message);
        }
    }
}
