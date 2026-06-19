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

    [Fact]
    public void NestedXmlItemTargetCanDeleteMatchingItem()
    {
        var xml = """
            <OutcomeRules>
              <Rule name="Delete banned item" priority="2000">
                <Target kind="Item" itemId="95" />
                <Outcome kind="Delete" />
              </Rule>
              <Rule name="Default keep" priority="0">
                <Target kind="Any" />
                <Outcome kind="Keep" />
              </Rule>
            </OutcomeRules>
            """;
        var asset = new PlayerAsset("banned", PlayerAssetSlot.SecondaryWeapon, itemId: 95);

        var rules = OutcomeRuleXmlParser.Parse(xml);
        var outcome = new DeathOutcomePlanner().Plan(asset, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Delete, outcome.Kind);
        Assert.Equal("Delete banned item", outcome.Rule.Name);
    }


    [Fact]
    public void NestedXmlClothingContentTargetCanKeepContentFromSourceSlot()
    {
        var xml = """
            <OutcomeRules>
              <Rule name="Keep backpack content" priority="100">
                <Target kind="ClothingContent" slot="Backpack" />
                <Outcome kind="Keep" />
              </Rule>
              <Rule name="Default drop" priority="0">
                <Target kind="Any" />
                <Outcome kind="Drop" chance="1.0" />
              </Rule>
            </OutcomeRules>
            """;
        var content = PlayerAsset.ClothingContent(
            "content-1",
            sourceClothingSlot: PlayerAssetSlot.Backpack,
            parentAssetId: "backpack",
            itemId: 15);

        var rules = OutcomeRuleXmlParser.Parse(xml);
        var outcome = new DeathOutcomePlanner().Plan(content, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Keep, outcome.Kind);
        Assert.Equal("Keep backpack content", outcome.Rule.Name);
    }

}
