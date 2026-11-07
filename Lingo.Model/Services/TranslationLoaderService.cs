using System.Text.Json;
using Lingo.Core.Documents;
using Lingo.Core.Files;
using Lingo.Core.Formats;

namespace Lingo.Model.Services;

public class TranslationLoaderService : ITranslationLoaderService
{
    private readonly TranslationFileLoader _fileLoader;
    private readonly FileCrawler _crawler;

    public TranslationLoaderService(TranslationFileLoader? fileLoader = null, FileCrawler? crawler = null)
    {
        _fileLoader = fileLoader ?? new TranslationFileLoader();
        _crawler = crawler ?? new FileCrawler();
    }

    public TranslationDictionary LoadFromPath(string filePath, LingoFormat? expectedFormat = null)
    {
        var dictionary = new TranslationDictionary();
        FillDictionary(dictionary, filePath, expectedFormat);
        return dictionary;
    }

    public TranslationDictionary LoadFromDirectory(string directoryPath, SearchOption searchOption = SearchOption.AllDirectories, LingoFormat? expectedFormat = null)
    {
        var dictionary = new TranslationDictionary();
        FillDictionaryFromDirectory(dictionary, directoryPath, searchOption, expectedFormat);
        return dictionary;
    }

    public TranslationDictionary LoadFromGroup(LoadedTranslationGroup group)
    {
        var dictionary = new TranslationDictionary();
        FillDictionaryFromGroup(dictionary, group);
        return dictionary;
    }

    public TranslationDictionary LoadFromFile(LoadedTranslationFile file)
    {
        var dictionary = new TranslationDictionary();
        FillDictionaryFromFile(dictionary, file);
        return dictionary;
    }

    public TranslationDictionary LoadFromDocument(ILingoDocument document, string cultureCode = "")
    {
        var dictionary = new TranslationDictionary();
        FillDictionaryFromDocument(dictionary, document, cultureCode);
        return dictionary;
    }

    public TranslationDictionary LoadFromJson(string json)
    {
        var dictionary = new TranslationDictionary();
        FillDictionaryFromJson(dictionary, json);
        return dictionary;
    }

    public void FillDictionary(IDictionary<string, LocalizedText> dictionary, string filePath, LingoFormat? expectedFormat = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return;
        }

        var group = _fileLoader.LoadGroupFromPath(filePath, expectedFormat);
        if (group != null)
        {
            FillDictionaryFromGroup(dictionary, group);
            return;
        }

        var fileInfo = LingoFileInfo.FromPath(filePath, expectedFormat);
        if (fileInfo != null)
        {
            var file = _fileLoader.LoadFile(fileInfo);
            if (file != null)
            {
                FillDictionaryFromFile(dictionary, file);
            }
        }
    }

    public void FillDictionaryFromDirectory(IDictionary<string, LocalizedText> dictionary, string directoryPath, SearchOption searchOption = SearchOption.AllDirectories, LingoFormat? expectedFormat = null)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        var dirInfo = new DirectoryInfo(directoryPath);
        var files = _crawler.Crawl(dirInfo, null, expectedFormat).ToList();

        var processedStubs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileInfo in files)
        {
            var stubKey = $"{fileInfo.File.DirectoryName}|{fileInfo.Stub}|{fileInfo.Format}";
            if (!processedStubs.Add(stubKey))
            {
                continue;
            }

            var group = _fileLoader.LoadGroup(fileInfo);
            if (group != null)
            {
                FillDictionaryFromGroup(dictionary, group);
            }
        }
    }

    public void FillDictionaryFromGroup(IDictionary<string, LocalizedText> dictionary, LoadedTranslationGroup group)
    {
        if (group == null || group.Files.Count == 0)
        {
            return;
        }

        // First process the source file to populate default culture values ("")
        var sourceFile = group.SourceFile;
        if (sourceFile != null)
        {
            FillDictionaryFromFile(dictionary, sourceFile);
        }

        // Then process target files to populate localized culture values
        foreach (var file in group.Files)
        {
            if (file == sourceFile)
            {
                continue;
            }

            FillDictionaryFromFile(dictionary, file);
        }
    }

    public void FillDictionaryFromFile(IDictionary<string, LocalizedText> dictionary, LoadedTranslationFile file)
    {
        if (file == null || file.Document == null)
        {
            return;
        }

        var cultureCode = file.FileInfo.Culture?.Name;
        if (string.IsNullOrEmpty(cultureCode))
        {
            cultureCode = file.LanguageLabel;
            if (cultureCode.EndsWith("(Source)", StringComparison.OrdinalIgnoreCase))
            {
                cultureCode = cultureCode.Replace("(Source)", "").Trim();
            }
            if (string.Equals(cultureCode, "Source", StringComparison.OrdinalIgnoreCase))
            {
                cultureCode = "";
            }
        }

        foreach (var unit in file.Document.GetAllUnits())
        {
            if (string.IsNullOrEmpty(unit.Id))
            {
                continue;
            }

            var value = file.Document.GetValue(unit.Id) ?? unit.Target ?? unit.Source;
            if (value == null)
            {
                continue;
            }

            if (!dictionary.TryGetValue(unit.Id, out var localizedText))
            {
                localizedText = new LocalizedText();
                dictionary[unit.Id] = localizedText;
            }

            if (file.IsSource)
            {
                localizedText[LocalizedText.DefaultCultureKey] = value;
                if (!string.IsNullOrEmpty(cultureCode))
                {
                    localizedText[cultureCode] = value;
                }
            }
            else
            {
                localizedText[cultureCode] = value;
            }
        }
    }

    public void FillDictionaryFromDocument(IDictionary<string, LocalizedText> dictionary, ILingoDocument document, string cultureCode = "")
    {
        if (document == null) return;

        foreach (var unit in document.GetAllUnits())
        {
            if (string.IsNullOrEmpty(unit.Id))
            {
                continue;
            }

            var value = document.GetValue(unit.Id) ?? unit.Target ?? unit.Source;
            if (value == null)
            {
                continue;
            }

            if (!dictionary.TryGetValue(unit.Id, out var localizedText))
            {
                localizedText = new LocalizedText();
                dictionary[unit.Id] = localizedText;
            }

            localizedText[cultureCode] = value;
            if (string.IsNullOrEmpty(cultureCode) || localizedText.DefaultText == null)
            {
                localizedText[LocalizedText.DefaultCultureKey] = value;
            }
        }
    }

    public void FillDictionaryFromJson(IDictionary<string, LocalizedText> dictionary, string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, LocalizedText>>(json);
        if (deserialized == null) return;

        foreach (var (key, localizedText) in deserialized)
        {
            if (dictionary.TryGetValue(key, out var existing))
            {
                foreach (var (culture, text) in localizedText)
                {
                    existing[culture] = text;
                }
            }
            else
            {
                dictionary[key] = localizedText;
            }
        }
    }
}
