using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Models
{
    /// <summary>
    /// Represents an inventory item pending restore along with its original page.
    /// </summary>
    public sealed class PendingInventoryItem
    {
        public PendingInventoryItem(Item item, byte sourcePage)
        {
            Item = item;
            SourcePage = sourcePage;
        }

        public Item Item { get; }

        public byte SourcePage { get; }
    }
}

