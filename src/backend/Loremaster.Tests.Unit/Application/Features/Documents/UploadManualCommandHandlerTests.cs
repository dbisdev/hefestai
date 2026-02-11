using FluentAssertions;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Documents.Commands.UploadManual;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Documents;

/// <summary>
/// Unit tests for UploadManualCommandHandler.
/// Tests PDF parsing, chunking, document creation, and embedding generation.
/// </summary>
public class UploadManualCommandHandlerTests
{
    private readonly Mock<IPdfParsingService> _pdfParsingServiceMock;
    private readonly Mock<ITextChunkingService> _textChunkingServiceMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IGameSystemRepository> _gameSystemRepositoryMock;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UploadManualCommandHandler>> _loggerMock;
    private readonly UploadManualCommandHandler _handler;

    private readonly Guid _gameSystemId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    public UploadManualCommandHandlerTests()
    {
        _pdfParsingServiceMock = new Mock<IPdfParsingService>();
        _textChunkingServiceMock = new Mock<ITextChunkingService>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _gameSystemRepositoryMock = new Mock<IGameSystemRepository>();
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UploadManualCommandHandler>>();

        _handler = new UploadManualCommandHandler(
            _pdfParsingServiceMock.Object,
            _textChunkingServiceMock.Object,
            _documentRepositoryMock.Object,
            _gameSystemRepositoryMock.Object,
            _embeddingServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Default setup
        SetupGameSystem();
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidPdf_ShouldCreateParentDocumentAndChunks()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Test content for chunking", 10);
        var chunks = CreateChunks(3);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(3);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Test Manual",
            pdfContent,
            RagSourceType.Rulebook,
            "v1.0");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Manual");
        result.PageCount.Should().Be(10);
        result.ChunkCount.Should().Be(3);
        result.EmbeddingsGenerated.Should().Be(3);
        result.TotalCharacters.Should().Be(parseResult.Content.Length);
        result.ManualId.Should().NotBeEmpty();

        // Verify parent document was created
        _documentRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Document>(d => d.Title == "Test Manual"), It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify chunk documents were created (3 chunks)
        _documentRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Document>(d => d.Title.Contains("Chunk")), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        // Verify SaveChanges was called (at least for parent, chunks, and embeddings)
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task Handle_WhenTitleNotProvided_ShouldUsePdfTitleOrDefault()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = new PdfParseResult
        {
            Content = "Some content here",
            PageCount = 5,
            Title = "PDF Extracted Title",
            Author = "Author Name",
            PageContents = new Dictionary<int, string>()
        };
        var chunks = CreateChunks(1);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(1);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            null, // No title provided
            pdfContent,
            RagSourceType.Rulebook);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Title.Should().Be("PDF Extracted Title");
    }

    [Fact]
    public async Task Handle_WhenPdfTitleNotAvailable_ShouldUseGameSystemNameAndSourceType()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = new PdfParseResult
        {
            Content = "Some content",
            PageCount = 5,
            Title = null, // No title in PDF
            Author = null,
            PageContents = new Dictionary<int, string>()
        };
        var chunks = CreateChunks(1);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(1);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            null, // No title provided
            pdfContent,
            RagSourceType.Supplement);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Title.Should().Contain("Test Game System");
        result.Title.Should().Contain("Supplement");
    }

    [Fact]
    public async Task Handle_WithManyChunks_ShouldBatchEmbeddingGeneration()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Long content", 100);
        var chunks = CreateChunks(25); // More than batch size (10)

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(25);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Large Manual",
            pdfContent,
            RagSourceType.Rulebook);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ChunkCount.Should().Be(25);
        
        // Should be called 3 times (10 + 10 + 5)
        _embeddingServiceMock.Verify(
            x => x.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_ShouldSetGameSystemIdOnDocuments()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Content", 1);
        var chunks = CreateChunks(1);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(1);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            pdfContent,
            RagSourceType.Rulebook);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - All documents should have the GameSystemId set
        _documentRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Document>(d => d.GameSystemId == _gameSystemId), It.IsAny<CancellationToken>()),
            Times.AtLeast(2)); // Parent + at least 1 chunk
    }

    [Fact]
    public async Task Handle_ShouldSetSourceTypeOnDocuments()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Content", 1);
        var chunks = CreateChunks(1);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(1);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            pdfContent,
            RagSourceType.Custom);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _documentRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Document>(d => d.SourceType == RagSourceType.Custom), It.IsAny<CancellationToken>()),
            Times.AtLeast(1));
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task Handle_WhenGameSystemNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        _gameSystemRepositoryMock
            .Setup(x => x.GetByIdAsync(_gameSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSystem?)null);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            CreateValidPdfBytes(),
            RagSourceType.Rulebook);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{_gameSystemId}*not found*");
    }

    [Fact]
    public async Task Handle_WhenPdfParsingReturnsEmptyContent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parseResult = new PdfParseResult
        {
            Content = "",
            PageCount = 0,
            Title = null,
            Author = null,
            PageContents = new Dictionary<int, string>()
        };

        SetupPdfParsing(parseResult);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            CreateValidPdfBytes(),
            RagSourceType.Rulebook);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty content*");
    }

    [Fact]
    public async Task Handle_WhenPdfParsingReturnsWhitespaceContent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parseResult = new PdfParseResult
        {
            Content = "   \n\t  ",
            PageCount = 1,
            Title = null,
            Author = null,
            PageContents = new Dictionary<int, string>()
        };

        SetupPdfParsing(parseResult);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            CreateValidPdfBytes(),
            RagSourceType.Rulebook);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty content*");
    }

    [Fact]
    public async Task Handle_WhenEmbeddingGenerationFails_ShouldContinueWithOtherBatches()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Content", 1);
        var chunks = CreateChunks(15); // 2 batches: 10 + 5

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);

        // First batch fails, second succeeds
        var callCount = 0;
        _embeddingServiceMock
            .Setup(x => x.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new Exception("Embedding service unavailable");
                }
                return new EmbeddingsResult(
                    Embeddings: Enumerable.Range(0, 5).Select(_ => new float[3072]).ToArray(),
                    Model: "gemini-embedding-001",
                    Dimensions: 3072);
            });

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            pdfContent,
            RagSourceType.Rulebook);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should have 5 embeddings (second batch only)
        result.EmbeddingsGenerated.Should().Be(5);
        result.ChunkCount.Should().Be(15);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task Handle_ShouldCreateMetadataWithCorrectFields()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = new PdfParseResult
        {
            Content = "Content here",
            PageCount = 42,
            Title = "Test Title",
            Author = "Test Author",
            PageContents = new Dictionary<int, string>()
        };
        var chunks = CreateChunks(1);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(1);

        Document? capturedDocument = null;
        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback<Document, CancellationToken>((doc, _) =>
            {
                if (capturedDocument == null) // Capture first (parent) document
                    capturedDocument = doc;
            })
            .ReturnsAsync((Document d, CancellationToken _) => d);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual with Metadata",
            pdfContent,
            RagSourceType.Supplement,
            "v2.0");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedDocument.Should().NotBeNull();
        capturedDocument!.Metadata.Should().Contain("pageCount");
        capturedDocument.Metadata.Should().Contain("42");
        capturedDocument.Metadata.Should().Contain("sourceType");
        capturedDocument.Metadata.Should().Contain("Supplement");
        capturedDocument.Metadata.Should().Contain("author");
        capturedDocument.Metadata.Should().Contain("Test Author");
        capturedDocument.Metadata.Should().Contain("version");
        capturedDocument.Metadata.Should().Contain("v2.0");
        capturedDocument.Metadata.Should().Contain("uploadedAt");
    }

    #endregion

    #region Chunk Document Tests

    [Fact]
    public async Task Handle_ChunkDocuments_ShouldHaveCorrectParentRelationship()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Content", 1);
        var chunks = CreateChunks(2);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(2);

        var capturedDocuments = new List<Document>();
        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback<Document, CancellationToken>((doc, _) => capturedDocuments.Add(doc))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "Manual",
            pdfContent,
            RagSourceType.Rulebook);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedDocuments.Should().HaveCount(3); // 1 parent + 2 chunks
        var parent = capturedDocuments.First();
        var chunkDocs = capturedDocuments.Skip(1).ToList();

        chunkDocs.Should().AllSatisfy(chunk =>
        {
            chunk.ParentDocumentId.Should().Be(parent.Id);
            chunk.ChunkIndex.Should().NotBeNull();
        });

        chunkDocs[0].ChunkIndex.Should().Be(0);
        chunkDocs[1].ChunkIndex.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ChunkDocuments_ShouldHaveDescriptiveTitles()
    {
        // Arrange
        var pdfContent = CreateValidPdfBytes();
        var parseResult = CreateParseResult("Content", 1);
        var chunks = CreateChunks(3);

        SetupPdfParsing(parseResult);
        SetupChunking(chunks);
        SetupEmbeddingGeneration(3);

        var capturedDocuments = new List<Document>();
        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback<Document, CancellationToken>((doc, _) => capturedDocuments.Add(doc))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        var command = new UploadManualCommand(
            _gameSystemId,
            _ownerId,
            "My Rulebook",
            pdfContent,
            RagSourceType.Rulebook);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var chunkDocs = capturedDocuments.Skip(1).ToList();
        chunkDocs[0].Title.Should().Be("My Rulebook - Chunk 1/3");
        chunkDocs[1].Title.Should().Be("My Rulebook - Chunk 2/3");
        chunkDocs[2].Title.Should().Be("My Rulebook - Chunk 3/3");
    }

    #endregion

    #region Helper Methods

    private void SetupGameSystem()
    {
        var gameSystem = GameSystem.Create(
            code: "test-system",
            name: "Test Game System",
            publisher: "TestCorp",
            description: "A test game system for unit tests");
        _gameSystemRepositoryMock
            .Setup(x => x.GetByIdAsync(_gameSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameSystem);
    }

    private static byte[] CreateValidPdfBytes()
    {
        // Return some bytes that represent a PDF (won't actually be parsed in unit tests)
        return new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF magic bytes
    }

    private PdfParseResult CreateParseResult(string content, int pageCount)
    {
        return new PdfParseResult
        {
            Content = content,
            PageCount = pageCount,
            Title = "Parsed Title",
            Author = "Author",
            PageContents = new Dictionary<int, string>()
        };
    }

    private void SetupPdfParsing(PdfParseResult result)
    {
        _pdfParsingServiceMock
            .Setup(x => x.ParseAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private static IReadOnlyList<TextChunk> CreateChunks(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new TextChunk
            {
                Content = $"Chunk content {i}",
                Index = i,
                StartOffset = i * 100,
                EndOffset = (i + 1) * 100
            })
            .ToList();
    }

    private void SetupChunking(IReadOnlyList<TextChunk> chunks)
    {
        _textChunkingServiceMock
            .Setup(x => x.ChunkText(It.IsAny<string>(), It.IsAny<ChunkingOptions>()))
            .Returns(chunks);
    }

    private void SetupEmbeddingGeneration(int expectedCount)
    {
        _embeddingServiceMock
            .Setup(x => x.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> texts, CancellationToken _) =>
                new EmbeddingsResult(
                    Embeddings: texts.Select(_ => new float[3072]).ToArray(),
                    Model: "gemini-embedding-001",
                    Dimensions: 3072));
    }

    #endregion
}
