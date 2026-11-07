using AwesomeAssertions;
using Lingo.Core.Files;
using Lingo.Core.Formats;
using Lingo.Core.Models;
using Lingo.Testing.Core;
using System.IO;
using System.Linq;

namespace Lingo.Core.Test.Files;

public class TranslationFileLoaderTests
{
    private static readonly string TestDataDir = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "../../../../TestData/Xliff12");

    [Test]
    public void LoadGroupFromPath_WithSourceFile_LoadsAllSiblings()
    {
        // Arrange
        var sourceFilePath = Path.Combine(TestDataDir, "TestData.xlf");
        if (!File.Exists(sourceFilePath))
        {
            // Fallback to relative project path
            sourceFilePath = Path.GetFullPath("TestData/Xliff12/TestData.xlf");
        }

        var loader = new TranslationFileLoader();

        // Act
        var group = loader.LoadGroupFromPath(sourceFilePath);

        // Assert
        group.Should().NotBeNull();
        group!.GroupStub.Should().Be("TestData");
        group.Format.Should().Be(LingoFormat.Xliff);
        group.Files.Should().HaveCount(3);

        var fileNames = group.Files.Select(f => f.FileName).ToList();
        fileNames.Should().Contain("TestData.xlf");
        fileNames.Should().Contain("TestData.de-DE.xlf");
        fileNames.Should().Contain("TestData.fr-FR.xlf");

        var sourceFile = group.SourceFile;
        sourceFile.Should().NotBeNull();
        sourceFile!.FileName.Should().Be("TestData.xlf");
        sourceFile.IsSource.Should().BeTrue();
    }

    [Test]
    public void LoadGroupFromPath_WithSiblingFile_FindsSameSiblingGroup()
    {
        // Arrange
        var deFilePath = Path.Combine(TestDataDir, "TestData.de-DE.xlf");
        if (!File.Exists(deFilePath))
        {
            deFilePath = Path.GetFullPath("TestData/Xliff12/TestData.de-DE.xlf");
        }

        var loader = new TranslationFileLoader();

        // Act
        var group = loader.LoadGroupFromPath(deFilePath);

        // Assert
        group.Should().NotBeNull();
        group!.GroupStub.Should().Be("TestData");
        group.Files.Should().HaveCount(3);

        var deFile = group.Files.FirstOrDefault(f => f.FileName == "TestData.de-DE.xlf");
        deFile.Should().NotBeNull();
        deFile!.LanguageLabel.Should().Be("de-DE");
        deFile.IsSource.Should().BeFalse();
    }

    [Test]
    public void LoadGroupFromPath_ReturnsNull_WhenFileDoesNotExist()
    {
        // Arrange
        var loader = new TranslationFileLoader();

        // Act
        var group = loader.LoadGroupFromPath("non_existent_file.xlf");

        // Assert
        group.Should().BeNull();
    }

    [Test]
    public void LoadGroup_WithSingleFileWithoutSiblings_LoadsSingleFileGroup()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        var filePath = tempDir.CreateFile("single.xlf");
        File.WriteAllText(filePath.FullName, """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" datatype="plaintext" original="single.txt">
                    <body>
                        <trans-unit id="app.title">
                            <source>My Application</source>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """);

        var loader = new TranslationFileLoader();

        // Act
        var group = loader.LoadGroupFromPath(filePath.FullName);

        // Assert
        group.Should().NotBeNull();
        group!.Files.Should().HaveCount(1);
        group.Files[0].FileName.Should().Be("single.xlf");
        group.Files[0].Document.GetValue("app.title").Should().Be("My Application");
    }

    [Test]
    public void SaveFile_WritesModifiedTranslationsToDisk()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        var filePath = tempDir.CreateFile("translations.de-DE.xlf");
        File.WriteAllText(filePath.FullName, """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" target-language="de-DE" datatype="plaintext" original="app.txt">
                    <body>
                        <trans-unit id="btn.save">
                            <source>Save</source>
                            <target>Speichern</target>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """);

        var loader = new TranslationFileLoader();
        var group = loader.LoadGroupFromPath(filePath.FullName);
        group.Should().NotBeNull();

        var loadedFile = group!.Files[0];

        // Act
        loadedFile.Document.SetValue("btn.save", "Datei speichern");
        loader.SaveFile(loadedFile);

        // Reload to verify disk content
        var reloadedGroup = loader.LoadGroupFromPath(filePath.FullName);

        // Assert
        reloadedGroup.Should().NotBeNull();
        reloadedGroup!.Files[0].Document.GetValue("btn.save").Should().Be("Datei speichern");
    }

    private static string GetSyncTestFilePath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../Xliff/TestData/SyncTest", fileName),
            Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../Lingo.Core.Test/Xliff/TestData/SyncTest", fileName),
            Path.GetFullPath(Path.Combine("Lingo.Core.Test/Xliff/TestData/SyncTest", fileName))
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    [Test]
    public void LoadGroupFromPath_WithSyncTestData20DeDe_LoadsSourceAndAllSiblings()
    {
        // Arrange
        var deFilePath = GetSyncTestFilePath("data20_de_de.xlf");
        var loader = new TranslationFileLoader();

        // Act
        var group = loader.LoadGroupFromPath(deFilePath);

        // Assert
        group.Should().NotBeNull();
        group!.GroupStub.Should().Be("data20");
        group.Files.Should().HaveCount(3);

        var fileNames = group.Files.Select(f => f.FileName).ToList();
        fileNames.Should().Contain("data20.xlf");
        fileNames.Should().Contain("data20_de_de.xlf");
        fileNames.Should().Contain("data20_fr_fr.xlf");

        var sourceFile = group.SourceFile;
        sourceFile.Should().NotBeNull();
        sourceFile!.FileName.Should().Be("data20.xlf");
        sourceFile.IsSource.Should().BeTrue();

        var frFile = group.Files.FirstOrDefault(f => f.FileName == "data20_fr_fr.xlf");
        frFile.Should().NotBeNull();
        frFile!.LanguageLabel.Should().Be("fr-FR");
    }

    [Test]
    public void LoadGroupFromPath_WithSyncTestData12DeDe_LoadsSourceAndAllSiblings()
    {
        // Arrange
        var deFilePath = GetSyncTestFilePath("data12_de_de.xlf");
        var loader = new TranslationFileLoader();

        // Act
        var group = loader.LoadGroupFromPath(deFilePath);

        // Assert
        group.Should().NotBeNull();
        group!.GroupStub.Should().Be("data12");
        group.Files.Should().HaveCount(3);

        var fileNames = group.Files.Select(f => f.FileName).ToList();
        fileNames.Should().Contain("data12.xlf");
        fileNames.Should().Contain("data12_de_de.xlf");
        fileNames.Should().Contain("data12_fr_fr.xlf");

        var sourceFile = group.SourceFile;
        sourceFile.Should().NotBeNull();
        sourceFile!.FileName.Should().Be("data12.xlf");
        sourceFile.IsSource.Should().BeTrue();

        var frFile = group.Files.FirstOrDefault(f => f.FileName == "data12_fr_fr.xlf");
        frFile.Should().NotBeNull();
        frFile!.LanguageLabel.Should().Be("fr-FR");
    }

    [Test]
    public void SetSourceValue_UpdatesSourceAcrossDocuments()
    {
        // Arrange
        var deFilePath = GetSyncTestFilePath("data20_de_de.xlf");
        var loader = new TranslationFileLoader();
        var group = loader.LoadGroupFromPath(deFilePath);
        group.Should().NotBeNull();

        const string unitId = "sample.text.that-has-not-changed";
        const string updatedSource = "Updated Source Text A";

        // Act - update source on all files in group
        foreach (var file in group!.Files)
        {
            if (file.Document is Documents.IHasSourceValue sourceDoc)
            {
                sourceDoc.SetSourceValue(unitId, updatedSource);
            }
        }

        // Assert
        foreach (var file in group.Files)
        {
            if (file.Document is Documents.IHasSourceValue sourceDoc)
            {
                sourceDoc.GetSourceValue(unitId).Should().Be(updatedSource);
            }
            file.Document.GetUnit(unitId)?.Source.Should().Be(updatedSource);
        }
    }

    [Test]
    public void LoadGroups_WithMultipleDistinctFilePaths_LoadsAllDistinctGroups()
    {
        var data12Path = GetSyncTestFilePath("data12.xlf");
        var data20Path = GetSyncTestFilePath("data20.xlf");
        var loader = new TranslationFileLoader();

        var groups = loader.LoadGroups(new[] { data12Path, data20Path });

        groups.Should().HaveCount(2);
        groups.Select(g => g.GroupStub).Should().Contain("data12");
        groups.Select(g => g.GroupStub).Should().Contain("data20");
    }

    [Test]
    public void SaveModifiedFiles_OnlySavesFilesMarkedAsModified()
    {
        using var tempDir = new TestDirectory();
        var file1 = tempDir.CreateFile("file1.xlf");
        var file2 = tempDir.CreateFile("file2.xlf");

        const string initialXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" datatype="plaintext" original="f.txt">
                    <body>
                        <trans-unit id="k1">
                            <source>Initial</source>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """;

        File.WriteAllText(file1.FullName, initialXml);
        File.WriteAllText(file2.FullName, initialXml);

        var loader = new TranslationFileLoader();
        var groups = loader.LoadGroups(new[] { file1.FullName, file2.FullName });
        groups.Should().HaveCount(2);

        var group1 = groups.First(g => g.GroupStub == "file1");
        var group2 = groups.First(g => g.GroupStub == "file2");

        // Only modify group1
        group1.Files[0].Document.SetValue("k1", "Modified in Group 1");
        group1.Files[0].IsModified = true;

        var savedCount = loader.SaveModifiedFiles(groups);

        savedCount.Should().Be(1);
        group1.Files[0].IsModified.Should().BeFalse();

        // Verify disk contents
        var reloaded1 = loader.LoadGroupFromPath(file1.FullName);
        var reloaded2 = loader.LoadGroupFromPath(file2.FullName);

        reloaded1!.Files[0].Document.GetValue("k1").Should().Be("Modified in Group 1");
        reloaded2!.Files[0].Document.GetValue("k1").Should().Be("Initial"); // unchanged initial value
    }
}
