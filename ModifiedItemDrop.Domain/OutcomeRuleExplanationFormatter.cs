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

        public static string FormatExplain(PlayerAssetOutcome outcome)
        {
            if (outcome == null)
            {
                return "Outcome Rules explain: (no outcome)";
            }

            var lines = new System.Collections.Generic.List<string>
            {
                string.Format(CultureInfo.InvariantCulture,
                    "target={0} item={1}",
                    outcome.Asset.Slot,
                    outcome.Asset.ItemId)
            };

            foreach (var evaluation in outcome.RuleEvaluations)
            {
                var chancePercent = evaluation.Rule.Chance * 100.0;
                var status = evaluation.OutcomeOccurred ? "matched" : "missed";
                if (evaluation.SampledRoll.HasValue)
                {
                    lines.Add(string.Format(CultureInfo.InvariantCulture,
                        "rule={0} chance={1:0.0}% roll={2:0.0}% {3}",
                        evaluation.Rule.Name,
                        chancePercent,
                        evaluation.SampledRoll.Value * 100.0,
                        status));
                }
                else
                {
                    lines.Add(string.Format(CultureInfo.InvariantCulture,
                        "rule={0} chance={1:0.0}% {2}",
                        evaluation.Rule.Name,
                        chancePercent,
                        status));
                }
            }

            lines.Add("decision=" + outcome.Kind + " rule=" + outcome.Rule.Name);
            if (outcome.Kind == PlayerAssetOutcomeKind.Keep)
            {
                lines.Add("configured Keep; Durable Claim is only a later fallback");
            }

            return string.Join("; ", lines);
        }

    }
}
