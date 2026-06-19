namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class OutcomeTarget
    {
        private OutcomeTarget(PlayerAssetSlot slot)
        {
            Slot = slot;
        }

        public PlayerAssetSlot Slot { get; }

        public static OutcomeTarget ForSlot(PlayerAssetSlot slot)
        {
            return new OutcomeTarget(slot);
        }

        public bool Matches(PlayerAsset asset)
        {
            return asset != null && asset.Slot == Slot;
        }
    }
}
