using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Models
{
    public sealed class ClothingContentSnapshot
    {
        public ClothingContentSnapshot(byte index, Item item)
        {
            Index = index;
            Item = item;
        }

        public byte Index { get; }

        public Item Item { get; }
    }
}

