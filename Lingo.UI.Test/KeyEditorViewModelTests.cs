using System.Collections.Generic;
using System.Globalization;
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
}
