namespace Lingo.Core.Sync;

public class KeySynchronizer
{
    /// <summary>
    /// Synchronizes a specific key across a sibling group of documents, always anchoring and starting from the source document.
    /// Since the source file is the source of truth, if the key is missing in the source document, it is removed from all target documents.
    /// If the key exists in the source document, it is propagated to all target documents.
    /// Returns the list of documents that were updated or modified.
    /// </summary>
    public List<ILingoDocument> SyncKeyInSiblingGroup(
        string keyId,
        ILingoDocument? sourceDocument,
        IEnumerable<ILingoDocument> targetDocuments,
        string? fallbackSourceText = null)
    {
        var targets = targetDocuments.ToList();
        var updated = new List<ILingoDocument>();

        if (sourceDocument != null)
        {
            var masterUnit = sourceDocument.GetUnit(keyId);
            if (masterUnit == null)
            {
                // Key is missing in the source file (source of truth).
                // Delete the key from all other sibling files.
                foreach (var target in targets)
                {
                    if (target.RemoveUnit(keyId))
                    {
                        updated.Add(target);
                    }
                }
                return updated;
            }

            var sourceText = masterUnit.Source ?? fallbackSourceText ?? keyId;
            var syncUnit = new Unit
            {
                Id = keyId,
                Source = sourceText,
                Target = null,
                State = TranslationState.NeedsTranslation,
                Type = masterUnit.Type
            };

            // Sync from source to all target sibling documents
            foreach (var target in targets)
            {
                var res = target.SyncUnit(syncUnit);
                if (res == SyncResult.NewUnitCreated || res == SyncResult.SourceValueHasChanged)
                {
                    updated.Add(target);
                }
            }

            return updated;
        }

        // When no source document exists (peer target files)
        Unit? peerUnit = null;
        foreach (var doc in targets)
        {
            var u = doc.GetUnit(keyId);
            if (u != null)
            {
                peerUnit = u;
                break;
            }
        }

        if (peerUnit == null && fallbackSourceText == null)
        {
            return updated;
        }

        var peerSourceText = peerUnit?.Source ?? fallbackSourceText ?? keyId;
        var peerSyncUnit = new Unit
        {
            Id = keyId,
            Source = peerSourceText,
            Target = null,
            State = TranslationState.NeedsTranslation,
            Type = peerUnit?.Type ?? UnitType.Default
        };

        foreach (var target in targets)
        {
            var res = target.SyncUnit(peerSyncUnit);
            if (res == SyncResult.NewUnitCreated || res == SyncResult.SourceValueHasChanged)
            {
                updated.Add(target);
            }
        }

        return updated;
    }

    /// <summary>
    /// Checks whether a key has differences between the source document and target documents in a sibling group.
    /// Returns true when:
    /// - The key is missing in the source document but present in at least one target document (considered to be removed).
    /// - The key is present in the source document but missing in at least one target document.
    /// - The key is present in both, but the source value in a target document differs from the source document (considered to be changed).
    /// </summary>
    public bool HasKeyDifferencesInSiblingGroup(
        string keyId,
        ILingoDocument? sourceDocument,
        IEnumerable<ILingoDocument> targetDocuments)
    {
        var targets = targetDocuments.ToList();
        if (sourceDocument != null)
        {
            var sourceUnit = sourceDocument.GetUnit(keyId);
            if (sourceUnit == null)
            {
                // Key is missing in source file: check if any target has it (needs to be deleted/removed)
                return targets.Any(t => t.GetUnit(keyId) != null);
            }

            var sourceText = sourceUnit.Source?.Trim() ?? keyId;
            foreach (var target in targets)
            {
                var targetUnit = target.GetUnit(keyId);
                if (targetUnit == null)
                {
                    // Target key is missing
                    return true;
                }

                if ((targetUnit.Source?.Trim() ?? "") != sourceText)
                {
                    // Source value is different in target document
                    return true;
                }
            }

            return false;
        }

        // When no designated source document exists (peer files)
        var presentTargets = targets.Where(t => t.GetUnit(keyId) != null).ToList();
        if (presentTargets.Count > 0 && presentTargets.Count < targets.Count)
        {
            return true;
        }

        if (presentTargets.Count > 1)
        {
            var firstSource = presentTargets[0].GetUnit(keyId)?.Source?.Trim();
            if (presentTargets.Any(t => (t.GetUnit(keyId)?.Source?.Trim() ?? "") != firstSource))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Explicitly adds a new key with specified source text into the source document and propagates it to all target documents.
    /// </summary>
    public List<ILingoDocument> AddKeyToSiblingGroup(
        string keyId,
        string sourceText,
        ILingoDocument? sourceDocument,
        IEnumerable<ILingoDocument> targetDocuments)
    {
        var targets = targetDocuments.ToList();
        var updated = new List<ILingoDocument>();

        if (sourceDocument != null)
        {
            sourceDocument.SyncUnit(new Unit
            {
                Id = keyId,
                Source = sourceText,
                Target = sourceText,
                State = TranslationState.Translated,
                Type = UnitType.Default
            });
            updated.Add(sourceDocument);
        }

        var syncUnit = new Unit
        {
            Id = keyId,
            Source = sourceText,
            Target = null,
            State = TranslationState.NeedsTranslation,
            Type = UnitType.Default
        };

        foreach (var target in targets)
        {
            var res = target.SyncUnit(syncUnit);
            if (res == SyncResult.NewUnitCreated || res == SyncResult.SourceValueHasChanged)
            {
                updated.Add(target);
            }
        }

        return updated;
    }

    /// <summary>
    /// Synchronizes all missing keys across a sibling group, always anchoring and starting from the source document.
    /// </summary>
    public Dictionary<string, List<ILingoDocument>> SyncMissingKeysInSiblingGroup(
        ILingoDocument? sourceDocument,
        IEnumerable<ILingoDocument> targetDocuments)
    {
        var targets = targetDocuments.ToList();
        var results = new Dictionary<string, List<ILingoDocument>>(StringComparer.OrdinalIgnoreCase);

        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (sourceDocument != null)
        {
            allKeys.UnionWith(sourceDocument.GetUnitIds());
        }
        foreach (var t in targets)
        {
            allKeys.UnionWith(t.GetUnitIds());
        }

        foreach (var key in allKeys)
        {
            var updatedDocs = SyncKeyInSiblingGroup(key, sourceDocument, targets);
            if (updatedDocs.Count > 0)
            {
                results[key] = updatedDocs;
            }
        }

        return results;
    }

    /// <summary>
    /// Synchronizes a specific unit/key to all target documents where it is missing or where the source value has changed.
    /// Returns the list of target documents that were updated.
    /// </summary>
    public List<ILingoDocument> SyncKeyToTargets(Unit unit, IEnumerable<ILingoDocument> targets)
    {
        var updatedDocuments = new List<ILingoDocument>();
        var syncUnit = new Unit
        {
            Id = unit.Id,
            Source = unit.Source,
            Target = null,
            State = TranslationState.NeedsTranslation,
            Type = unit.Type
        };

        foreach (var target in targets)
        {
            var result = target.SyncUnit(syncUnit);
            if (result == SyncResult.NewUnitCreated || result == SyncResult.SourceValueHasChanged)
            {
                updatedDocuments.Add(target);
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

        var results = new Dictionary<string, List<ILingoDocument>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, unit) in allUnitsByKey)
        {
            var syncUnit = new Unit
            {
                Id = key,
                Source = unit.Source,
                Target = null,
                State = TranslationState.NeedsTranslation,
                Type = unit.Type
            };

            foreach (var doc in docList)
            {
                var syncResult = doc.SyncUnit(syncUnit);
                if (syncResult == SyncResult.NewUnitCreated || syncResult == SyncResult.SourceValueHasChanged)
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

        return results;
    }

    /// <summary>
    /// Imports a specific unit/key from source document to target document (pull sync for a single key).
    /// </summary>
    public ImportResult ImportKey(string keyId, ILingoDocument source, ILingoDocument target)
    {
        var sourceUnit = source.GetUnit(keyId);
        if (sourceUnit == null)
        {
            return ImportResult.Ignored;
        }

        return target.ImportUnit(sourceUnit);
    }

    /// <summary>
    /// Merges a specific unit/key from source document to target document.
    /// </summary>
    public MergeResult MergeKey(string keyId, ILingoDocument source, ILingoDocument target)
    {
        var sourceUnit = source.GetUnit(keyId);
        if (sourceUnit == null)
        {
            return MergeResult.Conflict;
        }

        return target.MergeUnit(sourceUnit);
    }

    /// <summary>
    /// Imports a specific key from source document to all target documents in a sibling group.
    /// </summary>
    public List<ILingoDocument> PullKeyInSiblingGroup(string keyId, ILingoDocument sourceDocument, IEnumerable<ILingoDocument> targetDocuments)
    {
        var sourceUnit = sourceDocument.GetUnit(keyId);
        if (sourceUnit == null)
        {
            return new List<ILingoDocument>();
        }

        var updated = new List<ILingoDocument>();
        foreach (var target in targetDocuments)
        {
            var res = target.ImportUnit(sourceUnit);
            if (res == ImportResult.Imported)
            {
                updated.Add(target);
            }
        }

        return updated;
    }

    /// <summary>
    /// Merges a specific key from source document into all target documents in a sibling group.
    /// </summary>
    public List<ILingoDocument> MergeKeyInSiblingGroup(string keyId, ILingoDocument sourceDocument, IEnumerable<ILingoDocument> targetDocuments)
    {
        var sourceUnit = sourceDocument.GetUnit(keyId);
        if (sourceUnit == null)
        {
            return new List<ILingoDocument>();
        }

        var updated = new List<ILingoDocument>();
        foreach (var target in targetDocuments)
        {
            var res = target.MergeUnit(sourceUnit);
            if (res == MergeResult.Merged)
            {
                updated.Add(target);
            }
        }

        return updated;
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
