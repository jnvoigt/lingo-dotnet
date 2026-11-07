using Lingo.Core.Formats;

namespace Lingo.Core.Files;

public class FileCrawler
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules"
    };

    /// <summary>
    /// Crawls the directory tree to find all files matching the specified format.
    /// </summary>
    /// <param name="root">The root directory to start crawling from.</param>
    /// <param name="culture">The target culture for file content, if applicable.</param>
    /// <param name="format">The file format to filter by, if specified.</param>
    /// <returns>An enumerable of LingoFileInfo objects representing matching files.</returns>
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

        var results = new List<LingoFileInfo>();

        // 1. Files in the same directory
        var sameDirSiblings = directory.GetFiles()
            .Select(f => LingoFileInfo.FromFile(f, source.Format))
            .Where(f => f != null && source.IsSibling(f))
            .Cast<LingoFileInfo>();
        results.AddRange(sameDirSiblings);

        // 2. Files in sibling culture directories under the same parent
        var parent = directory.Parent;
        if (parent != null && parent.Exists && source.Culture != null)
        {
            foreach (var subDir in parent.GetDirectories())
            {
                if (string.Equals(subDir.FullName, directory.FullName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var candidate = new FileInfo(Path.Combine(subDir.FullName, source.File.Name));
                if (candidate.Exists)
                {
                    var fileInfo = LingoFileInfo.FromFile(candidate, source.Format);
                    if (fileInfo != null && source.IsSibling(fileInfo))
                    {
                        results.Add(fileInfo);
                    }
                }
            }
        }

        return results;
    }

    public IEnumerable<LingoFileInfo> GetSiblingGroup(LingoFileInfo source)
    {
        var all = new List<LingoFileInfo> { source };
        all.AddRange(GetSiblings(source));

        return all
            .DistinctBy(f => f.File.FullName)
            .OrderBy(f => f.Culture != null)
            .ThenBy(f => f.Culture?.Name ?? string.Empty)
            .ThenBy(f => f.File.Name);
    }

    private List<LingoFileInfo> CrawlInternal(DirectoryInfo current, CultureInfo? targetCulture,
        LingoFormat? targetFormat)
    {
        var results = new List<LingoFileInfo>();
        if (!AcceptDirectory(current))
        {
            return results;
        }

        foreach (var file in current.GetFiles())
        {
            var lingoFile = LingoFileInfo.FromFile(file, targetFormat);
            if (lingoFile != null)
            {
                // Filter by culture if requested
                if (targetCulture == null || (lingoFile.Culture != null && Equals(lingoFile.Culture, targetCulture)))
                {
                    results.Add(lingoFile);
                }
            }
        }

        foreach (var dir in current.GetDirectories())
        {
            results.AddRange(CrawlInternal(dir, targetCulture, targetFormat));
        }

        return results;
    }

    private static bool AcceptDirectory(DirectoryInfo current)
    {
        return !current.Name.StartsWith('.') && !IgnoredDirectories.Contains(current.Name);
    }
}