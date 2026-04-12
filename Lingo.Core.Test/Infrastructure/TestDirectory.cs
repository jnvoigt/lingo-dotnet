using System.IO;
using System;

namespace Lingo.Core.Test.Infrastructure;

public sealed class TestDirectory : IDisposable
{
    private readonly DirectoryInfo _directory;

    public TestDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "LingoTests_" + Guid.NewGuid().ToString("N"));
        _directory = Directory.CreateDirectory(tempPath);
    }

    public string FullName => _directory.FullName;

    public DirectoryInfo Info => _directory;

    public FileInfo CreateFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(FullName, relativePath);
        var fileInfo = new FileInfo(fullPath);

        if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
        {
            fileInfo.Directory.Create();
        }

        File.WriteAllText(fullPath, content);
        fileInfo.Refresh();
        return fileInfo;
    }

    public DirectoryInfo CreateDirectory(string relativePath)
    {
        var fullPath = Path.Combine(FullName, relativePath);
        return Directory.CreateDirectory(fullPath);
    }

    public void Dispose()
    {
        try
        {
            if (_directory.Exists)
            {
                _directory.Delete(true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    public static implicit operator DirectoryInfo(TestDirectory testDir) => testDir.Info;
    public static implicit operator string(TestDirectory testDir) => testDir.FullName;
}
