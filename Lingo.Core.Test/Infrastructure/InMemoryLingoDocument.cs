using Lingo.Core.Documents;
using Lingo.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Lingo.Core.Test.Infrastructure;

public class InMemoryLingoDocument : ILingoDocument
{
    private readonly Dictionary<string, Unit> _units = new();

    public string FormatId { get; set; } = "in-memory";

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
        // For in-memory, we don't strictly need to sort the dictionary, but we can if we want to mimic file behavior.
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
                Target = unit.Target, // In sync, usually target is empty for new units or copied if needed
                State = unit.State
            };
            return SyncResult.NewUnitCreated;
        }

        if (existing.Source != unit.Source)
        {
            existing.Source = unit.Source;
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
