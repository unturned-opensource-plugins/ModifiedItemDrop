using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class RespawnGrantRuleTests
{
    [Fact]
    public void AfterDeathRespawnTriggerProducesConfiguredGrantOutcome()
    {
        var xml = """
            <OutcomeRules>
              <Rule name="Respawn grant medkit" priority="100">
                <Trigger kind="AfterDeathRespawn" />
                <Outcome kind="Grant" itemId="15" amount="1" quality="100" />
              </Rule>
            </OutcomeRules>
            """;

        var rules = OutcomeRuleXmlParser.Parse(xml);
        var grants = new RespawnGrantPlanner().PlanAfterDeathRespawn(rules);

        var grant = Assert.Single(grants);
        Assert.Equal("Respawn grant medkit", grant.Rule.Name);
        Assert.Equal(15, grant.ItemId);
        Assert.Equal(1, grant.Amount);
        Assert.Equal(100, grant.Quality);
    }
}
