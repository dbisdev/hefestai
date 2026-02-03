using System.Text;
using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// PDF parsing service implementation using PdfPig library.
/// Extracts text content and metadata from PDF files.
/// </summary>
public class PdfParsingService : IPdfParsingService
{
    private readonly ILogger<PdfParsingService> _logger;

    public PdfParsingService(ILogger<PdfParsingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<PdfParseResult> ParseAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream == null)
            throw new ArgumentNullException(nameof(pdfStream));

        return Task.Run(() => ParseInternal(pdfStream), cancellationToken);
    }

    /// <inheritdoc />
    public Task<PdfParseResult> ParseAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF bytes cannot be null or empty", nameof(pdfBytes));

        // Create MemoryStream inside Task.Run to ensure it's not disposed prematurely
        return Task.Run(() =>
        {
            using var stream = new MemoryStream(pdfBytes);
            return ParseInternal(stream);
        }, cancellationToken);
    }

    /// <summary>
    /// Internal synchronous parsing method.
    /// </summary>
    private PdfParseResult ParseInternal(Stream pdfStream)
    {
        _logger.LogDebug("Starting PDF parsing");

        using var document = PdfDocument.Open(pdfStream);
        
        var pageContents = new Dictionary<int, string>();
        var fullContentBuilder = new StringBuilder();
        
        _logger.LogDebug("PDF has {PageCount} pages", document.NumberOfPages);

        foreach (var page in document.GetPages())
        {
            var pageText = ExtractPageText(page);
            pageContents[page.Number] = pageText;
            
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                if (fullContentBuilder.Length > 0)
                {
                    fullContentBuilder.AppendLine();
                    fullContentBuilder.AppendLine();
                }
                fullContentBuilder.Append(pageText);
            }
        }

        // Extract metadata
        var title = ExtractTitle(document);
        var author = ExtractAuthor(document);

        var fullContent = fullContentBuilder.ToString().Trim();
        
        _logger.LogInformation(
            "PDF parsed successfully: {PageCount} pages, {CharCount} characters, Title: {Title}",
            document.NumberOfPages,
            fullContent.Length,
            title ?? "(none)");

        return new PdfParseResult
        {
            Content = fullContent,
            PageCount = document.NumberOfPages,
            Title = title,
            Author = author,
            PageContents = pageContents
        };
    }

    /// <summary>
    /// Extracts text from a single page, normalizing whitespace and layout.
    /// </summary>
    private string ExtractPageText(Page page)
    {
        try
        {
            // Get all words on the page
            var words = page.GetWords().ToList();
            
            if (words.Count == 0)
            {
                // Fallback to raw text extraction
                return NormalizeText(page.Text);
            }

            // Group words by approximate line position (Y coordinate)
            var lines = GroupWordsIntoLines(words);
            
            var pageBuilder = new StringBuilder();
            foreach (var line in lines)
            {
                if (pageBuilder.Length > 0)
                    pageBuilder.AppendLine();
                pageBuilder.Append(line);
            }

            return NormalizeText(pageBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting text from page {PageNumber}, falling back to raw text", page.Number);
            return NormalizeText(page.Text);
        }
    }

    /// <summary>
    /// Groups words into lines based on their Y coordinate.
    /// </summary>
    private List<string> GroupWordsIntoLines(List<Word> words)
    {
        if (words.Count == 0)
            return new List<string>();

        // Sort words by Y (descending, since PDF Y starts at bottom) then X
        var sortedWords = words
            .OrderByDescending(w => w.BoundingBox.Bottom)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var lastY = sortedWords[0].BoundingBox.Bottom;
        var lineThreshold = 5.0; // Pixels tolerance for same line

        foreach (var word in sortedWords)
        {
            var wordY = word.BoundingBox.Bottom;
            
            // Check if this word is on a new line
            if (Math.Abs(wordY - lastY) > lineThreshold)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString().Trim());
                    currentLine.Clear();
                }
                lastY = wordY;
            }

            if (currentLine.Length > 0)
                currentLine.Append(' ');
            currentLine.Append(word.Text);
        }

        // Add the last line
        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString().Trim());

        return lines;
    }

    /// <summary>
    /// Normalizes text by cleaning up whitespace and special characters.
    /// </summary>
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Replace various whitespace characters with single space
        var normalized = text
            .Replace('\r', ' ')
            .Replace('\t', ' ');

        // Collapse multiple spaces into one
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        // Normalize line endings
        normalized = normalized.Replace(" \n", "\n").Replace("\n ", "\n");
        
        // Collapse multiple newlines into double newlines (paragraph breaks)
        while (normalized.Contains("\n\n\n"))
        {
            normalized = normalized.Replace("\n\n\n", "\n\n");
        }

        return normalized.Trim();
    }

    /// <summary>
    /// Extracts the document title from PDF metadata or content.
    /// </summary>
    private string? ExtractTitle(PdfDocument document)
    {
        // Try metadata first
        if (document.Information != null)
        {
            if (!string.IsNullOrWhiteSpace(document.Information.Title))
                return document.Information.Title.Trim();
        }

        return null;
    }

    /// <summary>
    /// Extracts the document author from PDF metadata.
    /// </summary>
    private string? ExtractAuthor(PdfDocument document)
    {
        if (document.Information != null)
        {
            if (!string.IsNullOrWhiteSpace(document.Information.Author))
                return document.Information.Author.Trim();
        }

        return null;
    }
}
