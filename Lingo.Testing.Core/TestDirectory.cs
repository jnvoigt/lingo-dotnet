namespace Lingo.Testing.Core;

public sealed class TestDirectory : IDisposable
{
    public TestDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "LingoTests_" + Guid.NewGuid().ToString("N"));
        Info = Directory.CreateDirectory(tempPath);
    }

    public string FullName => Info.FullName;

    public DirectoryInfo Info { get; }

    public void Dispose()
    {
        try
        {
            if (Info.Exists)
            {
                Info.Delete(true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

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

    public static implicit operator DirectoryInfo(TestDirectory testDir)
    {
        return testDir.Info;
    }

    public static implicit operator string(TestDirectory testDir)
    {
        return testDir.FullName;
    }
}