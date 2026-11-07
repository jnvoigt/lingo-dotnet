namespace Lingo.Core.Documents;

public interface IHasSourceValue
{
    string? GetSourceValue(string unitId);
    void SetSourceValue(string unitId, string sourceValue);
}
