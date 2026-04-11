using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.IO;
using System.Linq;
using System.Text;

namespace Lingo.Core.Test.Xliff;

public class Xliff12DocumentTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff12DocumentTests).Assembly;
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
    public void SyncUnit_AddingNewUnit_ShouldReturnNewUnitCreated()
    {
        // Arrange
        var originalContent = GetResourceContent("test12.xlf");
        var factory = new XliffDocumentFactory();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);
        var newUnit = new Unit { Id = "new_unit_id", Source = "New Content" };

        // Act
        var result = document.SyncUnit(newUnit);

        // Assert
        result.Should().Be(SyncResult.NewUnitCreated);
        var u1 = document.GetUnit("new_unit_id");
        u1.State.Should().Be(TranslationState.NeedsTranslation);
        u1.Target.Should().BeNull();
        u1.Source.Should().Be("New Content");
    }

    [Test]
    public void SyncUnit_OverridingExistingUnitSource_ShouldReturnSourceValueHasChanged()
    {
        // Arrange
        var originalContent = GetResourceContent("test12.xlf");
        var factory = new XliffDocumentFactory();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);
        var existingUnitId = document.GetUnitIds().First();

        var existingUnit = document.GetUnit(existingUnitId);
        var existingTargetValue = existingUnit.Target;

        var updatedUnit = new Unit { Id = existingUnitId, Target = "Updated Content", Source = "Updated Content" };

        // Act
        var result = document.SyncUnit(updatedUnit);

        // Assert
        result.Should().Be(SyncResult.SourceValueHasChanged);
        var unit = document.GetUnit(existingUnitId);
        unit.Source.Should().Be("Updated Content");
        unit.Target.Should().Be(existingTargetValue);
    }

    [Test]
    public void GetUnit_ShouldReturnCorrectUnit()
    {
        // Arrange
        var content = GetResourceContent("test12.xlf");
        var factory = new XliffDocumentFactory();
        var document = factory.Create(content);
        var unitId = "sample.textA";

        // Act
        var unit = document.GetUnit(unitId);

        // Assert
        unit.Should().NotBeNull();
        unit!.Id.Should().Be(unitId);
        unit.Source.Should().Be("This is text A");
        unit.Target.Should().BeNull();
    }
}