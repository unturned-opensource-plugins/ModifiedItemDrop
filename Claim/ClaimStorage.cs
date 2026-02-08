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
        private bool _isDirty = false;
        private DateTime _lastSaveTime = DateTime.UtcNow;
        private const int SaveIntervalMs = 5000; // 批量保存间隔

        // SteamId 索引，避免重复遍历
        private Dictionary<ulong, List<ClaimRecord>> _steamIdIndex = new Dictionary<ulong, List<ClaimRecord>>();

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
                        RebuildIndex();
                        return;
                    }

                    var json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _claims = new List<ClaimRecord>();
                        RebuildIndex();
                        return;
                    }

                    var loaded = JsonConvert.DeserializeObject<List<ClaimRecord>>(json);
                    _claims = loaded ?? new List<ClaimRecord>();
                    RebuildIndex();
                    Logger.Log($"[ModifiedItemDrop] Loaded {_claims.Count} claims from storage.");
                    _isDirty = false;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.LogWarning("[ModifiedItemDrop] Failed to load claims. Starting with empty list.");
                    _claims = new List<ClaimRecord>();
                    RebuildIndex();
                }
            }
        }

        public void Save()
        {
            SaveInternal(false);
        }

        public void ForceSave()
        {
            SaveInternal(true);
        }

        private void SaveInternal(bool force)
        {
            lock (_lock)
            {
                if (!_isDirty)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                if (!force && (now - _lastSaveTime).TotalMilliseconds < SaveIntervalMs)
                {
                    return;
                }

                try
                {
                    var json = JsonConvert.SerializeObject(_claims, Formatting.Indented);
                    File.WriteAllText(_filePath, json);
                    _isDirty = false;
                    _lastSaveTime = now;
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
                AddToIndex(claim);
                _isDirty = true;
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
                RemoveFromIndex(claim);
                _isDirty = true;
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
                var toRemove = new HashSet<ClaimRecord>(claimsToRemove);
                if (toRemove.Count == 0)
                {
                    return;
                }

                _claims.RemoveAll(c => toRemove.Contains(c));
                foreach (var claim in toRemove)
                {
                    RemoveFromIndex(claim);
                }
                _isDirty = true;
            }

            Save();
        }

        public List<ClaimRecord> GetBySteamId(ulong steamId)
        {
            lock (_lock)
            {
                if (_steamIdIndex.TryGetValue(steamId, out var list))
                {
                    return list.OrderBy(c => c.CreatedAt).ToList();
                }
                return new List<ClaimRecord>();
            }
        }

        public int GetCountBySteamId(ulong steamId)
        {
            lock (_lock)
            {
                if (_steamIdIndex.TryGetValue(steamId, out var list))
                {
                    return list.Count;
                }
                return 0;
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
                if (!_steamIdIndex.TryGetValue(steamId, out var list) || list.Count == 0)
                {
                    return null;
                }

                return list
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

        private void RebuildIndex()
        {
            _steamIdIndex.Clear();
            foreach (var claim in _claims)
            {
                AddToIndex(claim);
            }
        }

        private void AddToIndex(ClaimRecord claim)
        {
            if (!_steamIdIndex.TryGetValue(claim.SteamId, out var list))
            {
                list = new List<ClaimRecord>();
                _steamIdIndex[claim.SteamId] = list;
            }
            list.Add(claim);
        }

        private void RemoveFromIndex(ClaimRecord claim)
        {
            if (_steamIdIndex.TryGetValue(claim.SteamId, out var list))
            {
                list.Remove(claim);
                if (list.Count == 0)
                {
                    _steamIdIndex.Remove(claim.SteamId);
                }
            }
        }
    }
}
