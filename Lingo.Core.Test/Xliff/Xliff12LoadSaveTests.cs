using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using System.Text;
using System.Xml.Linq;

namespace Lingo.Core.Test.Xliff;

public class Xliff12LoadSaveTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff12LoadSaveTests).Assembly;
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
    public void LoadAndSave_Xliff12_ShouldBeEquivalent()
    {
        // Arrange
        var originalContent = GetResourceContent("test12.xlf");
        var factory = new XliffDocumentFactory();
        var writer = new XliffDocumentWriter();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var document = factory.Create(stream);

        using var outputStream = new MemoryStream();
        writer.Write(outputStream, document);
        outputStream.Position = 0;
        var savedContent = new StreamReader(outputStream).ReadToEnd();

        // Assert
        var originalXml = XDocument.Parse(originalContent);
        var savedXml = XDocument.Parse(savedContent);

        savedXml.Root!.Name.Should().Be(originalXml.Root!.Name);
        savedXml.Root!.Attribute("version")?.Value.Should()
            .Be(originalXml.Root!.Attribute("version")?.Value);

        var originalUnits = originalXml.Descendants(originalXml.Root!.Name.Namespace + "trans-unit").ToList();
        var savedUnits = savedXml.Descendants(savedXml.Root!.Name.Namespace + "trans-unit").ToList();
        savedUnits.Count.Should().Be(originalUnits.Count, "Should have same number of trans-units");

        foreach (var originalUnit in originalUnits)
        {
            var id = originalUnit.Attribute("id")?.Value;
            var savedUnit = savedUnits.FirstOrDefault(u => u.Attribute("id")?.Value == id);
            savedUnit.Should().NotBeNull($"Missing unit with id {id}");

            var originalSource = originalUnit.Element(originalXml.Root!.Name.Namespace + "source")?.Value;
            var savedSource = savedUnit!.Element(savedXml.Root!.Name.Namespace + "source")?.Value;
            savedSource.Should().Be(originalSource, $"Source mismatch for unit {id}");
        }
    }
}