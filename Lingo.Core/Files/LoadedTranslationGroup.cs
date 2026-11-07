using Lingo.Core.Formats;

namespace Lingo.Core.Files;

public class LoadedTranslationGroup
{
    public required string GroupStub { get; init; }
    public required string DirectoryPath { get; init; }
    public required LingoFormat Format { get; init; }
    public required IReadOnlyList<LoadedTranslationFile> Files { get; init; }

    public LoadedTranslationFile? SourceFile => Files.FirstOrDefault(f => f.IsSource) ?? Files.FirstOrDefault();
}
