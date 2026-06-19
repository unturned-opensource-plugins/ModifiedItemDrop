using System;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathSessionFinalizer
    {
        private readonly IDurableClaimCreator _claimCreator;
        private readonly DurableClaimFallbackPlanner _fallbackPlanner = new DurableClaimFallbackPlanner();

        public DeathSessionFinalizer(IDurableClaimCreator claimCreator)
        {
            _claimCreator = claimCreator ?? throw new ArgumentNullException(nameof(claimCreator));
        }

        public DeathSessionFinalizationResult FinalizeDisconnect(DeathSession session)
        {
            return FinalizeWithoutImmediateRestore(session);
        }

        public DeathSessionFinalizationResult FinalizePluginUnload(DeathSession session)
        {
            return FinalizeWithoutImmediateRestore(session);
        }

        public DeathSessionFinalizationResult FinalizeRespawnRestoreFailure(DeathSession session)
        {
            return FinalizeWithoutImmediateRestore(session);
        }

        private DeathSessionFinalizationResult FinalizeWithoutImmediateRestore(DeathSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var keptOutcomes = session.KeptOutcomes;
            if (keptOutcomes.Count == 0)
            {
                return new DeathSessionFinalizationResult(sessionEnded: true, durableClaimCreated: false, Array.Empty<DurableClaimFallbackDecision>());
            }

            var claim = ToDurableClaim(session, keptOutcomes);
            var createResult = _claimCreator.TryCreate(claim);
            if (createResult.Created)
            {
                return new DeathSessionFinalizationResult(sessionEnded: true, durableClaimCreated: true, Array.Empty<DurableClaimFallbackDecision>());
            }

            var fallbackDecisions = _fallbackPlanner.PlanAfterCreateFailure(
                keptOutcomes,
                createResult,
                immediateRestoreAvailable: false);
            return new DeathSessionFinalizationResult(sessionEnded: true, durableClaimCreated: false, fallbackDecisions);
        }

        private static DurableClaimRecord ToDurableClaim(DeathSession session, System.Collections.Generic.IEnumerable<PlayerAssetOutcome> keptOutcomes)
        {
            return new DurableClaimRecord(
                "death-session-" + session.Id,
                session.SteamId,
                keptOutcomes.Select(outcome => new DurableClaimAsset(
                    outcome.Asset.Id,
                    outcome.Asset.ItemId,
                    amount: 1,
                    quality: 100,
                    state: Array.Empty<byte>())));
        }
    }
}
