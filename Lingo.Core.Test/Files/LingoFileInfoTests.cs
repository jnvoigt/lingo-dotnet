using AwesomeAssertions;
using Lingo.Core.Files;
using Lingo.Core.Formats;
using System.Collections.Generic;

namespace Lingo.Core.Test.Files;

public class LingoFileInfoTests
{
    [TestCase("translations.de-DE.xlf", "translations", "de-DE", "xliff")]
    [TestCase("translations.de.xlf", "translations", "de", "xliff")]
    [TestCase("app.en-US.xliff", "app", "en-US", "xliff")]
    [TestCase("translations.de-DE.lingo", "translations", "de-DE", "lingo")]
    public void FromPath_ExtractsCultureFromFilename(string filename, string expectedStub, string expectedCulture,
        string expectedFormatId)
    {
        var result = LingoFileInfo.FromPath(filename);

        result.Should().NotBeNull();
        result!.Stub.Should().Be(expectedStub);
        result.Culture?.Name.Should().Be(expectedCulture);
        result.Format?.Id.Should().Be(expectedFormatId);
    }

    [TestCase("de-DE/translations.xlf", "translations", "de-DE", "xliff")]
    [TestCase("es/app.xliff", "app", "es", "xliff")]
    public void FromPath_ExtractsCultureFromParentDirectory(string path, string expectedStub, string expectedCulture,
        string expectedFormatId)
    {
        var result = LingoFileInfo.FromPath(path);

        result.Should().NotBeNull();
        result!.Stub.Should().Be(expectedStub);
        result.Culture?.Name.Should().Be(expectedCulture);
        result.Format?.Id.Should().Be(expectedFormatId);
    }

    [Test]
    public void FromPath_HandlesNoCulture()
    {
        var result = LingoFileInfo.FromPath("translations.xlf");

        result.Should().NotBeNull();
        result!.Stub.Should().Be("translations");
        result.Culture.Should().BeNull();
        result.Format.Should().Be(LingoFormat.Xliff);
    }

    [Test]
    public void FromPath_WithExpectedFormat_ReturnsNullIfExtensionDoesNotMatch()
    {
        var result =
            LingoFileInfo.FromPath("translations.xlf", new LingoFormat("json", new HashSet<string> { "json" }));

        result.Should().BeNull();
    }

    [Test]
    public void FromPath_WithExpectedFormat_ReturnsFileInfoIfExtensionMatches()
    {
        var format = new LingoFormat("json", new HashSet<string> { "json" });
        var result = LingoFileInfo.FromPath("translations.json", format);

        result.Should().NotBeNull();
        result!.Format.Should().Be(format);
    }

    [Test]
    public void IsSibling_ReturnsTrue_WhenFilesAreInSameDirectoryAndHaveSameStub()
    {
        var fileA = LingoFileInfo.FromPath("i18n/translations.en-US.xlf", LingoFormat.Xliff);
        var fileB = LingoFileInfo.FromPath("i18n/translations.de-DE.xlf", LingoFormat.Xliff);

        fileA!.IsSibling(fileB!).Should().BeTrue();
    }

    [Test]
    public void IsSibling_ReturnsFalse_WhenFilesAreInDifferentDirectories()
    {
        var fileA = LingoFileInfo.FromPath("i18n/translations.en-US.xlf", LingoFormat.Xliff);
        var fileB = LingoFileInfo.FromPath("other/translations.de-DE.xlf", LingoFormat.Xliff);

        fileA!.IsSibling(fileB!).Should().BeFalse();
    }

    [Test]
    public void IsSibling_ReturnsFalse_WhenFilesHaveDifferentStubs()
    {
        var fileA = LingoFileInfo.FromPath("i18n/app.en-US.xlf", LingoFormat.Xliff);
        var fileB = LingoFileInfo.FromPath("i18n/translations.en-US.xlf", LingoFormat.Xliff);

        fileA!.IsSibling(fileB!).Should().BeFalse();
    }

    [Test]
    public void IsSibling_ReturnsFalse_WhenSameFile()
    {
        var fileA = LingoFileInfo.FromPath("i18n/translations.en-US.xlf", LingoFormat.Xliff);

        fileA!.IsSibling(fileA).Should().BeFalse();
    }
}