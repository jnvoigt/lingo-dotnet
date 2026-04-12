namespace Lingo.Core.Models;

// Sync outcomes when pushing source→target
public enum SyncResult
{
    Nothing,
    NewUnitCreated,
    TargetNeedsTranslation,
    SourceValueHasChanged,
    DuplicateTranslationKey
}

// Merge outcomes (insert-only semantics)
public enum MergeResult
{
    Merged,
    Conflict,
    DuplicateTranslationKey
}

// Import (pull sync) outcomes
public enum ImportResult
{
    Ignored,
    AlreadyUpToDate,
    Imported,
    Conflict,
    DuplicateTranslationKey
}

// Translation state — only meaningful for formats that support it
public enum TranslationState
{
    None,
    NeedsTranslation,
    NeedsAdaptation,
    Translated
}

// Unit metadata type
public enum UnitType
{
    Default,
    TranslationKeyMissing,
    DuplicateTranslationKey
}