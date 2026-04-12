namespace Lingo.Core.Sync;

public class DocumentSynchronizer
{
    public void PushSync(ILingoDocument source, ILingoDocument target)
    {
        var added = 0;
        var updated = 0;
        var removed = 0;

        var sourceUnitIds = source.GetUnitIds();

        foreach (var unit in source.GetAllUnits())
        {
            var result = target.SyncUnit(unit);
            if (result == SyncResult.NewUnitCreated)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[+] Added: {unit.Id}");
                Console.ResetColor();
                added++;
            }
            else if (result == SyncResult.SourceValueHasChanged)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[~] Updated: {unit.Id}");
                Console.ResetColor();
                updated++;
            }
        }

        var removedIds = target.RetainUnitIds(sourceUnitIds).ToList();
        foreach (var id in removedIds)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[-] Removed: {id}");
            Console.ResetColor();
            removed++;
        }

        if (added == 0 && updated == 0 && removed == 0)
        {
            Console.WriteLine("No changes.");
        }
        else
        {
            Console.WriteLine($"Summary: {added} added, {updated} updated, {removed} removed");
        }
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