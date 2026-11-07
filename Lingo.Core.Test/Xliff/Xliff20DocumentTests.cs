using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Lingo.Core.Test.Xliff;

public class Xliff20DocumentTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff20DocumentTests).Assembly;
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
        var originalContent = GetResourceContent("test20.xlf");
        var factory = new XliffDocumentFactory();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);
        var newUnit = new Unit { Id = "new_unit_id", Target = "New Content", Source = "New Content" };

        // Act
        var result = document.SyncUnit(newUnit);

        // Assert
        result.Should().Be(SyncResult.NewUnitCreated);
        var u1 = document.GetUnit("new_unit_id");
        u1.State.Should().Be(TranslationState.Translated);
        u1.Target.Should().Be("New Content");
        u1.Source.Should().Be("New Content");
    }

    [Test]
    public void SyncUnit_OverridingExistingUnitSource_ShouldReturnSourceValueHasChanged()
    {
        // Arrange
        var originalContent = GetResourceContent("test20.xlf");
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
        var content = GetResourceContent("test20.xlf");
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

    [Test]
    public void SortByKey_ShouldSortAlphabetically()
    {
        // Arrange
        var factory = new XliffDocumentFactory();
        var doc = factory.Create("xliff-2.0", new[]
        {
            new Unit { Id = "z", Source = "z" },
            new Unit { Id = "a", Source = "a" },
            new Unit { Id = "m", Source = "m" }
        });

        // Act
        doc.SortByKey();

        // Assert
        var unitIds = doc.GetAllUnits().Select(u => u.Id).ToList();
        unitIds.Should().ContainInOrder("a", "m", "z");
    }

    [Test]
    public void RemoveUnit_RemovesUnitFromDocument()
    {
        var factory = new XliffDocumentFactory();
        var doc = factory.Create("xliff-2.0", new[]
        {
            new Unit { Id = "key1", Source = "Text 1" },
            new Unit { Id = "key2", Source = "Text 2" }
        });

        doc.GetUnitIds().Should().Contain("key1");
        var removed = doc.RemoveUnit("key1");

        removed.Should().BeTrue();
        doc.GetUnitIds().Should().NotContain("key1");
        doc.GetUnit("key1").Should().BeNull();
    }

    [Test]
    public void RenameUnit_ChangesUnitKey_PreservingSourceAndTargetTexts()
    {
        var factory = new XliffDocumentFactory();
        var doc = factory.Create("xliff-2.0", new[]
        {
            new Unit { Id = "old.key", Source = "Source Text 20", Target = "Target Text 20" }
        });

        var renamed = doc.RenameUnit("old.key", "new.renamed.key20");

        renamed.Should().BeTrue();
        doc.GetUnitIds().Should().NotContain("old.key");
        doc.GetUnitIds().Should().Contain("new.renamed.key20");

        var newUnit = doc.GetUnit("new.renamed.key20");
        newUnit.Should().NotBeNull();
        newUnit!.Source.Should().Be("Source Text 20");
        newUnit.Target.Should().Be("Target Text 20");
    }
}