using V12 = Lingo.Core.Formats.Xliff.V12;
using V20 = Lingo.Core.Formats.Xliff.V20;

namespace Lingo.Core.Parser;

public static class XlfTranslationParser
{
    // used by XLIFF 2.0
    public static string? ExtractText(V20.IInline? inline)
    {
        if (inline == null) return null;
        
        if (inline is V20.Source s && s.Text != null) return string.Concat(s.Text);
        if (inline is V20.Target t && t.Text != null) return string.Concat(t.Text);
        
        return null;
    }

    // used by XLIFF 1.2
    public static string? InnerXml(V12.IElemGroupTextContent? inline)
    {
        if (inline == null) return null;
        
        if (inline is V12.Source s && s.Text != null) return string.Concat(s.Text);
        if (inline is V12.Target t && t.Text != null) return string.Concat(t.Text);
        
        return null;
    }
}
