using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// Compresses images to WebP format using ImageSharp.
/// Moves compression responsibility out of <see cref="GenkitAiService"/> to keep
/// that class focused on HTTP communication only (Single Responsibility Principle).
/// </summary>
public sealed class ImageSharpCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageSharpCompressionService> _logger;

    /// <summary>Initializes a new instance of <see cref="ImageSharpCompressionService"/>.</summary>
    public ImageSharpCompressionService(ILogger<ImageSharpCompressionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> CompressToWebPAsync(string base64Image, int quality = 80, int maxWidth = 500)
    {
        // Remove data-URL prefix when present (e.g. "data:image/png;base64,...")
        var imageData = base64Image.Contains(',')
            ? base64Image.Split(',')[1]
            : base64Image;

        var imageBytes = Convert.FromBase64String(imageData);
        var originalSize = imageBytes.Length;

        _logger.LogInformation("Compressing image: {OriginalSize} bytes to WebP", originalSize);

        using var inputStream = new MemoryStream(imageBytes);
        using var outputStream = new MemoryStream();

        using (var image = await Image.LoadAsync(inputStream))
        {
            _logger.LogInformation("Image loaded: {Width}x{Height}", image.Width, image.Height);

            // Resize if wider than maxWidth while maintaining aspect ratio
            if (image.Width > maxWidth)
            {
                var newHeight = (int)((double)image.Height * maxWidth / image.Width);
                _logger.LogInformation("Resizing to {Width}x{Height}", maxWidth, newHeight);
                image.Mutate(x => x.Resize(maxWidth, newHeight));
            }

            var encoder = new WebpEncoder
            {
                Quality = quality,
                FileFormat = WebpFileFormatType.Lossy
            };

            await image.SaveAsync(outputStream, encoder);
        }

        var result = Convert.ToBase64String(outputStream.ToArray());
        var compressedSize = result.Length;
        var savings = ((double)(originalSize - compressedSize) / originalSize * 100).ToString("F1");

        _logger.LogInformation(
            "Image compressed: {OriginalSize} -> {CompressedSize} bytes ({Savings}% smaller)",
            originalSize, compressedSize, savings);

        return result;
    }
}
