using AwesomeAssertions;
using Lingo.Core.Documents;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using Lingo.Core.Sync;
using Lingo.Core.Test.Infrastructure;
using System.IO;
using System.Linq;
using System.Text;
using Unit = Lingo.Core.Models.Unit;

namespace Lingo.Core.Test.Sync;

[TestFixture]
public class KeySynchronizerTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(KeySynchronizerTests).Assembly;
        var fullResourceName = $"Lingo.Core.Test.Xliff.TestData.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            var names = string.Join(", ", assembly.GetManifestResourceNames());
            throw new FileNotFoundException($"Resource {fullResourceName} not found. Available resources: {names}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Test]
    public void DetectMissingKeys_FindsKeysMissingInTargetDocuments()
    {
        // Arrange
        var docEn = new InMemoryLingoDocument();
        docEn.AddUnit(new Unit { Id = "Key1", Source = "Hello" });
        docEn.AddUnit(new Unit { Id = "Key2", Source = "World" });

        var docDe = new InMemoryLingoDocument();
        docDe.AddUnit(new Unit { Id = "Key1", Target = "Hallo" });

        var docFr = new InMemoryLingoDocument();
        docFr.AddUnit(new Unit { Id = "Key3", Source = "Bonjour" });

        var synchronizer = new KeySynchronizer();

        // Act
        var missingMap = synchronizer.DetectMissingKeys(new[] { docEn, docDe, docFr });

        // Assert
        missingMap.Should().ContainKey("Key2");
        missingMap["Key2"].Should().Contain(docDe);
        missingMap["Key2"].Should().Contain(docFr);

        missingMap.Should().ContainKey("Key1");
        missingMap["Key1"].Should().Contain(docFr);

        missingMap.Should().ContainKey("Key3");
        missingMap["Key3"].Should().Contain(docEn);
        missingMap["Key3"].Should().Contain(docDe);
    }

    [Test]
    public void SyncKeyToTargets_AddsMissingKeyToTargets()
    {
        // Arrange
        var docEn = new InMemoryLingoDocument();
        var newUnit = new Unit { Id = "A.B.NewKey", Source = "Save File", Target = "Save File" };
        docEn.AddUnit(newUnit);

        var docDe = new InMemoryLingoDocument();
        var docFr = new InMemoryLingoDocument();

        var synchronizer = new KeySynchronizer();

        // Act
        var updated = synchronizer.SyncKeyToTargets(newUnit, new[] { docDe, docFr });

        // Assert
        updated.Should().HaveCount(2);
        docDe.GetUnitIds().Should().Contain("A.B.NewKey");
        docFr.GetUnitIds().Should().Contain("A.B.NewKey");

        var deUnit = docDe.GetUnit("A.B.NewKey");
        deUnit.Should().NotBeNull();
        deUnit!.Source.Should().Be("Save File");
        deUnit.State.Should().Be(TranslationState.NeedsTranslation);
    }

    [Test]
    public void SyncMissingKeysAcrossDocuments_WorksWithXliffDocuments()
    {
        // Arrange: XLIFF 1.2 document and XLIFF 2.0 document from test resources
        var factory = new XliffDocumentFactory();

        var content12 = GetResourceContent("test12.xlf");
        var doc12 = factory.Create(new MemoryStream(Encoding.UTF8.GetBytes(content12)));

        var content20 = GetResourceContent("test20.xlf");
        var doc20 = factory.Create(new MemoryStream(Encoding.UTF8.GetBytes(content20)));

        // Add a unique key into doc12
        var uniqueUnit = new Unit { Id = "Custom.Settings.Volume", Source = "Volume Level", Target = "Lautstärke" };
        doc12.SyncUnit(uniqueUnit);

        var synchronizer = new KeySynchronizer();

        // Act
        var results = synchronizer.SyncMissingKeysAcrossDocuments(new[] { doc12, doc20 });

        // Assert
        results.Should().ContainKey("Custom.Settings.Volume");
        results["Custom.Settings.Volume"].Should().Contain(doc20);

        doc20.GetUnitIds().Should().Contain("Custom.Settings.Volume");
        var syncedUnit = doc20.GetUnit("Custom.Settings.Volume");
        syncedUnit.Should().NotBeNull();
        syncedUnit!.Source.Should().Be("Volume Level");
        syncedUnit.State.Should().Be(TranslationState.NeedsTranslation);
    }

    [Test]
    public void SyncKeyInSiblingGroup_AnchorsFromSourceDocument_AndSyncsToTargets()
    {
        var docSource = new InMemoryLingoDocument();
        docSource.AddUnit(new Unit { Id = "app.header", Source = "Application Header", Target = "Application Header" });

        var docDe = new InMemoryLingoDocument();
        var docFr = new InMemoryLingoDocument();

        var synchronizer = new KeySynchronizer();

        var updated = synchronizer.SyncKeyInSiblingGroup("app.header", docSource, new[] { docDe, docFr });

        updated.Should().HaveCount(2);
        docDe.GetUnitIds().Should().Contain("app.header");
        docFr.GetUnitIds().Should().Contain("app.header");

        var deUnit = docDe.GetUnit("app.header");
        deUnit!.Source.Should().Be("Application Header");
        deUnit.State.Should().Be(TranslationState.NeedsTranslation);
    }

    [Test]
    public void SyncKeyInSiblingGroup_WhenKeyMissingInSource_DeletesKeyFromAllSiblingFiles()
    {
        var docSource = new InMemoryLingoDocument();
        // docSource does NOT have "app.orphaned"
        var docDe = new InMemoryLingoDocument();
        docDe.AddUnit(new Unit { Id = "app.orphaned", Source = "Master Footer", Target = "Fußzeile" });
        var docFr = new InMemoryLingoDocument();
        docFr.AddUnit(new Unit { Id = "app.orphaned", Source = "Master Footer", Target = "Pied de page" });

        var synchronizer = new KeySynchronizer();

        var updated = synchronizer.SyncKeyInSiblingGroup("app.orphaned", docSource, new[] { docDe, docFr });

        // Since docSource is the source of truth and lacks "app.orphaned", it must be deleted from de and fr
        updated.Should().HaveCount(2);
        docDe.GetUnitIds().Should().NotContain("app.orphaned");
        docFr.GetUnitIds().Should().NotContain("app.orphaned");
        docSource.GetUnitIds().Should().NotContain("app.orphaned");
    }

    [Test]
    public void SyncMissingKeysInSiblingGroup_PropagatesSourceKeys_AndDeletesOrphanedKeysInTargets()
    {
        var docSource = new InMemoryLingoDocument();
        docSource.AddUnit(new Unit { Id = "app.valid", Source = "Valid Text", Target = "Valid Text" });

        var docDe = new InMemoryLingoDocument();
        docDe.AddUnit(new Unit { Id = "app.orphaned", Source = "Old", Target = "Alt" });

        var docFr = new InMemoryLingoDocument();
        docFr.AddUnit(new Unit { Id = "app.valid", Source = "Valid Text", Target = "Texte Valide" });
        docFr.AddUnit(new Unit { Id = "app.orphaned", Source = "Old", Target = "Vieux" });

        var synchronizer = new KeySynchronizer();

        var results = synchronizer.SyncMissingKeysInSiblingGroup(docSource, new[] { docDe, docFr });

        // app.valid should be added to docDe
        results.Should().ContainKey("app.valid");
        results["app.valid"].Should().Contain(docDe);
        docDe.GetUnitIds().Should().Contain("app.valid");

        // app.orphaned should be deleted from docDe and docFr
        results.Should().ContainKey("app.orphaned");
        results["app.orphaned"].Should().Contain(docDe);
        results["app.orphaned"].Should().Contain(docFr);

        docDe.GetUnitIds().Should().NotContain("app.orphaned");
        docFr.GetUnitIds().Should().NotContain("app.orphaned");
    }

    [Test]
    public void AddKeyToSiblingGroup_AddsToSourceAndPropagatesToTargets()
    {
        var docSource = new InMemoryLingoDocument();
        var docDe = new InMemoryLingoDocument();
        var docFr = new InMemoryLingoDocument();

        var synchronizer = new KeySynchronizer();

        var updated = synchronizer.AddKeyToSiblingGroup("app.newKey", "Hello World", docSource, new[] { docDe, docFr });

        updated.Should().HaveCount(3);
        docSource.GetUnitIds().Should().Contain("app.newKey");
        docDe.GetUnitIds().Should().Contain("app.newKey");
        docFr.GetUnitIds().Should().Contain("app.newKey");

        docSource.GetUnit("app.newKey")!.Source.Should().Be("Hello World");
        docDe.GetUnit("app.newKey")!.State.Should().Be(TranslationState.NeedsTranslation);
    }

    [Test]
    public void HasKeyDifferencesInSiblingGroup_DetectsPresenceAndSourceValueDifferences()
    {
        var docSource = new InMemoryLingoDocument();
        docSource.AddUnit(new Unit { Id = "app.title", Source = "Updated App Title", Target = "Updated App Title" });
        docSource.AddUnit(new Unit { Id = "app.save", Source = "Save File", Target = "Save File" });
        docSource.AddUnit(new Unit { Id = "app.new", Source = "New Document", Target = "New Document" });

        var docDe = new InMemoryLingoDocument();
        docDe.AddUnit(new Unit { Id = "app.title", Source = "Old App Title", Target = "App Titel" }); // Different source value
        docDe.AddUnit(new Unit { Id = "app.save", Source = "Save File", Target = "Datei speichern" }); // Identical source value
        docDe.AddUnit(new Unit { Id = "app.orphan", Source = "Orphan", Target = "Waise" }); // Missing in source

        var synchronizer = new KeySynchronizer();

        // 1. Difference when source value changed
        synchronizer.HasKeyDifferencesInSiblingGroup("app.title", docSource, new[] { docDe })
            .Should().BeTrue();

        // 2. Difference when key is missing in target
        synchronizer.HasKeyDifferencesInSiblingGroup("app.new", docSource, new[] { docDe })
            .Should().BeTrue();

        // 3. Difference when key is missing in source (orphan)
        synchronizer.HasKeyDifferencesInSiblingGroup("app.orphan", docSource, new[] { docDe })
            .Should().BeTrue();

        // 4. No difference when key exists in both and source values match
        synchronizer.HasKeyDifferencesInSiblingGroup("app.save", docSource, new[] { docDe })
            .Should().BeFalse();

        // 5. No difference when key does not exist anywhere
        synchronizer.HasKeyDifferencesInSiblingGroup("non.existent", docSource, new[] { docDe })
            .Should().BeFalse();
    }

    [Test]
    public void SyncKeyInSiblingGroup_WhenSourceValueChanged_UpdatesSourceAndSetsNeedsAdaptation()
    {
        var docSource = new InMemoryLingoDocument();
        docSource.AddUnit(new Unit { Id = "app.welcome", Source = "Welcome to Lingo!", Target = "Welcome to Lingo!" });

        var docDe = new InMemoryLingoDocument();
        docDe.AddUnit(new Unit { Id = "app.welcome", Source = "Welcome!", Target = "Willkommen!", State = TranslationState.Translated });

        var synchronizer = new KeySynchronizer();

        var updated = synchronizer.SyncKeyInSiblingGroup("app.welcome", docSource, new[] { docDe });

        updated.Should().Contain(docDe);
        var deUnit = docDe.GetUnit("app.welcome");
        deUnit!.Source.Should().Be("Welcome to Lingo!");
        deUnit.Target.Should().Be("Willkommen!");
        deUnit.State.Should().Be(TranslationState.NeedsAdaptation);
    }

    [Test]
    public void SyncKeyToTargets_UpdatesSourceValueWhenChanged()
    {
        var docDe = new InMemoryLingoDocument();
        docDe.AddUnit(new Unit { Id = "app.header", Source = "Old Header", Target = "Kopfzeile", State = TranslationState.Translated });

        var synchronizer = new KeySynchronizer();
        var updatedUnit = new Unit { Id = "app.header", Source = "New Header" };

        var updated = synchronizer.SyncKeyToTargets(updatedUnit, new[] { docDe });

        updated.Should().Contain(docDe);
        var deUnit = docDe.GetUnit("app.header");
        deUnit!.Source.Should().Be("New Header");
        deUnit.State.Should().Be(TranslationState.NeedsAdaptation);
    }

    [Test]
    public void ImportKey_And_MergeKey_FunctionConsistentlyWithILingoDocument()
    {
        var sourceDoc = new InMemoryLingoDocument();
        sourceDoc.AddUnit(new Unit { Id = "app.title", Source = "Title", Target = "Titel" });

        var targetDoc = new InMemoryLingoDocument();
        targetDoc.AddUnit(new Unit { Id = "app.title", Source = "Title", Target = "Alter Titel" });

        var synchronizer = new KeySynchronizer();

        // Import updates existing unit
        var importRes = synchronizer.ImportKey("app.title", sourceDoc, targetDoc);
        importRes.Should().Be(ImportResult.Imported);
        targetDoc.GetUnit("app.title")!.Target.Should().Be("Titel");

        // Merge conflicts when unit already exists
        var mergeRes = synchronizer.MergeKey("app.title", sourceDoc, targetDoc);
        mergeRes.Should().Be(MergeResult.Conflict);

        // Merge succeeds when unit does not exist
        var newTarget = new InMemoryLingoDocument();
        var mergeNewRes = synchronizer.MergeKey("app.title", sourceDoc, newTarget);
        mergeNewRes.Should().Be(MergeResult.Merged);
        newTarget.GetUnit("app.title")!.Target.Should().Be("Titel");
    }

    [Test]
    public void DocumentSynchronizer_And_KeySynchronizer_ProduceIdenticalState()
    {
        // Setup dataset for DocumentSynchronizer
        var docSourceA = new InMemoryLingoDocument();
        docSourceA.AddUnit(new Unit { Id = "key.keep", Source = "Keep Source" });
        docSourceA.AddUnit(new Unit { Id = "key.changed", Source = "New Source" });
        docSourceA.AddUnit(new Unit { Id = "key.added", Source = "Added Source" });

        var docTargetA = new InMemoryLingoDocument();
        docTargetA.AddUnit(new Unit { Id = "key.keep", Source = "Keep Source", Target = "Behalten", State = TranslationState.Translated });
        docTargetA.AddUnit(new Unit { Id = "key.changed", Source = "Old Source", Target = "Geändert", State = TranslationState.Translated });
        docTargetA.AddUnit(new Unit { Id = "key.orphan", Source = "Orphan Source", Target = "Waise", State = TranslationState.Translated });

        var docSync = new DocumentSynchronizer();
        docSync.PushSync(docSourceA, docTargetA);

        // Setup identical dataset for KeySynchronizer
        var docSourceB = new InMemoryLingoDocument();
        docSourceB.AddUnit(new Unit { Id = "key.keep", Source = "Keep Source" });
        docSourceB.AddUnit(new Unit { Id = "key.changed", Source = "New Source" });
        docSourceB.AddUnit(new Unit { Id = "key.added", Source = "Added Source" });

        var docTargetB = new InMemoryLingoDocument();
        docTargetB.AddUnit(new Unit { Id = "key.keep", Source = "Keep Source", Target = "Behalten", State = TranslationState.Translated });
        docTargetB.AddUnit(new Unit { Id = "key.changed", Source = "Old Source", Target = "Geändert", State = TranslationState.Translated });
        docTargetB.AddUnit(new Unit { Id = "key.orphan", Source = "Orphan Source", Target = "Waise", State = TranslationState.Translated });

        var keySync = new KeySynchronizer();
        keySync.SyncMissingKeysInSiblingGroup(docSourceB, new[] { docTargetB });

        // Assert identical state
        docTargetB.GetUnitIds().Should().BeEquivalentTo(docTargetA.GetUnitIds());
        foreach (var id in docTargetA.GetUnitIds())
        {
            var unitA = docTargetA.GetUnit(id)!;
            var unitB = docTargetB.GetUnit(id)!;

            unitB.Source.Should().Be(unitA.Source);
            unitB.Target.Should().Be(unitA.Target);
            unitB.State.Should().Be(unitA.State);
        }
    }

    [Test]
    public void DocumentSynchronizer_And_KeySynchronizer_ProduceIdenticalState_OnXliff()
    {
        var factory = new XliffDocumentFactory();

        var sourceContent = GetResourceContent("SyncTest.data12.xlf");
        var targetContentA = GetResourceContent("SyncTest.data12_de_de.xlf");
        var targetContentB = GetResourceContent("SyncTest.data12_de_de.xlf");

        var sourceDocA = factory.Create(sourceContent);
        var targetDocA = factory.Create(targetContentA);

        var sourceDocB = factory.Create(sourceContent);
        var targetDocB = factory.Create(targetContentB);

        var docSync = new DocumentSynchronizer();
        docSync.PushSync(sourceDocA, targetDocA);

        var keySync = new KeySynchronizer();
        keySync.SyncMissingKeysInSiblingGroup(sourceDocB, new[] { targetDocB });

        targetDocB.GetUnitIds().Should().BeEquivalentTo(targetDocA.GetUnitIds());
        foreach (var id in targetDocA.GetUnitIds())
        {
            var unitA = targetDocA.GetUnit(id)!;
            var unitB = targetDocB.GetUnit(id)!;

            unitB.Source.Should().Be(unitA.Source);
            unitB.Target.Should().Be(unitA.Target);
            unitB.State.Should().Be(unitA.State);
        }
    }
}
