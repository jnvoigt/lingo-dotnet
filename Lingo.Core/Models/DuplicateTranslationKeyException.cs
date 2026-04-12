namespace Lingo.Core.Models;

public class DuplicateTranslationKeyException : Exception
{
    public string Key { get; }

    private DuplicateTranslationKeyException(string key, string message)
        : base(message)
    {
        Key = key;
    }

    public static DuplicateTranslationKeyException FromKey(string key)
        => new(key, $"{key} has duplicates in document");
}