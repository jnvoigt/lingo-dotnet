using AwesomeAssertions;
using Lingo.Core.Models;
using Lingo.Core.Sync;
using Lingo.Core.Test.Infrastructure;
using System;
using System.IO;

namespace Lingo.Core.Test.Sync;

public class DocumentSynchronizerTests
{
    [Test]
    public void PushSync_ReportsAddedUpdatedAndRemoved()
    {
        // Arrange
        var source = new InMemoryLingoDocument();
        source.AddUnit(new Unit { Id = "added", Source = "new source" });
        source.AddUnit(new Unit { Id = "updated", Source = "changed source" });

        var target = new InMemoryLingoDocument();
        target.AddUnit(new Unit { Id = "updated", Source = "old source", Target = "old target" });
        target.AddUnit(new Unit { Id = "removed", Source = "removed source", Target = "removed target" });

        var synchronizer = new DocumentSynchronizer();
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        synchronizer.PushSync(source, target);

        // Assert
        var output = sw.ToString();
        output.Should().Contain("[+] Added: added");
        output.Should().Contain("[~] Updated: updated");
        output.Should().Contain("[-] Removed: removed");
        output.Should().Contain("Summary: 1 added, 1 updated, 1 removed");

        // Restore standard output
        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
    }
}
