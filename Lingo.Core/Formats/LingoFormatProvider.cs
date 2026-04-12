using Lingo.Core.Formats.Xliff;

namespace Lingo.Core.Formats;

public static class LingoFormatProvider
{
    public static ILingoDocumentFactory<ILingoDocument> GetFactory(LingoFormat format)
    {
        if (format == LingoFormat.Xliff)
        {
            // We need to cast XliffDocumentFactory to ILingoDocumentFactory<ILingoDocument>
            // Since it implements ILingoDocumentFactory<IXliffDocument> and IXliffDocument : ILingoDocument,
            // we can use a wrapper if needed, but let's see if we can just return it.
            return new XliffDocumentFactoryWrapper();
        }

        throw new NotSupportedException($"Format {format.Id} is not supported.");
    }

    public static ILingoDocumentWriter<ILingoDocument> GetWriter(LingoFormat format)
    {
        if (format == LingoFormat.Xliff)
        {
            return new XliffDocumentWriterWrapper();
        }

        throw new NotSupportedException($"Format {format.Id} is not supported.");
    }

    private class XliffDocumentFactoryWrapper : ILingoDocumentFactory<ILingoDocument>
    {
        private readonly XliffDocumentFactory _factory = new();
        public ILingoDocument Create(Stream source) => _factory.Create(source);
        public ILingoDocument Create(string source) => _factory.Create(source);
        public ILingoDocument Create(string formatId, IEnumerable<Unit> units) => _factory.Create(formatId, units);
    }

    private class XliffDocumentWriterWrapper : ILingoDocumentWriter<ILingoDocument>
    {
        private readonly XliffDocumentWriter _writer = new();
        public string FormatId => _writer.FormatId;
        public void Write(Stream destination, ILingoDocument document)
        {
            if (document is IXliffDocument xliffDoc)
            {
                _writer.Write(destination, xliffDoc);
            }
            else
            {
                throw new ArgumentException("Document is not an XLIFF document", nameof(document));
            }
        }
    }
}
