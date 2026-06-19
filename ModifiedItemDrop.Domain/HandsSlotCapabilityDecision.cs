namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class HandsSlotCapabilityDecision
    {
        public HandsSlotCapabilityDecision(bool applied, string ruleName, int width, int height, string diagnostic)
        {
            Applied = applied;
            RuleName = ruleName ?? string.Empty;
            Width = width;
            Height = height;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool Applied { get; }

        public string RuleName { get; }

        public int Width { get; }

        public int Height { get; }

        public string Diagnostic { get; }
    }
}
