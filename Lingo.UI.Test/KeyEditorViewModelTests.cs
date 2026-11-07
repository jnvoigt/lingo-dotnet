using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Lingo.Core.Documents;
using Lingo.Core.Models;
using Lingo.Core.Test.Infrastructure;
using Lingo.UI.ViewModels;

namespace Lingo.UI.Test;

public class KeyEditorViewModelTests
{
    [Test]
    public void RevertKeyChanges_RestoresOriginalSourceAndTargetText()
    {
        var enDoc = new InMemoryLingoDocument
        {
            SourceCulture = new CultureInfo("en-US")
        };
        enDoc.AddUnit(new Unit { Id = "app.title", Source = "Original Title", Target = "Original Title" });

        var deDoc = new InMemoryLingoDocument
        {
            SourceCulture = new CultureInfo("en-US"),
            TargetCulture = new CultureInfo("de-DE")
        };
        deDoc.AddUnit(new Unit { Id = "app.title", Source = "Original Title", Target = "Originaler Titel" });

        var viewModel = new KeyEditorViewModel();
        viewModel.SetDocuments(new (ILingoDocument, string, string)[]
        {
            (enDoc, "en.xlf", "en-US"),
            (deDoc, "de.xlf", "de-DE")
        });

        viewModel.SelectKeyByPath("app.title");
        viewModel.HasSelectedKey.Should().BeTrue();
        viewModel.CurrentKeyTranslations.Should().HaveCount(2);

        var enEntry = viewModel.CurrentKeyTranslations.First(t => t.LanguageCode == "en-US");
        var deEntry = viewModel.CurrentKeyTranslations.First(t => t.LanguageCode == "de-DE");

        enEntry.SourceText.Should().Be("Original Title");
        deEntry.TargetText.Should().Be("Originaler Titel");
        enEntry.IsModified.Should().BeFalse();
        deEntry.IsModified.Should().BeFalse();
        viewModel.IsKeyModified.Should().BeFalse();
        viewModel.CanRevertKeyChanges.Should().BeFalse();
        viewModel.RevertKeyChangesCommand.CanExecute(null).Should().BeFalse();

        // Mutate target text and source text
        deEntry.TargetText = "Modified German";
        deEntry.IsModified.Should().BeTrue();
        viewModel.IsKeyModified.Should().BeTrue();
        viewModel.CanRevertKeyChanges.Should().BeTrue();
        viewModel.RevertKeyChangesCommand.CanExecute(null).Should().BeTrue();

        enEntry.SourceText = "Modified Title";
        enEntry.IsModified.Should().BeTrue();

        // Execute RevertKeyChangesCommand
        viewModel.RevertKeyChangesCommand.Execute(null);

        // Verify values are reverted back to document state
        var revertedEn = viewModel.CurrentKeyTranslations.First(t => t.LanguageCode == "en-US");
        var revertedDe = viewModel.CurrentKeyTranslations.First(t => t.LanguageCode == "de-DE");

        revertedEn.SourceText.Should().Be("Original Title");
        revertedDe.TargetText.Should().Be("Originaler Titel");
        revertedEn.IsModified.Should().BeFalse();
        revertedDe.IsModified.Should().BeFalse();
        viewModel.IsKeyModified.Should().BeFalse();
        viewModel.CanRevertKeyChanges.Should().BeFalse();
        viewModel.RevertKeyChangesCommand.CanExecute(null).Should().BeFalse();
    }

    [Test]
    public void RevertChanges_OnSingleEntry_RevertsOnlyThatEntry()
    {
        var deDoc = new InMemoryLingoDocument
        {
            SourceCulture = new CultureInfo("en-US"),
            TargetCulture = new CultureInfo("de-DE")
        };
        deDoc.AddUnit(new Unit { Id = "msg.hello", Source = "Hello", Target = "Hallo" });

        var entryVm = new KeyTranslationEntryViewModel(deDoc, "messages.de.xlf", "msg.hello", "de-DE");
        entryVm.TargetText.Should().Be("Hallo");
        entryVm.IsModified.Should().BeFalse();
        entryVm.RevertChangesCommand.CanExecute(null).Should().BeFalse();

        entryVm.TargetText = "Guten Tag (unsaved edit)";
        entryVm.IsModified.Should().BeTrue();
        entryVm.RevertChangesCommand.CanExecute(null).Should().BeTrue();

        entryVm.RevertChangesCommand.Execute(null);

        entryVm.TargetText.Should().Be("Hallo");
        entryVm.IsModified.Should().BeFalse();
        entryVm.RevertChangesCommand.CanExecute(null).Should().BeFalse();
    }

    [Test]
    public void SaveAllTranslations_ResetsCanRevertKeyChanges()
    {
        var enDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        enDoc.AddUnit(new Unit { Id = "app.title", Source = "Original Title", Target = "Original Title" });

        var viewModel = new KeyEditorViewModel();
        viewModel.SetDocuments(new (ILingoDocument, string, string)[] { (enDoc, "en.xlf", "en-US") });
        viewModel.SelectKeyByPath("app.title");

        var entry = viewModel.CurrentKeyTranslations.First();
        entry.TargetText = "New Saved Title";
        viewModel.CanRevertKeyChanges.Should().BeTrue();

        viewModel.SaveAllTranslationsCommand.Execute(null);

        viewModel.CanRevertKeyChanges.Should().BeFalse();
        viewModel.RevertKeyChangesCommand.CanExecute(null).Should().BeFalse();
        entry.IsModified.Should().BeFalse();
    }

    [Test]
    public void AddNewKey_AddsKeyAndPropagatesAcrossSiblingFiles()
    {
        var docSource = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        var docDe = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };

        var viewModel = new KeyEditorViewModel();
        viewModel.SetDocuments(new (ILingoDocument, string, string)[]
        {
            (docSource, "source.xlf", "en-US"),
            (docDe, "de.xlf", "de-DE")
        });

        viewModel.NewKeyPath = "dialog.confirm.title";
        viewModel.NewKeyDefaultSource = "Are you sure?";
        viewModel.AddNewKeyCommand.Execute(null);

        docSource.GetUnitIds().Should().Contain("dialog.confirm.title");
        docDe.GetUnitIds().Should().Contain("dialog.confirm.title");

        docSource.GetUnit("dialog.confirm.title")!.Source.Should().Be("Are you sure?");
        docDe.GetUnit("dialog.confirm.title")!.Source.Should().Be("Are you sure?");
        docDe.GetUnit("dialog.confirm.title")!.State.Should().Be(TranslationState.NeedsTranslation);

        viewModel.SelectedKeyFullPath.Should().Be("dialog.confirm.title");
    }

    [Test]
    public void DeleteKey_RemovesKeyFromAllAssociatedDocuments()
    {
        var docSource = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        docSource.AddUnit(new Unit { Id = "key.to.delete", Source = "Delete Me" });

        var docDe = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };
        docDe.AddUnit(new Unit { Id = "key.to.delete", Source = "Delete Me", Target = "Lösch mich" });

        var viewModel = new KeyEditorViewModel();
        viewModel.SetDocuments(new (ILingoDocument, string, string)[]
        {
            (docSource, "source.xlf", "en-US"),
            (docDe, "de.xlf", "de-DE")
        });

        viewModel.SelectKeyByPath("key.to.delete");
        viewModel.HasSelectedKey.Should().BeTrue();

        viewModel.DeleteSelectedKeyCommand.Execute(null);

        docSource.GetUnitIds().Should().NotContain("key.to.delete");
        docDe.GetUnitIds().Should().NotContain("key.to.delete");
        viewModel.HasSelectedKey.Should().BeFalse();
        viewModel.CurrentKeyTranslations.Should().BeEmpty();
    }

    [Test]
    public void RenameKey_RenamesKeyAcrossDocumentsWithoutChangingTexts()
    {
        var docSource = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        docSource.AddUnit(new Unit { Id = "old.key.name", Source = "Save File", Target = "Save File" });

        var docDe = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };
        docDe.AddUnit(new Unit { Id = "old.key.name", Source = "Save File", Target = "Datei speichern" });

        var viewModel = new KeyEditorViewModel();
        viewModel.SetDocuments(new (ILingoDocument, string, string)[]
        {
            (docSource, "source.xlf", "en-US"),
            (docDe, "de.xlf", "de-DE")
        });

        viewModel.SelectKeyByPath("old.key.name");
        viewModel.RenameKeyNewName = "new.key.name";
        viewModel.RenameSelectedKeyCommand.Execute(null);

        docSource.GetUnitIds().Should().NotContain("old.key.name");
        docSource.GetUnitIds().Should().Contain("new.key.name");
        docDe.GetUnitIds().Should().NotContain("old.key.name");
        docDe.GetUnitIds().Should().Contain("new.key.name");

        docSource.GetUnit("new.key.name")!.Source.Should().Be("Save File");
        docDe.GetUnit("new.key.name")!.Target.Should().Be("Datei speichern");
        viewModel.SelectedKeyFullPath.Should().Be("new.key.name");
    }

    [Test]
    public void MoveKey_TransfersKeyFromSourceGroupToTargetGroup()
    {
        var group1SourceDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        group1SourceDoc.AddUnit(new Unit { Id = "btn.ok", Source = "OK", Target = "OK" });
        var group1DeDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };
        group1DeDoc.AddUnit(new Unit { Id = "btn.ok", Source = "OK", Target = "OK (DE)" });

        var group1 = new Lingo.Core.Files.LoadedTranslationGroup
        {
            GroupStub = "common",
            DirectoryPath = "/test/common",
            Format = Lingo.Core.Formats.LingoFormat.Xliff,
            Files = new List<Lingo.Core.Files.LoadedTranslationFile>
            {
                new() { Document = group1SourceDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/common/common.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "en-US (Source)", IsSource = true },
                new() { Document = group1DeDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/common/common.de-DE.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "de-DE", IsSource = false }
            }
        };

        var group2SourceDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        var group2DeDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };

        var group2 = new Lingo.Core.Files.LoadedTranslationGroup
        {
            GroupStub = "dialogs",
            DirectoryPath = "/test/dialogs",
            Format = Lingo.Core.Formats.LingoFormat.Xliff,
            Files = new List<Lingo.Core.Files.LoadedTranslationFile>
            {
                new() { Document = group2SourceDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/dialogs/dialogs.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "en-US (Source)", IsSource = true },
                new() { Document = group2DeDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/dialogs/dialogs.de-DE.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "de-DE", IsSource = false }
            }
        };

        var viewModel = new KeyEditorViewModel();
        viewModel.LoadTranslationGroups(new[] { group1, group2 });

        viewModel.SelectKeyByPath("btn.ok");
        viewModel.CanMoveKey.Should().BeTrue();

        viewModel.TargetMoveGroup = group2;
        viewModel.MoveSelectedKeyCommand.Execute(null);

        // Should be removed from group 1
        group1SourceDoc.GetUnitIds().Should().NotContain("btn.ok");
        group1DeDoc.GetUnitIds().Should().NotContain("btn.ok");

        // Should be present in group 2
        group2SourceDoc.GetUnitIds().Should().Contain("btn.ok");
        group2DeDoc.GetUnitIds().Should().Contain("btn.ok");
        group2DeDoc.GetUnit("btn.ok")!.Target.Should().Be("OK (DE)");
    }

    [Test]
    public async System.Threading.Tasks.Task OpenRenameKeyDialog_ExecutesViaDialogService()
    {
        var docSource = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        docSource.AddUnit(new Unit { Id = "app.greeting", Source = "Hello" });

        var mockDialogService = new StubDialogService
        {
            RenameResult = "app.welcome"
        };

        var viewModel = new KeyEditorViewModel(mockDialogService);
        viewModel.SetDocuments(new (ILingoDocument, string, string)[] { (docSource, "source.xlf", "en-US") });
        viewModel.SelectKeyByPath("app.greeting");

        await viewModel.OpenRenameKeyDialogCommand.ExecuteAsync(null);

        docSource.GetUnitIds().Should().NotContain("app.greeting");
        docSource.GetUnitIds().Should().Contain("app.welcome");
        viewModel.SelectedKeyFullPath.Should().Be("app.welcome");
    }

    [Test]
    public async System.Threading.Tasks.Task OpenMoveKeyDialog_ExecutesViaDialogService()
    {
        var group1SourceDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        group1SourceDoc.AddUnit(new Unit { Id = "key.move.dialog", Source = "Move Dialog" });

        var group1 = new Lingo.Core.Files.LoadedTranslationGroup
        {
            GroupStub = "grp1",
            DirectoryPath = "/test/grp1",
            Format = Lingo.Core.Formats.LingoFormat.Xliff,
            Files = new List<Lingo.Core.Files.LoadedTranslationFile>
            {
                new() { Document = group1SourceDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/grp1/grp1.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "en-US", IsSource = true }
            }
        };

        var group2SourceDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        var group2 = new Lingo.Core.Files.LoadedTranslationGroup
        {
            GroupStub = "grp2",
            DirectoryPath = "/test/grp2",
            Format = Lingo.Core.Formats.LingoFormat.Xliff,
            Files = new List<Lingo.Core.Files.LoadedTranslationFile>
            {
                new() { Document = group2SourceDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/grp2/grp2.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "en-US", IsSource = true }
            }
        };

        var mockDialogService = new StubDialogService
        {
            MoveResult = group2
        };

        var viewModel = new KeyEditorViewModel(mockDialogService);
        viewModel.LoadTranslationGroups(new[] { group1, group2 });
        viewModel.SelectKeyByPath("key.move.dialog");

        await viewModel.OpenMoveKeyDialogCommand.ExecuteAsync(null);

        group1SourceDoc.GetUnitIds().Should().NotContain("key.move.dialog");
        group2SourceDoc.GetUnitIds().Should().Contain("key.move.dialog");
        viewModel.SelectedKeyFullPath.Should().Be("key.move.dialog");
    }

    [Test]
    public void SearchingKeys_ExpandsAllFoundResults()
    {
        var doc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        doc.AddUnit(new Unit { Id = "company.department.finance.budget", Source = "Budget" });
        doc.AddUnit(new Unit { Id = "company.department.hr.payroll", Source = "Payroll" });
        doc.AddUnit(new Unit { Id = "settings.general.theme", Source = "Theme" });

        var viewModel = new KeyEditorViewModel();
        viewModel.SetDocuments(new (ILingoDocument, string, string)[] { (doc, "en.xlf", "en-US") });

        // Search for "payroll"
        viewModel.SearchFilter = "payroll";

        viewModel.RootNodes.Should().NotBeEmpty();
        var companyNode = viewModel.RootNodes.First(n => n.Name == "company");
        companyNode.IsExpanded.Should().BeTrue();

        var deptNode = companyNode.Children.First(n => n.Name == "department");
        deptNode.IsExpanded.Should().BeTrue();

        var hrNode = deptNode.Children.First(n => n.Name == "hr");
        hrNode.IsExpanded.Should().BeTrue();

        var payrollNode = hrNode.Children.First(n => n.Name == "payroll");
        payrollNode.IsLeaf.Should().BeTrue();
    }

    [Test]
    public void ExpandAndCollapseAll_TogglesAllDescendantNodes()
    {
        var root = new KeyTreeNodeViewModel { Name = "root", IsExpanded = false };
        var child = new KeyTreeNodeViewModel { Name = "child", IsExpanded = false, Parent = root };
        var grandchild = new KeyTreeNodeViewModel { Name = "grandchild", IsExpanded = false, Parent = child };
        child.Children.Add(grandchild);
        root.Children.Add(child);

        root.ExpandAll();
        root.IsExpanded.Should().BeTrue();
        child.IsExpanded.Should().BeTrue();
        grandchild.IsExpanded.Should().BeTrue();

        root.CollapseAll();
        root.IsExpanded.Should().BeFalse();
        child.IsExpanded.Should().BeFalse();
        grandchild.IsExpanded.Should().BeFalse();
    }

    [Test]
    public void SyncSelectedKeyAcrossLanguages_WhenKeyMissingInSource_DeletesKeyAndClearsSelection()
    {
        var sourceDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        // sourceDoc does not have "orphan.key"
        var deDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };
        deDoc.AddUnit(new Unit { Id = "orphan.key", Source = "Orphan", Target = "Waise" });

        var group = new Lingo.Core.Files.LoadedTranslationGroup
        {
            GroupStub = "test_grp",
            DirectoryPath = "/test/group",
            Format = Lingo.Core.Formats.LingoFormat.Xliff,
            Files = new List<Lingo.Core.Files.LoadedTranslationFile>
            {
                new() { Document = sourceDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/group/data.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "en-US", IsSource = true },
                new() { Document = deDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/group/data.de-DE.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "de-DE", IsSource = false }
            }
        };

        var viewModel = new KeyEditorViewModel();
        viewModel.LoadTranslationGroups(new[] { group });
        viewModel.SelectKeyByPath("orphan.key");

        viewModel.HasSelectedKey.Should().BeTrue();
        deDoc.GetUnitIds().Should().Contain("orphan.key");

        // Sync key: since sourceDoc lacks it, it must be deleted from deDoc
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.Execute(null);

        deDoc.GetUnitIds().Should().NotContain("orphan.key");
        viewModel.HasSelectedKey.Should().BeFalse();
        viewModel.SelectedKeyFullPath.Should().BeNull();
        viewModel.SelectedNode.Should().BeNull();
        viewModel.CurrentKeyTranslations.Should().BeEmpty();
    }

    [Test]
    public void SyncButton_IsEnabledOnlyWhenSourceAndTargetKeysOrValuesAreDifferent()
    {
        var sourceDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US") };
        sourceDoc.AddUnit(new Unit { Id = "key.differentSource", Source = "Source Text V2", Target = "Source Text V2" });
        sourceDoc.AddUnit(new Unit { Id = "key.missingInTarget", Source = "New Key", Target = "New Key" });
        sourceDoc.AddUnit(new Unit { Id = "key.identical", Source = "Same Text", Target = "Same Text" });

        var deDoc = new InMemoryLingoDocument { SourceCulture = new CultureInfo("en-US"), TargetCulture = new CultureInfo("de-DE") };
        deDoc.AddUnit(new Unit { Id = "key.differentSource", Source = "Source Text V1", Target = "Alter Text", State = TranslationState.Translated });
        deDoc.AddUnit(new Unit { Id = "key.identical", Source = "Same Text", Target = "Gleicher Text", State = TranslationState.Translated });
        deDoc.AddUnit(new Unit { Id = "key.orphan", Source = "Orphan Text", Target = "Waise" });

        var group = new Lingo.Core.Files.LoadedTranslationGroup
        {
            GroupStub = "test_grp",
            DirectoryPath = "/test/group",
            Format = Lingo.Core.Formats.LingoFormat.Xliff,
            Files = new List<Lingo.Core.Files.LoadedTranslationFile>
            {
                new() { Document = sourceDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/group/data.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "en-US", IsSource = true },
                new() { Document = deDoc, FileInfo = Lingo.Core.Files.LingoFileInfo.FromFile(new FileInfo("/test/group/data.de-DE.xlf"), Lingo.Core.Formats.LingoFormat.Xliff)!, LanguageLabel = "de-DE", IsSource = false }
            }
        };

        var viewModel = new KeyEditorViewModel();
        viewModel.LoadTranslationGroups(new[] { group });

        // 1. Key with identical source across all files -> Sync button is DISABLED
        viewModel.SelectKeyByPath("key.identical");
        viewModel.CanSyncSelectedKey.Should().BeFalse();
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.CanExecute(null).Should().BeFalse();

        // 2. Key with different source text between source and target -> Sync button is ENABLED
        viewModel.SelectKeyByPath("key.differentSource");
        viewModel.CanSyncSelectedKey.Should().BeTrue();
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.CanExecute(null).Should().BeTrue();

        // Act: Sync the key
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.Execute(null);

        // Target document should have updated source text and adjusted state (NeedsAdaptation)
        var updatedDe = deDoc.GetUnit("key.differentSource");
        updatedDe!.Source.Should().Be("Source Text V2");
        updatedDe.State.Should().Be(TranslationState.NeedsAdaptation);

        // After sync, source and target are in sync -> Sync button becomes DISABLED
        viewModel.CanSyncSelectedKey.Should().BeFalse();
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.CanExecute(null).Should().BeFalse();

        // 3. Key missing in target -> Sync button is ENABLED
        viewModel.SelectKeyByPath("key.missingInTarget");
        viewModel.CanSyncSelectedKey.Should().BeTrue();
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.CanExecute(null).Should().BeTrue();

        viewModel.SyncSelectedKeyAcrossLanguagesCommand.Execute(null);
        deDoc.GetUnitIds().Should().Contain("key.missingInTarget");
        viewModel.CanSyncSelectedKey.Should().BeFalse();

        // 4. Orphaned key (missing in source) -> Sync button is ENABLED
        viewModel.SelectKeyByPath("key.orphan");
        viewModel.CanSyncSelectedKey.Should().BeTrue();
        viewModel.SyncSelectedKeyAcrossLanguagesCommand.CanExecute(null).Should().BeTrue();

        viewModel.SyncSelectedKeyAcrossLanguagesCommand.Execute(null);
        deDoc.GetUnitIds().Should().NotContain("key.orphan");
        viewModel.CanSyncSelectedKey.Should().BeFalse();
    }

    private class StubDialogService : Lingo.UI.Services.IDialogService
    {
        public string? RenameResult { get; set; }
        public Lingo.Core.Files.LoadedTranslationGroup? MoveResult { get; set; }

        public System.Threading.Tasks.Task<string?> ShowRenameKeyDialogAsync(string currentKey)
        {
            return System.Threading.Tasks.Task.FromResult(RenameResult);
        }

        public System.Threading.Tasks.Task<Lingo.Core.Files.LoadedTranslationGroup?> ShowMoveKeyDialogAsync(
            string currentKey,
            IReadOnlyList<Lingo.Core.Files.LoadedTranslationGroup> availableGroups,
            Lingo.Core.Files.LoadedTranslationGroup? currentGroup)
        {
            return System.Threading.Tasks.Task.FromResult(MoveResult);
        }
    }
}
