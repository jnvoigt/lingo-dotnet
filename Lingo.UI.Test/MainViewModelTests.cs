using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Lingo.Testing.Core;
using Lingo.UI.Services;
using Lingo.UI.ViewModels;

namespace Lingo.UI.Test;

public class MainViewModelTests
{
    private class MockFileDialogService : IFileDialogService
    {
        public List<string> FilesToReturn { get; set; } = new();

        public Task<string?> OpenFileDialogAsync() => Task.FromResult(FilesToReturn.FirstOrDefault());

        public Task<IReadOnlyList<string>> OpenFilesDialogAsync() => Task.FromResult<IReadOnlyList<string>>(FilesToReturn);
    }

    [Test]
    public async Task OpenFileAsync_LoadsMultipleTranslationFileGroups()
    {
        using var tempDir = new TestDirectory();
        var g1Source = tempDir.CreateFile("g1.xlf");
        var g1De = tempDir.CreateFile("g1.de-DE.xlf");
        var g2Source = tempDir.CreateFile("g2.xlf");

        const string g1SourceXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" datatype="plaintext" original="g1.txt">
                    <body>
                        <trans-unit id="g1.key1">
                            <source>Group 1 Key 1</source>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """;

        const string g1DeXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" target-language="de-DE" datatype="plaintext" original="g1.txt">
                    <body>
                        <trans-unit id="g1.key1">
                            <source>Group 1 Key 1</source>
                            <target>Gruppe 1 Schlüssel 1</target>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """;

        const string g2SourceXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" datatype="plaintext" original="g2.txt">
                    <body>
                        <trans-unit id="g2.key1">
                            <source>Group 2 Key 1</source>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """;

        File.WriteAllText(g1Source.FullName, g1SourceXml);
        File.WriteAllText(g1De.FullName, g1DeXml);
        File.WriteAllText(g2Source.FullName, g2SourceXml);

        var dialogMock = new MockFileDialogService
        {
            FilesToReturn = new List<string> { g1Source.FullName, g2Source.FullName }
        };

        var mainVm = new MainViewModel(dialogMock);
        mainVm.HasLoadedFiles.Should().BeFalse();

        await mainVm.OpenFileCommand.ExecuteAsync(null);

        mainVm.HasLoadedFiles.Should().BeTrue();
        mainVm.LoadedGroups.Should().HaveCount(2);

        var group1 = mainVm.LoadedGroups.First(g => g.GroupStub == "g1");
        group1.Files.Should().HaveCount(2); // g1.xlf and g1.de-DE.xlf

        var group2 = mainVm.LoadedGroups.First(g => g.GroupStub == "g2");
        group2.Files.Should().HaveCount(1); // g2.xlf

        // Hierarchy should contain keys from both groups
        mainVm.KeyEditor.RootNodes.Should().NotBeEmpty();
    }

    [Test]
    public void MainViewModel_InitializesWithDialogService_AndPassesToKeyEditor()
    {
        var stubDialog = new StubDialogService();
        var mainVm = new MainViewModel(null, stubDialog);

        mainVm.KeyEditor.Should().NotBeNull();
        mainVm.Title.Should().Be("Lingo Localization Editor");
        mainVm.HasLoadedFiles.Should().BeFalse();
    }

    private class StubDialogService : IDialogService
    {
        public Task<string?> ShowRenameKeyDialogAsync(string currentKey) => Task.FromResult<string?>(null);
        public Task<Lingo.Core.Files.LoadedTranslationGroup?> ShowMoveKeyDialogAsync(
            string currentKey,
            IReadOnlyList<Lingo.Core.Files.LoadedTranslationGroup> availableGroups,
            Lingo.Core.Files.LoadedTranslationGroup? currentGroup) => Task.FromResult<Lingo.Core.Files.LoadedTranslationGroup?>(null);
    }
}
