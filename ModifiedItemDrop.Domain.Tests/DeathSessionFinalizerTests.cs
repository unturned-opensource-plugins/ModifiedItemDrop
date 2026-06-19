using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DeathSessionFinalizerTests
{
    [Fact]
    public void DisconnectWithFailedDurableClaimCreationDropsUnresolvedKeptAssetsBeforeSessionEnds()
    {
        var asset = new PlayerAsset("primary", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var keptOutcome = new PlayerAssetOutcome(
            asset,
            PlayerAssetOutcomeKind.Keep,
            OutcomeRule.Keep("keep primary", 100, OutcomeTarget.Any(), chance: 1.0));
        var session = new DeathSession("session-1", steamId: 76561198000000001UL, outcomes: new[] { keptOutcome });
        var finalizer = new DeathSessionFinalizer(new FailingDurableClaimCreator("disk full"));

        var result = finalizer.FinalizeDisconnect(session);

        Assert.True(result.SessionEnded);
        Assert.False(result.DurableClaimCreated);
        var decision = Assert.Single(result.FallbackDecisions);
        Assert.Equal("primary", decision.Asset.Id);
        Assert.Equal(DurableClaimFallbackKind.DropFallback, decision.Kind);
    }

    private sealed class FailingDurableClaimCreator : IDurableClaimCreator
    {
        private readonly string _errorMessage;

        public FailingDurableClaimCreator(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public DurableClaimCreateResult TryCreate(DurableClaimRecord claim)
        {
            return DurableClaimCreateResult.Failure(_errorMessage);
        }
    }
}
