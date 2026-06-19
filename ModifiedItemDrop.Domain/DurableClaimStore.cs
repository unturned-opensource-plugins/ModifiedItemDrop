using System;
using System.Collections.Generic;
using System.IO;
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

        public DurableClaimLoadResult Load()
        {
            if (!File.Exists(_paths.PrimaryPath))
            {
                return new DurableClaimLoadResult(Array.Empty<DurableClaimRecord>());
            }

            var json = File.ReadAllText(_paths.PrimaryPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new DurableClaimLoadResult(Array.Empty<DurableClaimRecord>());
            }

            var claims = JsonConvert.DeserializeObject<List<DurableClaimRecord>>(json) ?? new List<DurableClaimRecord>();
            return new DurableClaimLoadResult(claims);
        }

        private void WriteAll(IEnumerable<DurableClaimRecord> claims)
        {
            Directory.CreateDirectory(_paths.RootDirectory);
            var tempPath = _paths.PrimaryPath + ".tmp";
            var json = JsonConvert.SerializeObject(claims, Formatting.Indented);
            File.WriteAllText(tempPath, json);

            if (File.Exists(_paths.PrimaryPath))
            {
                File.Copy(_paths.PrimaryPath, _paths.BackupPath, overwrite: true);
                File.Delete(_paths.PrimaryPath);
            }

            File.Move(tempPath, _paths.PrimaryPath);
        }
    }
}
