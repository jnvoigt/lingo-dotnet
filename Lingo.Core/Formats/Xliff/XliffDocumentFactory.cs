using Lingo.Core.Formats.Xliff.v12;
using Lingo.Core.Formats.Xliff.V12;
using Lingo.Core.Formats.Xliff.v20;
using System.Xml;
using System.Xml.Serialization;

namespace Lingo.Core.Formats.Xliff;

public class XliffDocumentFactory : ILingoDocumentFactory<IXliffDocument>
{
    private readonly string _xliff20NameSpace = "urn:oasis:names:tc:xliff:document:2.0";

    public IXliffDocument Create(Stream source)
    {
        // Try to detect version from version attribute
        using var reader = XmlReader.Create(source);
        reader.MoveToContent();
        var version = reader.GetAttribute("version");
        source.Position = 0;

        if (version == "2.0")
        {
            var serializer = new XmlSerializer(typeof(V20.Xliff));
            var xliff = (V20.Xliff)serializer.Deserialize(source)!;
            return new Xliff20Document(xliff);
        }
        else
        {
            var serializer = new XmlSerializer(typeof(V12.Xliff));
            var xliff = (V12.Xliff)serializer.Deserialize(source)!;
            return new Xliff12Document(xliff);
        }
    }

    public IXliffDocument Create(string source)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(source));
        return Create(stream);
    }

    public IXliffDocument Create(string formatId, IEnumerable<Unit> units)
    {
        if (formatId == "xliff-2.0")
        {
            var xliff = new V20.Xliff { Version = "2.0", SrcLang = "en" };
            var doc = new Xliff20Document(xliff);
            foreach (var unit in units)
            {
                doc.SyncUnit(unit);
            }

            return doc;
        }
        else
        {
            var xliff = new V12.Xliff { Version = AttrTypeVersion.Item12 };
            var doc = new Xliff12Document(xliff);
            foreach (var unit in units)
            {
                doc.SyncUnit(unit);
            }

            return doc;
        }
    }
}