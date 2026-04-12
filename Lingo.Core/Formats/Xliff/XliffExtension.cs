using Lingo.Core.Formats.Xliff.V12;
using Lingo.Core.Formats.Xliff.V20;
using Lingo.Core.Parser;

namespace Lingo.Core.Formats.Xliff;

internal static class XliffExtension
{
    public static string? FlattenInline(this IInline? inline)
    {
        return inline == null ? null : XlfTranslationParser.ExtractText(inline);
    }

    public static string? FlattenInline(this IElemGroupTextContent? inline)
    {
        return inline == null ? null : XlfTranslationParser.InnerXml(inline);
    }
}