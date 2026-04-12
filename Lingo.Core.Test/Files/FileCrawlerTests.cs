using AwesomeAssertions;
using Lingo.Core.Files;
using Lingo.Core.Formats;
using Lingo.Core.Test.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Lingo.Core.Test.Files;

public class FileCrawlerTests
{
    [Test]
    public void Crawl_FindsFilesWithDifferentCulturePatterns()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        tempDir.CreateDirectory("de-DE");
        tempDir.CreateDirectory("values-es");
        tempDir.CreateDirectory("bin");
        tempDir.CreateDirectory(".github");

        tempDir.CreateFile("translations.fr.xlf");
        tempDir.CreateFile("de-DE/translations.xlf");
        tempDir.CreateFile("values-es/strings.xlf");
        tempDir.CreateFile("bin/ignored.xlf");
        tempDir.CreateFile(".github/workflow.xlf");

        var crawler = new FileCrawler();

        // Act
        var results = crawler.Crawl(tempDir).ToList();

        // Assert
        results.Should().HaveCount(3);
        results.Any(r => r.Culture?.Name == "fr").Should().BeTrue();
        results.Any(r => r.Culture?.Name == "de-DE").Should().BeTrue();
        results.Any(r => r.Culture?.Name == "es").Should().BeTrue();
        results.Any(r => r.File.Name == "ignored.xlf").Should().BeFalse();
        results.Any(r => r.File.Name == "workflow.xlf").Should().BeFalse();
    }

    [Test]
    public void Crawl_FiltersByFormat()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        tempDir.CreateFile("translations.xlf");
        tempDir.CreateFile("strings.xml");
        tempDir.CreateFile("other.xliff");

        var crawler = new FileCrawler();

        // Act & Assert
        var xliffResults = crawler.Crawl(tempDir, format: LingoFormat.Xliff).ToList();
        xliffResults.Should().HaveCount(2);
        xliffResults.All(r => r.Format == LingoFormat.Xliff).Should().BeTrue();

        var otherFormat = new LingoFormat("xml", new HashSet<string> { "xml" });
        var xmlResults = crawler.Crawl(tempDir, format: otherFormat).ToList();
        xmlResults.Should().HaveCount(1);
        xmlResults.All(r => r.Format == otherFormat).Should().BeTrue();
    }

    [Test]
    public void GetSiblings_FindsFilesWithSameStubButDifferentCultures()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        var sourceFile = tempDir.CreateFile("translations.xlf");
        tempDir.CreateFile("translations.de-DE.xlf");
        tempDir.CreateFile("translations.fr.xlf");
        tempDir.CreateFile("other.xlf");

        var crawler = new FileCrawler();
        var source = LingoFileInfo.FromFile(sourceFile);

        // Act
        var siblings = crawler.GetSiblings(source!).ToList();

        // Assert
        siblings.Should().HaveCount(2);
        siblings.Any(s => s.File.Name == "translations.de-DE.xlf").Should().BeTrue();
        siblings.Any(s => s.File.Name == "translations.fr.xlf").Should().BeTrue();
        siblings.Any(s => s.File.Name == "other.xlf").Should().BeFalse();
    }
}