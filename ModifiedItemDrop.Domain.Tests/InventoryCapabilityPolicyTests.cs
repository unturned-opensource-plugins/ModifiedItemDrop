using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class InventoryCapabilityPolicyTests
{
    [Fact]
    public void LaterPermissionRuleWinsWhenMultipleHandsSlotRulesMatch()
    {
        var rules = new[]
        {
            new HandsSlotCapabilityRule("default", 5, 3),
            new HandsSlotCapabilityRule("vip", 8, 8)
        };

        var decision = InventoryCapabilityPolicy.SelectHandsSlotRule(
            rules,
            permission => permission == "ModifiedItemDrop.Hands.default" || permission == "ModifiedItemDrop.Hands.vip");

        Assert.True(decision.Applied);
        Assert.Equal("vip", decision.RuleName);
        Assert.Equal(8, decision.Width);
        Assert.Equal(8, decision.Height);
    }

    [Fact]
    public void DefaultRuleProvidesExplicitFallbackForHandsSlot()
    {
        var decision = InventoryCapabilityPolicy.SelectHandsSlotRule(
            new[] { new HandsSlotCapabilityRule("default", 5, 3) },
            permission => false);

        Assert.True(decision.Applied);
        Assert.Equal("default", decision.RuleName);
        Assert.Contains("fallback", decision.Diagnostic);
    }

    [Fact]
    public void HandsSlotCanUseTallDimensions()
    {
        var decision = InventoryCapabilityPolicy.SelectHandsSlotRule(
            new[] { new HandsSlotCapabilityRule("default", 12, 24) },
            permission => false);

        Assert.True(decision.Applied);
        Assert.Equal(12, decision.Width);
        Assert.Equal(24, decision.Height);
        Assert.Contains("12x24", decision.Diagnostic);
    }

    [Fact]
    public void NonPositiveDimensionsAreClampedWithDiagnostic()
    {
        var decision = InventoryCapabilityPolicy.SelectHandsSlotRule(
            new[] { new HandsSlotCapabilityRule("default", 0, -1) },
            permission => false);

        Assert.Equal(1, decision.Width);
        Assert.Equal(1, decision.Height);
        Assert.Contains("clamped", decision.Diagnostic);
    }
}
