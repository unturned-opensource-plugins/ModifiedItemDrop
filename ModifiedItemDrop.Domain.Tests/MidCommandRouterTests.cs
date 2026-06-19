using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class MidCommandRouterTests
{
    [Theory]
    [InlineData("reload", "/mid config reload")]
    [InlineData("preview", "/mid rules preview [player]")]
    [InlineData("dump", "/mid inventory dump [player]")]
    [InlineData("claim", "/mid claims recover oldest")]
    public void V1FlatCommandsAreRejectedWithMigrationHints(string command, string replacement)
    {
        var result = MidCommandRouter.Parse(new[] { command });

        Assert.False(result.Accepted);
        Assert.Equal(MidCommandRouteKind.RemovedV1Command, result.Kind);
        Assert.Contains(replacement, result.Message);
    }


    [Fact]
    public void V2ConfigReloadCommandIsAccepted()
    {
        var result = MidCommandRouter.Parse(new[] { "config", "reload" });

        Assert.True(result.Accepted);
        Assert.Equal(MidCommandRouteKind.ConfigReload, result.Kind);
    }


    [Theory]
    [InlineData("rules preview", MidCommandRouteKind.RulesPreview)]
    [InlineData("inventory dump", MidCommandRouteKind.InventoryDump)]
    [InlineData("claims recover oldest", MidCommandRouteKind.ClaimsRecover)]
    [InlineData("diagnostics status", MidCommandRouteKind.DiagnosticsStatus)]
    [InlineData("diagnostics export", MidCommandRouteKind.DiagnosticsExport)]
    public void V2CommandGroupsAreAccepted(string commandLine, MidCommandRouteKind expectedKind)
    {
        var result = MidCommandRouter.Parse(commandLine.Split(' '));

        Assert.True(result.Accepted);
        Assert.Equal(expectedKind, result.Kind);
    }
}
