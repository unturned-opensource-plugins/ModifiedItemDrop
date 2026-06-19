using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class OutcomeRuleXmlParserTests
{
    [Fact]
    public void NestedXmlOutcomeRulesCanPlanPrimaryWeaponDropWithExplicitFallback()
    {
        var xml = """
            <OutcomeRules>
              <Rule name="Primary weapon drop" priority="100">
                <Target kind="Slot" slot="PrimaryWeapon" />
                <Outcome kind="Drop" chance="1.0" />
              </Rule>
              <Rule name="Default keep" priority="0">
                <Target kind="Any" />
                <Outcome kind="Keep" />
              </Rule>
            </OutcomeRules>
            """;
        var asset = new PlayerAsset("primary", PlayerAssetSlot.PrimaryWeapon, itemId: 363);

        var rules = OutcomeRuleXmlParser.Parse(xml);
        var outcome = new DeathOutcomePlanner().Plan(asset, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Drop, outcome.Kind);
        Assert.Equal("Primary weapon drop", outcome.Rule.Name);
    }
}
