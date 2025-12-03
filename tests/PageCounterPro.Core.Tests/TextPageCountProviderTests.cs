using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PageCounterPro.Core.Models;
using PageCounterPro.Core.PageCountProviders;
using Xunit;

namespace PageCounterPro.Core.Tests;

public class TextPageCountProviderTests
{
    private readonly TextPageCountProvider _provider;
    private readonly AppSettings _settings;
    private readonly string _testDataPath;

    public TextPageCountProviderTests()
    {
        var logger = new Mock<ILogger<TextPageCountProvider>>();
        _provider = new TextPageCountProvider(logger.Object);
        _settings = new AppSettings
        {
            LinesPerPage = 50,
            CharactersPerPage = 1800
        };

        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(_testDataPath);
    }

    [Fact]
    public void CanHandle_TextFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test.txt");
        File.WriteAllText(filePath, "test content");
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.CanHandle(file);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        File.Delete(filePath);
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".log")]
    [InlineData(".md")]
    [InlineData(".json")]
    [InlineData(".xml")]
    [InlineData(".cs")]
    [InlineData(".py")]
    public void CanHandle_VariousTextExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, $"test{extension}");
        File.WriteAllText(filePath, "test content");
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.CanHandle(file);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void CanHandle_NonTextFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test.pdf");
        File.WriteAllText(filePath, "test content");
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.CanHandle(file);

        // Assert
        result.Should().BeFalse();

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_EmptyFile_ReturnsMinimumOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "empty.txt");
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
    public void GetPageCount_SingleLine_ReturnsOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "single.txt");
        File.WriteAllText(filePath, "This is a single line of text.");
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(1);
        result.Notes.Should().Contain("totalLines=1");

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_50Lines_ReturnsOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "fifty.txt");
        var content = string.Join("\n", Enumerable.Range(1, 50).Select(i => $"Line {i}"));
        File.WriteAllText(filePath, content);
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(1);

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_100Lines_ReturnsTwoPages()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "hundred.txt");
        var content = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i}"));
        File.WriteAllText(filePath, content);
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(2);
        result.Notes.Should().Contain("50 lines per page");

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_345Lines_Returns7Pages()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "many.txt");
        var content = string.Join("\n", Enumerable.Range(1, 345).Select(i => $"Line {i}"));
        File.WriteAllText(filePath, content);
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(7); // ceil(345/50) = 7
        result.Notes.Should().Contain("totalLines=345");

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_CustomLinesPerPage_UsesCustomSetting()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "custom.txt");
        var content = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i}"));
        File.WriteAllText(filePath, content);
        var file = new FileInfo(filePath);
        var customSettings = new AppSettings { LinesPerPage = 25 };

        // Act
        var result = _provider.GetPageCount(file, customSettings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(4); // ceil(100/25) = 4
        result.Notes.Should().Contain("25 lines per page");

        // Cleanup
        File.Delete(filePath);
    }
}
