namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class MidRulesExplainTarget
    {
        public MidRulesExplainTarget(bool accepted, PlayerAsset? asset, string message)
        {
            Accepted = accepted;
            Asset = asset;
            Message = message ?? string.Empty;
        }

        public bool Accepted { get; }

        public PlayerAsset? Asset { get; }

        public string Message { get; }
    }
}
