using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.Text;

namespace Lingo.Core.Test.Xliff;

public class Xliff20ReadOnlyTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff20ReadOnlyTests).Assembly;
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
    public void WriteOperations_ShouldThrowNotImplementedException()
    {
        // Arrange
        var content = GetResourceContent("test20.xlf");
        var factory = new XliffDocumentFactory();
        var document = factory.Create(content);
        var unit = new Unit { Id = "test", Target = "test", Source = "test" };

        // Act & Assert
        document.Invoking(d => d.SetValue("id", "value")).Should().Throw<NotImplementedException>();
        document.Invoking(d => d.SyncUnit(unit)).Should().Throw<NotImplementedException>();
        document.Invoking(d => d.MergeUnit(unit)).Should().Throw<NotImplementedException>();
        document.Invoking(d => d.ImportUnit(unit)).Should().Throw<NotImplementedException>();
        document.Invoking(d => d.RetainUnitIds(new[] { "id" })).Should().Throw<NotImplementedException>();
        document.Invoking(d => d.SortByKey()).Should().Throw<NotImplementedException>();
    }
}
