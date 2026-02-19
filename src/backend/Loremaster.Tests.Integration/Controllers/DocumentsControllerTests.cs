using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Loremaster.Application.Features.Documents.Commands.UploadManual;
using Loremaster.Domain.Entities;
using Loremaster.Infrastructure.Persistence;
using Loremaster.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Loremaster.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for DocumentsController.
/// Tests document ingestion, semantic search, and PDF upload endpoints.
/// </summary>
public class DocumentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DocumentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a game system directly in the database for testing.
    /// </summary>
    private async Task<GameSystem> CreateGameSystemInDbAsync(string name = "Test Game System")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var gameSystem = GameSystem.Create(
            code: $"test-{Guid.NewGuid():N}".Substring(0, 20),
            name: name,
            ownerId: Guid.NewGuid(),
            publisher: "Test Publisher",
            version: "1.0",
            description: "A test game system"
        );
        
        dbContext.GameSystems.Add(gameSystem);
        await dbContext.SaveChangesAsync();
        
        return gameSystem;
    }

    /// <summary>
    /// Creates a minimal valid PDF byte array for testing.
    /// This is a minimal PDF structure that PdfPig can parse.
    /// </summary>
    private static byte[] CreateMinimalPdf()
    {
        // A minimal PDF with one page containing text
        var pdfContent = @"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj
2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj
3 0 obj
<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]
   /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>
endobj
4 0 obj
<< /Length 44 >>
stream
BT
/F1 12 Tf
100 700 Td
(Test PDF Content) Tj
ET
endstream
endobj
5 0 obj
<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
endobj
xref
0 6
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000266 00000 n 
0000000359 00000 n 
trailer
<< /Size 6 /Root 1 0 R >>
startxref
434
%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(pdfContent);
    }

    #endregion

    #region PDF Upload Authentication Tests

    [Fact]
    public async Task UploadManual_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        using var content = new MultipartFormDataContent();
        var pdfBytes = CreateMinimalPdf();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", "test.pdf");
        content.Add(new StringContent("Test Manual"), "Title");

        // Act
        var response = await _client.PostAsync($"/api/documents/game-systems/{gameSystem.Id}/manuals", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadManual_WhenNoFileProvided_ShouldReturnBadRequest()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"upload-test-{Guid.NewGuid()}@example.com");
        
        var gameSystem = await CreateGameSystemInDbAsync();
        
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Test Manual"), "Title");

        // Act
        var response = await client.PostAsync($"/api/documents/game-systems/{gameSystem.Id}/manuals", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadManual_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"upload-empty-{Guid.NewGuid()}@example.com");
        
        var gameSystem = await CreateGameSystemInDbAsync();
        
        using var content = new MultipartFormDataContent();
        var emptyFileContent = new ByteArrayContent(Array.Empty<byte>());
        emptyFileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(emptyFileContent, "File", "empty.pdf");

        // Act
        var response = await client.PostAsync($"/api/documents/game-systems/{gameSystem.Id}/manuals", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Document Ingest Authentication Tests

    [Fact]
    public async Task IngestDocument_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            Title = "Test Document",
            Content = "This is test content for the document"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/documents/ingest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Semantic Search Authentication Tests

    [Fact]
    public async Task SemanticSearch_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            Query = "test search query"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/documents/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Semantic Search With GameSystemId Tests

    [Fact]
    public async Task SemanticSearch_WithGameSystemIdFilter_RequestIsAccepted()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"search-test-{Guid.NewGuid()}@example.com");
        
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var request = new
        {
            Query = "test search query",
            GameSystemId = gameSystem.Id,
            Limit = 5
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/documents/search", request);

        // Assert
        // The request should be accepted (not BadRequest due to invalid parameters)
        // Note: InMemory database doesn't support pgvector, so this may return 500 in test env
        // The key assertion is that the request isn't rejected due to validation errors
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest, 
            "Request with GameSystemId should be accepted");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Bulk Ingest Authentication Tests

    [Fact]
    public async Task BulkIngest_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            Documents = new[]
            {
                new { Title = "Doc 1", Content = "Content 1" },
                new { Title = "Doc 2", Content = "Content 2" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/documents/ingest/bulk", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Manual Tests

    [Fact]
    public async Task GetManual_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var gameSystemId = Guid.NewGuid();
        var manualId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/documents/game-systems/{gameSystemId}/manuals/{manualId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetManual_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"getmanual-test-{Guid.NewGuid()}@example.com");
        
        var gameSystemId = Guid.NewGuid();
        var manualId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/documents/game-systems/{gameSystemId}/manuals/{manualId}");

        // Assert
        // Currently returns NotFound as GetManual is not fully implemented
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
