namespace Lingo.Core.Parser;

public class XlfTranslationParserOptions
{
    // If true and target text is missing, fall back to source text
    public bool InheritSource { get; set; } = false;
}
