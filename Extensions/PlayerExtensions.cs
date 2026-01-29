using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly SlotType[] ClothingSlots =
        {
            SlotType.Shirt,
            SlotType.Pants,
            SlotType.Backpack,
            SlotType.Vest,
            SlotType.Hat,
            SlotType.Mask,
            SlotType.Glasses
        };

        // Clothing storage page mapping in PlayerInventory.items[]
        // Page 3 = Backpack, Page 4 = Vest, Page 5 = Shirt, Page 6 = Pants
        private static readonly Dictionary<SlotType, byte> ClothingSlotToPage = new Dictionary<SlotType, byte>
        {
            { SlotType.Shirt, 5 },
            { SlotType.Pants, 6 },
            { SlotType.Backpack, 3 },
            { SlotType.Vest, 4 }
        };

        // 缓存衣物属性访问器，避免重复的 switch 语句
        private static readonly Dictionary<SlotType, Func<PlayerClothing, (ushort id, byte quality, byte[] state)>> ClothingAccessors =
            new Dictionary<SlotType, Func<PlayerClothing, (ushort, byte, byte[])>>
            {
                { SlotType.Shirt, c => (c.shirt, c.shirtQuality, c.shirtState) },
                { SlotType.Pants, c => (c.pants, c.pantsQuality, c.pantsState) },
                { SlotType.Backpack, c => (c.backpack, c.backpackQuality, c.backpackState) },
                { SlotType.Vest, c => (c.vest, c.vestQuality, c.vestState) },
                { SlotType.Hat, c => (c.hat, c.hatQuality, c.hatState) },
                { SlotType.Mask, c => (c.mask, c.maskQuality, c.maskState) },
                { SlotType.Glasses, c => (c.glasses, c.glassesQuality, c.glassesState) }
            };

        public static List<InventoryItemSnapshot> CaptureInventory(this UnturnedPlayer player)
        {
            var snapshots = new List<InventoryItemSnapshot>();

            if (player?.Player?.inventory == null)
            {
                return snapshots;
            }

            var inventory = player.Player.inventory;
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                if (inventory.items == null || page >= inventory.items.Length)
                {
                    continue;
                }

                var pageItems = inventory.items[page];
                if (pageItems == null)
                {
                    continue;
                }

                var count = pageItems.getItemCount();
                for (byte index = 0; index < count; index++)
                {
                    var jar = pageItems.getItem(index);
                    if (jar != null)
                    {
                        snapshots.Add(new InventoryItemSnapshot(page, index, jar));
                    }
                }
            }

            return snapshots;
        }

        public static List<ClothingItemSnapshot> CaptureClothing(this UnturnedPlayer player)
        {
            var snapshots = new List<ClothingItemSnapshot>();
            var clothing = player?.Player?.clothing;
            if (clothing == null)
            {
                return snapshots;
            }

            foreach (var slot in ClothingSlots)
            {
                var snapshot = CaptureClothingSlot(player.Player, clothing, slot);
                if (snapshot != null)
                {
                    snapshots.Add(snapshot);
                }
            }

            return snapshots;
        }

        internal static ClothingItemSnapshot CaptureClothingSlot(Player player, PlayerClothing clothing, SlotType slot)
        {
            var item = CloneClothingItem(clothing, slot);
            if (item == null || item.id == 0)
            {
                return null;
            }

            var container = GetClothingContainer(player, slot);
            var contents = CaptureClothingContents(container);
            return new ClothingItemSnapshot(slot, item, contents);
        }

        internal static ClothingItemSnapshot CaptureClothingSlot(PlayerClothing clothing, SlotType slot)
        {
            // Legacy overload - gets player from clothing.player
            if (clothing?.player == null)
            {
                return null;
            }

            return CaptureClothingSlot(clothing.player, clothing, slot);
        }

        /// <summary>
        /// Gets the Items container for a clothing slot from PlayerInventory.
        /// Clothing storage uses inventory pages: 3=Backpack, 4=Vest, 5=Shirt, 6=Pants
        /// </summary>
        internal static Items GetClothingContainer(Player player, SlotType slot)
        {
            if (player?.inventory?.items == null)
            {
                return null;
            }

            if (!ClothingSlotToPage.TryGetValue(slot, out var page))
            {
                // Hat, Mask, Glasses don't have storage
                return null;
            }

            if (page >= player.inventory.items.Length)
            {
                return null;
            }

            return player.inventory.items[page];
        }

        /// <summary>
        /// Gets the Items container for a clothing slot. Uses PlayerClothing to find the player.
        /// </summary>
        internal static Items GetClothingContainer(PlayerClothing clothing, SlotType slot)
        {
            if (clothing?.player == null)
            {
                return null;
            }

            return GetClothingContainer(clothing.player, slot);
        }

        internal static List<ClothingContentSnapshot> CaptureClothingContents(Items container)
        {
            var contents = new List<ClothingContentSnapshot>();

            if (container == null)
            {
                return contents;
            }

            var count = container.getItemCount();
            for (byte index = 0; index < count; index++)
            {
                var jar = container.getItem(index);
                if (jar?.item != null)
                {
                    contents.Add(new ClothingContentSnapshot(index, UtilityHelper.CloneItem(jar.item)));
                }
            }

            return contents;
        }

        private static Item CloneClothingItem(PlayerClothing clothing, SlotType slot)
        {
            if (!ClothingAccessors.TryGetValue(slot, out var accessor))
            {
                return null;
            }

            var (id, quality, state) = accessor(clothing);

            if (id == 0)
            {
                return null;
            }

            var stateCopy = state != null ? (byte[])state.Clone() : Array.Empty<byte>();
            return new Item(id, 1, quality, stateCopy);
        }
    }
}
