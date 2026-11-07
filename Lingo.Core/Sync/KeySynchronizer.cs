namespace Lingo.Core.Sync;

public class KeySynchronizer
{
    /// <summary>
    /// Synchronizes a specific unit/key to all target documents where it is missing.
    /// Returns the list of target documents that were updated with a new unit.
    /// </summary>
    public List<ILingoDocument> SyncKeyToTargets(Unit unit, IEnumerable<ILingoDocument> targets)
    {
        var updatedDocuments = new List<ILingoDocument>();
        foreach (var target in targets)
        {
            var targetUnitIds = target.GetUnitIds();
            if (!targetUnitIds.Contains(unit.Id))
            {
                var result = target.SyncUnit(new Unit
                {
                    Id = unit.Id,
                    Source = unit.Source,
                    Target = null,
                    State = TranslationState.NeedsTranslation,
                    Type = UnitType.Default
                });

                if (result == SyncResult.NewUnitCreated)
                {
                    updatedDocuments.Add(target);
                }
            }
        }
        return updatedDocuments;
    }

    /// <summary>
    /// Finds all keys missing across a set of documents and synchronizes them from documents where they exist.
    /// </summary>
    public Dictionary<string, List<ILingoDocument>> SyncMissingKeysAcrossDocuments(IEnumerable<ILingoDocument> documents)
    {
        var docList = documents.ToList();
        var allUnitsByKey = new Dictionary<string, Unit>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in docList)
        {
            foreach (var unit in doc.GetAllUnits())
            {
                if (!allUnitsByKey.ContainsKey(unit.Id))
                {
                    allUnitsByKey[unit.Id] = unit;
                }
                else if (string.IsNullOrEmpty(allUnitsByKey[unit.Id].Source) && !string.IsNullOrEmpty(unit.Source))
                {
                    allUnitsByKey[unit.Id] = unit;
                }
            }
        }

        var results = new Dictionary<string, List<ILingoDocument>>();

        foreach (var (key, unit) in allUnitsByKey)
        {
            foreach (var doc in docList)
            {
                if (!doc.GetUnitIds().Contains(key))
                {
                    var syncResult = doc.SyncUnit(new Unit
                    {
                        Id = key,
                        Source = unit.Source,
                        Target = null,
                        State = TranslationState.NeedsTranslation,
                        Type = UnitType.Default
                    });

                    if (syncResult == SyncResult.NewUnitCreated)
                    {
                        if (!results.TryGetValue(key, out var list))
                        {
                            list = new List<ILingoDocument>();
                            results[key] = list;
                        }
                        list.Add(doc);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Identifies all keys present in at least one document but missing in others.
    /// </summary>
    public Dictionary<string, List<ILingoDocument>> DetectMissingKeys(IEnumerable<ILingoDocument> documents)
    {
        var docList = documents.ToList();
        var allKeys = new HashSet<string>(docList.SelectMany(d => d.GetUnitIds()), StringComparer.OrdinalIgnoreCase);
        var missingMap = new Dictionary<string, List<ILingoDocument>>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            var missingIn = docList.Where(d => !d.GetUnitIds().Contains(key)).ToList();
            if (missingIn.Count > 0)
            {
                missingMap[key] = missingIn;
            }
        }

        return missingMap;
    }
}
