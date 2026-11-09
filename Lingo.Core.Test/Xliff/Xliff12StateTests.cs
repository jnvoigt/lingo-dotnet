using AwesomeAssertions;
using Lingo.Core.Documents;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Models;
using System.IO;
using System.Linq;

namespace Lingo.Core.Test.Xliff;

public class Xliff12StateTests
{
    private string GetResourceContent(string resourceName)
    {
        var assembly = typeof(Xliff12StateTests).Assembly;
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
    public void GetTargetState_ShouldMapXliff12StatesCorrectly()
    {
        // Arrange
        var content = GetResourceContent("state12.xlf");
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

    [Test]
    public void SetValue_And_SetTargetState_ShouldManageNeedsReviewAndValidation()
    {
        var content = GetResourceContent("state12.xlf");
        var factory = new XliffDocumentFactory();
        var doc = factory.Create(content);
        var stateDoc = doc as IHasTranslationState;
        stateDoc.Should().NotBeNull();

        // 1. Updating value sets state to NeedsReview
        doc.SetValue("u2", "Updated Target 2");
        stateDoc!.GetTargetState("u2").Should().Be(TranslationState.NeedsReview);

        var unit = doc.GetUnit("u2");
        unit!.State.Should().Be(TranslationState.NeedsReview);

        // 2. Marking as validated sets state to None (state attribute removed)
        stateDoc.SetTargetState("u2", TranslationState.None);
        stateDoc.GetTargetState("u2").Should().Be(TranslationState.None);
        doc.GetUnit("u2")!.State.Should().Be(TranslationState.None);

        var writer = new XliffDocumentWriter();
        using var outputStream = new MemoryStream();
        writer.Write(outputStream, (IXliffDocument)doc);
        outputStream.Position = 0;
        var xml = new StreamReader(outputStream).ReadToEnd();
        xml.Should().Contain("<target>Updated Target 2</target>");
    }
}