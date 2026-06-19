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

    [Fact]
    public void RulesExplainIncludesMissedProbabilisticRuleAndFinalFallbackDecision()
    {
        var asset = new PlayerAsset("explain-target", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var outcome = new DeathOutcomePlanner(new FixedRollProvider(0.75)).Plan(asset, new[]
        {
            OutcomeRule.Drop("primary weapon sometimes drops", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 0.50),
            OutcomeRule.Keep("catch-all keep", 0, OutcomeTarget.Any(), chance: 1.0)
        });

        var explanation = OutcomeRuleExplanationFormatter.FormatExplain(outcome);

        Assert.Contains("target=PrimaryWeapon item=363", explanation);
        Assert.Contains("primary weapon sometimes drops", explanation);
        Assert.Contains("chance=50.0%", explanation);
        Assert.Contains("roll=75.0%", explanation);
        Assert.Contains("missed", explanation);
        Assert.Contains("decision=Keep", explanation);
        Assert.Contains("rule=catch-all keep", explanation);
        Assert.Contains("configured Keep; Durable Claim is only a later fallback", explanation);
    }

}
