using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Models
{
    public sealed class InventoryItemSnapshot
    {
        public InventoryItemSnapshot(byte page, byte index, ItemJar jar)
        {
            Page = page;
            Index = index;
            Jar = jar;
        }

        public byte Page { get; }

        public byte Index { get; }

        public ItemJar Jar { get; }
    }
}

