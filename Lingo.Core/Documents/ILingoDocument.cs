namespace Lingo.Core.Documents;

public interface ILingoDocument
{
    // Format identifier e.g. "xliff-1.2", "android", "factorio"
    string FormatId { get; }

    HashSet<string> GetUnitIds();
    Unit? GetUnit(string unitId);
    string? GetValue(string unitId);
    void SetValue(string unitId, string value);
    void SortByKey();
    IEnumerable<Unit> GetAllUnits();
    SyncResult SyncUnit(Unit unit);
    MergeResult MergeUnit(Unit unit);
    ImportResult ImportUnit(Unit unit);
    IEnumerable<string> RetainUnitIds(IEnumerable<string> ids);
}