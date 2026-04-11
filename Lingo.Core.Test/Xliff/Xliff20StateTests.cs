using AwesomeAssertions;
using Lingo.Core.Documents;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.IO;
using System.Linq;

namespace Lingo.Core.Test.Xliff;

public class Xliff20StateTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff20StateTests).Assembly;
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
    public void GetTargetState_ShouldMapXliff20StatesCorrectly()
    {
        // Arrange
        var content = GetResourceContent("state20.xlf");
        var factory = new XliffDocumentFactory();
        var doc = factory.Create(content) as IHasTranslationState;

        // Act & Assert
        doc!.GetTargetState("u1").Should().Be(TranslationState.NeedsTranslation);
        doc.GetTargetState("u2").Should().Be(TranslationState.Translated);
        doc.GetTargetState("u3").Should().Be(TranslationState.Translated);
        doc.GetTargetState("u4").Should().Be(TranslationState.Translated);
        doc.GetTargetState("u5").Should().Be(TranslationState.NeedsAdaptation);

        var units = (doc as ILingoDocument)!.GetAllUnits().ToList();
        units.First(u => u.Id == "u5").State.Should().Be(TranslationState.NeedsAdaptation);
    }
}