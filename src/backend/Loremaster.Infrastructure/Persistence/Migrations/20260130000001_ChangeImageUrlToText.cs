using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loremaster.Infrastructure.Persistence.Migrations;

/// <summary>
/// Migration to change image_url and avatar_url columns from VARCHAR(500) to TEXT.
/// This allows storing base64 data URLs which can be thousands of characters long.
/// </summary>
public partial class ChangeImageUrlToText : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Change lore_entity.image_url from VARCHAR(500) to TEXT
        migrationBuilder.AlterColumn<string>(
            name: "image_url",
            table: "lore_entity",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500,
            oldNullable: true);

        // Change user.avatar_url from VARCHAR(500) to TEXT
        migrationBuilder.AlterColumn<string>(
            name: "avatar_url",
            table: "user",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500,
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert lore_entity.image_url back to VARCHAR(500)
        // WARNING: This may fail if existing data exceeds 500 characters
        migrationBuilder.AlterColumn<string>(
            name: "image_url",
            table: "lore_entity",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        // Revert user.avatar_url back to VARCHAR(500)
        // WARNING: This may fail if existing data exceeds 500 characters
        migrationBuilder.AlterColumn<string>(
            name: "avatar_url",
            table: "user",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
