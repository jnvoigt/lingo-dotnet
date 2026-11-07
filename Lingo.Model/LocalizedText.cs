using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lingo.Model.Formatting;

namespace Lingo.Model;

/// <summary>
/// Represents localized text across various culture codes (e.g., "en-US", "de-DE", "de-CH"),
/// using "" (empty string) as the default culture / fallback value.
/// Implements IDictionary&lt;string, string&gt; and IReadOnlyDictionary&lt;string, string&gt;.
/// </summary>
[JsonConverter(typeof(LocalizedTextJsonConverter))]
public class LocalizedText : IDictionary<string, string>, IReadOnlyDictionary<string, string>, IEquatable<LocalizedText>
{
    public const string DefaultCultureKey = "";

    private readonly Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

    public LocalizedText()
    {
    }

    public LocalizedText(string defaultText)
    {
        _translations[DefaultCultureKey] = defaultText;
    }

    public LocalizedText(IDictionary<string, string> translations)
    {
        if (translations != null)
        {
            foreach (var kvp in translations)
            {
                _translations[kvp.Key] = kvp.Value;
            }
        }
    }

    public LocalizedText(params (string Culture, string Text)[] entries)
    {
        if (entries != null)
        {
            foreach (var (culture, text) in entries)
            {
                _translations[culture] = text;
            }
        }
    }

    public LocalizedText(IEnumerable<KeyValuePair<string, string>> entries)
    {
        if (entries != null)
        {
            foreach (var kvp in entries)
            {
                _translations[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the translation for the default culture ("").
    /// </summary>
    public string? DefaultText
    {
        get => _translations.TryGetValue(DefaultCultureKey, out var text) ? text : null;
        set
        {
            if (value != null)
            {
                _translations[DefaultCultureKey] = value;
            }
            else
            {
                _translations.Remove(DefaultCultureKey);
            }
        }
    }

    /// <summary>
    /// Gets the text for the specified culture code, falling back to parent culture and default culture ("") if necessary.
    /// </summary>
    public string? GetText(string? cultureCode = null)
    {
        if (string.IsNullOrEmpty(cultureCode))
        {
            if (_translations.TryGetValue(DefaultCultureKey, out var defaultText))
            {
                return defaultText;
            }

            return _translations.Count > 0 ? _translations.Values.FirstOrDefault() : null;
        }

        // 1. Exact match
        if (_translations.TryGetValue(cultureCode, out var exactMatch))
        {
            return exactMatch;
        }

        // 2. Parent culture match (e.g. "de" for "de-CH" or "de-DE")
        var dashIndex = cultureCode.IndexOf('-');
        if (dashIndex > 0)
        {
            var parentCode = cultureCode.Substring(0, dashIndex);
            if (_translations.TryGetValue(parentCode, out var parentMatch))
            {
                return parentMatch;
            }
        }

        // 3. Fallback to default culture ("")
        if (_translations.TryGetValue(DefaultCultureKey, out var fallbackText))
        {
            return fallbackText;
        }

        return null;
    }

    /// <summary>
    /// Gets the text for the specified CultureInfo, falling back through culture hierarchy and default culture ("").
    /// </summary>
    public string? GetText(CultureInfo? culture)
    {
        if (culture == null || string.IsNullOrEmpty(culture.Name))
        {
            return GetText(DefaultCultureKey);
        }

        var current = culture;
        while (current != null && !string.IsNullOrEmpty(current.Name))
        {
            if (_translations.TryGetValue(current.Name, out var text))
            {
                return text;
            }

            current = current.Parent;
        }

        return GetText(DefaultCultureKey);
    }

    /// <summary>
    /// Gets the text for the culture, or returns the provided default fallback text if no translation is found.
    /// </summary>
    public string GetTextOrDefault(string? cultureCode, string defaultFallback) =>
        GetText(cultureCode) ?? defaultFallback;

    /// <summary>
    /// Formats the translation text for the specified culture using variables from a translation signature.
    /// </summary>
    public string Format(TranslationSignature signature, CultureInfo? culture = null, ITranslationFormatter? formatter = null)
    {
        if (signature == null) throw new ArgumentNullException(nameof(signature));
        return Format(signature.GetVariables(), culture, formatter);
    }

    /// <summary>
    /// Formats the translation text for the specified culture using given variables and ICU formatter.
    /// </summary>
    public string Format(IReadOnlyDictionary<string, object?>? variables, CultureInfo? culture = null, ITranslationFormatter? formatter = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        formatter ??= TranslationFormatter.Instance;

        var pattern = GetText(culture) ?? string.Empty;
        return formatter.Format(pattern, variables, culture);
    }

    /// <summary>
    /// Formats the translation text for the specified culture using given arguments object and ICU formatter.
    /// </summary>
    public string Format(object? args, CultureInfo? culture = null, ITranslationFormatter? formatter = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        formatter ??= TranslationFormatter.Instance;

        var pattern = GetText(culture) ?? string.Empty;
        return formatter.Format(pattern, args, culture);
    }

    /// <summary>
    /// Serializes this LocalizedText to a JSON dictionary string.
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(this, options);

    /// <summary>
    /// Deserializes a LocalizedText from a JSON dictionary string.
    /// </summary>
    public static LocalizedText FromJson(string json, JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<LocalizedText>(json, options) ?? new LocalizedText();

    #region IDictionary<string, string> & IReadOnlyDictionary<string, string>

    public string this[string key]
    {
        get
        {
            key ??= DefaultCultureKey;
            return _translations[key];
        }
        set
        {
            key ??= DefaultCultureKey;
            _translations[key] = value;
        }
    }

    public ICollection<string> Keys => _translations.Keys;

    public ICollection<string> Values => _translations.Values;

    IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => _translations.Keys;

    IEnumerable<string> IReadOnlyDictionary<string, string>.Values => _translations.Values;

    public int Count => _translations.Count;

    public bool IsReadOnly => false;

    public void Add(string key, string value)
    {
        key ??= DefaultCultureKey;
        _translations.Add(key, value);
    }

    public void Add(KeyValuePair<string, string> item)
    {
        var key = item.Key ?? DefaultCultureKey;
        _translations.Add(key, item.Value);
    }

    public void Clear() => _translations.Clear();

    public bool Contains(KeyValuePair<string, string> item) =>
        ((ICollection<KeyValuePair<string, string>>)_translations).Contains(
            new KeyValuePair<string, string>(item.Key ?? DefaultCultureKey, item.Value));

    public bool ContainsKey(string key) =>
        _translations.ContainsKey(key ?? DefaultCultureKey);

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, string>>)_translations).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _translations.GetEnumerator();

    public bool Remove(string key) => _translations.Remove(key ?? DefaultCultureKey);

    public bool Remove(KeyValuePair<string, string> item) =>
        ((ICollection<KeyValuePair<string, string>>)_translations).Remove(
            new KeyValuePair<string, string>(item.Key ?? DefaultCultureKey, item.Value));

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) =>
        _translations.TryGetValue(key ?? DefaultCultureKey, out value);

    IEnumerator IEnumerable.GetEnumerator() => _translations.GetEnumerator();

    #endregion

    #region Equality & Object

    public bool Equals(LocalizedText? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_translations.Count != other._translations.Count) return false;

        foreach (var (k, v) in _translations)
        {
            if (!other._translations.TryGetValue(k, out var otherVal) || otherVal != v)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as LocalizedText);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (k, v) in _translations.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(k, StringComparer.OrdinalIgnoreCase);
            hash.Add(v);
        }
        return hash.ToHashCode();
    }

    public override string ToString() => GetText() ?? string.Empty;

    #endregion
}

/// <summary>
/// Custom JSON Converter for LocalizedText to serialize and deserialize as a JSON dictionary:
/// { "en-US": "...", "de-DE": "...", "": "..." }
/// </summary>
public class LocalizedTextJsonConverter : JsonConverter<LocalizedText>
{
    public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token but found {reader.TokenType}");
        }

        var localizedText = new LocalizedText();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return localizedText;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token but found {reader.TokenType}");
            }

            var propertyName = reader.GetString() ?? string.Empty;

            reader.Read();
            var propertyValue = reader.GetString() ?? string.Empty;

            localizedText[propertyName] = propertyValue;
        }

        return localizedText;
    }

    public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WriteString(kvp.Key, kvp.Value);
        }

        writer.WriteEndObject();
    }
}
