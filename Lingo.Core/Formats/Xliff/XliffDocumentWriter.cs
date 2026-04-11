using Lingo.Core.Formats.Xliff.v12;
using Lingo.Core.Formats.Xliff.v20;
using System.Xml.Serialization;

namespace Lingo.Core.Formats.Xliff;

public class XliffDocumentWriter : ILingoDocumentWriter
{
    public string FormatId => "xliff-mixed"; // Handles both 1.2 and 2.0 depending on document

    public void Write(Stream destination, ILingoDocument document)
    {
        if (document is Xliff20Document doc20)
        {
            var xliff = GetInternalXliff(doc20);
            var serializer = new XmlSerializer(typeof(V20.Xliff));
            serializer.Serialize(destination, xliff);
        }
        else if (document is Xliff12Document doc12)
        {
            var xliff = GetInternalXliff(doc12);
            var serializer = new XmlSerializer(typeof(V12.Xliff));
            serializer.Serialize(destination, xliff);
        }
    }

    private V20.Xliff GetInternalXliff(Xliff20Document doc)
    {
        return doc.InternalXliff;
    }

    private V12.Xliff GetInternalXliff(Xliff12Document doc)
    {
        return doc.InternalXliff;
    }
}