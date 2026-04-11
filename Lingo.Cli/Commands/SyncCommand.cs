using Lingo.Core.Files;
using Lingo.Core.Formats.Xliff;
using Lingo.Core.Sync;
using System.CommandLine;

namespace Lingo.Cli.Commands;

public static class SyncCommand
{
    public static Command GetCommand()
    {
        var sourceOption = new Option<FileInfo>(
            "--source",
            "The source localization file.") { IsRequired = true };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<FileInfo>(
            "--target",
            "The target localization file.") { IsRequired = false };
        targetOption.AddAlias("-t");

        var command =
            new Command("sync",
                "Synchronizes a source file to a target file or all sibling files if target is omitted.")
            {
                sourceOption, targetOption
            };

        command.SetHandler(HandleSync, sourceOption, targetOption);

        return command;
    }

    private static void HandleSync(FileInfo sourceFile, FileInfo? targetFile)
    {
        if (!sourceFile.Exists)
        {
            Console.Error.WriteLine($"Source file not found: {sourceFile.FullName}");
            Environment.Exit(1);
        }

        if (targetFile != null)
        {
            SyncFiles(sourceFile, targetFile);
        }
        else
        {
            var lingoSource = LingoFileInfo.FromFile(sourceFile);
            if (lingoSource == null)
            {
                Console.Error.WriteLine($"Could not determine Lingo format for source file: {sourceFile.FullName}");
                Environment.Exit(1);
            }

            var crawler = new FileCrawler();
            var siblingFiles = crawler.GetSiblings(lingoSource).ToList();

            if (siblingFiles.Count == 0)
            {
                Console.WriteLine("No sibling localization files found to synchronize.");
                return;
            }

            foreach (var sibling in siblingFiles)
            {
                SyncFiles(sourceFile, sibling.File);
            }
        }
    }

    private static void SyncFiles(FileInfo sourceFile, FileInfo targetFile)
    {
        if (!targetFile.Exists)
        {
            Console.Error.WriteLine($"Target file not found: {targetFile.FullName}");
            Environment.Exit(1);
        }

        // TODO: to not use xliff factory, find the correct factory based on target file
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