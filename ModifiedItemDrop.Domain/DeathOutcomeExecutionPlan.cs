using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathOutcomeExecutionPlan
    {
        private readonly Dictionary<string, DeathOutcomeExecutionAction> _actionsByAssetId;

        public DeathOutcomeExecutionPlan(IEnumerable<DeathOutcomeExecutionAction> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            Actions = actions.ToList().AsReadOnly();
            _actionsByAssetId = Actions.ToDictionary(action => action.Asset.Id, action => action);
        }

        public IReadOnlyList<DeathOutcomeExecutionAction> Actions { get; }

        public DeathOutcomeExecutionAction ForAsset(string assetId)
        {
            if (!_actionsByAssetId.TryGetValue(assetId, out var action))
            {
                throw new KeyNotFoundException("No execution action exists for asset id '" + assetId + "'.");
            }

            return action;
        }
    }
}
