using FFEmqo.ModifiedItemDrop.Models;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace FFEmqo.ModifiedItemDrop.Utilities
{
    public static class UtilityHelper
    {
        /// <summary>
        /// Clamps a probability value to the valid range [0, 1].
        /// </summary>
        public static double ClampChance(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0d;
            }

            if (value < 0d)
            {
                return 0d;
            }

            if (value > 1d)
            {
                return 1d;
            }

            return value;
        }

        /// <summary>
        /// Creates a deep copy of an Item, including cloning its state array.
        /// </summary>
        public static Item CloneItem(Item item)
        {
            if (item == null)
            {
                return null;
            }

            var state = item.state != null ? (byte[])item.state.Clone() : System.Array.Empty<byte>();
            return new Item(item.id, item.amount, item.quality, state);
        }

        /// <summary>
        /// Converts an inventory page number to its corresponding SlotType.
        /// </summary>
        public static SlotType GetSlotTypeForPage(byte page)
        {
            // Unturned inventory pages:
            // 0 = Primary Weapon, 1 = Secondary Weapon, 2 = Hands
            // 3-6 = Clothing storage (handled separately)
            // 7 = Storage (external), 8 = Area
            switch (page)
            {
                case 0:
                    return SlotType.PrimaryWeapon;
                case 1:
                    return SlotType.SecondaryWeapon;
                case 2:
                    return SlotType.Hands;
                default:
                    return SlotType.Inventory;
            }
        }

        /// <summary>
        /// Safely sends a chat notification to a player, catching any chat errors.
        /// </summary>
        public static void TryNotify(UnturnedPlayer player, string message)
        {
            try
            {
                UnturnedChat.Say(player, message, Color.yellow);
            }
            catch
            {
                // Ignore chat errors
            }
        }

        /// <summary>
        /// Drops an item to the world at the specified position.
        /// </summary>
        public static void DropWorldItem(Item item, Vector3 position)
        {
            var clone = CloneItem(item);
            var dropPosition = position + Vector3.up * 0.5f;
            ItemManager.dropItem(clone, dropPosition, false, true, true);
        }
    }
}
