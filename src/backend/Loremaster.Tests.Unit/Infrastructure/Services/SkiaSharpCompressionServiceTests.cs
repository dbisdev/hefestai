using Loremaster.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace Loremaster.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Verifies image conversion and validation performed by <see cref="SkiaSharpCompressionService"/>.
/// </summary>
public sealed class SkiaSharpCompressionServiceTests
{
    private readonly SkiaSharpCompressionService _service =
        new(NullLogger<SkiaSharpCompressionService>.Instance);

    /// <summary>Converts a prefixed PNG data URL to WebP and preserves its aspect ratio while resizing.</summary>
    [Fact]
    public async Task CompressToWebPAsync_ConvertsAndResizesImage()
    {
        var input = $"data:image/png;base64,{CreatePngBase64(200, 100)}";

        var result = await _service.CompressToWebPAsync(input, quality: 80, maxWidth: 80);
        var outputBytes = Convert.FromBase64String(result);

        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(outputBytes, 0, 4));
        Assert.Equal("WEBP", System.Text.Encoding.ASCII.GetString(outputBytes, 8, 4));

        using var outputBitmap = SKBitmap.Decode(outputBytes);
        Assert.NotNull(outputBitmap);
        Assert.Equal(80, outputBitmap.Width);
        Assert.Equal(40, outputBitmap.Height);
    }

    /// <summary>Rejects payloads that do not contain a supported encoded image.</summary>
    [Fact]
    public async Task CompressToWebPAsync_ThrowsForInvalidImageData()
    {
        var action = () => _service.CompressToWebPAsync(Convert.ToBase64String([1, 2, 3]));

        await Assert.ThrowsAsync<InvalidDataException>(action);
    }

    /// <summary>Creates a solid-color PNG payload for image compression tests.</summary>
    private static string CreatePngBase64(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);

        using var image = SKImage.FromBitmap(bitmap);
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, 100);

        return Convert.ToBase64String(encodedImage.ToArray());
    }
}
