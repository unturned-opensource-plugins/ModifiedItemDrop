using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class PlayerAsset
    {
        public PlayerAsset(string id, PlayerAssetSlot slot, ushort itemId)
            : this(id, slot, itemId, amount: 1, quality: 100, state: null)
        {
        }

        public PlayerAsset(string id, PlayerAssetSlot slot, ushort itemId, byte amount, byte quality, byte[]? state)
            : this(id, slot, itemId, amount, quality, state, isClothingContent: false, sourceClothingSlot: null, parentAssetId: null, inventoryPage: null, inventoryIndex: null)
        {
        }

        private PlayerAsset(
            string id,
            PlayerAssetSlot slot,
            ushort itemId,
            byte amount,
            byte quality,
            byte[]? state,
            bool isClothingContent,
            PlayerAssetSlot? sourceClothingSlot,
            string? parentAssetId,
            byte? inventoryPage,
            byte? inventoryIndex)
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
            Amount = amount;
            Quality = quality;
            State = state != null ? (byte[])state.Clone() : Array.Empty<byte>();
            IsClothingContent = isClothingContent;
            SourceClothingSlot = sourceClothingSlot;
            ParentAssetId = parentAssetId;
            InventoryPage = inventoryPage;
            InventoryIndex = inventoryIndex;
        }

        public string Id { get; }

        public PlayerAssetSlot Slot { get; }

        public ushort ItemId { get; }

        public byte Amount { get; }

        public byte Quality { get; }

        public byte[] State { get; }

        public bool IsClothingContent { get; }

        public PlayerAssetSlot? SourceClothingSlot { get; }

        public string? ParentAssetId { get; }

        public byte? InventoryPage { get; }

        public byte? InventoryIndex { get; }

        public PlayerAsset WithInventoryLocation(byte page, byte index)
        {
            return new PlayerAsset(
                Id,
                Slot,
                ItemId,
                Amount,
                Quality,
                State,
                IsClothingContent,
                SourceClothingSlot,
                ParentAssetId,
                page,
                index);
        }

        public static PlayerAsset ClothingContent(
            string id,
            PlayerAssetSlot sourceClothingSlot,
            string parentAssetId,
            ushort itemId)
        {
            return ClothingContent(id, sourceClothingSlot, parentAssetId, itemId, amount: 1, quality: 100, state: null);
        }

        public static PlayerAsset ClothingContent(
            string id,
            PlayerAssetSlot sourceClothingSlot,
            string parentAssetId,
            ushort itemId,
            byte amount,
            byte quality,
            byte[]? state)
        {
            return new PlayerAsset(
                id,
                sourceClothingSlot,
                itemId,
                amount,
                quality,
                state,
                isClothingContent: true,
                sourceClothingSlot: sourceClothingSlot,
                parentAssetId: parentAssetId,
                inventoryPage: null,
                inventoryIndex: null);
        }
    }
}
