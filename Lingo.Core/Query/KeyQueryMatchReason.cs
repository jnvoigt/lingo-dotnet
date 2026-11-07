namespace Lingo.Core.Query;

[Flags]
public enum KeyQueryMatchReason
{
    None = 0,
    Untranslated = 1 << 0,
    Outdated = 1 << 1,
    MissingInLocale = 1 << 2,
    StateMatch = 1 << 3,
    PatternMatch = 1 << 4,
    TextMatch = 1 << 5
}
