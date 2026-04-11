using AwesomeAssertions;
using System.IO;

namespace Lingo.Core.Test.Infrastructure;

public class TestDirectoryTests
{
    [Test]
    public void TestDirectory_CreatesDirectoryOnInit()
    {
        // Act
        using var tempDir = new TestDirectory();

        // Assert
        tempDir.Info.Exists.Should().BeTrue();
    }

    [Test]
    public void TestDirectory_DeletesDirectoryOnDispose()
    {
        // Arrange
        DirectoryInfo directoryInfo;
        using (var tempDir = new TestDirectory())
        {
            directoryInfo = tempDir.Info;
            directoryInfo.Exists.Should().BeTrue();
        }

        // Assert
        directoryInfo.Exists.Should().BeFalse();
    }

    [Test]
    public void CreateFile_CreatesFileAndParentDirectories()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        var relativePath = "subdir/test.txt";
        var content = "Hello World";

        // Act
        var fileInfo = tempDir.CreateFile(relativePath, content);

        // Assert
        fileInfo.Exists.Should().BeTrue();
        File.ReadAllText(fileInfo.FullName).Should().Be(content);
        fileInfo.Directory?.Name.Should().Be("subdir");
    }

    [Test]
    public void CreateDirectory_CreatesSubdirectory()
    {
        // Arrange
        using var tempDir = new TestDirectory();
        var relativePath = "newdir";

        // Act
        var dirInfo = tempDir.CreateDirectory(relativePath);

        // Assert
        dirInfo.Exists.Should().BeTrue();
        dirInfo.Name.Should().Be("newdir");
    }

    [Test]
    public void ImplicitOperator_ToString_ReturnsFullName()
    {
        // Arrange
        using var tempDir = new TestDirectory();

        // Act
        string path = tempDir;

        // Assert
        path.Should().Be(tempDir.FullName);
    }

    [Test]
    public void ImplicitOperator_ToDirectoryInfo_ReturnsDirectoryInfo()
    {
        // Arrange
        using var tempDir = new TestDirectory();

        // Act
        DirectoryInfo info = tempDir;

        // Assert
        info.FullName.Should().Be(tempDir.FullName);
    }
}