using System.CommandLine;
using Lingo.Core.Sync;
using Lingo.Core.Formats.Xliff;

namespace Lingo.Cli.Commands;

public static class SyncCommand
{
    public static Command GetCommand()
    {
        var sourceOption = new Option<FileInfo>(
            name: "--source",
            description: "The source localization file.") { IsRequired = true };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<FileInfo>(
            name: "--target",
            description: "The target localization file.") { IsRequired = true };
        targetOption.AddAlias("-t");

        var command = new Command("sync", "Synchronizes a source file to a target file.")
        {
            sourceOption,
            targetOption
        };

        command.SetHandler(HandleSync, sourceOption, targetOption);

        return command;
    }

    private static void HandleSync(FileInfo sourceFile, FileInfo targetFile)
    {
        if (!sourceFile.Exists)
        {
            Console.Error.WriteLine($"Source file not found: {sourceFile.FullName}");
            Environment.Exit(1);
        }

        if (!targetFile.Exists)
        {
            Console.Error.WriteLine($"Target file not found: {targetFile.FullName}");
            Environment.Exit(1);
        }

        var factory = new XliffDocumentFactory();
        var synchronizer = new DocumentSynchronizer();
        var writer = new XliffDocumentWriter();

        using (var sourceStream = sourceFile.OpenRead())
        using (var targetStream = targetFile.OpenRead())
        {
            var sourceDoc = factory.Create(sourceStream);
            var targetDoc = factory.Create(targetStream);

            synchronizer.PushSync(sourceDoc, targetDoc);

            // Re-open target for writing
            targetStream.Close();
            using (var writeStream = targetFile.OpenWrite())
            {
                writeStream.SetLength(0);
                writer.Write(writeStream, targetDoc);
            }
        }

        Console.WriteLine($"Synchronized {sourceFile.Name} -> {targetFile.Name}");
    }
}
