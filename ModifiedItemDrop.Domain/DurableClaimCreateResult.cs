namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimCreateResult
    {
        private DurableClaimCreateResult(bool created, string? errorMessage)
        {
            Created = created;
            ErrorMessage = errorMessage;
        }

        public bool Created { get; }

        public string? ErrorMessage { get; }

        public static DurableClaimCreateResult Success()
        {
            return new DurableClaimCreateResult(true, null);
        }

        public static DurableClaimCreateResult Failure(string errorMessage)
        {
            return new DurableClaimCreateResult(false, errorMessage);
        }
    }
}
