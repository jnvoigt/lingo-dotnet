using System.Globalization;
using Lingo.Model.Formatting;

namespace Lingo.Model;

public interface ITranslationDictionary : IDictionary<string, LocalizedText>, IReadOnlyDictionary<string, LocalizedText>
{
    /// <summary>
    /// Gets the localized text for a key based on the current thread UI culture (or specified culture), or null if key is not found.
    /// </summary>
    string? Get(string key, CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized text for a key based on the specified culture code (or current thread UI culture if null), or null if key is not found.
    /// </summary>
    string? Get(string key, string? cultureCode);

    /// <summary>
    /// Gets the localized text for a key in the specified culture, or null if key is not found.
    /// </summary>
    string? GetText(string key, CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized text for a key in the specified culture code, or null if key is not found.
    /// </summary>
    string? GetText(string key, string? cultureCode);

    /// <summary>
    /// Formats the localized text for the translation signature using its variables.
    /// </summary>
    string? Format(TranslationSignature signature, CultureInfo? culture = null, ITranslationFormatter? formatter = null);

    /// <summary>
    /// Formats the localized text for the given key and arguments.
    /// </summary>
    string? Format(string key, object? args, CultureInfo? culture = null, ITranslationFormatter? formatter = null);

    /// <summary>
    /// Adds or updates a translation text for a given key and culture code.
    /// </summary>
    void AddTranslation(string key, string cultureCode, string text);
}
