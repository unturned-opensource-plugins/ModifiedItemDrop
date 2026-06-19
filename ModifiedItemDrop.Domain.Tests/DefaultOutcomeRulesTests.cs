using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DefaultOutcomeRulesTests
{
    [Fact]
    public void DefaultOutcomeRulesXmlProvidesExplicitCatchAllKeepRule()
    {
        var rules = OutcomeRuleXmlParser.Parse(DefaultOutcomeRules.Xml);
        var asset = new PlayerAsset("inventory:0:0", PlayerAssetSlot.PrimaryWeapon, itemId: 363);

        var outcome = new DeathOutcomePlanner().Plan(asset, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Keep, outcome.Kind);
        Assert.Equal("Default keep", outcome.Rule.Name);
    }
}
