using AwesomeAssertions;
using Lingo.Core.Hierarchy;
using Lingo.Core.Models;
using Lingo.Core.Test.Infrastructure;
using System.Linq;

namespace Lingo.Core.Test.Hierarchy;

[TestFixture]
public class KeyHierarchyBuilderTests
{
    [Test]
    public void Build_CommonPrefixKeys_CreatesHierarchicalTree()
    {
        // Arrange: keys with common prefix as in user issue ("A.B.Key" and "A.C.Key2")
        var keys = new[] { "A.B.Key", "A.C.Key2" };
        var builder = new KeyHierarchyBuilder();

        // Act
        var rootNodes = builder.Build(keys);

        // Assert
        rootNodes.Should().HaveCount(1);
        var nodeA = rootNodes[0];
        nodeA.Name.Should().Be("A");
        nodeA.FullPath.Should().Be("A");
        nodeA.IsLeaf.Should().BeFalse();
        nodeA.Children.Should().HaveCount(2);

        var nodeB = nodeA.Children.FirstOrDefault(c => c.Name == "B");
        nodeB.Should().NotBeNull();
        nodeB!.FullPath.Should().Be("A.B");
        nodeB.IsLeaf.Should().BeFalse();
        nodeB.Children.Should().HaveCount(1);

        var nodeKey = nodeB.Children[0];
        nodeKey.Name.Should().Be("Key");
        nodeKey.FullPath.Should().Be("A.B.Key");
        nodeKey.IsLeaf.Should().BeTrue();

        var nodeC = nodeA.Children.FirstOrDefault(c => c.Name == "C");
        nodeC.Should().NotBeNull();
        nodeC!.FullPath.Should().Be("A.C");
        nodeC.IsLeaf.Should().BeFalse();
        nodeC.Children.Should().HaveCount(1);

        var nodeKey2 = nodeC.Children[0];
        nodeKey2.Name.Should().Be("Key2");
        nodeKey2.FullPath.Should().Be("A.C.Key2");
        nodeKey2.IsLeaf.Should().BeTrue();
    }

    [Test]
    public void Build_SingleSegmentKeys_TreatedAsRootLeaves()
    {
        // Arrange
        var keys = new[] { "SimpleKey1", "SimpleKey2" };
        var builder = new KeyHierarchyBuilder();

        // Act
        var rootNodes = builder.Build(keys);

        // Assert
        rootNodes.Should().HaveCount(2);
        rootNodes[0].Name.Should().Be("SimpleKey1");
        rootNodes[0].FullPath.Should().Be("SimpleKey1");
        rootNodes[0].IsLeaf.Should().BeTrue();
        rootNodes[0].Children.Should().BeEmpty();

        rootNodes[1].Name.Should().Be("SimpleKey2");
        rootNodes[1].FullPath.Should().Be("SimpleKey2");
        rootNodes[1].IsLeaf.Should().BeTrue();
    }

    [Test]
    public void Build_OverlappingParentAndLeafKeys_MarksNodeAsLeafWithChildren()
    {
        // Arrange: "user" and "user.profile"
        var keys = new[] { "user", "user.profile" };
        var builder = new KeyHierarchyBuilder();

        // Act
        var rootNodes = builder.Build(keys);

        // Assert
        rootNodes.Should().HaveCount(1);
        var userNode = rootNodes[0];
        userNode.Name.Should().Be("user");
        userNode.IsLeaf.Should().BeTrue();
        userNode.Children.Should().HaveCount(1);

        var profileNode = userNode.Children[0];
        profileNode.Name.Should().Be("profile");
        profileNode.FullPath.Should().Be("user.profile");
        profileNode.IsLeaf.Should().BeTrue();
    }

    [Test]
    public void Build_WithUnits_AttachesUnitsToLeaves()
    {
        // Arrange
        var units = new[]
        {
            new Unit { Id = "App.Settings.Theme", Source = "Theme" },
            new Unit { Id = "App.Settings.Language", Source = "Language" }
        };
        var builder = new KeyHierarchyBuilder();

        // Act
        var rootNodes = builder.Build(units);

        // Assert
        rootNodes.Should().HaveCount(1);
        var appNode = rootNodes[0];
        var settingsNode = appNode.FindChild("Settings");
        settingsNode.Should().NotBeNull();
        settingsNode!.Children.Should().HaveCount(2);

        var themeNode = settingsNode.FindChild("Theme");
        themeNode.Should().NotBeNull();
        themeNode!.Units.Should().ContainSingle(u => u.Id == "App.Settings.Theme");

        var leaves = appNode.GetAllLeaves().ToList();
        leaves.Should().HaveCount(2);
        leaves.Select(l => l.FullPath).Should().BeEquivalentTo(new[] { "App.Settings.Theme", "App.Settings.Language" });
    }

    [Test]
    public void Build_FromMultipleDocuments_AggregatesAllKeys()
    {
        // Arrange
        var doc1 = new InMemoryLingoDocument();
        doc1.AddUnit(new Unit { Id = "Nav.Home", Source = "Home" });
        doc1.AddUnit(new Unit { Id = "Nav.About", Source = "About" });

        var doc2 = new InMemoryLingoDocument();
        doc2.AddUnit(new Unit { Id = "Nav.Home", Target = "Startseite" });
        doc2.AddUnit(new Unit { Id = "Nav.Contact", Target = "Kontakt" });

        var builder = new KeyHierarchyBuilder();

        // Act
        var rootNodes = builder.Build(new[] { doc1, doc2 });

        // Assert
        rootNodes.Should().HaveCount(1);
        var navNode = rootNodes[0];
        navNode.Name.Should().Be("Nav");
        navNode.Children.Should().HaveCount(3);
        navNode.Children.Select(c => c.Name).Should().BeEquivalentTo(new[] { "Home", "About", "Contact" });
    }
}
