using AwesomeAssertions;
using Lingo.Core.Files;
using Lingo.Core.Formats;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lingo.Core.Test.Files;

public class FileCrawlerTests
{
    [Test]
    public void Crawl_FindsFilesWithDifferentCulturePatterns()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "de-DE"));
            Directory.CreateDirectory(Path.Combine(tempDir, "values-es"));
            Directory.CreateDirectory(Path.Combine(tempDir, "bin"));
            Directory.CreateDirectory(Path.Combine(tempDir, ".github"));

            File.WriteAllText(Path.Combine(tempDir, "translations.fr.xlf"), "");
            File.WriteAllText(Path.Combine(tempDir, "de-DE", "translations.xlf"), "");
            File.WriteAllText(Path.Combine(tempDir, "values-es", "strings.xlf"), "");
            File.WriteAllText(Path.Combine(tempDir, "bin", "ignored.xlf"), "");
            File.WriteAllText(Path.Combine(tempDir, ".github", "workflow.xlf"), "");

            var crawler = new FileCrawler();

            // Act
            var results = crawler.Crawl(new DirectoryInfo(tempDir)).ToList();

            // Assert
            results.Should().HaveCount(3);
            results.Any(r => r.Culture?.Name == "fr").Should().BeTrue();
            results.Any(r => r.Culture?.Name == "de-DE").Should().BeTrue();
            results.Any(r => r.Culture?.Name == "es").Should().BeTrue();
            results.Any(r => r.File.Name == "ignored.xlf").Should().BeFalse();
            results.Any(r => r.File.Name == "workflow.xlf").Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void Crawl_FiltersByFormat()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "translations.xlf"), "");
            File.WriteAllText(Path.Combine(tempDir, "strings.xml"), "");
            File.WriteAllText(Path.Combine(tempDir, "other.xliff"), "");

            var crawler = new FileCrawler();

            // Act & Assert
            var xliffResults = crawler.Crawl(new DirectoryInfo(tempDir), format: LingoFormat.Xliff).ToList();
            xliffResults.Should().HaveCount(2);
            xliffResults.All(r => r.Format == LingoFormat.Xliff).Should().BeTrue();

            var otherFormat = new LingoFormat("xml", new HashSet<string> { "xml" });
            var xmlResults = crawler.Crawl(new DirectoryInfo(tempDir), format: otherFormat).ToList();
            xmlResults.Should().HaveCount(1);
            xmlResults.All(r => r.Format == otherFormat).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void GetSiblings_FindsFilesWithSameStubButDifferentCultures()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var sourcePath = Path.Combine(tempDir, "translations.xlf");
            var dePath = Path.Combine(tempDir, "translations.de-DE.xlf");
            var frPath = Path.Combine(tempDir, "translations.fr.xlf");
            var otherPath = Path.Combine(tempDir, "other.xlf");

            File.WriteAllText(sourcePath, "");
            File.WriteAllText(dePath, "");
            File.WriteAllText(frPath, "");
            File.WriteAllText(otherPath, "");

            var crawler = new FileCrawler();
            var source = LingoFileInfo.FromPath(sourcePath);

            // Act
            var siblings = crawler.GetSiblings(source!).ToList();

            // Assert
            siblings.Should().HaveCount(2);
            siblings.Any(s => s.File.Name == "translations.de-DE.xlf").Should().BeTrue();
            siblings.Any(s => s.File.Name == "translations.fr.xlf").Should().BeTrue();
            siblings.Any(s => s.File.Name == "other.xlf").Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}