using System.Globalization;
using Lingo.Core.Documents;
using Lingo.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Lingo.Core.Test.Infrastructure;

public class InMemoryLingoDocument : ILingoDocument, IHasCultureInfo, IHasSourceValue
{
    private readonly Dictionary<string, Unit> _units = new();

    public string FormatId { get; set; } = "in-memory";
    public string? FilePath { get; set; }
    public CultureInfo SourceCulture { get; set; } = CultureInfo.InvariantCulture;
    public CultureInfo? TargetCulture { get; set; }

    public CultureInfo GetSourceCulture() => SourceCulture;
    public CultureInfo? GetTargetCulture() => TargetCulture;

    public string? GetSourceValue(string unitId) => GetUnit(unitId)?.Source;

    public void SetSourceValue(string unitId, string sourceValue)
    {
        if (_units.TryGetValue(unitId, out var unit))
        {
            unit.Source = sourceValue;
        }
        else
        {
            _units[unitId] = new Unit { Id = unitId, Source = sourceValue };
        }
    }

    public HashSet<string> GetUnitIds() => _units.Keys.ToHashSet();

    public Unit? GetUnit(string unitId) => _units.TryGetValue(unitId, out var unit) ? unit : null;

    public string? GetValue(string unitId) => GetUnit(unitId)?.Target;

    public void SetValue(string unitId, string value)
    {
        if (_units.TryGetValue(unitId, out var unit))
        {
            unit.Target = value;
        }
        else
        {
            _units[unitId] = new Unit { Id = unitId, Target = value };
        }
    }

    public void SortByKey()
    {
        var sorted = _units.OrderBy(u => u.Key).ToList();
        _units.Clear();
        foreach (var pair in sorted)
        {
            _units.Add(pair.Key, pair.Value);
        }
    }

    public IEnumerable<Unit> GetAllUnits() => _units.Values;

    public SyncResult SyncUnit(Unit unit)
    {
        if (!_units.TryGetValue(unit.Id, out var existing))
        {
            _units[unit.Id] = new Unit
            {
                Id = unit.Id,
                Source = unit.Source,
                Target = unit.Target,
                State = unit.State ?? (string.IsNullOrEmpty(unit.Target) ? TranslationState.NeedsTranslation : TranslationState.Translated)
            };
            return SyncResult.NewUnitCreated;
        }

        if (existing.Source != unit.Source)
        {
            existing.Source = unit.Source;
            existing.State = TranslationState.NeedsAdaptation;
            return SyncResult.SourceValueHasChanged;
        }

        return SyncResult.Nothing;
    }

    public MergeResult MergeUnit(Unit unit)
    {
        if (!_units.ContainsKey(unit.Id))
        {
            _units[unit.Id] = unit;
            return MergeResult.Merged;
        }
        return MergeResult.Conflict;
    }

    public ImportResult ImportUnit(Unit unit)
    {
        if (!_units.TryGetValue(unit.Id, out var existing))
        {
            _units[unit.Id] = unit;
            return ImportResult.Imported;
        }

        if (existing.Target == unit.Target && existing.Source == unit.Source)
        {
            return ImportResult.AlreadyUpToDate;
        }

        existing.Source = unit.Source;
        existing.Target = unit.Target;
        return ImportResult.Imported;
    }

    public bool RemoveUnit(string unitId) => _units.Remove(unitId);

    public bool RenameUnit(string oldUnitId, string newUnitId)
    {
        if (!_units.TryGetValue(oldUnitId, out var unit))
        {
            return false;
        }

        _units.Remove(oldUnitId);
        unit.Id = newUnitId;
        _units[newUnitId] = unit;
        return true;
    }

    public IEnumerable<string> RetainUnitIds(IEnumerable<string> ids)
    {
        var idsToKeep = ids.ToHashSet();
        var idsToRemove = _units.Keys.Where(id => !idsToKeep.Contains(id)).ToList();
        foreach (var id in idsToRemove)
        {
            _units.Remove(id);
        }
        return idsToRemove;
    }

    public void AddUnit(Unit unit)
    {
        _units[unit.Id] = unit;
    }
}
