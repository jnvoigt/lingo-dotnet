using Lingo.Core.Formats.Xliff.V20;
using Lingo.Core.Parser;
using File = Lingo.Core.Formats.Xliff.V20.File;
using Unit = Lingo.Core.Models.Unit;

namespace Lingo.Core.Formats.Xliff.v20;

public class Xliff20Document : ILingoDocument, IHasSourceValue, IHasTranslationState, IHasCultureInfo
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
        return segment?.Source != null ? FlattenInline(segment.Source) : null;
    }

    public TranslationState GetTargetState(string unitId)
    {
        var unit = FindUnit(unitId);
        var segment = unit?.Segment.FirstOrDefault();
        return segment != null ? MapState(segment.State) : TranslationState.None;
    }

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
            return FlattenInline(segment.Target);
        }

        // Fallback to source if target is missing? Usually GetValue on a document returns the "value" which is target for XLIFF
        return segment?.Source != null ? FlattenInline(segment.Source) : null;
    }

    public void SetValue(string unitId, string value)
    {
        throw new NotImplementedException("XLIFF 2.0 driver is read-only.");
    }

    public void SortByKey()
    {
        throw new NotImplementedException("XLIFF 2.0 driver is read-only.");
    }

    public IEnumerable<Unit> GetAllUnits()
    {
        foreach (var file in InternalXliff.File)
        {
            foreach (var unit in file.Unit)
            {
                var segment = unit.Segment.FirstOrDefault();
                yield return new Unit
                {
                    Id = unit.Id,
                    Target =
                        segment?.Target != null ? FlattenInline(segment.Target) :
                        segment?.Source != null ? FlattenInline(segment.Source) : "",
                    Source = segment?.Source != null ? FlattenInline(segment.Source) : null,
                    State = segment != null ? MapState(segment.State) : TranslationState.None
                };
            }
        }
    }

    public SyncResult SyncUnit(Unit unit)
    {
        throw new NotImplementedException("XLIFF 2.0 driver is read-only.");
    }

    public MergeResult MergeUnit(Unit unit)
    {
        throw new NotImplementedException("XLIFF 2.0 driver is read-only.");
    }

    public ImportResult ImportUnit(Unit unit)
    {
        throw new NotImplementedException("XLIFF 2.0 driver is read-only.");
    }

    public IEnumerable<string> RetainUnitIds(IEnumerable<string> ids)
    {
        throw new NotImplementedException("XLIFF 2.0 driver is read-only.");
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

    private string FlattenInline(IInline inline)
    {
        return XlfTranslationParser.ExtractText(inline) ?? "";
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
}