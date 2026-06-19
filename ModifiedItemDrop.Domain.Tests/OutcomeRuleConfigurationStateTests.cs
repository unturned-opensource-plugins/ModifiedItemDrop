using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class OutcomeRuleConfigurationStateTests
{
    [Fact]
    public void InvalidOutcomeRulesEnterSafeModeWithoutHiddenFallbackRules()
    {
        var state = OutcomeRuleConfigurationState.FromXml("<OutcomeRules><Broken /></OutcomeRules>");

        Assert.False(state.DeathProcessingEnabled);
        Assert.Empty(state.Rules);
        Assert.Contains("Unsupported element", state.Diagnostic);
    }
}
