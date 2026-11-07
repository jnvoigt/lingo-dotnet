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
}
