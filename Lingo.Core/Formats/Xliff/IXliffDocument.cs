namespace Lingo.Core.Formats.Xliff;

public interface IXliffDocument : ILingoDocument
{
    object InternalXliff { get; }
    Type XliffType { get; }
}
