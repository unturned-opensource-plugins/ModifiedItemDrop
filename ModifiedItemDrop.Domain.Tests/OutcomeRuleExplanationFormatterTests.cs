using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class OutcomeRuleExplanationFormatterTests
{
    [Fact]
    public void OutcomeRulePreviewLineIncludesOutcomeRuleDecisionInsteadOfLegacyChanceSource()
    {
        var rule = OutcomeRule.Drop(
            "primary weapons drop",
            priority: 100,
            OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon),
            chance: 1.0);
        var asset = new PlayerAsset("inventory:0:0", PlayerAssetSlot.PrimaryWeapon, 363, amount: 1, quality: 80, state: null);
        var outcome = new DeathOutcomePlanner().Plan(asset, new[]
        {
            rule,
            OutcomeRule.Keep("fallback keep", 0, OutcomeTarget.Any(), chance: 1.0)
        });

        var line = OutcomeRuleExplanationFormatter.FormatPreviewLine(outcome);

        Assert.Contains("PrimaryWeapon", line);
        Assert.Contains("item=363", line);
        Assert.Contains("outcome=Drop", line);
        Assert.Contains("rule=primary weapons drop", line);
        Assert.Contains("chance=100.0%", line);
    }
}
