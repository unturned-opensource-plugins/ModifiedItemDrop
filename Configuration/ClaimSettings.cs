namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public enum OverLimitBehavior
    {
        DeleteOldest,
        DropToGround,
        IgnoreLimit
    }

    public enum ExpirationBehavior
    {
        Delete,
        DropAtDeathPosition
    }

    public sealed class ClaimSettings
    {
        public bool EnableClaim { get; set; } = true;

        /// <summary>
        /// Maximum number of claims per player. 0 = unlimited.
        /// </summary>
        public int MaxClaimsPerPlayer { get; set; } = 10;

        /// <summary>
        /// Expiration time in minutes. 0 = never expires.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 1440;

        public bool AutoClaimOnJoin { get; set; } = true;

        public OverLimitBehavior OverLimitBehavior { get; set; } = OverLimitBehavior.DeleteOldest;

        public ExpirationBehavior ExpirationBehavior { get; set; } = ExpirationBehavior.Delete;

        public static ClaimSettings CreateDefault()
        {
            return new ClaimSettings();
        }
    }
}
