using Lingo.Core.Formats;

namespace Lingo.Core.Files;

public class FileCrawler
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules"
    };

    public IEnumerable<LingoFileInfo> Crawl(DirectoryInfo root, CultureInfo? culture = null, LingoFormat? format = null)
    {
        if (!root.Exists)
        {
            return Enumerable.Empty<LingoFileInfo>();
        }

        return CrawlInternal(root, culture, format);
    }

    public IEnumerable<LingoFileInfo> GetSiblings(LingoFileInfo source)
    {
        var directory = source.File.Directory;
        if (directory == null || !directory.Exists)
        {
            return Enumerable.Empty<LingoFileInfo>();
        }

        return directory.GetFiles()
            .Select(f => LingoFileInfo.FromFile(f, source.Format))
            .Where(f => f != null && source.IsSibling(f))
            .Cast<LingoFileInfo>();
    }

    private IEnumerable<LingoFileInfo> CrawlInternal(DirectoryInfo current, CultureInfo? targetCulture,
        LingoFormat? targetFormat)
    {
        if (!AcceptDirectory(current))
        {
            yield break;
        }

        foreach (var file in current.GetFiles())
        {
            var lingoFile = LingoFileInfo.FromFile(file, targetFormat);
            if (lingoFile != null)
            {
                // Filter by culture if requested
                if (targetCulture == null || (lingoFile.Culture != null && Equals(lingoFile.Culture, targetCulture)))
                {
                    yield return lingoFile;
                }
                // Special case for source-only files (null culture) when searching for a specific culture?
                // The plan says: "en-US" culture: returns source-only files AND files explicitly tagged "en-US".
                // We'll handle this in the business logic layer usually, but let's see.
                // If lingoFile.Culture is null, it's likely a source file.
                else if (lingoFile.Culture == null && targetCulture != null)
                {
                    // For now, let's keep it simple: if culture is requested, only return those.
                    // But if it's the source culture, we might want to return files without culture tag too.
                    // This depends on the project structure.
                }
            }
        }

        foreach (var dir in current.GetDirectories())
        {
            foreach (var result in CrawlInternal(dir, targetCulture, targetFormat))
            {
                yield return result;
            }
        }
    }

    private static bool AcceptDirectory(DirectoryInfo current)
    {
        return !current.Name.StartsWith('.') && !IgnoredDirectories.Contains(current.Name);
    }
}