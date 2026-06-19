namespace FFEmqo.ModifiedItemDrop.Domain
{
    public interface IDurableClaimCreator
    {
        DurableClaimCreateResult TryCreate(DurableClaimRecord claim);
    }
}
