using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Formats.Xliff.v12;
using Lingo.Core.Models;

namespace Lingo.Core.Test.Xliff;

public class XliffSyncDataSetTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(XliffSyncDataSetTests).Assembly;
        var fullResourceName = $"Lingo.Core.Test.{resourceName}";
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
    public void Sync_Xliff12_ComplexSync()
    {
        // Arrange
        var sourceContent = GetResourceContent("data12.xlf");
        var targetContent = GetResourceContent("data12_de_de.xlf");
        var factory = new XliffDocumentFactory();

        var sourceDoc = factory.Create(sourceContent);
        var targetDoc = factory.Create(targetContent) as Xliff12Document;

        var sourceUnits = sourceDoc.GetAllUnits().ToList();

        // Act
        var newUnits = 0;
        var changedUnits = 0;
        foreach (var unit in sourceUnits)
        {
            var result = targetDoc.SyncUnit(unit);
            if (result == SyncResult.NewUnitCreated)
            {
                newUnits++;
            }

            if (result == SyncResult.SourceValueHasChanged)
            {
                changedUnits++;
            }
        }

        var removed = targetDoc.RetainUnitIds(sourceUnits.Select(u => u.Id)).ToList();

        // Assert
        newUnits.Should().Be(1); // sample.text.new
        changedUnits.Should().Be(1); // sample.text.changed
        removed.Should().Contain("sample.text.removed");

        targetDoc.GetUnitIds().Should().NotContain("sample.text.removed");
        targetDoc.GetUnitIds().Should().Contain("sample.text.new");
        targetDoc.GetUnitIds().Should().Contain("sample.text.changed");
        targetDoc.GetUnitIds().Should().Contain("sample.text.that-has-not-changed");

        targetDoc.GetTargetState("sample.text.changed").Should()
            .Be(TranslationState.NeedsAdaptation);

        targetDoc.GetTargetState("sample.text.new").Should()
            .Be(TranslationState.NeedsTranslation);
    }

    [Test]
    public void Read_Xliff20_DataSet()
    {
        // Arrange
        var content = GetResourceContent("data20_de_de.xlf");
        var factory = new XliffDocumentFactory();

        // Act
        var doc = factory.Create(content);
        var units = doc.GetAllUnits().ToList();

        // Assert
        units.Should().HaveCount(3);
        units.Should().Contain(u => u.Id == "sample.text.that-has-not-changed" && u.Target == "Dies is Text A");
        units.Should().Contain(u =>
            u.Id == "sample.text.removed" && u.Target == "Dies ist Text B, der aus der Quelle entfernt wurde");
        units.Should().Contain(u => u.Id == "sample.text.changed" && u.Target == "Dies ist Text C mit altem Inhalt");
    }
}