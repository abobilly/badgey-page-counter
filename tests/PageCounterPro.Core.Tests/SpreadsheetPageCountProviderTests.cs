using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PageCounterPro.Core.Models;
using PageCounterPro.Core.PageCountProviders;
using Xunit;

namespace PageCounterPro.Core.Tests;

public class SpreadsheetPageCountProviderTests
{
    private readonly SpreadsheetPageCountProvider _provider;
    private readonly AppSettings _settings;
    private readonly string _testDataPath;

    public SpreadsheetPageCountProviderTests()
    {
        var logger = new Mock<ILogger<SpreadsheetPageCountProvider>>();
        _provider = new SpreadsheetPageCountProvider(logger.Object);
        _settings = new AppSettings();

        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(_testDataPath);
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    [InlineData(".csv")]
    public void CanHandle_SpreadsheetExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, $"spreadsheet{extension}");
        File.WriteAllText(filePath, "test");
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.CanHandle(file);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void CanHandle_NonSpreadsheetFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test.pdf");
        File.WriteAllBytes(filePath, new byte[] { 0 });
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.CanHandle(file);

        // Assert
        result.Should().BeFalse();

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_EmptyCsvFile_ReturnsMinimumOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "empty.csv");
        File.WriteAllText(filePath, "");
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().BeGreaterOrEqualTo(1);

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_CsvWithFewRows_ReturnsOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "small.csv");
        var lines = new List<string> { "Name,Value" };
        for (int i = 0; i < 10; i++)
        {
            lines.Add($"Item{i},{i}");
        }
        File.WriteAllLines(filePath, lines);
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(1);
        result.Notes.Should().Contain("lines");

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_CsvWithManyRows_ReturnsMultiplePages()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "large.csv");
        var lines = new List<string> { "Name,Value" };
        for (int i = 0; i < 150; i++) // More than default rows per page (50)
        {
            lines.Add($"Item{i},{i}");
        }
        File.WriteAllLines(filePath, lines);
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().BeGreaterThan(1);

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_CsvWithCustomLinesPerPage_UsesSettingCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "custom.csv");
        var lines = new List<string> { "Name,Value" };
        for (int i = 0; i < 100; i++)
        {
            lines.Add($"Item{i},{i}");
        }
        File.WriteAllLines(filePath, lines);
        var file = new FileInfo(filePath);

        var customSettings = new AppSettings
        {
            LinesPerPage = 25 // Custom setting for CSV
        };

        // Act
        var result = _provider.GetPageCount(file, customSettings);

        // Assert
        result.Success.Should().BeTrue();
        // 101 rows / 25 lines per page = 5 pages (with rounding up)
        result.PageCount.Should().BeGreaterOrEqualTo(4);

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_XlsFile_ReturnsEstimatedPageCount()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "legacy.xls");
        File.WriteAllBytes(filePath, new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }); // OLE header
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().BeGreaterOrEqualTo(1);
        result.Notes.Should().Contain("legacy");

        // Cleanup
        File.Delete(filePath);
    }
}
