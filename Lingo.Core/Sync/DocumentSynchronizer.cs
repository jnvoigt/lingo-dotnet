using Lingo.Core.Documents;

namespace Lingo.Core.Sync;

public class DocumentSynchronizer
{
    public void PushSync(ILingoDocument source, ILingoDocument target)
    {
        var sourceUnitIds = source.GetUnitIds();
        foreach (var unit in source.GetAllUnits())
        {
            target.SyncUnit(unit);
        }

        target.RetainUnitIds(sourceUnitIds);
    }

    public void PullSync(ILingoDocument source, ILingoDocument target)
    {
        foreach (var unit in source.GetAllUnits())
        {
            target.ImportUnit(unit);
        }
    }

    public void Merge(ILingoDocument source, ILingoDocument target)
    {
        foreach (var unit in source.GetAllUnits())
        {
            target.MergeUnit(unit);
        }
    }
}
