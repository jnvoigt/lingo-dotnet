using Lingo.Core.Formats.Xliff.V12;
using File = Lingo.Core.Formats.Xliff.V12.File;

namespace Lingo.Core.Formats.Xliff.v12;

public class Xliff12Document : IXliffDocument, IHasSourceValue, IHasTranslationState, IHasCultureInfo
{
    public Xliff12Document(V12.Xliff xliff)
    {
        InternalXliff = xliff;
    }

    public V12.Xliff InternalXliff { get; }

    public CultureInfo GetSourceCulture()
    {
        var file = InternalXliff.File.FirstOrDefault();
        return string.IsNullOrEmpty(file?.SourceLanguage)
            ? CultureInfo.InvariantCulture
            : new CultureInfo(file.SourceLanguage);
    }

    public CultureInfo? GetTargetCulture()
    {
        var file = InternalXliff.File.FirstOrDefault();
        return string.IsNullOrEmpty(file?.TargetLanguage) ? null : new CultureInfo(file.TargetLanguage);
    }

    public string? GetSourceValue(string unitId)
    {
        var tu = FindTransUnit(unitId);
        return tu?.Source.FlattenInline();
    }

    public TranslationState GetTargetState(string unitId)
    {
        var tu = FindTransUnit(unitId);
        return tu?.Target != null ? MapState(tu.Target.State) : TranslationState.None;
    }

    object IXliffDocument.InternalXliff => InternalXliff;
    public Type XliffType => typeof(V12.Xliff);


    public string FormatId => "xliff-1.2";

    public HashSet<string> GetUnitIds()
    {
        var ids = new HashSet<string>();
        foreach (var file in InternalXliff.File)
        {
            if (file.Body?.TransUnit != null)
            {
                foreach (var tu in file.Body.TransUnit)
                {
                    if (!string.IsNullOrEmpty(tu.Id))
                    {
                        ids.Add(tu.Id);
                    }
                }
            }
        }

        return ids;
    }

    public Unit? GetUnit(string unitId)
    {
        var tu = FindTransUnit(unitId);
        if (tu == null)
        {
            return null;
        }

        return ConvertToUnit(tu);
    }

    public string? GetValue(string unitId)
    {
        var tu = FindTransUnit(unitId);
        if (tu == null)
        {
            return null;
        }

        if (tu.Target != null)
        {
            return tu.Target.FlattenInline();
        }

        return tu.Source.FlattenInline();
    }

    public void SetValue(string unitId, string value)
    {
        var tu = FindTransUnit(unitId);
        if (tu == null)
        {
            return;
        }

        if (tu.Target == null)
        {
            tu.Target = new Target();
        }

        tu.Target.Text = new[] { value };
        // Clear other inline collections
        tu.Target.GProperty.Clear();
        tu.Target.Bpt.Clear();
        tu.Target.Ept.Clear();
        tu.Target.Ph.Clear();
        tu.Target.It.Clear();
        tu.Target.MrkProperty.Clear();
        tu.Target.X.Clear();
        tu.Target.Bx.Clear();
        tu.Target.Ex.Clear();

        tu.Target.State = "translated";
    }

    public void SortByKey()
    {
        foreach (var file in InternalXliff.File)
        {
            if (file.Body?.TransUnit != null)
            {
                var sorted = file.Body.TransUnit.OrderBy(u => u.Id).ToList();
                file.Body.TransUnit.Clear();
                foreach (var tu in sorted)
                {
                    file.Body.TransUnit.Add(tu);
                }
            }
        }
    }

    public IEnumerable<Unit> GetAllUnits()
    {
        foreach (var file in InternalXliff.File)
        {
            if (file.Body?.TransUnit != null)
            {
                foreach (var tu in file.Body.TransUnit)
                {
                    yield return ConvertToUnit(tu);
                }
            }
        }
    }

    public SyncResult SyncUnit(Unit unit)
    {
        var existing = FindTransUnit(unit.Id);
        if (existing == null)
        {
            var file = InternalXliff.File.FirstOrDefault();
            if (file == null)
            {
                file = new File
                {
                    SourceLanguage = GetSourceCulture().Name, Original = "manual", Datatype = "plaintext"
                };
                InternalXliff.File.Add(file);
            }

            if (file.Body == null)
            {
                file.Body = new Body();
            }

            var tu = new TransUnit { Id = unit.Id };
            tu.Source = new Source { Text = [unit.Source] };
            if (!string.IsNullOrEmpty(unit.Target))
            {
                tu.Target = new Target { State = "translated", Text = [unit.Target] };
            }
            else
            {
                tu.Target = new Target { State = "needs-translation", Text = null };
            }

            file.Body.TransUnit.Add(tu);
            return SyncResult.NewUnitCreated;
        }

        var changed = false;
        var newSource = unit.Source?.Trim();
        var oldSource = existing.Source.FlattenInline();

        if (oldSource != newSource)
        {
            existing.Source = new Source { Text = [newSource] };
            if (existing.Target == null)
            {
                existing.Target = new Target();
            }

            existing.Target.State = "needs-adaptation";

            changed = true;
        }
        else
        {
            var newTargetValue = unit.Target;
            var oldTargetValue = existing.Target.FlattenInline();

            if (newTargetValue != null && oldTargetValue != newTargetValue)
            {
                if (existing.Target == null)
                {
                    existing.Target = new Target();
                }

                existing.Target.Text = [newTargetValue];
                changed = true;
            }
        }

        return changed ? SyncResult.SourceValueHasChanged : SyncResult.Nothing;
    }

    public MergeResult MergeUnit(Unit unit)
    {
        var existing = FindTransUnit(unit.Id);
        if (existing != null)
        {
            return MergeResult.Conflict;
        }

        SyncUnit(unit);
        return MergeResult.Merged;
    }

    public ImportResult ImportUnit(Unit unit)
    {
        var existing = FindTransUnit(unit.Id);
        if (existing == null)
        {
            return ImportResult.Ignored;
        }

        var currentValue = existing.Target.FlattenInline();
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
            if (file.Body?.TransUnit != null)
            {
                var toRemove = file.Body.TransUnit.Where(u => !idSet.Contains(u.Id)).ToList();
                foreach (var u in toRemove)
                {
                    file.Body.TransUnit.Remove(u);
                    removed.Add(u.Id);
                }
            }
        }

        return removed;
    }

    private TransUnit? FindTransUnit(string id)
    {
        foreach (var file in InternalXliff.File)
        {
            if (file.Body?.TransUnit != null)
            {
                var tu = file.Body.TransUnit.FirstOrDefault(u => u.Id == id);
                if (tu != null)
                {
                    return tu;
                }
            }
        }

        return null;
    }

    private TranslationState MapState(string? state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return TranslationState.None;
        }

        return state.ToLowerInvariant() switch
        {
            "needs-translation" => TranslationState.NeedsTranslation,
            "needs-adaptation" => TranslationState.NeedsAdaptation,
            "translated" => TranslationState.Translated,
            "final" => TranslationState.Translated,
            "signed-off" => TranslationState.Translated,
            _ => TranslationState.None
        };
    }

    private Unit ConvertToUnit(TransUnit tu)
    {
        return new Unit
        {
            Id = tu.Id,
            Target = tu.Target.FlattenInline(),
            Source = tu.Source.FlattenInline(),
            State = MapState(tu.Target?.State)
        };
    }
}