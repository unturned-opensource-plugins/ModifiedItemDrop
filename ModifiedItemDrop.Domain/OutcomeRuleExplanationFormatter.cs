using System.Globalization;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class OutcomeRuleExplanationFormatter
    {
        public static string FormatPreviewLine(PlayerAssetOutcome outcome)
        {
            if (outcome == null)
            {
                return "Outcome Rules: (no outcome)";
            }

            var chancePercent = outcome.Rule.Chance * 100.0;
            return string.Format(CultureInfo.InvariantCulture,
                " - [{0}] item={1} outcome={2} rule={3} chance={4:0.0}%",
                outcome.Asset.Slot,
                outcome.Asset.ItemId,
                outcome.Kind,
                outcome.Rule.Name,
                chancePercent);
        }
    }
}
