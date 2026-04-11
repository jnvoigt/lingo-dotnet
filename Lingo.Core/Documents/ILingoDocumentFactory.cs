namespace Lingo.Core.Documents;

public interface ILingoDocumentFactory<TDocument> where TDocument : ILingoDocument
{
    TDocument Create(Stream source);
    TDocument Create(string source);
    TDocument Create(string formatId, IEnumerable<Unit> units);
}