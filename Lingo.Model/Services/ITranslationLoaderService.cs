using Lingo.Core.Documents;
using Lingo.Core.Files;
using Lingo.Core.Formats;

namespace Lingo.Model.Services;

public interface ITranslationLoaderService
{
    /// <summary>
    /// Loads a translation group or file from the specified path into a new TranslationDictionary.
    /// </summary>
    TranslationDictionary LoadFromPath(string filePath, LingoFormat? expectedFormat = null);

    /// <summary>
    /// Discovers and loads all translation files and groups from the specified directory.
    /// </summary>
    TranslationDictionary LoadFromDirectory(string directoryPath, SearchOption searchOption = SearchOption.AllDirectories, LingoFormat? expectedFormat = null);

    /// <summary>
    /// Populates a TranslationDictionary from a LoadedTranslationGroup.
    /// </summary>
    TranslationDictionary LoadFromGroup(LoadedTranslationGroup group);

    /// <summary>
    /// Populates a TranslationDictionary from a single LoadedTranslationFile.
    /// </summary>
    TranslationDictionary LoadFromFile(LoadedTranslationFile file);

    /// <summary>
    /// Populates a TranslationDictionary from an ILingoDocument for the given culture code.
    /// </summary>
    TranslationDictionary LoadFromDocument(ILingoDocument document, string cultureCode = "");

    /// <summary>
    /// Loads translations from a JSON string into a new TranslationDictionary.
    /// </summary>
    TranslationDictionary LoadFromJson(string json);

    /// <summary>
    /// Fills an existing dictionary with translations loaded from the specified path.
    /// </summary>
    void FillDictionary(IDictionary<string, LocalizedText> dictionary, string filePath, LingoFormat? expectedFormat = null);

    /// <summary>
    /// Fills an existing dictionary with translations discovered in the specified directory.
    /// </summary>
    void FillDictionaryFromDirectory(IDictionary<string, LocalizedText> dictionary, string directoryPath, SearchOption searchOption = SearchOption.AllDirectories, LingoFormat? expectedFormat = null);

    /// <summary>
    /// Fills an existing dictionary from a LoadedTranslationGroup.
    /// </summary>
    void FillDictionaryFromGroup(IDictionary<string, LocalizedText> dictionary, LoadedTranslationGroup group);

    /// <summary>
    /// Fills an existing dictionary from a LoadedTranslationFile.
    /// </summary>
    void FillDictionaryFromFile(IDictionary<string, LocalizedText> dictionary, LoadedTranslationFile file);

    /// <summary>
    /// Fills an existing dictionary from an ILingoDocument.
    /// </summary>
    void FillDictionaryFromDocument(IDictionary<string, LocalizedText> dictionary, ILingoDocument document, string cultureCode = "");

    /// <summary>
    /// Fills an existing dictionary from a JSON string.
    /// </summary>
    void FillDictionaryFromJson(IDictionary<string, LocalizedText> dictionary, string json);
}
