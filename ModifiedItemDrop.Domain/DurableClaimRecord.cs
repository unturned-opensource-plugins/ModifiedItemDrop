using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimRecord
    {
        public DurableClaimRecord(string id, ulong steamId, IEnumerable<DurableClaimAsset> assets)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Claim id must be provided.", nameof(id));
            }

            Id = id;
            SteamId = steamId;
            Assets = (assets ?? throw new ArgumentNullException(nameof(assets))).ToList().AsReadOnly();
            if (Assets.Count == 0)
            {
                throw new ArgumentException("A Durable Claim must contain at least one Player Asset.", nameof(assets));
            }
        }

        public string Id { get; }

        public ulong SteamId { get; }

        public IReadOnlyList<DurableClaimAsset> Assets { get; }
    }
}
