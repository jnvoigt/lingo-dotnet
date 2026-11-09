using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Lingo.Core.Hierarchy;
using Lingo.Core.Models;
using Lingo.Core.Query;
using Lingo.Core.Test.Infrastructure;

namespace Lingo.Core.Test.Query;

[TestFixture]
public class KeyQueryServiceTests
{
    [Test]
    public void GetUntranslatedKeys_IdentifiesMissingEmptyAndNeedsTranslationUnits()
    {
        // Arrange
        var docEn = new InMemoryLingoDocument { FilePath = "/workspace/App.en-US.xlf", TargetCulture = new CultureInfo("en-US") };
        docEn.AddUnit(new Unit { Id = "App.Title", Source = "Lingo App", Target = "Lingo App", State = TranslationState.Translated });
        docEn.AddUnit(new Unit { Id = "App.Untranslated", Source = "Untranslated String", Target = "Untranslated String", State = TranslationState.Translated });
        docEn.AddUnit(new Unit { Id = "App.EmptyTarget", Source = "Empty String", Target = "Empty String", State = TranslationState.Translated });
        docEn.AddUnit(new Unit { Id = "App.Missing", Source = "Missing in DE", Target = "Missing in DE", State = TranslationState.Translated });

        var docDe = new InMemoryLingoDocument { FilePath = "/workspace/App.de-DE.xlf", TargetCulture = new CultureInfo("de-DE") };
        docDe.AddUnit(new Unit { Id = "App.Title", Source = "Lingo App", Target = "Lingo Anwendung", State = TranslationState.Translated });
        docDe.AddUnit(new Unit { Id = "App.Untranslated", Source = "Untranslated String", Target = "Untranslated String", State = TranslationState.NeedsTranslation });
        docDe.AddUnit(new Unit { Id = "App.EmptyTarget", Source = "Empty String", Target = "", State = TranslationState.Translated });
        // App.Missing is omitted in docDe

        var service = new KeyQueryService();

        // Act
        var result = service.GetUntranslatedKeys(new[] { docEn, docDe }, "de-DE");

        // Assert
        var matchedKeys = result.Select(m => m.Key).Distinct().ToList();
        matchedKeys.Should().Contain("App.Untranslated");
        matchedKeys.Should().Contain("App.EmptyTarget");
        matchedKeys.Should().Contain("App.Missing");
        matchedKeys.Should().NotContain("App.Title");

        var missingMatch = result.First(m => m.Key == "App.Missing");
        missingMatch.IsMissing.Should().BeTrue();
        missingMatch.MatchReasons.Should().HaveFlag(KeyQueryMatchReason.MissingInLocale);
        missingMatch.MatchReasons.Should().HaveFlag(KeyQueryMatchReason.Untranslated);
    }

    [Test]
    public void GetOutdatedTranslations_FindsUnitsRequiringAdaptation()
    {
        // Arrange
        var docDe = new InMemoryLingoDocument { FilePath = "/workspace/App.de-DE.xlf" };
        docDe.AddUnit(new Unit { Id = "App.Welcome", Source = "Welcome back!", Target = "Willkommen!", State = TranslationState.NeedsAdaptation });
        docDe.AddUnit(new Unit { Id = "App.Save", Source = "Save", Target = "Speichern", State = TranslationState.Translated });
        docDe.AddUnit(new Unit { Id = "App.Cancel", Source = "Cancel", Target = null, State = TranslationState.NeedsTranslation });

        var service = new KeyQueryService();

        // Act
        var outdatedMatches = service.GetOutdatedTranslations(new[] { docDe });

        // Assert
        outdatedMatches.Should().HaveCount(1);
        outdatedMatches[0].Key.Should().Be("App.Welcome");
        outdatedMatches[0].IsOutdated.Should().BeTrue();
        outdatedMatches[0].MatchReasons.Should().HaveFlag(KeyQueryMatchReason.Outdated);
    }

    [Test]
    public void Query_NeedsReview_FindsUnitsRequiringReview()
    {
        // Arrange
        var docDe = new InMemoryLingoDocument { FilePath = "/workspace/App.de-DE.xlf" };
        docDe.AddUnit(new Unit { Id = "App.Welcome", Source = "Welcome back!", Target = "Willkommen!", State = TranslationState.NeedsReview });
        docDe.AddUnit(new Unit { Id = "App.Save", Source = "Save", Target = "Speichern", State = TranslationState.Translated });
        docDe.AddUnit(new Unit { Id = "App.Cancel", Source = "Cancel", Target = null, State = TranslationState.NeedsTranslation });

        var service = new KeyQueryService();

        // Act
        var result = service.Query(docDe, KeyQueryCriteria.NeedsReview());

        // Assert
        result.TotalDistinctKeys.Should().Be(1);
        result.MatchingKeys.Should().Contain("App.Welcome");
    }

    [Test]
    public void Query_WithKeyPattern_FiltersCorrectly()
    {
        // Arrange
        var doc = new InMemoryLingoDocument { FilePath = "/workspace/App.en.xlf" };
        doc.AddUnit(new Unit { Id = "UI.Dialog.Confirm", Source = "Confirm" });
        doc.AddUnit(new Unit { Id = "UI.Dialog.Cancel", Source = "Cancel" });
        doc.AddUnit(new Unit { Id = "Data.User.Name", Source = "User Name" });

        var service = new KeyQueryService();

        // Act
        var result = service.Query(doc, KeyQueryCriteria.WithPattern("UI.Dialog.*"));

        // Assert
        result.TotalDistinctKeys.Should().Be(2);
        result.MatchingKeys.Should().Contain("UI.Dialog.Confirm");
        result.MatchingKeys.Should().Contain("UI.Dialog.Cancel");
        result.MatchingKeys.Should().NotContain("Data.User.Name");
    }

    [Test]
    public void Query_WithSearchText_SearchesAcrossSourceAndTarget()
    {
        // Arrange
        var doc = new InMemoryLingoDocument { FilePath = "/workspace/App.de.xlf" };
        doc.AddUnit(new Unit { Id = "K1", Source = "Export Settings", Target = "Einstellungen exportieren" });
        doc.AddUnit(new Unit { Id = "K2", Source = "Import Settings", Target = "Einstellungen importieren" });
        doc.AddUnit(new Unit { Id = "K3", Source = "Delete", Target = "Löschen" });

        var service = new KeyQueryService();

        // Act
        var result = service.Query(doc, KeyQueryCriteria.Search("export"));

        // Assert
        result.TotalDistinctKeys.Should().Be(1);
        result.MatchingKeys.Should().Contain("K1");
        result.MatchingKeys.Should().NotContain("K2");
        result.MatchingKeys.Should().NotContain("K3");
    }

    [Test]
    public void FilterHierarchy_PrunesNonMatchingBranchesWhilePreservingMatchingPaths()
    {
        // Arrange
        var builder = new KeyHierarchyBuilder('.');
        var keys = new[] { "UI.Buttons.Save", "UI.Buttons.Cancel", "Data.User.Profile" };
        var rootNodes = builder.Build(keys);

        var doc = new InMemoryLingoDocument { FilePath = "/workspace/App.de.xlf" };
        doc.AddUnit(new Unit { Id = "UI.Buttons.Save", Target = "Speichern", State = TranslationState.Translated });
        doc.AddUnit(new Unit { Id = "UI.Buttons.Cancel", Target = "", State = TranslationState.NeedsTranslation });
        doc.AddUnit(new Unit { Id = "Data.User.Profile", Target = "Profil", State = TranslationState.Translated });

        var service = new KeyQueryService();

        // Act: Filter hierarchy for untranslated keys only
        var filteredRoots = service.FilterHierarchy(rootNodes, KeyQueryCriteria.Untranslated(), new[] { doc });

        // Assert
        filteredRoots.Should().HaveCount(1);
        filteredRoots[0].Name.Should().Be("UI");
        filteredRoots[0].Children.Should().HaveCount(1);
        filteredRoots[0].Children[0].Name.Should().Be("Buttons");
        filteredRoots[0].Children[0].Children.Should().HaveCount(1);
        filteredRoots[0].Children[0].Children[0].FullPath.Should().Be("UI.Buttons.Cancel");
    }
}
