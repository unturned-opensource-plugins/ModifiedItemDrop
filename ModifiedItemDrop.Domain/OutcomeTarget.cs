namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class OutcomeTarget
    {
        private readonly OutcomeTargetKind _kind;
        private readonly PlayerAssetSlot? _slot;
        private readonly ushort? _itemId;

        private OutcomeTarget(OutcomeTargetKind kind, PlayerAssetSlot? slot, ushort? itemId)
        {
            _kind = kind;
            _slot = slot;
            _itemId = itemId;
        }

        public bool IsCatchAll
        {
            get { return _kind == OutcomeTargetKind.Any; }
        }

        public static OutcomeTarget Any()
        {
            return new OutcomeTarget(OutcomeTargetKind.Any, null, null);
        }

        public static OutcomeTarget ForSlot(PlayerAssetSlot slot)
        {
            return new OutcomeTarget(OutcomeTargetKind.Slot, slot, null);
        }

        public static OutcomeTarget ForClothingContent(PlayerAssetSlot sourceClothingSlot)
        {
            return new OutcomeTarget(OutcomeTargetKind.ClothingContent, sourceClothingSlot, null);
        }

        public static OutcomeTarget ForItem(ushort itemId)
        {
            return new OutcomeTarget(OutcomeTargetKind.Item, null, itemId);
        }

        public bool Matches(PlayerAsset asset)
        {
            if (asset == null)
            {
                return false;
            }

            switch (_kind)
            {
                case OutcomeTargetKind.Any:
                    return true;
                case OutcomeTargetKind.Slot:
                    return !asset.IsClothingContent && asset.Slot == _slot;
                case OutcomeTargetKind.ClothingContent:
                    return asset.IsClothingContent && asset.SourceClothingSlot == _slot;
                case OutcomeTargetKind.Item:
                    return asset.ItemId == _itemId;
                default:
                    return false;
            }
        }

        private enum OutcomeTargetKind
        {
            Any,
            Slot,
            ClothingContent,
            Item
        }
    }
}
