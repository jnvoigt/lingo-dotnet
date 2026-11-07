using System.Text.RegularExpressions;
using Lingo.Core.Hierarchy;

namespace Lingo.Core.Query;

public class KeyQueryService
{
    public KeyQueryResult Query(IEnumerable<ILingoDocument> documents, KeyQueryCriteria criteria)
    {
        var docList = documents.ToList();
        var matches = new List<KeyQueryMatch>();

        if (docList.Count == 0)
        {
            return new KeyQueryResult(matches);
        }

        // Collect all distinct key IDs across all documents
        var allKeys = docList
            .SelectMany(d => d.GetAllUnits())
            .Select(u => u.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Target documents to query
        var targetDocs = docList;
        if (!string.IsNullOrWhiteSpace(criteria.TargetLanguage))
        {
            targetDocs = docList.Where(d =>
            {
                var lang = GetDocumentLanguage(d);
                return string.Equals(lang, criteria.TargetLanguage, StringComparison.OrdinalIgnoreCase) ||
                       (lang != null && lang.StartsWith(criteria.TargetLanguage + "-", StringComparison.OrdinalIgnoreCase));
            }).ToList();

            if (targetDocs.Count == 0)
            {
                targetDocs = docList;
            }
        }

        foreach (var key in allKeys)
        {
            // First check if key matches key pattern
            if (!MatchesPattern(key, criteria.KeyPattern, criteria.CaseSensitive))
            {
                continue;
            }

            foreach (var doc in targetDocs)
            {
                var docLang = GetDocumentLanguage(doc);
                var docName = GetDocumentName(doc);
                var unit = doc.GetUnit(key);

                if (unit == null)
                {
                    // Unit is missing in this document
                    if (!criteria.IncludeMissingKeys)
                    {
                        continue;
                    }

                    bool isMatch = false;
                    var reasons = KeyQueryMatchReason.MissingInLocale;

                    if (criteria.FilterMode == KeyFilterMode.MissingOnly)
                    {
                        isMatch = true;
                    }
                    else if (criteria.FilterMode == KeyFilterMode.UntranslatedOnly)
                    {
                        isMatch = true;
                        reasons |= KeyQueryMatchReason.Untranslated;
                    }
                    else if (criteria.FilterMode == KeyFilterMode.All && string.IsNullOrWhiteSpace(criteria.SearchText) && criteria.RequiredState == null)
                    {
                        isMatch = true;
                    }

                    if (isMatch)
                    {
                        matches.Add(new KeyQueryMatch
                        {
                            Key = key,
                            Document = doc,
                            DocumentName = docName,
                            Language = docLang,
                            Unit = null,
                            MatchReasons = reasons
                        });
                    }
                }
                else
                {
                    // Unit exists
                    if (criteria.FilterMode == KeyFilterMode.MissingOnly)
                    {
                        continue;
                    }

                    var reasons = KeyQueryMatchReason.None;
                    bool isUntranslated = unit.State == TranslationState.NeedsTranslation ||
                                          (criteria.IncludeEmptyTargets && string.IsNullOrWhiteSpace(unit.Target));
                    bool isOutdated = unit.State == TranslationState.NeedsAdaptation;

                    if (isUntranslated) reasons |= KeyQueryMatchReason.Untranslated;
                    if (isOutdated) reasons |= KeyQueryMatchReason.Outdated;

                    if (criteria.RequiredState.HasValue && unit.State == criteria.RequiredState.Value)
                    {
                        reasons |= KeyQueryMatchReason.StateMatch;
                    }

                    bool textMatched = MatchesText(unit, criteria.SearchText, criteria.CaseSensitive);
                    if (!string.IsNullOrWhiteSpace(criteria.SearchText) && textMatched)
                    {
                        reasons |= KeyQueryMatchReason.TextMatch;
                    }

                    if (!string.IsNullOrWhiteSpace(criteria.KeyPattern))
                    {
                        reasons |= KeyQueryMatchReason.PatternMatch;
                    }

                    bool matchesMode = criteria.FilterMode switch
                    {
                        KeyFilterMode.All => true,
                        KeyFilterMode.UntranslatedOnly => isUntranslated,
                        KeyFilterMode.OutdatedOnly => isOutdated,
                        KeyFilterMode.TranslatedOnly => unit.State == TranslationState.Translated && !string.IsNullOrWhiteSpace(unit.Target),
                        KeyFilterMode.Custom => (!criteria.RequiredState.HasValue || unit.State == criteria.RequiredState.Value),
                        _ => false
                    };

                    bool matchesSearch = string.IsNullOrWhiteSpace(criteria.SearchText) || textMatched;

                    if (matchesMode && matchesSearch)
                    {
                        matches.Add(new KeyQueryMatch
                        {
                            Key = key,
                            Document = doc,
                            DocumentName = docName,
                            Language = docLang,
                            Unit = unit,
                            MatchReasons = reasons
                        });
                    }
                }
            }
        }

        return new KeyQueryResult(matches);
    }

    public KeyQueryResult Query(ILingoDocument document, KeyQueryCriteria criteria)
    {
        return Query([document], criteria);
    }

    public IReadOnlyList<KeyQueryMatch> GetUntranslatedKeys(IEnumerable<ILingoDocument> documents, string? targetLanguage = null)
    {
        var criteria = KeyQueryCriteria.Untranslated(targetLanguage);
        return Query(documents, criteria).Matches;
    }

    public IReadOnlyList<KeyQueryMatch> GetOutdatedTranslations(IEnumerable<ILingoDocument> documents, string? targetLanguage = null)
    {
        var criteria = KeyQueryCriteria.Outdated(targetLanguage);
        return Query(documents, criteria).Matches;
    }

    public IReadOnlyList<string> GetMissingKeyIds(IEnumerable<ILingoDocument> documents, string targetLanguage)
    {
        var criteria = KeyQueryCriteria.MissingInLocale(targetLanguage);
        return Query(documents, criteria).MatchingKeys;
    }

    public List<KeyNode> FilterHierarchy(IEnumerable<KeyNode> rootNodes, KeyQueryCriteria criteria, IEnumerable<ILingoDocument>? documents = null)
    {
        if (criteria.FilterMode == KeyFilterMode.All &&
            string.IsNullOrWhiteSpace(criteria.KeyPattern) &&
            string.IsNullOrWhiteSpace(criteria.SearchText) &&
            !criteria.RequiredState.HasValue &&
            string.IsNullOrWhiteSpace(criteria.TargetLanguage))
        {
            return CloneHierarchy(rootNodes);
        }

        HashSet<string> matchingKeys;
        if (documents != null)
        {
            var queryResult = Query(documents, criteria);
            matchingKeys = new HashSet<string>(queryResult.MatchingKeys, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            // If no documents provided, query against units attached to KeyNodes
            var allUnits = GetAllUnitsFromHierarchy(rootNodes);
            matchingKeys = new HashSet<string>(
                allUnits.Where(u => MatchesUnitStandalone(u, criteria)).Select(u => u.Id),
                StringComparer.OrdinalIgnoreCase);
        }

        return FilterHierarchyByKeys(rootNodes, matchingKeys);
    }

    public List<KeyNode> FilterHierarchyByKeys(IEnumerable<KeyNode> rootNodes, ISet<string> matchingKeys)
    {
        var result = new List<KeyNode>();

        foreach (var node in rootNodes)
        {
            var filtered = FilterNode(node, matchingKeys);
            if (filtered != null)
            {
                result.Add(filtered);
            }
        }

        return result;
    }

    private static KeyNode? FilterNode(KeyNode node, ISet<string> matchingKeys)
    {
        bool isDirectMatch = node.IsLeaf && matchingKeys.Contains(node.FullPath);
        var filteredChildren = new List<KeyNode>();

        foreach (var child in node.Children)
        {
            var filteredChild = FilterNode(child, matchingKeys);
            if (filteredChild != null)
            {
                filteredChildren.Add(filteredChild);
            }
        }

        if (isDirectMatch || filteredChildren.Count > 0)
        {
            var cloned = new KeyNode
            {
                Name = node.Name,
                FullPath = node.FullPath,
                IsLeaf = node.IsLeaf
            };
            cloned.Units.AddRange(node.Units);

            foreach (var child in filteredChildren)
            {
                child.Parent = cloned;
                cloned.Children.Add(child);
            }

            return cloned;
        }

        return null;
    }

    private static List<KeyNode> CloneHierarchy(IEnumerable<KeyNode> rootNodes)
    {
        var result = new List<KeyNode>();
        foreach (var node in rootNodes)
        {
            result.Add(CloneNode(node, null));
        }
        return result;
    }

    private static KeyNode CloneNode(KeyNode node, KeyNode? parent)
    {
        var clone = new KeyNode
        {
            Name = node.Name,
            FullPath = node.FullPath,
            IsLeaf = node.IsLeaf,
            Parent = parent
        };
        clone.Units.AddRange(node.Units);

        foreach (var child in node.Children)
        {
            clone.Children.Add(CloneNode(child, clone));
        }

        return clone;
    }

    private static List<Unit> GetAllUnitsFromHierarchy(IEnumerable<KeyNode> nodes)
    {
        var units = new List<Unit>();
        foreach (var node in nodes)
        {
            units.AddRange(node.Units);
            if (node.Children.Count > 0)
            {
                units.AddRange(GetAllUnitsFromHierarchy(node.Children));
            }
        }
        return units;
    }

    private static bool MatchesUnitStandalone(Unit unit, KeyQueryCriteria criteria)
    {
        if (!MatchesPattern(unit.Id, criteria.KeyPattern, criteria.CaseSensitive))
            return false;

        if (!string.IsNullOrWhiteSpace(criteria.SearchText) && !MatchesText(unit, criteria.SearchText, criteria.CaseSensitive))
            return false;

        bool isUntranslated = unit.State == TranslationState.NeedsTranslation ||
                              (criteria.IncludeEmptyTargets && string.IsNullOrWhiteSpace(unit.Target));
        bool isOutdated = unit.State == TranslationState.NeedsAdaptation;

        return criteria.FilterMode switch
        {
            KeyFilterMode.All => true,
            KeyFilterMode.UntranslatedOnly => isUntranslated,
            KeyFilterMode.OutdatedOnly => isOutdated,
            KeyFilterMode.TranslatedOnly => unit.State == TranslationState.Translated && !string.IsNullOrWhiteSpace(unit.Target),
            KeyFilterMode.Custom => (!criteria.RequiredState.HasValue || unit.State == criteria.RequiredState.Value),
            _ => true
        };
    }

    private static bool MatchesPattern(string key, string? pattern, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return true;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            return key.Contains(pattern, comparison);
        }

        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        return Regex.IsMatch(key, regexPattern, regexOptions);
    }

    private static bool MatchesText(Unit unit, string? searchText, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return true;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (unit.Id.Contains(searchText, comparison)) return true;
        if (unit.Source != null && unit.Source.Contains(searchText, comparison)) return true;
        if (unit.Target != null && unit.Target.Contains(searchText, comparison)) return true;

        return false;
    }

    private static string? GetDocumentLanguage(ILingoDocument doc)
    {
        if (doc is IHasCultureInfo cultureDoc && cultureDoc.GetTargetCulture() is { } culture)
        {
            return culture.Name;
        }

        return "unknown";
    }

    private static string GetDocumentName(ILingoDocument doc)
    {
        if (doc is IHasCultureInfo cultureDoc && cultureDoc.GetTargetCulture() is { } culture)
        {
            return $"{doc.FormatId} ({culture.Name})";
        }

        return $"{doc.FormatId}";
    }
}
