using AwesomeAssertions;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Formats.Xliff.v12;
using Lingo.Core.Formats.Xliff.V12;
using System.Text;
using System.Xml.Linq;
using File = Lingo.Core.Formats.Xliff.V12.File;

namespace Lingo.Core.Test.Xliff;

public class Xliff12XmlSpaceTests
{
    [Test]
    public void Save_Xliff12_ShouldNotContainXmlSpaceDefault()
    {
        // Arrange
        var xliff = new Formats.Xliff.V12.Xliff();
        var file = new File { Original = "test.txt", SourceLanguage = "en", Datatype = "plaintext" };
        xliff.File.Add(file);
        file.Body = new Body();
        var tu = new TransUnit { Id = "unit1" };
        tu.Source = new Source { Text = new[] { "Hello" } };
        file.Body.TransUnit.Add(tu);

        var doc = new Xliff12Document(xliff);
        var writer = new XliffDocumentWriter();

        // Act
        using var outputStream = new MemoryStream();
        writer.Write(outputStream, doc);
        outputStream.Position = 0;
        var savedContent = Encoding.UTF8.GetString(outputStream.ToArray());

        // Assert
        var xDoc = XDocument.Parse(savedContent);
        var ns = xDoc.Root!.GetDefaultNamespace();
        var xmlNs = XNamespace.Get("http://www.w3.org/XML/1998/namespace");

        var transUnit = xDoc.Descendants(ns + "trans-unit").First();
        var xmlSpaceAttr = transUnit.Attribute(xmlNs + "space");

        xmlSpaceAttr.Should().BeNull("xml:space attribute should not be generated when it has the default value");

        var fileElem = xDoc.Descendants(ns + "file").First();
        fileElem.Attribute(xmlNs + "space").Should()
            .BeNull("xml:space attribute should not be generated on file when it has the default value");
    }

    [Test]
    public void Save_Xliff12_ShouldContainXmlSpacePreserve_WhenSet()
    {
        // Arrange
        var xliff = new Formats.Xliff.V12.Xliff();
        var file = new File { Original = "test.txt", SourceLanguage = "en", Datatype = "plaintext" };
        xliff.File.Add(file);
        file.Body = new Body();
        var tu = new TransUnit { Id = "unit1", Space = Space.Preserve };
        tu.Source = new Source { Text = new[] { "Hello" } };
        file.Body.TransUnit.Add(tu);

        var doc = new Xliff12Document(xliff);
        var writer = new XliffDocumentWriter();

        // Act
        using var outputStream = new MemoryStream();
        writer.Write(outputStream, doc);
        outputStream.Position = 0;
        var savedContent = Encoding.UTF8.GetString(outputStream.ToArray());

        // Assert
        var xDoc = XDocument.Parse(savedContent);
        var ns = xDoc.Root!.GetDefaultNamespace();
        var xmlNs = XNamespace.Get("http://www.w3.org/XML/1998/namespace");

        var transUnit = xDoc.Descendants(ns + "trans-unit").First();
        var xmlSpaceAttr = transUnit.Attribute(xmlNs + "space");

        xmlSpaceAttr.Should().NotBeNull();
        xmlSpaceAttr!.Value.Should().Be("preserve");
    }
}