using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class PlayerAsset
    {
        public PlayerAsset(string id, PlayerAssetSlot slot, ushort itemId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Asset id must be provided.", nameof(id));
            }

            Id = id;
            Slot = slot;
            ItemId = itemId;
        }

        public string Id { get; }

        public PlayerAssetSlot Slot { get; }

        public ushort ItemId { get; }
    }
}
