using System.Xml.Linq;
using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class PackagedConfigurationTests
{
    [Fact]
    public void PackagedConfigurationUsesV2OutcomeRulesAndNoV1FlatCommands()
    {
        var root = FindRepositoryRoot();
        var configPath = Path.Combine(root, "ModifiedItemDrop.configuration.xml");
        var xml = File.ReadAllText(configPath);
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var outcomeRulesXml = document.Root?.Element("OutcomeRulesXml")?.Value;

        Assert.DoesNotContain("<RuleSet>", xml);
        Assert.DoesNotContain("/mid reload", xml);
        Assert.DoesNotContain("/mid preview", xml);
        Assert.False(string.IsNullOrWhiteSpace(outcomeRulesXml));
        var rules = OutcomeRuleXmlParser.Parse(outcomeRulesXml!);
        Assert.NotEmpty(rules);
        Assert.Contains(rules, rule => rule.Target != null && rule.Target.IsCatchAll);
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
