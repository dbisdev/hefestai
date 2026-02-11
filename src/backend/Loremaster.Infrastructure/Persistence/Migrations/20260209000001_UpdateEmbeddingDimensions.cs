using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loremaster.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to update embedding column from vector(768) to vector(3072).
    /// This change supports the gemini-embedding-001 model which produces 3072-dimensional embeddings.
    /// IMPORTANT: Existing embeddings will be cleared as they have incompatible dimensions.
    /// </summary>
    public partial class UpdateEmbeddingDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing embeddings since they have incompatible dimensions (768 vs 3072)
            // This is necessary because pgvector cannot cast between different vector dimensions
            migrationBuilder.Sql(@"
                UPDATE documents 
                SET embedding = NULL, embedding_dimensions = NULL 
                WHERE embedding IS NOT NULL;
            ");

            // Alter the embedding column from vector(768) to vector(3072)
            // Using raw SQL because EF Core doesn't support AlterColumn for vector types directly
            migrationBuilder.Sql(@"
                ALTER TABLE documents 
                ALTER COLUMN embedding TYPE vector(3072);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear existing embeddings since they have incompatible dimensions (3072 vs 768)
            migrationBuilder.Sql(@"
                UPDATE documents 
                SET embedding = NULL, embedding_dimensions = NULL 
                WHERE embedding IS NOT NULL;
            ");

            // Revert the embedding column back to vector(768)
            migrationBuilder.Sql(@"
                ALTER TABLE documents 
                ALTER COLUMN embedding TYPE vector(768);
            ");
        }
    }
}
