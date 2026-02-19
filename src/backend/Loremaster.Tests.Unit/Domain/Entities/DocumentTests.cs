using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Pgvector;

namespace Loremaster.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for Document entity.
/// Tests document creation, chunking, embedding, and ownership.
/// </summary>
public class DocumentTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _gameSystemId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateDocument()
    {
        // Arrange & Act
        var document = Document.Create(
            title: "Test Document",
            content: "This is the content of the test document.",
            ownerId: _ownerId,
            source: "Test Source",
            metadata: "{\"key\": \"value\"}");

        // Assert
        document.Should().NotBeNull();
        document.Title.Should().Be("Test Document");
        document.Content.Should().Be("This is the content of the test document.");
        document.OwnerId.Should().Be(_ownerId);
        document.Source.Should().Be("Test Source");
        document.Metadata.Should().Be("{\"key\": \"value\"}");
        document.HasEmbedding.Should().BeFalse();
    }

    [Fact]
    public void Create_WithGameSystem_ShouldSetGameSystemId()
    {
        // Arrange & Act
        var document = Document.Create(
            title: "Test Document",
            content: "Content",
            ownerId: _ownerId,
            gameSystemId: _gameSystemId,
            sourceType: RagSourceType.Rulebook);

        // Assert
        document.GameSystemId.Should().Be(_gameSystemId);
        document.SourceType.Should().Be(RagSourceType.Rulebook);
    }

    [Fact]
    public void Create_ShouldTrimTitleAndSource()
    {
        // Arrange & Act
        var document = Document.Create(
            title: "  Test Document  ",
            content: "Content",
            ownerId: _ownerId,
            source: "  Test Source  ");

        // Assert
        document.Title.Should().Be("Test Document");
        document.Source.Should().Be("Test Source");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Act
        var act = () => Document.Create(invalidTitle!, "Content", _ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidContent_ShouldThrowArgumentException(string? invalidContent)
    {
        // Act
        var act = () => Document.Create("Title", invalidContent!, _ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    #endregion

    #region CreateChunk Tests

    [Fact]
    public void CreateChunk_WithValidData_ShouldCreateChunk()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var chunk = Document.CreateChunk(
            title: "Chunk 1",
            content: "Chunk content",
            ownerId: _ownerId,
            parentDocumentId: parentId,
            chunkIndex: 0);

        // Assert
        chunk.Should().NotBeNull();
        chunk.Title.Should().Be("Chunk 1");
        chunk.Content.Should().Be("Chunk content");
        chunk.ParentDocumentId.Should().Be(parentId);
        chunk.ChunkIndex.Should().Be(0);
        chunk.IsChunk.Should().BeTrue();
    }

    [Fact]
    public void CreateChunk_WithGameSystem_ShouldSetGameSystemId()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var chunk = Document.CreateChunk(
            title: "Chunk",
            content: "Content",
            ownerId: _ownerId,
            parentDocumentId: parentId,
            chunkIndex: 0,
            gameSystemId: _gameSystemId);

        // Assert
        chunk.GameSystemId.Should().Be(_gameSystemId);
    }

    [Fact]
    public void CreateChunk_WithNegativeIndex_ShouldThrowArgumentException()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var act = () => Document.CreateChunk(
            title: "Chunk",
            content: "Content",
            ownerId: _ownerId,
            parentDocumentId: parentId,
            chunkIndex: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("chunkIndex");
    }

    #endregion

    #region UpdateContent Tests

    [Fact]
    public void UpdateContent_ShouldUpdateDocument()
    {
        // Arrange
        var document = Document.Create("Original", "Original content", _ownerId);

        // Act
        document.UpdateContent("Updated", "Updated content", "New Source");

        // Assert
        document.Title.Should().Be("Updated");
        document.Content.Should().Be("Updated content");
        document.Source.Should().Be("New Source");
    }

    [Fact]
    public void UpdateContent_ShouldClearEmbedding()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);
        document.SetEmbedding(new float[] { 0.1f, 0.2f, 0.3f });
        document.HasEmbedding.Should().BeTrue();

        // Act
        document.UpdateContent("Updated", "New Content");

        // Assert
        document.HasEmbedding.Should().BeFalse();
        document.Embedding.Should().BeNull();
        document.EmbeddingDimensions.Should().BeNull();
    }

    #endregion

    #region Embedding Tests

    [Fact]
    public void SetEmbedding_ShouldSetEmbeddingAndDimensions()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);
        var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

        // Act
        document.SetEmbedding(embedding);

        // Assert
        document.HasEmbedding.Should().BeTrue();
        document.Embedding.Should().NotBeNull();
        document.EmbeddingDimensions.Should().Be(5);
    }

    [Fact]
    public void SetEmbedding_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);

        // Act
        var act = () => document.SetEmbedding(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearEmbedding_ShouldClearEmbedding()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);
        document.SetEmbedding(new float[] { 0.1f, 0.2f });

        // Act
        document.ClearEmbedding();

        // Assert
        document.HasEmbedding.Should().BeFalse();
        document.Embedding.Should().BeNull();
        document.EmbeddingDimensions.Should().BeNull();
    }

    #endregion

    #region Ownership Tests

    [Fact]
    public void IsOwnedBy_WithCorrectOwner_ShouldReturnTrue()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);

        // Act
        var result = document.IsOwnedBy(_ownerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_WithWrongOwner_ShouldReturnFalse()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);
        var otherUserId = Guid.NewGuid();

        // Act
        var result = document.IsOwnedBy(otherUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Game System Association Tests

    [Fact]
    public void SetGameSystem_ShouldAssociateWithGameSystem()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);

        // Act
        document.SetGameSystem(_gameSystemId, RagSourceType.Supplement);

        // Assert
        document.GameSystemId.Should().Be(_gameSystemId);
        document.SourceType.Should().Be(RagSourceType.Supplement);
    }

    [Fact]
    public void ClearGameSystem_ShouldRemoveAssociation()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId, gameSystemId: _gameSystemId);

        // Act
        document.ClearGameSystem();

        // Assert
        document.GameSystemId.Should().BeNull();
        document.SourceType.Should().BeNull();
    }

    #endregion

    #region IsChunk Tests

    [Fact]
    public void IsChunk_ForRegularDocument_ShouldReturnFalse()
    {
        // Arrange
        var document = Document.Create("Test", "Content", _ownerId);

        // Assert
        document.IsChunk.Should().BeFalse();
    }

    [Fact]
    public void IsChunk_ForChunkDocument_ShouldReturnTrue()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var chunk = Document.CreateChunk("Chunk", "Content", _ownerId, parentId, 0);

        // Assert
        chunk.IsChunk.Should().BeTrue();
    }

    #endregion
}
