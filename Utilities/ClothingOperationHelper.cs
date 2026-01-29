using System;
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
        /// <summary>
        /// Clears a clothing slot by setting it to empty (ID 0).
        /// </summary>
        public static void ClearClothingSlot(PlayerClothing clothing, SlotType slot)
        {
            var emptyState = Array.Empty<byte>();
            switch (slot)
            {
                case SlotType.Shirt:
                    clothing.ReceiveWearShirt(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Pants:
                    clothing.ReceiveWearPants(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Backpack:
                    clothing.ReceiveWearBackpack(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Vest:
                    clothing.ReceiveWearVest(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Hat:
                    clothing.ReceiveWearHat(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Mask:
                    clothing.ReceiveWearMask(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Glasses:
                    clothing.ReceiveWearGlasses(Guid.Empty, 0, emptyState, false);
                    break;
            }
        }

        /// <summary>
        /// Wears a clothing item in the specified slot.
        /// </summary>
        public static void WearClothingItem(PlayerClothing clothing, SlotType slot, ushort itemId, byte quality, byte[] state)
        {
            var itemState = state ?? Array.Empty<byte>();
            switch (slot)
            {
                case SlotType.Shirt:
                    clothing.askWearShirt(itemId, quality, itemState, true);
                    break;
                case SlotType.Pants:
                    clothing.askWearPants(itemId, quality, itemState, true);
                    break;
                case SlotType.Backpack:
                    clothing.askWearBackpack(itemId, quality, itemState, true);
                    break;
                case SlotType.Vest:
                    clothing.askWearVest(itemId, quality, itemState, true);
                    break;
                case SlotType.Hat:
                    clothing.askWearHat(itemId, quality, itemState, true);
                    break;
                case SlotType.Mask:
                    clothing.askWearMask(itemId, quality, itemState, true);
                    break;
                case SlotType.Glasses:
                    clothing.askWearGlasses(itemId, quality, itemState, true);
                    break;
            }
        }

        /// <summary>
        /// Attempts to wear a clothing item, checking if the slot is currently empty.
        /// Returns true if the item was successfully worn, false otherwise.
        /// </summary>
        public static bool TryWearClothing(PlayerClothing clothing, SlotType slot, ushort itemId, byte quality, byte[] state)
        {
            var itemState = state ?? Array.Empty<byte>();
            switch (slot)
            {
                case SlotType.Shirt:
                    if (clothing.shirt == 0)
                    {
                        clothing.askWearShirt(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
                case SlotType.Pants:
                    if (clothing.pants == 0)
                    {
                        clothing.askWearPants(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
                case SlotType.Backpack:
                    if (clothing.backpack == 0)
                    {
                        clothing.askWearBackpack(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
                case SlotType.Vest:
                    if (clothing.vest == 0)
                    {
                        clothing.askWearVest(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
                case SlotType.Hat:
                    if (clothing.hat == 0)
                    {
                        clothing.askWearHat(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
                case SlotType.Mask:
                    if (clothing.mask == 0)
                    {
                        clothing.askWearMask(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
                case SlotType.Glasses:
                    if (clothing.glasses == 0)
                    {
                        clothing.askWearGlasses(itemId, quality, itemState, true);
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
