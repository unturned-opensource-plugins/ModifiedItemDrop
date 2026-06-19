using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimStore
    {
        private readonly V2ClaimStoragePaths _paths;

        public DurableClaimStore(V2ClaimStoragePaths paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        public DurableClaimCreateResult TryCreate(DurableClaimRecord claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            try
            {
                var claims = Load().Claims;
                var next = new List<DurableClaimRecord>(claims) { claim };
                WriteAll(next);
                return DurableClaimCreateResult.Success();
            }
            catch (Exception ex)
            {
                return DurableClaimCreateResult.Failure(ex.Message);
            }
        }

        public DurableClaimRemoveResult TryRemove(string claimId)
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                throw new ArgumentException("Claim id must be provided.", nameof(claimId));
            }

            try
            {
                var claims = new List<DurableClaimRecord>(Load().Claims);
                var removed = claims.RemoveAll(claim => claim.Id == claimId);
                if (removed == 0)
                {
                    return DurableClaimRemoveResult.Failure("Claim '" + claimId + "' was not found.");
                }

                WriteAll(claims);
                return DurableClaimRemoveResult.Success();
            }
            catch (Exception ex)
            {
                return DurableClaimRemoveResult.Failure(ex.Message);
            }
        }

        public DurableClaimRemoveResult TryPruneAssets(string claimId, IEnumerable<string> restoredAssetIds)
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                throw new ArgumentException("Claim id must be provided.", nameof(claimId));
            }

            if (restoredAssetIds == null)
            {
                throw new ArgumentNullException(nameof(restoredAssetIds));
            }

            try
            {
                var restored = new HashSet<string>(restoredAssetIds.Where(id => !string.IsNullOrWhiteSpace(id)));
                if (restored.Count == 0)
                {
                    return DurableClaimRemoveResult.Failure("No restored asset ids were provided.");
                }

                var claims = new List<DurableClaimRecord>(Load().Claims);
                var claimIndex = claims.FindIndex(claim => claim.Id == claimId);
                if (claimIndex < 0)
                {
                    return DurableClaimRemoveResult.Failure("Claim '" + claimId + "' was not found.");
                }

                var claim = claims[claimIndex];
                var remainingAssets = claim.Assets.Where(asset => !restored.Contains(asset.AssetId)).ToList();
                if (remainingAssets.Count == claim.Assets.Count)
                {
                    return DurableClaimRemoveResult.Failure("No matching assets were found for claim '" + claimId + "'.");
                }

                if (remainingAssets.Count == 0)
                {
                    claims.RemoveAt(claimIndex);
                }
                else
                {
                    claims[claimIndex] = new DurableClaimRecord(claim.Id, claim.SteamId, remainingAssets);
                }

                WriteAll(claims);
                return DurableClaimRemoveResult.Success();
            }
            catch (Exception ex)
            {
                return DurableClaimRemoveResult.Failure(ex.Message);
            }
        }

        public DurableClaimLoadResult Load()
        {
            if (!File.Exists(_paths.PrimaryPath))
            {
                return new DurableClaimLoadResult(Array.Empty<DurableClaimRecord>());
            }

            try
            {
                return new DurableClaimLoadResult(ReadClaims(_paths.PrimaryPath));
            }
            catch (JsonException)
            {
                var corruptPath = PreserveCorruptPrimary();
                var warning = "Durable Claim primary storage is corrupt: " + _paths.PrimaryPath + "; preserved copy: " + corruptPath + ".";
                if (File.Exists(_paths.BackupPath))
                {
                    return new DurableClaimLoadResult(
                        ReadClaims(_paths.BackupPath),
                        recoveredFromBackup: true,
                        preservedCorruptPath: corruptPath,
                        warnings: new[] { warning + " Recovered from backup: " + _paths.BackupPath + "." });
                }

                return new DurableClaimLoadResult(
                    Array.Empty<DurableClaimRecord>(),
                    recoveredFromBackup: false,
                    preservedCorruptPath: corruptPath,
                    warnings: new[] { warning + " No backup was available." });
            }
        }

        private static List<DurableClaimRecord> ReadClaims(string path)
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<DurableClaimRecord>();
            }

            return JsonConvert.DeserializeObject<List<DurableClaimRecord>>(json) ?? new List<DurableClaimRecord>();
        }

        private string PreserveCorruptPrimary()
        {
            Directory.CreateDirectory(_paths.CorruptDirectory);
            var corruptPath = Path.Combine(
                _paths.CorruptDirectory,
                "claims." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json");
            File.Copy(_paths.PrimaryPath, corruptPath, overwrite: false);
            return corruptPath;
        }

        private void WriteAll(IEnumerable<DurableClaimRecord> claims)
        {
            Directory.CreateDirectory(_paths.RootDirectory);
            var tempPath = _paths.PrimaryPath + ".tmp";
            var json = JsonConvert.SerializeObject(claims, Formatting.Indented);
            File.WriteAllText(tempPath, json);

            if (File.Exists(_paths.PrimaryPath))
            {
                File.Replace(tempPath, _paths.PrimaryPath, _paths.BackupPath, ignoreMetadataErrors: true);
                return;
            }

            File.Move(tempPath, _paths.PrimaryPath);
        }
    }
}
