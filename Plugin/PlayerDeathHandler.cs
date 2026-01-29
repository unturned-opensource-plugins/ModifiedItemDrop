using System;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Drop;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    public sealed class PlayerDeathHandler
    {
        private readonly DropService _dropService;
        private readonly ClaimService _claimService;
        private bool _isEnabled;

        public PlayerDeathHandler(DropService dropService, ClaimService claimService)
        {
            _dropService = dropService ?? throw new ArgumentNullException(nameof(dropService));
            _claimService = claimService;
        }

        public void Enable()
        {
            if (_isEnabled)
            {
                return;
            }

            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            _isEnabled = true;
        }

        public void Disable()
        {
            if (!_isEnabled)
            {
                return;
            }

            UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevive;
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

            _isEnabled = false;
        }

        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (player == null)
            {
                return;
            }

            _dropService.HandlePlayerDying(player);
        }

        private void OnPlayerRevive(UnturnedPlayer player, UnityEngine.Vector3 position, byte angle)
        {
            if (player == null)
            {
                return;
            }

            // Apply hands slot size based on player permission before restoring items
            _dropService.ApplyHandsSlotSize(player);

            _dropService.HandlePlayerRevived(player);
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                // Apply hands slot size based on player permission when they join
                _dropService.ApplyHandsSlotSize(player);

                _claimService?.CleanupExpired();

                var claimSettings = _claimService?.GetClaimSettings();
                if (claimSettings?.AutoClaimOnJoin == true)
                {
                    // Auto-claim all pending items
                    _dropService.ClaimAllPending(player);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            _dropService.HandlePlayerDisconnected(player);
        }
    }
}

