namespace Lingo.Core.Documents;

public interface ILingoDocumentFactory
{
    ILingoDocument Create(Stream source);
    ILingoDocument Create(string source);
    ILingoDocument Create(string formatId, IEnumerable<Unit> units);
}
