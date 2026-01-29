using System;
using System.Collections.Generic;

namespace FFEmqo.ModifiedItemDrop.Models
{
    public sealed class ClaimRecord
    {
        public string Id { get; set; }
        public ulong SteamId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public float DeathX { get; set; }
        public float DeathY { get; set; }
        public float DeathZ { get; set; }
        public List<ClaimItem> Items { get; set; } = new List<ClaimItem>();
        public List<ClaimClothing> Clothing { get; set; } = new List<ClaimClothing>();

        public bool IsEmpty => (Items == null || Items.Count == 0) && (Clothing == null || Clothing.Count == 0);

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    public sealed class ClaimItem
    {
        public ushort ItemId { get; set; }
        public byte Amount { get; set; }
        public byte Quality { get; set; }
        public byte[] State { get; set; }
    }

    public sealed class ClaimClothing
    {
        public SlotType Slot { get; set; }
        public ushort ItemId { get; set; }
        public byte Quality { get; set; }
        public byte[] State { get; set; }
        public List<ClaimItem> Contents { get; set; } = new List<ClaimItem>();
    }
}
