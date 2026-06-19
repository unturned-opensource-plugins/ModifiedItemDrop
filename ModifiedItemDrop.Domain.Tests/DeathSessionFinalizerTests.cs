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


    [Fact]
    public void PluginUnloadWithSuccessfulDurableClaimCreationEndsSessionWithoutFallback()
    {
        var asset = new PlayerAsset("primary", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var keptOutcome = new PlayerAssetOutcome(
            asset,
            PlayerAssetOutcomeKind.Keep,
            OutcomeRule.Keep("keep primary", 100, OutcomeTarget.Any(), chance: 1.0));
        var session = new DeathSession("session-1", steamId: 76561198000000001UL, outcomes: new[] { keptOutcome });
        var claimCreator = new RecordingDurableClaimCreator(DurableClaimCreateResult.Success());
        var finalizer = new DeathSessionFinalizer(claimCreator);

        var result = finalizer.FinalizePluginUnload(session);

        Assert.True(result.SessionEnded);
        Assert.True(result.DurableClaimCreated);
        Assert.Empty(result.FallbackDecisions);
        var claim = Assert.Single(claimCreator.CreatedClaims);
        Assert.Equal("death-session-session-1", claim.Id);
        Assert.Equal("primary", Assert.Single(claim.Assets).AssetId);
    }


    [Fact]
    public void RespawnRestoreOverflowWithFailedDurableClaimDropsUnresolvedAssets()
    {
        var asset = new PlayerAsset("hands", PlayerAssetSlot.Hands, itemId: 116);
        var keptOutcome = new PlayerAssetOutcome(
            asset,
            PlayerAssetOutcomeKind.Keep,
            OutcomeRule.Keep("keep hands", 100, OutcomeTarget.Any(), chance: 1.0));
        var session = new DeathSession("session-2", steamId: 76561198000000001UL, outcomes: new[] { keptOutcome });
        var finalizer = new DeathSessionFinalizer(new FailingDurableClaimCreator("disk full"));

        var result = finalizer.FinalizeRespawnRestoreFailure(session);

        Assert.True(result.SessionEnded);
        Assert.False(result.DurableClaimCreated);
        var decision = Assert.Single(result.FallbackDecisions);
        Assert.Equal("hands", decision.Asset.Id);
        Assert.Equal(DurableClaimFallbackKind.DropFallback, decision.Kind);
        Assert.Contains("disk full", decision.Reason);
    }



    [Fact]
    public void EmergencyFailureWithFailedDurableClaimDropsUnresolvedKeptAssetsBeforeSessionEnds()
    {
        var content = PlayerAsset.ClothingContent(
            "content-1",
            sourceClothingSlot: PlayerAssetSlot.Backpack,
            parentAssetId: "backpack",
            itemId: 15);
        var keptOutcome = new PlayerAssetOutcome(
            content,
            PlayerAssetOutcomeKind.Keep,
            OutcomeRule.Keep("keep content", 100, OutcomeTarget.Any(), chance: 1.0));
        var session = new DeathSession("session-3", steamId: 76561198000000001UL, outcomes: new[] { keptOutcome });
        var finalizer = new DeathSessionFinalizer(new FailingDurableClaimCreator("disk full"));

        var result = finalizer.FinalizeEmergencyFailure(session, immediateRestoreAvailable: false);

        Assert.True(result.SessionEnded);
        var decision = Assert.Single(result.FallbackDecisions);
        Assert.Equal("content-1", decision.Asset.Id);
        Assert.Equal(DurableClaimFallbackKind.DropFallback, decision.Kind);
    }


    private sealed class RecordingDurableClaimCreator : IDurableClaimCreator
    {
        private readonly DurableClaimCreateResult _result;
        private readonly List<DurableClaimRecord> _createdClaims = new List<DurableClaimRecord>();

        public RecordingDurableClaimCreator(DurableClaimCreateResult result)
        {
            _result = result;
        }

        public IReadOnlyList<DurableClaimRecord> CreatedClaims => _createdClaims;

        public DurableClaimCreateResult TryCreate(DurableClaimRecord claim)
        {
            _createdClaims.Add(claim);
            return _result;
        }
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
