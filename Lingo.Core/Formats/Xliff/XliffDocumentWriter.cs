using System.Xml.Serialization;

namespace Lingo.Core.Formats.Xliff;

public class XliffDocumentWriter : ILingoDocumentWriter<IXliffDocument>
{
    public string FormatId => "xliff-mixed"; // Handles both 1.2 and 2.0 depending on document

    public void Write(Stream destination, IXliffDocument document)
    {
        var serializer = new XmlSerializer(document.XliffType);
        serializer.Serialize(destination, document.InternalXliff);
    }
}