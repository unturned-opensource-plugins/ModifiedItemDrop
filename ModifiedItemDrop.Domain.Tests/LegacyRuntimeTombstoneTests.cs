using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class LegacyRuntimeTombstoneTests
{
    [Fact]
    public void RuntimeNoLongerReferencesLegacyChanceRuleSetOrRespawnSettings()
    {
        var root = FindRepositoryRoot();
        var runtimeFiles = Directory.GetFiles(Path.Combine(root, "Drop"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(root, "Configuration"), "*.cs"))
            .Concat(Directory.GetFiles(Path.Combine(root, "Plugin"), "*.cs"));
        var text = string.Join("\n", runtimeFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("ChanceResolver", text);
        Assert.DoesNotContain("DropRuleSet", text);
        Assert.DoesNotContain("DeathSettings", text);
        Assert.DoesNotContain("GiveRespawnItems", text);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ModifiedItemDrop.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing ModifiedItemDrop.csproj.");
    }
}
