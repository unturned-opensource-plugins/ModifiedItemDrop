using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimAsset
    {
        public DurableClaimAsset(string assetId, ushort itemId, byte amount, byte quality, byte[] state)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                throw new ArgumentException("Asset id must be provided.", nameof(assetId));
            }

            AssetId = assetId;
            ItemId = itemId;
            Amount = amount;
            Quality = quality;
            State = state ?? Array.Empty<byte>();
        }

        public string AssetId { get; }

        public ushort ItemId { get; }

        public byte Amount { get; }

        public byte Quality { get; }

        public byte[] State { get; }
    }
}
