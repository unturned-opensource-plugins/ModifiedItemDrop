namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimRemoveResult
    {
        private DurableClaimRemoveResult(bool removed, string? errorMessage)
        {
            Removed = removed;
            ErrorMessage = errorMessage;
        }

        public bool Removed { get; }

        public string? ErrorMessage { get; }

        public static DurableClaimRemoveResult Success()
        {
            return new DurableClaimRemoveResult(true, null);
        }

        public static DurableClaimRemoveResult Failure(string errorMessage)
        {
            return new DurableClaimRemoveResult(false, errorMessage);
        }
    }
}
