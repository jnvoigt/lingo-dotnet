namespace Lingo.Core.Documents;

public interface ILingoDocumentWriter
{
    string FormatId { get; }
    void Write(Stream destination, ILingoDocument document);
}
