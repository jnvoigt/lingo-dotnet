using Lingo.Core.Formats.Xliff.V20;
using File = Lingo.Core.Formats.Xliff.V20.File;
using Unit = Lingo.Core.Models.Unit;

namespace Lingo.Core.Formats.Xliff.v20;

public class Xliff20Document : IXliffDocument, IHasSourceValue, IHasTranslationState, IHasCultureInfo
{
    public Xliff20Document(V20.Xliff xliff)
    {
        InternalXliff = xliff;
    }

    public V20.Xliff InternalXliff { get; }

    public CultureInfo GetSourceCulture()
    {
        return string.IsNullOrEmpty(InternalXliff.SrcLang)
            ? CultureInfo.InvariantCulture
            : new CultureInfo(InternalXliff.SrcLang);
    }

    public CultureInfo? GetTargetCulture()
    {
        return string.IsNullOrEmpty(InternalXliff.TrgLang) ? null : new CultureInfo(InternalXliff.TrgLang);
    }

    public string? GetSourceValue(string unitId)
    {
        var unit = FindUnit(unitId);
        var segment = unit?.Segment.FirstOrDefault();
        return segment?.Source.FlattenInline();
    }

    public TranslationState GetTargetState(string unitId)
    {
        var unit = FindUnit(unitId);
        var segment = unit?.Segment.FirstOrDefault();
        if (segment == null)
        {
            return TranslationState.None;
        }

        if (segment.SubState == "x-needs-adaptation")
        {
            return TranslationState.NeedsAdaptation;
        }

        return MapState(segment.State);
    }

    object IXliffDocument.InternalXliff => InternalXliff;
    public Type XliffType => typeof(V20.Xliff);

    public string FormatId => "xliff-2.0";

    public HashSet<string> GetUnitIds()
    {
        HashSet<string> ids = new();
        foreach (var file in InternalXliff.File)
        {
            foreach (var unit in file.Unit)
            {
                if (!string.IsNullOrEmpty(unit.Id))
                {
                    ids.Add(unit.Id);
                }
            }
        }

        return ids;
    }

    public Unit? GetUnit(string unitId)
    {
        var unit = FindUnit(unitId);
        if (unit == null)
        {
            return null;
        }

        var segment = unit.Segment.FirstOrDefault();
        return ConvertToUnit(unit, segment);
    }

    public string? GetValue(string unitId)
    {
        var unit = FindUnit(unitId);
        if (unit == null)
        {
            return null;
        }

        // XLIFF 2.0 can have multiple segments per unit. For simple ILingoDocument, we might just take the first one's target
        var segment = unit.Segment.FirstOrDefault();
        if (segment?.Target != null)
        {
            return segment.Target.FlattenInline();
        }

        // Fallback to source if target is missing? Usually GetValue on a document returns the "value" which is target for XLIFF
        return segment?.Source.FlattenInline();
    }

    public void SetValue(string unitId, string value)
    {
        var unit = FindUnit(unitId);
        if (unit == null)
        {
            return;
        }

        var segment = unit.Segment.FirstOrDefault();
        if (segment == null)
        {
            segment = new Segment();
            unit.Segment.Add(segment);
        }

        if (segment.Target == null)
        {
            segment.Target = new Target();
        }

        segment.Target.Text = new[] { value };
        segment.State = StateType.Translated;
    }

    public void SortByKey()
    {
        foreach (var file in InternalXliff.File)
        {
            var sorted = file.Unit.OrderBy(u => u.Id).ToList();
            file.Unit.Clear();
            foreach (var unit in sorted)
            {
                file.Unit.Add(unit);
            }
        }
    }

    public IEnumerable<Unit> GetAllUnits()
    {
        foreach (var file in InternalXliff.File)
        {
            foreach (var unit in file.Unit)
            {
                var segment = unit.Segment.FirstOrDefault();
                yield return ConvertToUnit(unit, segment);
            }
        }
    }

    public SyncResult SyncUnit(Unit unit)
    {
        var existing = FindUnit(unit.Id);
        if (existing == null)
        {
            var file = InternalXliff.File.FirstOrDefault();
            if (file == null)
            {
                file = new File();
                InternalXliff.File.Add(file);
            }

            var u = new V20.Unit { Id = unit.Id };
            var segment = new Segment();
            segment.Source = new Source { Text = [unit.Source] };

            if (!string.IsNullOrEmpty(unit.Target))
            {
                segment.Target = new Target { Text = [unit.Target] };
                segment.State = StateType.Translated;
            }
            else
            {
                segment.State = StateType.Initial;
            }

            u.Segment.Add(segment);
            file.Unit.Add(u);
            return SyncResult.NewUnitCreated;
        }

        var changed = false;
        var segmentToSync = existing.Segment.FirstOrDefault();
        if (segmentToSync == null)
        {
            segmentToSync = new Segment();
            existing.Segment.Add(segmentToSync);
        }

        var newSource = unit.Source ?? unit.Target;
        var oldSource = segmentToSync.Source.FlattenInline();

        if (oldSource != newSource)
        {
            segmentToSync.Source = new Source { Text = [newSource] };
            segmentToSync.State = StateType.Translated;
            segmentToSync.SubState = "x-needs-adaptation";
            changed = true;
        }
        else
        {
            var newTargetValue = unit.Target;
            var oldTargetValue = segmentToSync.Target.FlattenInline();

            if (newTargetValue != null && oldTargetValue != newTargetValue)
            {
                if (segmentToSync.Target == null)
                {
                    segmentToSync.Target = new Target();
                }

                segmentToSync.Target.Text = [newTargetValue];
                changed = true;
            }
        }

        return changed ? SyncResult.SourceValueHasChanged : SyncResult.Nothing;
    }

    public MergeResult MergeUnit(Unit unit)
    {
        var existing = FindUnit(unit.Id);
        if (existing != null)
        {
            return MergeResult.Conflict;
        }

        SyncUnit(unit);
        return MergeResult.Merged;
    }

    public ImportResult ImportUnit(Unit unit)
    {
        var existing = FindUnit(unit.Id);
        if (existing == null)
        {
            return ImportResult.Ignored;
        }

        var segment = existing.Segment.FirstOrDefault();
        var currentValue = segment?.Target.FlattenInline();
        if (currentValue == unit.Target)
        {
            return ImportResult.AlreadyUpToDate;
        }

        SetValue(unit.Id, unit.Target);
        return ImportResult.Imported;
    }

    public IEnumerable<string> RetainUnitIds(IEnumerable<string> ids)
    {
        var idSet = ids.ToHashSet();
        var removed = new List<string>();
        foreach (var file in InternalXliff.File)
        {
            var toRemove = file.Unit.Where(u => !idSet.Contains(u.Id)).ToList();
            foreach (var u in toRemove)
            {
                file.Unit.Remove(u);
                removed.Add(u.Id);
            }
        }

        return removed;
    }

    private V20.Unit? FindUnit(string unitId)
    {
        foreach (var file in InternalXliff.File)
        {
            var unit = file.Unit.FirstOrDefault(u => u.Id == unitId);
            if (unit != null)
            {
                return unit;
            }
        }

        return null;
    }

    private TranslationState MapState(StateType state)
    {
        return state switch
        {
            StateType.Initial => TranslationState.NeedsTranslation,
            StateType.Translated => TranslationState.Translated,
            StateType.Reviewed => TranslationState.Translated,
            StateType.Final => TranslationState.Translated,
            _ => TranslationState.None
        };
    }

    private Unit ConvertToUnit(V20.Unit unit, Segment? segment)
    {
        return new Unit
        {
            Id = unit.Id,
            Target = segment?.Target.FlattenInline(),
            Source = segment?.Source.FlattenInline(),
            State = segment != null ? GetTargetState(unit.Id) : TranslationState.None
        };
    }
}