using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Models;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Utilities
{
    /// <summary>
    /// Helper class for clothing-related operations.
    /// Consolidates all clothing slot manipulation logic in one place.
    /// </summary>
    public static class ClothingOperationHelper
    {
        private static readonly Dictionary<SlotType, Action<PlayerClothing, Guid, byte, byte[], bool>> ClearActions =
            new Dictionary<SlotType, Action<PlayerClothing, Guid, byte, byte[], bool>>
            {
                { SlotType.Shirt, (c, g, q, s, b) => c.ReceiveWearShirt(g, q, s, b) },
                { SlotType.Pants, (c, g, q, s, b) => c.ReceiveWearPants(g, q, s, b) },
                { SlotType.Backpack, (c, g, q, s, b) => c.ReceiveWearBackpack(g, q, s, b) },
                { SlotType.Vest, (c, g, q, s, b) => c.ReceiveWearVest(g, q, s, b) },
                { SlotType.Hat, (c, g, q, s, b) => c.ReceiveWearHat(g, q, s, b) },
                { SlotType.Mask, (c, g, q, s, b) => c.ReceiveWearMask(g, q, s, b) },
                { SlotType.Glasses, (c, g, q, s, b) => c.ReceiveWearGlasses(g, q, s, b) }
            };

        private static readonly Dictionary<SlotType, Action<PlayerClothing, ushort, byte, byte[], bool>> WearActions =
            new Dictionary<SlotType, Action<PlayerClothing, ushort, byte, byte[], bool>>
            {
                { SlotType.Shirt, (c, id, q, s, b) => c.askWearShirt(id, q, s, b) },
                { SlotType.Pants, (c, id, q, s, b) => c.askWearPants(id, q, s, b) },
                { SlotType.Backpack, (c, id, q, s, b) => c.askWearBackpack(id, q, s, b) },
                { SlotType.Vest, (c, id, q, s, b) => c.askWearVest(id, q, s, b) },
                { SlotType.Hat, (c, id, q, s, b) => c.askWearHat(id, q, s, b) },
                { SlotType.Mask, (c, id, q, s, b) => c.askWearMask(id, q, s, b) },
                { SlotType.Glasses, (c, id, q, s, b) => c.askWearGlasses(id, q, s, b) }
            };

        private static readonly Dictionary<SlotType, Func<PlayerClothing, ushort>> SlotIdAccessors =
            new Dictionary<SlotType, Func<PlayerClothing, ushort>>
            {
                { SlotType.Shirt, c => c.shirt },
                { SlotType.Pants, c => c.pants },
                { SlotType.Backpack, c => c.backpack },
                { SlotType.Vest, c => c.vest },
                { SlotType.Hat, c => c.hat },
                { SlotType.Mask, c => c.mask },
                { SlotType.Glasses, c => c.glasses }
            };

        /// <summary>
        /// Clears a clothing slot by setting it to empty (ID 0).
        /// </summary>
        public static void ClearClothingSlot(PlayerClothing clothing, SlotType slot)
        {
            if (ClearActions.TryGetValue(slot, out var action))
            {
                action(clothing, Guid.Empty, 0, Array.Empty<byte>(), false);
            }
        }

        /// <summary>
        /// Wears a clothing item in the specified slot.
        /// </summary>
        public static void WearClothingItem(PlayerClothing clothing, SlotType slot, ushort itemId, byte quality, byte[] state)
        {
            if (WearActions.TryGetValue(slot, out var action))
            {
                action(clothing, itemId, quality, state ?? Array.Empty<byte>(), true);
            }
        }

        /// <summary>
        /// Attempts to wear a clothing item, checking if the slot is currently empty.
        /// Returns true if the item was successfully worn, false otherwise.
        /// </summary>
        public static bool TryWearClothing(PlayerClothing clothing, SlotType slot, ushort itemId, byte quality, byte[] state)
        {
            if (!SlotIdAccessors.TryGetValue(slot, out var accessor) || accessor(clothing) != 0)
            {
                return false;
            }

            WearClothingItem(clothing, slot, itemId, quality, state);
            return true;
        }
    }
}
