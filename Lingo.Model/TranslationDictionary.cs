using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lingo.Model.Formatting;

namespace Lingo.Model;

/// <summary>
/// A dictionary holding localized texts indexed by translation key.
/// </summary>
public class TranslationDictionary : ITranslationDictionary, IEquatable<TranslationDictionary>
{
    private readonly Dictionary<string, LocalizedText> _entries;

    public TranslationDictionary(IEqualityComparer<string>? keyComparer = null)
    {
        _entries = new Dictionary<string, LocalizedText>(keyComparer ?? StringComparer.Ordinal);
    }

    public TranslationDictionary(IDictionary<string, LocalizedText> dictionary, IEqualityComparer<string>? keyComparer = null)
    {
        _entries = new Dictionary<string, LocalizedText>(keyComparer ?? StringComparer.Ordinal);
        if (dictionary != null)
        {
            foreach (var kvp in dictionary)
            {
                _entries[kvp.Key] = kvp.Value;
            }
        }
    }

    public string? GetText(string key, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return _entries.TryGetValue(key, out var localized) ? localized.GetText(culture) : null;
    }

    public string? GetText(string key, string? cultureCode)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return _entries.TryGetValue(key, out var localized) ? localized.GetText(cultureCode) : null;
    }

    public string? Format(TranslationSignature signature, CultureInfo? culture = null, ITranslationFormatter? formatter = null)
    {
        if (signature == null) throw new ArgumentNullException(nameof(signature));
        if (!_entries.TryGetValue(signature.Key, out var localized))
        {
            return null;
        }

        return localized.Format(signature, culture, formatter);
    }

    public string? Format(string key, object? args, CultureInfo? culture = null, ITranslationFormatter? formatter = null)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!_entries.TryGetValue(key, out var localized))
        {
            return null;
        }

        return localized.Format(args, culture, formatter);
    }

    public void AddTranslation(string key, string cultureCode, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_entries.TryGetValue(key, out var localizedText))
        {
            localizedText = new LocalizedText();
            _entries[key] = localizedText;
        }

        localizedText[cultureCode] = text;
    }

    /// <summary>
    /// Serializes this translation dictionary to a JSON string.
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(_entries, options);

    /// <summary>
    /// Deserializes a translation dictionary from a JSON string.
    /// </summary>
    public static TranslationDictionary FromJson(string json, JsonSerializerOptions? options = null)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, LocalizedText>>(json, options);
        var result = new TranslationDictionary();
        if (dict != null)
        {
            foreach (var kvp in dict)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    #region IDictionary<string, LocalizedText> & IReadOnlyDictionary<string, LocalizedText>

    public LocalizedText this[string key]
    {
        get => _entries[key];
        set => _entries[key] = value;
    }

    public ICollection<string> Keys => _entries.Keys;

    public ICollection<LocalizedText> Values => _entries.Values;

    IEnumerable<string> IReadOnlyDictionary<string, LocalizedText>.Keys => _entries.Keys;

    IEnumerable<LocalizedText> IReadOnlyDictionary<string, LocalizedText>.Values => _entries.Values;

    public int Count => _entries.Count;

    public bool IsReadOnly => false;

    public void Add(string key, LocalizedText value) => _entries.Add(key, value);

    public void Add(KeyValuePair<string, LocalizedText> item) => _entries.Add(item.Key, item.Value);

    public void Clear() => _entries.Clear();

    public bool Contains(KeyValuePair<string, LocalizedText> item) =>
        ((ICollection<KeyValuePair<string, LocalizedText>>)_entries).Contains(item);

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public void CopyTo(KeyValuePair<string, LocalizedText>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, LocalizedText>>)_entries).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, LocalizedText>> GetEnumerator() => _entries.GetEnumerator();

    public bool Remove(string key) => _entries.Remove(key);

    public bool Remove(KeyValuePair<string, LocalizedText> item) =>
        ((ICollection<KeyValuePair<string, LocalizedText>>)_entries).Remove(item);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out LocalizedText value) =>
        _entries.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    #endregion

    #region Equality

    public bool Equals(TranslationDictionary? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_entries.Count != other._entries.Count) return false;

        foreach (var (k, v) in _entries)
        {
            if (!other._entries.TryGetValue(k, out var otherVal) || !v.Equals(otherVal))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as TranslationDictionary);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (k, v) in _entries.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            hash.Add(k, StringComparer.Ordinal);
            hash.Add(v);
        }
        return hash.ToHashCode();
    }

    #endregion
}
