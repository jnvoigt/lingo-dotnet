using System.Reflection;

namespace Lingo.Model;

/// <summary>
/// Base record class to define a translation key.
/// Can have variables in derived primary constructor / properties for substitution into ICU text.
/// </summary>
public record TranslationSignature
{
    public string Key { get; init; }

    public TranslationSignature(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
    }

    /// <summary>
    /// Extracts variables defined on this signature (such as constructor parameters/properties)
    /// for substitution into ICU formatted translations.
    /// </summary>
    public virtual IReadOnlyDictionary<string, object?> GetVariables()
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in properties)
        {
            if (prop.Name == nameof(Key) || prop.Name == "EqualityContract")
            {
                continue;
            }

            if (prop.CanRead)
            {
                dict[prop.Name] = prop.GetValue(this);
            }
        }

        return dict;
    }

    public static implicit operator string(TranslationSignature signature) => signature.Key;

    public override string ToString() => Key;
}
