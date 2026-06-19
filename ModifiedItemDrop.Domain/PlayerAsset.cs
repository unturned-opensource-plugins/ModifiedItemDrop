using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class PlayerAsset
    {
        public PlayerAsset(string id, PlayerAssetSlot slot, ushort itemId)
            : this(id, slot, itemId, isClothingContent: false, sourceClothingSlot: null, parentAssetId: null)
        {
        }

        private PlayerAsset(
            string id,
            PlayerAssetSlot slot,
            ushort itemId,
            bool isClothingContent,
            PlayerAssetSlot? sourceClothingSlot,
            string? parentAssetId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Asset id must be provided.", nameof(id));
            }

            if (isClothingContent && string.IsNullOrWhiteSpace(parentAssetId))
            {
                throw new ArgumentException("Clothing content must reference its parent asset.", nameof(parentAssetId));
            }

            Id = id;
            Slot = slot;
            ItemId = itemId;
            IsClothingContent = isClothingContent;
            SourceClothingSlot = sourceClothingSlot;
            ParentAssetId = parentAssetId;
        }

        public string Id { get; }

        public PlayerAssetSlot Slot { get; }

        public ushort ItemId { get; }

        public bool IsClothingContent { get; }

        public PlayerAssetSlot? SourceClothingSlot { get; }

        public string? ParentAssetId { get; }

        public static PlayerAsset ClothingContent(
            string id,
            PlayerAssetSlot sourceClothingSlot,
            string parentAssetId,
            ushort itemId)
        {
            return new PlayerAsset(
                id,
                sourceClothingSlot,
                itemId,
                isClothingContent: true,
                sourceClothingSlot: sourceClothingSlot,
                parentAssetId: parentAssetId);
        }
    }
}
