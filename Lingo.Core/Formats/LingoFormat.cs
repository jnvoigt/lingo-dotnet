namespace Lingo.Core.Formats;

public record LingoFormat(string Id, IReadOnlySet<string> Extensions)
{
    public static readonly LingoFormat Xliff = new("xliff",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "xlf", "xliff" });

    public bool IsMatch(string extension)
    {
        return Extensions.Contains(extension.TrimStart('.'));
    }

    public static LingoFormat? TryGetFromExtension(string extension)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        if (Xliff.IsMatch(ext))
        {
            return Xliff;
        }

        return null;
    }
}