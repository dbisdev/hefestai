namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Result of parsing a PDF document.
/// </summary>
public record PdfParseResult
{
    /// <summary>
    /// The full extracted text content from the PDF.
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Number of pages in the PDF.
    /// </summary>
    public required int PageCount { get; init; }
    
    /// <summary>
    /// Optional title extracted from PDF metadata.
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// Optional author extracted from PDF metadata.
    /// </summary>
    public string? Author { get; init; }
    
    /// <summary>
    /// Text content organized by page number (1-indexed).
    /// </summary>
    public Dictionary<int, string>? PageContents { get; init; }
}

/// <summary>
/// Service for parsing PDF documents and extracting text content.
/// </summary>
public interface IPdfParsingService
{
    /// <summary>
    /// Parses a PDF file and extracts text content.
    /// </summary>
    /// <param name="pdfStream">Stream containing the PDF file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed PDF result with extracted text.</returns>
    Task<PdfParseResult> ParseAsync(Stream pdfStream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Parses a PDF file from a byte array.
    /// </summary>
    /// <param name="pdfBytes">PDF file as byte array.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed PDF result with extracted text.</returns>
    Task<PdfParseResult> ParseAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
}
