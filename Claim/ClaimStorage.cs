using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Models;
using Newtonsoft.Json;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Claim
{
    public sealed class ClaimStorage
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private List<ClaimRecord> _claims = new List<ClaimRecord>();

        public ClaimStorage(string pluginDirectory)
        {
            if (string.IsNullOrEmpty(pluginDirectory))
            {
                pluginDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            _filePath = Path.Combine(pluginDirectory, "claims.json");
        }

        public void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        _claims = new List<ClaimRecord>();
                        return;
                    }

                    var json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _claims = new List<ClaimRecord>();
                        return;
                    }

                    var loaded = JsonConvert.DeserializeObject<List<ClaimRecord>>(json);
                    _claims = loaded ?? new List<ClaimRecord>();
                    Logger.Log($"[ModifiedItemDrop] Loaded {_claims.Count} claims from storage.");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.LogWarning("[ModifiedItemDrop] Failed to load claims. Starting with empty list.");
                    _claims = new List<ClaimRecord>();
                }
            }
        }

        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(_claims, Formatting.Indented);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.LogWarning("[ModifiedItemDrop] Failed to save claims.");
                }
            }
        }

        public void Add(ClaimRecord claim)
        {
            if (claim == null)
            {
                return;
            }

            lock (_lock)
            {
                _claims.Add(claim);
            }

            Save();
        }

        public void Remove(ClaimRecord claim)
        {
            if (claim == null)
            {
                return;
            }

            lock (_lock)
            {
                _claims.Remove(claim);
            }

            Save();
        }

        public void RemoveRange(IEnumerable<ClaimRecord> claimsToRemove)
        {
            if (claimsToRemove == null)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var claim in claimsToRemove.ToList())
                {
                    _claims.Remove(claim);
                }
            }

            Save();
        }

        public List<ClaimRecord> GetBySteamId(ulong steamId)
        {
            lock (_lock)
            {
                return _claims.Where(c => c.SteamId == steamId).OrderBy(c => c.CreatedAt).ToList();
            }
        }

        public int GetCountBySteamId(ulong steamId)
        {
            lock (_lock)
            {
                return _claims.Count(c => c.SteamId == steamId);
            }
        }

        public List<ClaimRecord> GetExpired()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                return _claims.Where(c => c.ExpiresAt.HasValue && c.ExpiresAt.Value < now).ToList();
            }
        }

        public ClaimRecord GetOldest(ulong steamId)
        {
            lock (_lock)
            {
                return _claims
                    .Where(c => c.SteamId == steamId)
                    .OrderBy(c => c.ExpiresAt ?? DateTime.MaxValue)
                    .ThenBy(c => c.CreatedAt)
                    .FirstOrDefault();
            }
        }

        public List<ClaimRecord> GetAll()
        {
            lock (_lock)
            {
                return new List<ClaimRecord>(_claims);
            }
        }
    }
}
