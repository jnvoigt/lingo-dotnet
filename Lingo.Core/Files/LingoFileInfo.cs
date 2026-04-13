using Lingo.Core.Formats;
using System.Text.RegularExpressions;

namespace Lingo.Core.Files;

public record LingoFileInfo(FileInfo File, string Stub, CultureInfo? Culture, LingoFormat? Format)
{
    private static readonly Regex CultureRegex = new(@"\b([a-z]{2,3}([-_][a-zA-Z]{2,4})?)\b", RegexOptions.Compiled);

    public static LingoFileInfo? FromPath(string path, LingoFormat? expectedFormat = null)
    {
        return FromFile(new FileInfo(path), expectedFormat);
    }

    public static LingoFileInfo? FromFile(FileInfo fileInfo, LingoFormat? expectedFormat = null)
    {
        var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
        var extension = fileInfo.Extension.TrimStart('.').ToLowerInvariant();

        var format = expectedFormat ?? LingoFormat.TryGetFromExtension(extension);
        if (format == null || !format.IsMatch(extension))
        {
            return null;
        }

        // 1. Try to extract culture from filename (e.g., translations.de-DE.xlf)
        var parts = fileName.Split('.');
        if (parts.Length > 1)
        {
            for (var i = parts.Length - 1; i >= 1; i--)
            {
                if (TryParseCulture(parts[i], out var culture))
                {
                    var stub = string.Join(".", parts.Take(i));
                    return new LingoFileInfo(fileInfo, stub, culture, format);
                }
            }
        }

        // 2. Try to extract culture from parent directory (e.g., de-DE/translations.xlf)
        var parentDir = fileInfo.Directory;
        if (parentDir != null)
        {
            if (TryParseCulture(parentDir.Name, out var culture))
            {
                return new LingoFileInfo(fileInfo, fileName, culture, format);
            }
        }

        // No culture found
        return new LingoFileInfo(fileInfo, fileName, null, format);
    }

    public bool IsSibling(LingoFileInfo other)
    {
        return File.FullName != other.File.FullName && Stub == other.Stub &&
               File.DirectoryName == other.File.DirectoryName;
    }

    private static bool TryParseCulture(string input, out CultureInfo? culture)
    {
        try
        {
            // Handle Android-style "values-xx" folders
            var effectiveInput = input;
            if (input.StartsWith("values-", StringComparison.OrdinalIgnoreCase))
            {
                effectiveInput = input["values-".Length..].Replace("-r", "-");
            }

            var match = CultureRegex.Match(effectiveInput);
            if (match.Success)
            {
                culture = CultureInfo.GetCultureInfo(match.Value);
                return true;
            }
        }
        catch (CultureNotFoundException)
        {
        }

        culture = null;
        return false;
    }
}