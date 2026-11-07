namespace Lingo.Core.Query;

public class KeyQueryMatch
{
    public required string Key { get; init; }
    public ILingoDocument? Document { get; init; }
    public string? DocumentName { get; init; }
    public string? Language { get; init; }
    public Unit? Unit { get; init; }
    public string? Source => Unit?.Source;
    public string? Target => Unit?.Target;
    public TranslationState? State => Unit?.State;

    public bool IsMissing => Unit == null;

    public bool IsUntranslated =>
        IsMissing ||
        State == TranslationState.NeedsTranslation ||
        string.IsNullOrWhiteSpace(Target);

    public bool IsOutdated => State == TranslationState.NeedsAdaptation;

    public KeyQueryMatchReason MatchReasons { get; set; } = KeyQueryMatchReason.None;
}
