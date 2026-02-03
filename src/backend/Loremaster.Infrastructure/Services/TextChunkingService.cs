using System.Text;
using System.Text.RegularExpressions;
using Loremaster.Application.Common.Interfaces;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// Text chunking service that splits text into overlapping chunks suitable for embedding.
/// Attempts to preserve semantic boundaries (paragraphs, sentences) when possible.
/// </summary>
public partial class TextChunkingService : ITextChunkingService
{
    // Regex patterns for sentence and paragraph detection
    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-Z])", RegexOptions.Compiled)]
    private static partial Regex SentenceBoundaryRegex();
    
    [GeneratedRegex(@"\n\s*\n", RegexOptions.Compiled)]
    private static partial Regex ParagraphBoundaryRegex();

    /// <inheritdoc />
    public IReadOnlyList<TextChunk> ChunkText(string text)
    {
        return ChunkText(text, ChunkingOptions.Default);
    }

    /// <inheritdoc />
    public IReadOnlyList<TextChunk> ChunkText(string text, ChunkingOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<TextChunk>();

        options ??= ChunkingOptions.Default;
        ValidateOptions(options);

        var chunks = new List<TextChunk>();
        var normalizedText = NormalizeText(text);
        
        if (normalizedText.Length <= options.MaxChunkSize)
        {
            // Text fits in a single chunk
            chunks.Add(new TextChunk
            {
                Content = normalizedText,
                Index = 0,
                StartOffset = 0,
                EndOffset = normalizedText.Length
            });
            return chunks;
        }

        // Split into semantic units based on options
        var units = SplitIntoUnits(normalizedText, options);
        
        // Build chunks from units
        var currentChunk = new StringBuilder();
        var currentStartOffset = 0;
        var chunkIndex = 0;
        var currentOffset = 0;

        foreach (var unit in units)
        {
            var unitLength = unit.Length;
            
            // Check if adding this unit would exceed max size
            if (currentChunk.Length > 0 && 
                currentChunk.Length + unitLength + 1 > options.MaxChunkSize)
            {
                // Save current chunk if it meets minimum size
                if (currentChunk.Length >= options.MinChunkSize)
                {
                    chunks.Add(new TextChunk
                    {
                        Content = currentChunk.ToString().Trim(),
                        Index = chunkIndex++,
                        StartOffset = currentStartOffset,
                        EndOffset = currentOffset
                    });

                    // Start new chunk with overlap
                    var overlapStart = Math.Max(0, currentChunk.Length - options.OverlapSize);
                    var overlap = currentChunk.ToString().Substring(overlapStart);
                    
                    currentChunk.Clear();
                    currentChunk.Append(overlap);
                    currentStartOffset = currentOffset - overlap.Length;
                }
            }

            // Add separator if needed
            if (currentChunk.Length > 0 && !currentChunk.ToString().EndsWith('\n'))
            {
                currentChunk.Append(' ');
                currentOffset++;
            }

            currentChunk.Append(unit);
            currentOffset += unitLength;
        }

        // Add final chunk
        if (currentChunk.Length >= options.MinChunkSize)
        {
            chunks.Add(new TextChunk
            {
                Content = currentChunk.ToString().Trim(),
                Index = chunkIndex,
                StartOffset = currentStartOffset,
                EndOffset = currentOffset
            });
        }
        else if (currentChunk.Length > 0 && chunks.Count > 0)
        {
            // Merge small final chunk with previous
            var lastChunk = chunks[^1];
            chunks[^1] = new TextChunk
            {
                Content = lastChunk.Content + " " + currentChunk.ToString().Trim(),
                Index = lastChunk.Index,
                StartOffset = lastChunk.StartOffset,
                EndOffset = currentOffset
            };
        }
        else if (currentChunk.Length > 0)
        {
            // Only chunk, even if small
            chunks.Add(new TextChunk
            {
                Content = currentChunk.ToString().Trim(),
                Index = 0,
                StartOffset = currentStartOffset,
                EndOffset = currentOffset
            });
        }

        return chunks;
    }

    /// <summary>
    /// Splits text into semantic units (paragraphs or sentences) for chunking.
    /// </summary>
    private List<string> SplitIntoUnits(string text, ChunkingOptions options)
    {
        var units = new List<string>();

        if (options.PreserveParagraphs)
        {
            // First split by paragraphs
            var paragraphs = ParagraphBoundaryRegex().Split(text)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Length <= options.MaxChunkSize)
                {
                    units.Add(paragraph.Trim());
                }
                else if (options.PreserveSentences)
                {
                    // Paragraph is too long, split by sentences
                    var sentences = SplitIntoSentences(paragraph);
                    units.AddRange(sentences);
                }
                else
                {
                    // Split by character count
                    units.AddRange(SplitBySize(paragraph, options.MaxChunkSize));
                }
            }
        }
        else if (options.PreserveSentences)
        {
            units.AddRange(SplitIntoSentences(text));
        }
        else
        {
            units.AddRange(SplitBySize(text, options.MaxChunkSize));
        }

        return units;
    }

    /// <summary>
    /// Splits text into sentences.
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        var sentences = SentenceBoundaryRegex().Split(text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        return sentences;
    }

    /// <summary>
    /// Splits text into chunks of approximately the given size.
    /// Tries to break at word boundaries.
    /// </summary>
    private List<string> SplitBySize(string text, int maxSize)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 > maxSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }

            if (current.Length > 0)
                current.Append(' ');
            current.Append(word);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }

    /// <summary>
    /// Normalizes text whitespace and formatting.
    /// </summary>
    private static string NormalizeText(string text)
    {
        // Replace various whitespace with standard characters
        var normalized = text
            .Replace('\r', '\n')
            .Replace('\t', ' ');

        // Collapse multiple spaces
        while (normalized.Contains("  "))
            normalized = normalized.Replace("  ", " ");

        // Normalize line endings
        normalized = normalized.Replace(" \n", "\n").Replace("\n ", "\n");
        
        // Keep paragraph breaks but collapse excessive newlines
        while (normalized.Contains("\n\n\n"))
            normalized = normalized.Replace("\n\n\n", "\n\n");

        return normalized.Trim();
    }

    /// <summary>
    /// Validates chunking options.
    /// </summary>
    private static void ValidateOptions(ChunkingOptions options)
    {
        if (options.MaxChunkSize <= 0)
            throw new ArgumentException("MaxChunkSize must be positive", nameof(options));
        
        if (options.OverlapSize < 0)
            throw new ArgumentException("OverlapSize cannot be negative", nameof(options));
        
        if (options.OverlapSize >= options.MaxChunkSize)
            throw new ArgumentException("OverlapSize must be less than MaxChunkSize", nameof(options));
        
        if (options.MinChunkSize < 0)
            throw new ArgumentException("MinChunkSize cannot be negative", nameof(options));
        
        if (options.MinChunkSize > options.MaxChunkSize)
            throw new ArgumentException("MinChunkSize must be less than or equal to MaxChunkSize", nameof(options));
    }
}
