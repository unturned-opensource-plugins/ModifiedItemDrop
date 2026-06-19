using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class MidCommandPermissionPolicyTests
{
    [Theory]
    [InlineData(MidCommandRouteKind.ConfigReload, "modifieditemdrop.config.reload")]
    [InlineData(MidCommandRouteKind.RulesPreview, "modifieditemdrop.rules.preview")]
    [InlineData(MidCommandRouteKind.RulesExplain, "modifieditemdrop.rules.explain")]
    [InlineData(MidCommandRouteKind.InventoryDump, "modifieditemdrop.inventory.dump")]
    [InlineData(MidCommandRouteKind.ClaimsList, "modifieditemdrop.claims.list")]
    [InlineData(MidCommandRouteKind.ClaimsRecover, "modifieditemdrop.claims.recover")]
    [InlineData(MidCommandRouteKind.DiagnosticsStatus, "modifieditemdrop.diagnostics.status")]
    [InlineData(MidCommandRouteKind.DiagnosticsExport, "modifieditemdrop.diagnostics.export")]
    public void V2RoutesMapToGroupedPermissions(MidCommandRouteKind kind, string expectedPermission)
    {
        Assert.Equal(expectedPermission, MidCommandPermissionPolicy.RequiredPermissionFor(kind));
    }
}
