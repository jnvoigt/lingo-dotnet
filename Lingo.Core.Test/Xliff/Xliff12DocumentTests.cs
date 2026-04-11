using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.Text;

namespace Lingo.Core.Test.Xliff;

public class Xliff12DocumentTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff12DocumentTests).Assembly;
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
    public void SyncUnit_AddingNewUnit_ShouldReturnNewUnitCreated()
    {
        // Arrange
        var originalContent = GetResourceContent("test12.xlf");
        var factory = new XliffDocumentFactory();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);
        var newUnit = new Unit { Id = "new_unit_id", Target = "New Content", Source = "New Content" };
        
        // Act
        var result = document.SyncUnit(newUnit);

        // Assert
        result.Should().Be(SyncResult.NewUnitCreated);
        document.GetUnitIds().Should().Contain("new_unit_id");
        document.GetValue("new_unit_id").Should().Be("New Content");
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
        var updatedUnit = new Unit { Id = existingUnitId, Target = "Updated Content", Source = "Updated Content" };

        // Act
        var result = document.SyncUnit(updatedUnit);

        // Assert
        result.Should().Be(SyncResult.SourceValueHasChanged);
        document.GetValue(existingUnitId).Should().Be("Updated Content");
    }
}