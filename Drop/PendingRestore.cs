using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Models;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    /// <summary>
    /// Represents pending items that need to be restored to a player.
    /// Used during the death-to-revive cycle to track items that should be kept.
    /// </summary>
    public sealed class PendingRestore
    {
        public PendingRestore(UnturnedPlayer player, Vector3 deathPosition)
        {
            DeathPosition = deathPosition;
        }

        public Vector3 DeathPosition { get; }

        public List<Item> InventoryItems { get; } = new List<Item>();

        public List<ClothingItemSnapshot> ClothingItems { get; } = new List<ClothingItemSnapshot>();

        /// <summary>
        /// Maps clothing slot type to items that should be restored to that slot's contents.
        /// </summary>
        public Dictionary<SlotType, List<Item>> ClothingContentsToRestore { get; } = new Dictionary<SlotType, List<Item>>();

        public bool IsEmpty => InventoryItems.Count == 0 && ClothingItems.Count == 0 && ClothingContentsToRestore.Count == 0;
    }
}

