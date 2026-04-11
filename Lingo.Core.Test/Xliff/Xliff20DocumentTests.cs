using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.Text;

namespace Lingo.Core.Test.Xliff;

public class Xliff20DocumentTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff20DocumentTests).Assembly;
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
    public void SyncUnit_AddingNewUnit_ShouldThrowNotImplementedException()
    {
        // Arrange
        var originalContent = GetResourceContent("test20.xlf");
        var factory = new XliffDocumentFactory();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);
        var newUnit = new Unit { Id = "new_unit_id", Target = "New Content", Source = "New Content" };

        // Act
        Action act = () => document.SyncUnit(newUnit);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Test]
    public void SyncUnit_OverridingExistingUnitSource_ShouldThrowNotImplementedException()
    {
        // Arrange
        var originalContent = GetResourceContent("test20.xlf");
        var factory = new XliffDocumentFactory();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);
        var existingUnitId = document.GetUnitIds().First();
        var updatedUnit = new Unit { Id = existingUnitId, Target = "Updated Content", Source = "Updated Content" };

        // Act
        Action act = () => document.SyncUnit(updatedUnit);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }
}