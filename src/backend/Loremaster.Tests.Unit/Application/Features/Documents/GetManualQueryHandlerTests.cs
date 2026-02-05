using FluentAssertions;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Documents.Queries.GetManual;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Documents;

/// <summary>
/// Unit tests for GetManualQueryHandler.
/// Tests manual retrieval with chunk counts and authorization.
/// </summary>
public class GetManualQueryHandlerTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<ILogger<GetManualQueryHandler>> _loggerMock;
    private readonly GetManualQueryHandler _handler;

    private readonly Guid _manualId = Guid.NewGuid();
    private readonly Guid _gameSystemId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    public GetManualQueryHandlerTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _loggerMock = new Mock<ILogger<GetManualQueryHandler>>();

        _handler = new GetManualQueryHandler(
            _documentRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidManual_ShouldReturnManualWithChunkCount()
    {
        // Arrange
        var manual = CreateManual(_manualId, _ownerId, _gameSystemId, "Test Manual", RagSourceType.Rulebook);
        var manualWithChunks = new ManualWithChunkCount(manual, 15);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_manualId);
        result.GameSystemId.Should().Be(_gameSystemId);
        result.Title.Should().Be("Test Manual");
        result.ChunkCount.Should().Be(15);
        result.SourceType.Should().Be(RagSourceType.Rulebook);
    }

    [Fact]
    public async Task Handle_WithDifferentSourceTypes_ShouldReturnCorrectSourceType()
    {
        // Arrange
        var manual = CreateManual(_manualId, _ownerId, _gameSystemId, "Supplement Guide", RagSourceType.Supplement);
        var manualWithChunks = new ManualWithChunkCount(manual, 5);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceType.Should().Be(RagSourceType.Supplement);
    }

    [Fact]
    public async Task Handle_WithZeroChunks_ShouldReturnZeroChunkCount()
    {
        // Arrange
        var manual = CreateManual(_manualId, _ownerId, _gameSystemId, "Empty Manual", RagSourceType.Custom);
        var manualWithChunks = new ManualWithChunkCount(manual, 0);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ChunkCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTimestamps()
    {
        // Arrange
        var manual = CreateManual(_manualId, _ownerId, _gameSystemId, "Timestamped Manual", RagSourceType.Rulebook);
        var manualWithChunks = new ManualWithChunkCount(manual, 10);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CreatedAt.Should().Be(manual.CreatedAt);
        result.UpdatedAt.Should().Be(manual.UpdatedAt);
    }

    [Fact]
    public async Task Handle_WithSourceProperty_ShouldReturnSource()
    {
        // Arrange
        var manual = CreateManualWithSource(_manualId, _ownerId, _gameSystemId, "Manual With Source", "v2.5");
        var manualWithChunks = new ManualWithChunkCount(manual, 8);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Source.Should().Be("v2.5");
    }

    #endregion

    #region Not Found Cases

    [Fact]
    public async Task Handle_WhenManualNotFound_ShouldReturnNull()
    {
        // Arrange
        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualWithChunkCount?)null);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenManualBelongsToDifferentGameSystem_ShouldReturnNull()
    {
        // Arrange
        var differentGameSystemId = Guid.NewGuid();
        var manual = CreateManual(_manualId, _ownerId, differentGameSystemId, "Wrong Game System Manual", RagSourceType.Rulebook);
        var manualWithChunks = new ManualWithChunkCount(manual, 5);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenManualBelongsToDifferentOwner_ShouldReturnNull()
    {
        // Arrange - Repository returns null because owner doesn't match
        var differentOwnerId = Guid.NewGuid();
        
        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManualWithChunkCount?)null); // Repository filters by owner

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNullSourceType_ShouldReturnNullSourceType()
    {
        // Arrange
        var manual = CreateManualWithNullSourceType(_manualId, _ownerId, _gameSystemId, "No Source Type Manual");
        var manualWithChunks = new ManualWithChunkCount(manual, 3);

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceType.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithLargeChunkCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var manual = CreateManual(_manualId, _ownerId, _gameSystemId, "Large Manual", RagSourceType.Rulebook);
        var manualWithChunks = new ManualWithChunkCount(manual, 1500); // Large manual

        _documentRepositoryMock
            .Setup(x => x.GetManualWithChunkCountAsync(_manualId, _ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manualWithChunks);

        var query = new GetManualQuery(_manualId, _gameSystemId, _ownerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ChunkCount.Should().Be(1500);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock Document entity for testing.
    /// Uses reflection to set private properties since Document has factory methods.
    /// </summary>
    private static Document CreateManual(
        Guid id, 
        Guid ownerId, 
        Guid gameSystemId, 
        string title, 
        RagSourceType sourceType)
    {
        var manual = Document.Create(
            title: title,
            content: "Manual content placeholder",
            ownerId: ownerId,
            source: null,
            metadata: null,
            gameSystemId: gameSystemId,
            sourceType: sourceType);

        // Use reflection to set the Id property since it's usually set by EF Core
        typeof(Document).GetProperty("Id")!
            .SetValue(manual, id);

        return manual;
    }

    private static Document CreateManualWithSource(
        Guid id,
        Guid ownerId,
        Guid gameSystemId,
        string title,
        string source)
    {
        var manual = Document.Create(
            title: title,
            content: "Manual content placeholder",
            ownerId: ownerId,
            source: source,
            metadata: null,
            gameSystemId: gameSystemId,
            sourceType: RagSourceType.Rulebook);

        typeof(Document).GetProperty("Id")!
            .SetValue(manual, id);

        return manual;
    }

    private static Document CreateManualWithNullSourceType(
        Guid id,
        Guid ownerId,
        Guid gameSystemId,
        string title)
    {
        var manual = Document.Create(
            title: title,
            content: "Manual content placeholder",
            ownerId: ownerId,
            source: null,
            metadata: null,
            gameSystemId: gameSystemId,
            sourceType: null); // Explicitly null

        typeof(Document).GetProperty("Id")!
            .SetValue(manual, id);

        return manual;
    }

    #endregion
}
