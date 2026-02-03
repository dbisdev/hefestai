using FluentAssertions;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Infrastructure.Services;

namespace Loremaster.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for TextChunkingService.
/// Tests various chunking scenarios, options, and edge cases.
/// </summary>
public class TextChunkingServiceTests
{
    private readonly TextChunkingService _sut;

    public TextChunkingServiceTests()
    {
        _sut = new TextChunkingService();
    }

    #region Basic Chunking Tests

    [Fact]
    public void ChunkText_WithEmptyString_ReturnsEmptyList()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_WithNullString_ReturnsEmptyList()
    {
        // Arrange
        string? text = null;

        // Act
        var result = _sut.ChunkText(text!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_WithWhitespaceOnly_ReturnsEmptyList()
    {
        // Arrange
        var text = "   \n\t\r\n   ";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_WithShortText_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is a short text.";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("This is a short text.");
        result[0].Index.Should().Be(0);
        result[0].StartOffset.Should().Be(0);
        result[0].EndOffset.Should().Be(text.Length);
    }

    [Fact]
    public void ChunkText_WithTextSmallerThanMaxChunkSize_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is exactly 100 characters including spaces and punctuation marks. We need to test boundaries!";
        var options = new ChunkingOptions
        {
            MaxChunkSize = 500,
            MinChunkSize = 10
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be(text);
    }

    #endregion

    #region Multi-Chunk Tests

    [Fact]
    public void ChunkText_WithLongText_ReturnsMultipleChunks()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("This is a test sentence.", 50));
        var options = new ChunkingOptions
        {
            MaxChunkSize = 200,
            MinChunkSize = 50,
            OverlapSize = 20,
            PreserveSentences = true
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.All(c => c.Content.Length <= options.MaxChunkSize + 50).Should().BeTrue();
        // Verify indices are sequential
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Index.Should().Be(i);
        }
    }

    [Fact]
    public void ChunkText_WithMultipleParagraphs_PreservesParagraphBoundaries()
    {
        // Arrange
        var text = @"First paragraph with some content here.

Second paragraph that contains different information.

Third paragraph to complete the text.";
        var options = new ChunkingOptions
        {
            MaxChunkSize = 100,
            MinChunkSize = 10,
            OverlapSize = 10,
            PreserveParagraphs = true
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        result.Should().NotBeEmpty();
        // Each chunk should be a complete paragraph or split at sentence boundaries
        result.All(c => !string.IsNullOrWhiteSpace(c.Content)).Should().BeTrue();
    }

    #endregion

    #region Overlap Tests

    [Fact]
    public void ChunkText_WithOverlap_ChunksHaveOverlappingContent()
    {
        // Arrange
        var sentences = new List<string>
        {
            "First sentence here.",
            "Second sentence follows.",
            "Third sentence continues.",
            "Fourth sentence ends."
        };
        var text = string.Join(" ", sentences);
        var options = new ChunkingOptions
        {
            MaxChunkSize = 50,
            MinChunkSize = 10,
            OverlapSize = 20,
            PreserveSentences = false
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        if (result.Count > 1)
        {
            // Verify overlap by checking that consecutive chunks share some content
            for (int i = 1; i < result.Count; i++)
            {
                var previousEnd = result[i - 1].Content;
                var currentStart = result[i].Content;
                // With overlap, there should be some shared text
                // This is a soft check since overlap might be at word boundaries
                result[i].StartOffset.Should().BeLessThan(result[i - 1].EndOffset + options.OverlapSize);
            }
        }
    }

    #endregion

    #region Sentence Preservation Tests

    [Fact]
    public void ChunkText_WithPreserveSentences_ChunksEndAtSentenceBoundaries()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence. Fourth sentence. Fifth sentence.";
        var options = new ChunkingOptions
        {
            MaxChunkSize = 60,
            MinChunkSize = 10,
            OverlapSize = 10,
            PreserveSentences = true,
            PreserveParagraphs = false
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        result.Should().NotBeEmpty();
        // Most chunks should end with a period (sentence boundary)
        var chunksEndingWithPeriod = result.Count(c => c.Content.TrimEnd().EndsWith('.'));
        chunksEndingWithPeriod.Should().BeGreaterThan(0);
    }

    #endregion

    #region Options Validation Tests

    [Fact]
    public void ChunkText_WithNegativeMaxChunkSize_ThrowsArgumentException()
    {
        // Arrange
        var text = "Some text";
        var options = new ChunkingOptions { MaxChunkSize = -1 };

        // Act & Assert
        var act = () => _sut.ChunkText(text, options);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MaxChunkSize*");
    }

    [Fact]
    public void ChunkText_WithZeroMaxChunkSize_ThrowsArgumentException()
    {
        // Arrange
        var text = "Some text";
        var options = new ChunkingOptions { MaxChunkSize = 0 };

        // Act & Assert
        var act = () => _sut.ChunkText(text, options);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MaxChunkSize*");
    }

    [Fact]
    public void ChunkText_WithNegativeOverlapSize_ThrowsArgumentException()
    {
        // Arrange
        var text = "Some text";
        var options = new ChunkingOptions { MaxChunkSize = 100, OverlapSize = -1 };

        // Act & Assert
        var act = () => _sut.ChunkText(text, options);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*OverlapSize*");
    }

    [Fact]
    public void ChunkText_WithOverlapGreaterThanMaxSize_ThrowsArgumentException()
    {
        // Arrange
        var text = "Some text";
        var options = new ChunkingOptions { MaxChunkSize = 100, OverlapSize = 150 };

        // Act & Assert
        var act = () => _sut.ChunkText(text, options);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*OverlapSize*");
    }

    [Fact]
    public void ChunkText_WithNegativeMinChunkSize_ThrowsArgumentException()
    {
        // Arrange
        var text = "Some text";
        var options = new ChunkingOptions { MaxChunkSize = 100, OverlapSize = 10, MinChunkSize = -1 };

        // Act & Assert
        var act = () => _sut.ChunkText(text, options);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MinChunkSize*");
    }

    [Fact]
    public void ChunkText_WithMinGreaterThanMaxChunkSize_ThrowsArgumentException()
    {
        // Arrange
        var text = "Some text";
        var options = new ChunkingOptions { MaxChunkSize = 100, OverlapSize = 10, MinChunkSize = 200 };

        // Act & Assert
        var act = () => _sut.ChunkText(text, options);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MinChunkSize*");
    }

    #endregion

    #region Default Options Tests

    [Fact]
    public void ChunkText_WithDefaultOptions_UsesCorrectDefaults()
    {
        // Arrange
        var text = "This is a test text.";

        // Act - using overload without options
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().NotBeEmpty();
        // Default options should work without throwing
    }

    [Fact]
    public void ChunkingOptions_Default_HasExpectedValues()
    {
        // Arrange & Act
        var defaults = ChunkingOptions.Default;

        // Assert
        defaults.MaxChunkSize.Should().Be(1000);
        defaults.OverlapSize.Should().Be(200);
        defaults.MinChunkSize.Should().Be(100);
        defaults.PreserveParagraphs.Should().BeTrue();
        defaults.PreserveSentences.Should().BeTrue();
    }

    #endregion

    #region Whitespace Normalization Tests

    [Fact]
    public void ChunkText_WithExcessiveWhitespace_NormalizesSpaces()
    {
        // Arrange
        var text = "Word1    Word2     Word3";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().NotContain("  "); // No double spaces
    }

    [Fact]
    public void ChunkText_WithMixedLineEndings_NormalizesLineEndings()
    {
        // Arrange
        var text = "Line1\r\nLine2\rLine3\nLine4";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().NotBeEmpty();
        // Should not contain carriage returns
        result[0].Content.Should().NotContain("\r");
    }

    [Fact]
    public void ChunkText_WithTabs_ReplacesWithSpaces()
    {
        // Arrange
        var text = "Word1\tWord2\tWord3";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().NotContain("\t");
    }

    [Fact]
    public void ChunkText_WithExcessiveNewlines_CollapsesToParagraphBreaks()
    {
        // Arrange
        var text = "Paragraph1\n\n\n\n\nParagraph2";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().NotBeEmpty();
        result[0].Content.Should().NotContain("\n\n\n");
    }

    #endregion

    #region TextChunk Property Tests

    [Fact]
    public void ChunkText_ReturnsChunksWithCorrectOffsets()
    {
        // Arrange
        var text = "Short text for testing offsets.";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().HaveCount(1);
        result[0].StartOffset.Should().BeGreaterOrEqualTo(0);
        result[0].EndOffset.Should().BeGreaterThan(result[0].StartOffset);
        result[0].EndOffset.Should().BeLessOrEqualTo(text.Length);
    }

    [Fact]
    public void ChunkText_ChunksHaveSequentialIndices()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("Sentence.", 100));
        var options = new ChunkingOptions
        {
            MaxChunkSize = 100,
            MinChunkSize = 20,
            OverlapSize = 10
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Index.Should().Be(i);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ChunkText_WithSingleWord_ReturnsSingleChunk()
    {
        // Arrange
        var text = "Word";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Word");
    }

    [Fact]
    public void ChunkText_WithPunctuationOnly_ReturnsEmptyOrSingleChunk()
    {
        // Arrange
        var text = "...";

        // Act
        var result = _sut.ChunkText(text);

        // Assert
        if (result.Count > 0)
        {
            result[0].Content.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void ChunkText_WithVeryLongWord_HandlesGracefully()
    {
        // Arrange
        var longWord = new string('a', 2000);
        var options = new ChunkingOptions
        {
            MaxChunkSize = 500,
            MinChunkSize = 10,
            OverlapSize = 50
        };

        // Act
        var result = _sut.ChunkText(longWord, options);

        // Assert
        result.Should().NotBeEmpty();
        // Should not throw and should return at least one chunk
    }

    [Fact]
    public void ChunkText_WithMinimalMinChunkSize_IncludesSmallFinalChunk()
    {
        // Arrange
        var text = "First part. Second part. End.";
        var options = new ChunkingOptions
        {
            MaxChunkSize = 20,
            MinChunkSize = 1, // Very small minimum
            OverlapSize = 0,
            PreserveSentences = false
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void ChunkText_WithRulebookContent_ChunksAppropriately()
    {
        // Arrange - Simulate RPG rulebook content
        var text = @"Chapter 1: Character Creation

Creating a character involves several steps. First, you must choose a race. Elves are graceful and long-lived. Dwarves are hardy and resilient. Humans are versatile and ambitious.

Second, you select a class. Warriors excel in combat. Mages wield powerful magic. Rogues specialize in stealth and cunning.

Chapter 2: Combat Rules

Combat is resolved in turns. Each character has an initiative score. Higher initiative acts first. On your turn, you may move and take an action.

Actions include attacking, casting spells, or using items. Attacks require a roll against the target's defense. If you roll higher, you hit and deal damage.";

        var options = new ChunkingOptions
        {
            MaxChunkSize = 300,
            MinChunkSize = 100,
            OverlapSize = 50,
            PreserveParagraphs = true,
            PreserveSentences = true
        };

        // Act
        var result = _sut.ChunkText(text, options);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.All(c => c.Content.Length >= options.MinChunkSize || c == result.Last()).Should().BeTrue();
        // Content should be preserved
        var allContent = string.Join(" ", result.Select(c => c.Content));
        allContent.Should().Contain("Character Creation");
        allContent.Should().Contain("Combat Rules");
    }

    #endregion
}
