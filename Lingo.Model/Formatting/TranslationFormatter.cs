using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Lingo.Model.Formatting;

/// <summary>
/// Formats ICU MessageFormat strings with variable substitution, plurals, select, selectordinal, number, and date/time formatting.
/// </summary>
public class TranslationFormatter : ITranslationFormatter
{
    public static TranslationFormatter Instance { get; } = new();

    public static string FormatText(string pattern, IReadOnlyDictionary<string, object?>? variables, CultureInfo? culture = null) =>
        Instance.Format(pattern, variables, culture);

    public static string FormatText(string pattern, TranslationSignature signature, CultureInfo? culture = null) =>
        Instance.Format(pattern, signature, culture);

    public static string FormatText(string pattern, object? args, CultureInfo? culture = null) =>
        Instance.Format(pattern, args, culture);

    public string Format(string pattern, TranslationSignature signature, CultureInfo? culture = null)
    {
        if (signature == null) throw new ArgumentNullException(nameof(signature));
        return Format(pattern, signature.GetVariables(), culture);
    }

    public string Format(string pattern, object? args, CultureInfo? culture = null)
    {
        if (args == null)
        {
            return Format(pattern, (IReadOnlyDictionary<string, object?>?)null, culture);
        }

        if (args is TranslationSignature signature)
        {
            return Format(pattern, signature.GetVariables(), culture);
        }

        if (args is IReadOnlyDictionary<string, object?> readOnlyDict)
        {
            return Format(pattern, readOnlyDict, culture);
        }

        if (args is IDictionary dict)
        {
            var dictWrapper = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Key != null)
                {
                    dictWrapper[entry.Key.ToString()!] = entry.Value;
                }
            }
            return Format(pattern, dictWrapper, culture);
        }

        if (args is IEnumerable<KeyValuePair<string, object?>> kvpEnum)
        {
            var dictWrapper = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in kvpEnum)
            {
                dictWrapper[kvp.Key] = kvp.Value;
            }
            return Format(pattern, dictWrapper, culture);
        }

        // Anonymous object or class instance
        var properties = args.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var objDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in properties)
        {
            if (prop.CanRead)
            {
                objDict[prop.Name] = prop.GetValue(args);
            }
        }

        return Format(pattern, objDict, culture);
    }

    public string Format(string pattern, IReadOnlyDictionary<string, object?>? variables, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return string.Empty;
        }

        culture ??= CultureInfo.CurrentUICulture;
        variables ??= new Dictionary<string, object?>();

        var (ast, _) = ParseMessage(pattern, 0, false);
        var sb = new StringBuilder();
        FormatNodeList(ast, variables, culture, null, sb);
        return sb.ToString();
    }

    #region AST & Parsing

    private abstract class AstNode { }

    private sealed class TextNode(string text) : AstNode
    {
        public string Text { get; } = text;
    }

    private sealed class PluralNumberNode : AstNode { }

    private sealed class ArgumentNode : AstNode
    {
        public required string Name { get; init; }
        public string? FormatType { get; init; }
        public string? FormatStyle { get; init; }
        public double PluralOffset { get; set; }
        public Dictionary<string, List<AstNode>> Branches { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static (List<AstNode> Nodes, int NextIndex) ParseMessage(string pattern, int index, bool insideSubMessage)
    {
        var nodes = new List<AstNode>();
        var textBuf = new StringBuilder();

        while (index < pattern.Length)
        {
            var c = pattern[index];

            if (c == '\'')
            {
                // Escaping handling in ICU
                if (index + 1 < pattern.Length && pattern[index + 1] == '\'')
                {
                    textBuf.Append('\'');
                    index += 2;
                    continue;
                }

                // Check for quoted segment like '{' or '{var}' or '#'
                var closingQuote = pattern.IndexOf('\'', index + 1);
                if (closingQuote != -1)
                {
                    var quotedText = pattern.Substring(index + 1, closingQuote - index - 1);
                    textBuf.Append(quotedText);
                    index = closingQuote + 1;
                }
                else
                {
                    textBuf.Append('\'');
                    index++;
                }
                continue;
            }

            if (c == '}' && insideSubMessage)
            {
                if (textBuf.Length > 0)
                {
                    nodes.Add(new TextNode(textBuf.ToString()));
                    textBuf.Clear();
                }
                return (nodes, index + 1);
            }

            if (c == '#')
            {
                if (textBuf.Length > 0)
                {
                    nodes.Add(new TextNode(textBuf.ToString()));
                    textBuf.Clear();
                }
                nodes.Add(new PluralNumberNode());
                index++;
                continue;
            }

            if (c == '{')
            {
                if (textBuf.Length > 0)
                {
                    nodes.Add(new TextNode(textBuf.ToString()));
                    textBuf.Clear();
                }

                var (argNode, nextIdx) = ParseArgument(pattern, index + 1);
                nodes.Add(argNode);
                index = nextIdx;
                continue;
            }

            textBuf.Append(c);
            index++;
        }

        if (textBuf.Length > 0)
        {
            nodes.Add(new TextNode(textBuf.ToString()));
        }

        return (nodes, index);
    }

    private static (AstNode Node, int NextIndex) ParseArgument(string pattern, int index)
    {
        // Read argument name up to ',', '}', or end
        var nameBuf = new StringBuilder();
        while (index < pattern.Length && pattern[index] != ',' && pattern[index] != '}')
        {
            nameBuf.Append(pattern[index]);
            index++;
        }

        var argName = nameBuf.ToString().Trim();

        if (index >= pattern.Length || pattern[index] == '}')
        {
            var next = index < pattern.Length ? index + 1 : index;
            return (new ArgumentNode { Name = argName }, next);
        }

        // Skip comma
        index++;

        // Read formatType up to ',', '}', or end
        var typeBuf = new StringBuilder();
        while (index < pattern.Length && pattern[index] != ',' && pattern[index] != '}')
        {
            typeBuf.Append(pattern[index]);
            index++;
        }

        var formatType = typeBuf.ToString().Trim().ToLowerInvariant();

        if (index >= pattern.Length || pattern[index] == '}')
        {
            var next = index < pattern.Length ? index + 1 : index;
            return (new ArgumentNode
            {
                Name = argName,
                FormatType = formatType
            }, next);
        }

        // Skip comma
        index++;

        if (formatType == "plural" || formatType == "select" || formatType == "selectordinal")
        {
            var argNode = new ArgumentNode
            {
                Name = argName,
                FormatType = formatType
            };

            // Parse branches and optional offset
            double offset = 0;
            while (index < pattern.Length)
            {
                // Skip whitespace
                while (index < pattern.Length && char.IsWhiteSpace(pattern[index]))
                {
                    index++;
                }

                if (index >= pattern.Length || pattern[index] == '}')
                {
                    index++;
                    break;
                }

                // Read branch selector key (e.g. '=0', 'one', 'other', or 'offset:1')
                var keyBuf = new StringBuilder();
                while (index < pattern.Length && !char.IsWhiteSpace(pattern[index]) && pattern[index] != '{' && pattern[index] != '}')
                {
                    keyBuf.Append(pattern[index]);
                    index++;
                }

                var key = keyBuf.ToString().Trim();
                if (key.StartsWith("offset:", StringComparison.OrdinalIgnoreCase))
                {
                    var offsetStr = key.Substring(7);
                    double.TryParse(offsetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out offset);
                    continue;
                }

                // Skip whitespace to '{'
                while (index < pattern.Length && char.IsWhiteSpace(pattern[index]))
                {
                    index++;
                }

                if (index < pattern.Length && pattern[index] == '{')
                {
                    var (subNodes, nextIdx) = ParseMessage(pattern, index + 1, true);
                    argNode.Branches[key] = subNodes;
                    index = nextIdx;
                }
            }

            argNode.PluralOffset = offset;
            return (argNode, index);
        }
        else
        {
            // Simple format style (number/date/time style or custom format pattern)
            var styleBuf = new StringBuilder();
            var braceDepth = 0;
            while (index < pattern.Length)
            {
                var c = pattern[index];
                if (c == '{')
                {
                    braceDepth++;
                    styleBuf.Append(c);
                }
                else if (c == '}')
                {
                    if (braceDepth > 0)
                    {
                        braceDepth--;
                        styleBuf.Append(c);
                    }
                    else
                    {
                        index++;
                        break;
                    }
                }
                else
                {
                    styleBuf.Append(c);
                }
                index++;
            }

            var formatStyle = styleBuf.ToString().Trim();
            return (new ArgumentNode
            {
                Name = argName,
                FormatType = formatType,
                FormatStyle = formatStyle
            }, index);
        }
    }

    #endregion

    #region Formatting Evaluation

    private static void FormatNodeList(
        List<AstNode> nodes,
        IReadOnlyDictionary<string, object?> variables,
        CultureInfo culture,
        double? currentPluralNumber,
        StringBuilder sb)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case TextNode textNode:
                    sb.Append(textNode.Text);
                    break;

                case PluralNumberNode:
                    if (currentPluralNumber.HasValue)
                    {
                        sb.Append(currentPluralNumber.Value.ToString("N0", culture));
                    }
                    else
                    {
                        sb.Append('#');
                    }
                    break;

                case ArgumentNode argNode:
                    FormatArgumentNode(argNode, variables, culture, sb);
                    break;
            }
        }
    }

    private static void FormatArgumentNode(
        ArgumentNode node,
        IReadOnlyDictionary<string, object?> variables,
        CultureInfo culture,
        StringBuilder sb)
    {
        object? value = null;
        if (!TryGetVariableValue(variables, node.Name, out value))
        {
            // If variable not found, leave placeholder or empty
            sb.Append('{').Append(node.Name).Append('}');
            return;
        }

        if (string.IsNullOrEmpty(node.FormatType))
        {
            sb.Append(FormatSimpleValue(value, culture));
            return;
        }

        switch (node.FormatType)
        {
            case "number":
                sb.Append(FormatNumber(value, node.FormatStyle, culture));
                break;

            case "date":
                sb.Append(FormatDate(value, node.FormatStyle, culture));
                break;

            case "time":
                sb.Append(FormatTime(value, node.FormatStyle, culture));
                break;

            case "select":
                FormatSelect(node, value, variables, culture, sb);
                break;

            case "plural":
                FormatPlural(node, value, variables, culture, false, sb);
                break;

            case "selectordinal":
                FormatPlural(node, value, variables, culture, true, sb);
                break;

            default:
                sb.Append(FormatSimpleValue(value, culture));
                break;
        }
    }

    private static bool TryGetVariableValue(IReadOnlyDictionary<string, object?> variables, string name, out object? value)
    {
        if (variables.TryGetValue(name, out value))
        {
            return true;
        }

        foreach (var kvp in variables)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string FormatSimpleValue(object? value, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, culture);
        }
        return value.ToString() ?? string.Empty;
    }

    private static string FormatNumber(object? value, string? style, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        if (!TryConvertToDouble(value, out var dbl))
        {
            return value.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(style))
        {
            return dbl.ToString(culture);
        }

        var styleLower = style.ToLowerInvariant();
        return styleLower switch
        {
            "integer" => ((long)Math.Round(dbl)).ToString(culture),
            "currency" => dbl.ToString("C", culture),
            "percent" => dbl.ToString("P", culture),
            _ => dbl.ToString(style, culture)
        };
    }

    private static string FormatDate(object? value, string? style, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        DateTime dt;
        if (value is DateTime dateTime)
        {
            dt = dateTime;
        }
        else if (value is DateTimeOffset dto)
        {
            dt = dto.DateTime;
        }
        else if (value is DateOnly dateOnly)
        {
            dt = dateOnly.ToDateTime(TimeOnly.MinValue);
        }
        else if (DateTime.TryParse(value.ToString(), culture, DateTimeStyles.None, out var parsed))
        {
            dt = parsed;
        }
        else
        {
            return value.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(style) || style.Equals("short", StringComparison.OrdinalIgnoreCase))
        {
            return dt.ToString("d", culture);
        }

        if (style.Equals("medium", StringComparison.OrdinalIgnoreCase) || style.Equals("long", StringComparison.OrdinalIgnoreCase) || style.Equals("full", StringComparison.OrdinalIgnoreCase))
        {
            return dt.ToString("D", culture);
        }

        return dt.ToString(style, culture);
    }

    private static string FormatTime(object? value, string? style, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        TimeOnly to;
        if (value is TimeOnly timeOnly)
        {
            to = timeOnly;
        }
        else if (value is DateTime dt)
        {
            to = TimeOnly.FromDateTime(dt);
        }
        else if (value is DateTimeOffset dto)
        {
            to = TimeOnly.FromDateTime(dto.DateTime);
        }
        else if (TimeOnly.TryParse(value.ToString(), culture, DateTimeStyles.None, out var parsed))
        {
            to = parsed;
        }
        else
        {
            return value.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(style) || style.Equals("short", StringComparison.OrdinalIgnoreCase))
        {
            return to.ToString("t", culture);
        }

        if (style.Equals("medium", StringComparison.OrdinalIgnoreCase) || style.Equals("long", StringComparison.OrdinalIgnoreCase) || style.Equals("full", StringComparison.OrdinalIgnoreCase))
        {
            return to.ToString("T", culture);
        }

        return to.ToString(style, culture);
    }

    private static void FormatSelect(
        ArgumentNode node,
        object? value,
        IReadOnlyDictionary<string, object?> variables,
        CultureInfo culture,
        StringBuilder sb)
    {
        var strValue = value?.ToString() ?? string.Empty;

        List<AstNode>? branchNodes = null;
        if (!node.Branches.TryGetValue(strValue, out branchNodes))
        {
            node.Branches.TryGetValue("other", out branchNodes);
        }

        if (branchNodes != null)
        {
            FormatNodeList(branchNodes, variables, culture, null, sb);
        }
    }

    private static void FormatPlural(
        ArgumentNode node,
        object? value,
        IReadOnlyDictionary<string, object?> variables,
        CultureInfo culture,
        bool isOrdinal,
        StringBuilder sb)
    {
        if (!TryConvertToDouble(value, out var num))
        {
            num = 0;
        }

        var exactKey = $"={num.ToString(CultureInfo.InvariantCulture)}";
        List<AstNode>? branchNodes = null;

        if (node.Branches.TryGetValue(exactKey, out branchNodes))
        {
            var pluralNum = num - node.PluralOffset;
            FormatNodeList(branchNodes, variables, culture, pluralNum, sb);
            return;
        }

        var offsetNum = num - node.PluralOffset;
        var category = isOrdinal
            ? PluralRules.SelectOrdinal(offsetNum, culture)
            : PluralRules.SelectCardinal(offsetNum, culture);

        var catName = category.ToString().ToLowerInvariant();

        if (!node.Branches.TryGetValue(catName, out branchNodes))
        {
            node.Branches.TryGetValue("other", out branchNodes);
        }

        if (branchNodes != null)
        {
            FormatNodeList(branchNodes, variables, culture, offsetNum, sb);
        }
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        if (value == null)
        {
            result = 0;
            return false;
        }

        switch (value)
        {
            case sbyte sb: result = sb; return true;
            case byte b: result = b; return true;
            case short s: result = s; return true;
            case ushort us: result = us; return true;
            case int i: result = i; return true;
            case uint ui: result = ui; return true;
            case long l: result = l; return true;
            case ulong ul: result = ul; return true;
            case float f: result = f; return true;
            case double d: result = d; return true;
            case decimal dec: result = (double)dec; return true;
            case string str:
                return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                    || double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
            default:
                try
                {
                    result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    result = 0;
                    return false;
                }
        }
    }

    #endregion
}
