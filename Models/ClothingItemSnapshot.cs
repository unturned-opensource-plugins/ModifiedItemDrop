using System.Collections.Generic;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Models
{
    public sealed class ClothingItemSnapshot
    {
        public ClothingItemSnapshot(SlotType slotType, Item item, List<ClothingContentSnapshot> contents)
        {
            SlotType = slotType;
            Item = item;
            Contents = contents ?? new List<ClothingContentSnapshot>();
        }

        public SlotType SlotType { get; }

        public Item Item { get; }

        public List<ClothingContentSnapshot> Contents { get; }
    }
}

