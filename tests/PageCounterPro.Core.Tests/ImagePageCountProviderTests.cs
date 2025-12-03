using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PageCounterPro.Core.Models;
using PageCounterPro.Core.PageCountProviders;
using Xunit;

namespace PageCounterPro.Core.Tests;

public class ImagePageCountProviderTests
{
    private readonly ImagePageCountProvider _provider;
    private readonly AppSettings _settings;
    private readonly string _testDataPath;

    public ImagePageCountProviderTests()
    {
        var logger = new Mock<ILogger<ImagePageCountProvider>>();
        _provider = new ImagePageCountProvider(logger.Object);
        _settings = new AppSettings();

        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(_testDataPath);
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData(".tif")]
    [InlineData(".tiff")]
    [InlineData(".webp")]
    public void CanHandle_ImageExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, $"image{extension}");
        File.WriteAllBytes(filePath, new byte[] { 0 });
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.CanHandle(file);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void CanHandle_NonImageFile_ReturnsFalse()
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
    public void GetPageCount_AnyImage_ReturnsOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test.jpg");
        File.WriteAllBytes(filePath, new byte[] { 0xFF, 0xD8, 0xFF }); // JPEG header
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(1);
        result.Notes.Should().Contain("1 page");

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void GetPageCount_TiffImage_ReturnsOnePage()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test.tiff");
        File.WriteAllBytes(filePath, new byte[] { 0x49, 0x49, 0x2A, 0x00 }); // TIFF header (little endian)
        var file = new FileInfo(filePath);

        // Act
        var result = _provider.GetPageCount(file, _settings);

        // Assert
        result.Success.Should().BeTrue();
        result.PageCount.Should().Be(1);
        result.Notes.Should().Contain("TIFF");

        // Cleanup
        File.Delete(filePath);
    }
}
