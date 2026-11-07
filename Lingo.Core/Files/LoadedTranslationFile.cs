using Lingo.Core.Documents;

namespace Lingo.Core.Files;

public class LoadedTranslationFile
{
    public required ILingoDocument Document { get; init; }
    public required LingoFileInfo FileInfo { get; init; }
    public string FilePath => FileInfo.File.FullName;
    public string FileName => FileInfo.File.Name;
    public required string LanguageLabel { get; init; }
    public bool IsSource { get; init; }
    public bool IsModified { get; set; }
}
