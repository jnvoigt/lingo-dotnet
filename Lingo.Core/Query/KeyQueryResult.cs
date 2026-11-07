namespace Lingo.Core.Query;

public class KeyQueryResult
{
    public IReadOnlyList<KeyQueryMatch> Matches { get; }

    public KeyQueryResult(IEnumerable<KeyQueryMatch> matches)
    {
        Matches = matches.ToList().AsReadOnly();
    }

    public IReadOnlyList<string> MatchingKeys =>
        Matches.Select(m => m.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();

    public int TotalMatches => Matches.Count;

    public int TotalDistinctKeys => MatchingKeys.Count;

    public int UntranslatedCount => Matches.Count(m => m.IsUntranslated);

    public int OutdatedCount => Matches.Count(m => m.IsOutdated);

    public int MissingCount => Matches.Count(m => m.IsMissing);

    public IReadOnlyDictionary<string, List<KeyQueryMatch>> ByKey =>
        Matches.GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
               .ToDictionary(g => g.Key, g => g.ToList());

    public IReadOnlyDictionary<string, List<KeyQueryMatch>> ByLanguage =>
        Matches.Where(m => !string.IsNullOrEmpty(m.Language))
               .GroupBy(m => m.Language!, StringComparer.OrdinalIgnoreCase)
               .ToDictionary(g => g.Key, g => g.ToList());
}
