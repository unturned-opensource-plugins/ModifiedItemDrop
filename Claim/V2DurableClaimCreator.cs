using System;
using FFEmqo.ModifiedItemDrop.Domain;

namespace FFEmqo.ModifiedItemDrop.Claim
{
    public sealed class V2DurableClaimCreator : IDurableClaimCreator
    {
        private readonly DurableClaimStore _store;

        public V2DurableClaimCreator(DurableClaimStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public DurableClaimCreateResult TryCreate(DurableClaimRecord claim)
        {
            return _store.TryCreate(claim);
        }
    }
}
