using System.Xml.Linq;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class ReleaseMetadataIntegrityTests
{
    [Fact]
    public void ReleaseMetadataUsesCanonicalMitLicenseAndV2Version()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var license = File.ReadAllText(Path.Combine(root, "LICENSE"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));
        var project = XDocument.Load(Path.Combine(root, "ModifiedItemDrop.csproj"));
        var version = project.Root?.Element("PropertyGroup")?.Element("Version")?.Value;
        var packageLicense = project.Root?.Element("PropertyGroup")?.Element("PackageLicenseExpression")?.Value;

        Assert.Contains("MIT License", license);
        Assert.DoesNotContain("GNU GENERAL PUBLIC LICENSE", license);
        Assert.Contains("license-MIT", readme);
        Assert.Contains("MIT License", readme);
        Assert.Equal("2.0.0", version);
        Assert.Equal("MIT", packageLicense);
        Assert.Contains("v2.0.0", workflow);
        Assert.DoesNotContain("v1.0.0", workflow);
        Assert.DoesNotContain("/mid reload", workflow);
        Assert.Contains("docs/release/v$version.md", workflow);
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
