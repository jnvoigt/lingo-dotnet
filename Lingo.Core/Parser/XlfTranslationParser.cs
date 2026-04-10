namespace Lingo.Core.Parser;

public static class XlfTranslationParser
{
    // used by XLIFF 2.0
    public static string? ExtractText(XContainer? container)
    {
        if (container == null) return null;
        var sb = new StringBuilder();
        foreach (var node in container.DescendantNodes())
        {
            if (node is XText text) sb.Append(text.Value);
            else if (node is XElement el && el.Attribute("id") != null) 
                sb.Append($"{{${el.Attribute("id")!.Value}}}");
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    // used by XLIFF 1.2
    public static string? InnerXml(XContainer? container)
    {
        if (container == null) return null;
        var sb = new StringBuilder();
        foreach (var node in container.DescendantNodes())
        {
            if (node is XText text) sb.Append(text.Value);
            else if (node is XElement el) 
            {
                var copy = new XElement(el);
                copy.Attributes().Where(a => a.IsNamespaceDeclaration).Remove();
                sb.Append(copy.ToString(SaveOptions.DisableFormatting));
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}
