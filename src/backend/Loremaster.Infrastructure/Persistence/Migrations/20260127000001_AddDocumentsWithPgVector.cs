using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loremaster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDocumentsWithPgVector : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Ensure pgvector extension exists
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

        migrationBuilder.CreateTable(
            name: "Documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                Source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Metadata = table.Column<string>(type: "jsonb", nullable: true),
                Embedding = table.Column<string>(type: "vector(768)", nullable: true),
                EmbeddingDimensions = table.Column<int>(type: "integer", nullable: true),
                OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documents", x => x.Id);
                table.ForeignKey(
                    name: "FK_Documents_Users_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Documents_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Documents_OwnerId",
            table: "Documents",
            column: "OwnerId");

        migrationBuilder.CreateIndex(
            name: "IX_Documents_ProjectId",
            table: "Documents",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Documents_CreatedAt",
            table: "Documents",
            column: "CreatedAt");

        // Create pgvector index for similarity search (IVFFlat)
        // This significantly speeds up vector similarity queries
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ""IX_Documents_Embedding_Vector"" 
            ON ""Documents"" 
            USING ivfflat (""Embedding"" vector_cosine_ops) 
            WITH (lists = 100)
            WHERE ""Embedding"" IS NOT NULL;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Documents");
    }
}
