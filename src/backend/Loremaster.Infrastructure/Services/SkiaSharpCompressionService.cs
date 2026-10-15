using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// Compresses images to WebP format using SkiaSharp.
/// Moves compression responsibility out of <see cref="GenkitAiService"/> to keep
/// that class focused on HTTP communication only (Single Responsibility Principle).
/// </summary>
public sealed class SkiaSharpCompressionService : IImageCompressionService
{
    private readonly ILogger<SkiaSharpCompressionService> _logger;

    /// <summary>Initializes a new instance of <see cref="SkiaSharpCompressionService"/>.</summary>
    public SkiaSharpCompressionService(ILogger<SkiaSharpCompressionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> CompressToWebPAsync(string base64Image, int quality = 80, int maxWidth = 500)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Image);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWidth);

        // Remove a data-URL prefix when one is present.
        var separatorIndex = base64Image.IndexOf(',');
        var imageData = separatorIndex >= 0
            ? base64Image[(separatorIndex + 1)..]
            : base64Image;
        var imageBytes = Convert.FromBase64String(imageData);

        _logger.LogInformation("Compressing image: {OriginalSize} bytes to WebP", imageBytes.Length);

        using var inputStream = new MemoryStream(imageBytes);
        using var sourceBitmap = SKBitmap.Decode(inputStream)
            ?? throw new InvalidDataException("The provided data is not a supported image.");

        _logger.LogInformation("Image loaded: {Width}x{Height}", sourceBitmap.Width, sourceBitmap.Height);

        SKBitmap? resizedBitmap = null;
        var outputBitmap = sourceBitmap;

        if (sourceBitmap.Width > maxWidth)
        {
            var newHeight = Math.Max(1, (int)Math.Round((double)sourceBitmap.Height * maxWidth / sourceBitmap.Width));
            _logger.LogInformation("Resizing to {Width}x{Height}", maxWidth, newHeight);

            resizedBitmap = sourceBitmap.Resize(new SKImageInfo(maxWidth, newHeight), SKFilterQuality.High)
                ?? throw new InvalidOperationException("The image could not be resized.");
            outputBitmap = resizedBitmap;
        }

        using (resizedBitmap)
        using (var image = SKImage.FromBitmap(outputBitmap))
        using (var encodedImage = image.Encode(SKEncodedImageFormat.Webp, Math.Clamp(quality, 0, 100))
            ?? throw new InvalidOperationException("The image could not be encoded as WebP."))
        {
            var result = Convert.ToBase64String(encodedImage.ToArray());
            var savings = ((double)(imageBytes.Length - result.Length) / imageBytes.Length * 100).ToString("F1");

            _logger.LogInformation(
                "Image compressed: {OriginalSize} -> {CompressedSize} bytes ({Savings}% smaller)",
                imageBytes.Length, result.Length, savings);

            return Task.FromResult(result);
        }
    }
}
