using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DurableClaimStoreTests
{
    [Fact]
    public void CreateClaimWritesV2StorageWithoutTouchingV1ClaimsJson()
    {
        var pluginDirectory = CreateTempDirectory();
        try
        {
            var v1ClaimsPath = Path.Combine(pluginDirectory, "claims.json");
            File.WriteAllText(v1ClaimsPath, "v1 claim data");
            var paths = V2ClaimStoragePaths.ForPluginDirectory(pluginDirectory);
            var store = new DurableClaimStore(paths);
            var claim = new DurableClaimRecord(
                id: "claim-1",
                steamId: 76561198000000001UL,
                assets: new[]
                {
                    new DurableClaimAsset("asset-1", itemId: 363, amount: 1, quality: 100, state: Array.Empty<byte>())
                });

            var result = store.TryCreate(claim);
            var loaded = store.Load();

            Assert.True(result.Created);
            Assert.True(File.Exists(paths.PrimaryPath));
            Assert.Equal("v1 claim data", File.ReadAllText(v1ClaimsPath));
            var loadedClaim = Assert.Single(loaded.Claims);
            Assert.Equal("claim-1", loadedClaim.Id);
            Assert.Equal(363, Assert.Single(loadedClaim.Assets).ItemId);
        }
        finally
        {
            Directory.Delete(pluginDirectory, recursive: true);
        }
    }


    [Fact]
    public void RemoveClaimDurablyUpdatesV2Storage()
    {
        var pluginDirectory = CreateTempDirectory();
        try
        {
            var store = new DurableClaimStore(V2ClaimStoragePaths.ForPluginDirectory(pluginDirectory));
            store.TryCreate(CreateClaim("claim-1"));
            store.TryCreate(CreateClaim("claim-2"));

            var result = store.TryRemove("claim-1");
            var reloaded = new DurableClaimStore(V2ClaimStoragePaths.ForPluginDirectory(pluginDirectory)).Load();

            Assert.True(result.Removed);
            var remaining = Assert.Single(reloaded.Claims);
            Assert.Equal("claim-2", remaining.Id);
        }
        finally
        {
            Directory.Delete(pluginDirectory, recursive: true);
        }
    }


    private static DurableClaimRecord CreateClaim(string id)
    {
        return new DurableClaimRecord(
            id,
            steamId: 76561198000000001UL,
            assets: new[]
            {
                new DurableClaimAsset(id + "-asset", itemId: 363, amount: 1, quality: 100, state: Array.Empty<byte>())
            });
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mid-v2-claims-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
