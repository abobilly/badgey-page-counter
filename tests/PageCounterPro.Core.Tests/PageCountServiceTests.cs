using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using PageCounterPro.Core.Services;
using Xunit;

namespace PageCounterPro.Core.Tests;

public class PageCountServiceTests
{
    private readonly Mock<IFileScanner> _mockFileScanner;
    private readonly Mock<ILogger<PageCountService>> _mockLogger;
    private readonly List<Mock<IPageCountProvider>> _mockProviders;
    private readonly AppSettings _settings;
    private readonly PageCountService _service;
    private readonly string _testDataPath;

    public PageCountServiceTests()
    {
        _mockFileScanner = new Mock<IFileScanner>();
        _mockLogger = new Mock<ILogger<PageCountService>>();
        _mockProviders = new List<Mock<IPageCountProvider>>();
        _settings = new AppSettings();

        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(_testDataPath);

        // Create a mock provider that handles .test files
        var testProvider = new Mock<IPageCountProvider>();
        testProvider.Setup(p => p.CanHandle(It.IsAny<FileInfo>()))
            .Returns((FileInfo f) => f.Extension.Equals(".test", StringComparison.OrdinalIgnoreCase));
        testProvider.Setup(p => p.GetPageCount(It.IsAny<FileInfo>(), It.IsAny<AppSettings>()))
            .Returns(new PageCountResult { Success = true, PageCount = 5 });
        _mockProviders.Add(testProvider);

        _service = new PageCountService(
            _mockFileScanner.Object,
            _mockProviders.Select(m => m.Object),
            _settings,
            _mockLogger.Object);
    }

    [Fact]
    public void GetPageCount_WithSupportedFile_ReturnsCorrectPageCount()
    {
        // Arrange
        var testFile = Path.Combine(_testDataPath, "test.test");
        File.WriteAllText(testFile, "content");
        var fileInfo = new FileInfo(testFile);

        // Act
        var result = _service.GetPageCount(fileInfo);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(5);

        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public void GetPageCount_WithUnsupportedFile_ReturnsNullPageCount()
    {
        // Arrange
        var testFile = Path.Combine(_testDataPath, "unsupported.xyz");
        File.WriteAllText(testFile, "content");
        var fileInfo = new FileInfo(testFile);

        // Act
        var result = _service.GetPageCount(fileInfo);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.PageCount.Should().BeNull();

        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task ExecuteScanAsync_ReturnsValidResult()
    {
        // Arrange
        var options = new ScanOptions
        {
            RootFolderPath = _testDataPath,
            IncludeSubfolders = false
        };

        _mockFileScanner.Setup(s => s.GetTotalFileCountAsync(options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockFileScanner.Setup(s => s.ScanFilesAsync(options, It.IsAny<IProgress<ScanProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(GetEmptyAsyncEnumerable());

        // Act
        var result = await _service.ExecuteScanAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().BeEmpty();
        result.TotalFilesFound.Should().Be(0);
    }

    private static async IAsyncEnumerable<FileInfo> GetEmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }
}
