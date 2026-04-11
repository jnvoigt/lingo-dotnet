namespace Lingo.Core.Documents;

public interface ILingoDocumentFactory
{
    ILingoDocument Create(string formatId, IEnumerable<Unit> units);
}