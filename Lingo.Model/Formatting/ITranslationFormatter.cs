using System.Globalization;

namespace Lingo.Model.Formatting;

public interface ITranslationFormatter
{
    /// <summary>
    /// Formats an ICU message pattern with named/positional variables.
    /// </summary>
    string Format(string pattern, IReadOnlyDictionary<string, object?>? variables, CultureInfo? culture = null);

    /// <summary>
    /// Formats an ICU message pattern using variables extracted from a translation signature.
    /// </summary>
    string Format(string pattern, TranslationSignature signature, CultureInfo? culture = null);

    /// <summary>
    /// Formats an ICU message pattern using variables extracted from an object or dictionary.
    /// </summary>
    string Format(string pattern, object? args, CultureInfo? culture = null);
}
