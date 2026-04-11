namespace Lingo.Core.Documents;

public interface ILingoDocumentWriter<TDocument> where TDocument : ILingoDocument
{
    string FormatId { get; }
    void Write(Stream destination, TDocument document);
}