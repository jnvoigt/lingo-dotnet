using AwesomeAssertions;
using Lingo.Cli.Commands;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Lingo.Cli.Test;

public class SyncCommandTests
{
    private string _tempDir;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LingoTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public void Sync_ShouldSortTargetDocumentByKeys()
    {
        // Arrange
        var sourcePath = Path.Combine(_tempDir, "source.xlf");
        var targetPath = Path.Combine(_tempDir, "target.xlf");

        var factory = new XliffDocumentFactory();
        var writer = new XliffDocumentWriter();

        // Create source with units in some order
        var sourceDoc = factory.Create("xliff-1.2", new[]
        {
            new Unit { Id = "z", Source = "Source Z" },
            new Unit { Id = "a", Source = "Source A" },
            new Unit { Id = "m", Source = "Source M" },
            new Unit { Id = "b", Source = "Source B" }
        });

        using (var fs = File.Create(sourcePath))
        {
            writer.Write(fs, sourceDoc);
        }

        // Create target with no units (or some units that will be overwritten/removed)
        var targetDoc = factory.Create("xliff-1.2", Enumerable.Empty<Unit>());

        using (var fs = File.Create(targetPath))
        {
            writer.Write(fs, targetDoc);
        }

        var rootCommand = new RootCommand();
        rootCommand.Add(SyncCommand.GetCommand());

        // Act
        rootCommand.Parse(new[] { "sync", "--source", sourcePath, "--target", targetPath }).Invoke();

        // Assert
        using (var fs = File.OpenRead(targetPath))
        {
            var updatedTargetDoc = factory.Create(fs);
            var unitIds = updatedTargetDoc.GetAllUnits().Select(u => u.Id).ToList();

            // Should contain all units from source and target, sorted
            // Source: a, b
            // Target: m, z
            // Combined and sorted: a, b, m, z
            unitIds.Should().ContainInOrder("a", "b", "m", "z");
        }
    }
}
