using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using PageCounterPro.Core.Services;
using Xunit;

namespace PageCounterPro.Core.Tests;

public class FileScannerTests
{
    private readonly FileScanner _scanner;
    private readonly string _testDataPath;

    public FileScannerTests()
    {
        var logger = new Mock<ILogger<FileScanner>>();
        _scanner = new FileScanner(logger.Object);

        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ScannerTests");
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
        Directory.CreateDirectory(_testDataPath);
    }

    [Fact]
    public async Task ScanFilesAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = false
        };

        // Act
        var files = new List<FileInfo>();
        await foreach (var file in _scanner.ScanFilesAsync(options))
        {
            files.Add(file);
        }

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanFilesAsync_WithFiles_ReturnsAllFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDataPath, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_testDataPath, "file2.pdf"), "content");
        File.WriteAllText(Path.Combine(_testDataPath, "file3.docx"), "content");

        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = false
        };

        // Act
        var files = new List<FileInfo>();
        await foreach (var file in _scanner.ScanFilesAsync(options))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(3);
    }

    [Fact]
    public async Task ScanFilesAsync_WithSubfolders_ReturnsFilesFromSubdirectories()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDataPath, "root.txt"), "content");

        var subDir = Path.Combine(_testDataPath, "SubFolder");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "sub.txt"), "content");

        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = true
        };

        // Act
        var files = new List<FileInfo>();
        await foreach (var file in _scanner.ScanFilesAsync(options))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(2);
        files.Should().Contain(f => f.Name == "root.txt");
        files.Should().Contain(f => f.Name == "sub.txt");
    }

    [Fact]
    public async Task ScanFilesAsync_WithoutSubfolders_OnlyReturnsRootFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDataPath, "root.txt"), "content");

        var subDir = Path.Combine(_testDataPath, "SubFolder");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "sub.txt"), "content");

        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = false
        };

        // Act
        var files = new List<FileInfo>();
        await foreach (var file in _scanner.ScanFilesAsync(options))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(1);
        files.Should().OnlyContain(f => f.Name == "root.txt");
    }

    [Fact]
    public async Task ScanFilesAsync_WithFileTypeFilter_ReturnsOnlyMatchingFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDataPath, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_testDataPath, "file2.pdf"), "content");
        File.WriteAllText(Path.Combine(_testDataPath, "file3.txt"), "content");

        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = false,
            FileTypeFilter = new List<string> { "txt" }
        };

        // Act
        var files = new List<FileInfo>();
        await foreach (var file in _scanner.ScanFilesAsync(options))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(2);
        files.Should().OnlyContain(f => f.Extension == ".txt");
    }

    [Fact]
    public async Task GetTotalFileCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDataPath, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_testDataPath, "file2.pdf"), "content");
        File.WriteAllText(Path.Combine(_testDataPath, "file3.docx"), "content");

        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = false
        };

        // Act
        var count = await _scanner.GetTotalFileCountAsync(options, CancellationToken.None);

        // Assert
        count.Should().Be(3);
    }
}
