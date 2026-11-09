namespace Lingo.Core.Query;

public class KeyQueryCriteria
{
    public KeyFilterMode FilterMode { get; set; } = KeyFilterMode.All;

    public string? KeyPattern { get; set; }

    public string? SearchText { get; set; }

    public TranslationState? RequiredState { get; set; }

    public string? TargetLanguage { get; set; }

    public bool IncludeMissingKeys { get; set; } = true;

    public bool IncludeEmptyTargets { get; set; } = true;

    public bool CaseSensitive { get; set; } = false;

    public static KeyQueryCriteria All() => new() { FilterMode = KeyFilterMode.All };

    public static KeyQueryCriteria Untranslated(string? targetLanguage = null) => new()
    {
        FilterMode = KeyFilterMode.UntranslatedOnly,
        TargetLanguage = targetLanguage
    };

    public static KeyQueryCriteria Outdated(string? targetLanguage = null) => new()
    {
        FilterMode = KeyFilterMode.OutdatedOnly,
        TargetLanguage = targetLanguage
    };

    public static KeyQueryCriteria NeedsReview(string? targetLanguage = null) => new()
    {
        FilterMode = KeyFilterMode.NeedsReviewOnly,
        TargetLanguage = targetLanguage
    };

    public static KeyQueryCriteria MissingInLocale(string targetLanguage) => new()
    {
        FilterMode = KeyFilterMode.MissingOnly,
        TargetLanguage = targetLanguage
    };

    public static KeyQueryCriteria ForState(TranslationState state, string? targetLanguage = null) => new()
    {
        FilterMode = KeyFilterMode.Custom,
        RequiredState = state,
        TargetLanguage = targetLanguage
    };

    public static KeyQueryCriteria Search(string searchText) => new()
    {
        FilterMode = KeyFilterMode.Custom,
        SearchText = searchText
    };

    public static KeyQueryCriteria WithPattern(string pattern) => new()
    {
        FilterMode = KeyFilterMode.Custom,
        KeyPattern = pattern
    };
}
