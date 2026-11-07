using Lingo.Core.Documents;
using Lingo.Core.Formats;

namespace Lingo.Core.Files;

public class TranslationFileLoader
{
    private readonly FileCrawler _crawler = new();

    public LoadedTranslationGroup? LoadGroupFromPath(string path, LingoFormat? expectedFormat = null)
    {
        var fileInfo = LingoFileInfo.FromPath(path, expectedFormat);
        if (fileInfo == null || !fileInfo.File.Exists)
        {
            return null;
        }

        return LoadGroup(fileInfo);
    }

    public IReadOnlyList<LoadedTranslationGroup> LoadGroups(IEnumerable<string> paths, LingoFormat? expectedFormat = null)
    {
        var groups = new List<LoadedTranslationGroup>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            var fileInfo = LingoFileInfo.FromPath(path, expectedFormat);
            if (fileInfo == null || !fileInfo.File.Exists)
            {
                continue;
            }

            var dir = fileInfo.File.DirectoryName ?? string.Empty;
            var groupKey = $"{dir}::{fileInfo.Stub}::{fileInfo.Format}";
            if (seenKeys.Add(groupKey))
            {
                var grp = LoadGroup(fileInfo);
                if (grp != null)
                {
                    groups.Add(grp);
                }
            }
        }

        return groups;
    }

    public LoadedTranslationGroup? LoadGroup(LingoFileInfo fileInfo)
    {
        if (!fileInfo.File.Exists || fileInfo.Format == null)
        {
            return null;
        }

        var siblingFiles = _crawler.GetSiblingGroup(fileInfo).ToList();
        var loadedFiles = new List<LoadedTranslationFile>();

        foreach (var sInfo in siblingFiles)
        {
            var loaded = LoadFile(sInfo);
            if (loaded != null)
            {
                loadedFiles.Add(loaded);
            }
        }

        if (loadedFiles.Count == 0)
        {
            return null;
        }

        // Ensure source files are listed first
        var ordered = loadedFiles
            .OrderByDescending(f => f.IsSource)
            .ThenBy(f => f.LanguageLabel)
            .ToList();

        var dirPath = fileInfo.File.DirectoryName ?? string.Empty;

        return new LoadedTranslationGroup
        {
            GroupStub = fileInfo.Stub,
            DirectoryPath = dirPath,
            Format = fileInfo.Format,
            Files = ordered.AsReadOnly()
        };
    }

    public LoadedTranslationFile? LoadFile(LingoFileInfo fileInfo)
    {
        if (!fileInfo.File.Exists || fileInfo.Format == null)
        {
            return null;
        }

        var factory = LingoFormatProvider.GetFactory(fileInfo.Format);
        using var stream = File.OpenRead(fileInfo.File.FullName);
        var document = factory.Create(stream);

        var (languageLabel, isSource) = DetermineLanguageAndRole(fileInfo, document);

        return new LoadedTranslationFile
        {
            Document = document,
            FileInfo = fileInfo,
            LanguageLabel = languageLabel,
            IsSource = isSource,
            IsModified = false
        };
    }

    public void SaveFile(LoadedTranslationFile file)
    {
        var format = file.FileInfo.Format ?? LingoFormat.Xliff;
        var writer = LingoFormatProvider.GetWriter(format);

        using var stream = File.Create(file.FilePath);
        writer.Write(stream, file.Document);
        file.IsModified = false;
    }

    public void SaveGroup(LoadedTranslationGroup group)
    {
        foreach (var file in group.Files)
        {
            SaveFile(file);
        }
    }

    public int SaveModifiedFiles(IEnumerable<LoadedTranslationGroup> groups)
    {
        var savedCount = 0;
        foreach (var group in groups)
        {
            foreach (var file in group.Files)
            {
                if (file.IsModified)
                {
                    SaveFile(file);
                    savedCount++;
                }
            }
        }
        return savedCount;
    }

    private static (string LanguageLabel, bool IsSource) DetermineLanguageAndRole(LingoFileInfo fileInfo, ILingoDocument document)
    {
        if (document is IHasCultureInfo cultureDoc)
        {
            var targetCulture = cultureDoc.GetTargetCulture();
            if (targetCulture != null)
            {
                return (targetCulture.Name, false);
            }

            var sourceCulture = cultureDoc.GetSourceCulture();
            if (sourceCulture != null && sourceCulture != CultureInfo.InvariantCulture)
            {
                return ($"{sourceCulture.Name} (Source)", true);
            }
        }

        if (fileInfo.Culture != null)
        {
            return (fileInfo.Culture.Name, false);
        }

        return ("Source", true);
    }
}
