namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Compresses and converts images to WebP format.
/// </summary>
public interface IImageCompressionService
{
    /// <summary>
    /// Compresses a base64-encoded image to WebP format at the specified quality and max width.
    /// </summary>
    /// <param name="base64Image">Base64-encoded source image (with or without data-URL prefix).</param>
    /// <param name="quality">WebP encoding quality (0–100).</param>
    /// <param name="maxWidth">Maximum output width in pixels; aspect ratio is preserved.</param>
    /// <returns>Base64-encoded WebP image without data-URL prefix.</returns>
    Task<string> CompressToWebPAsync(string base64Image, int quality = 80, int maxWidth = 500);
}
