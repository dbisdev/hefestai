using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loremaster.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldEntitiesAndUserMasterRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Player",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "User");

            migrationBuilder.AddColumn<string>(
                name: "invitation_code",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "master_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "world_entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    meta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    image = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    stats = table.Column<string>(type: "jsonb", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_entities", x => x.id);
                    table.ForeignKey(
                        name: "FK_world_entities_users_creator_id",
                        column: x => x.creator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_invitation_code",
                table: "users",
                column: "invitation_code",
                unique: true,
                filter: "invitation_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_master_id",
                table: "users",
                column: "master_id",
                filter: "master_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_world_entities_category",
                table: "world_entities",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_world_entities_creator_category",
                table: "world_entities",
                columns: new[] { "creator_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_world_entities_creator_id",
                table: "world_entities",
                column: "creator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_master_id",
                table: "users",
                column: "master_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_master_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "world_entities");

            migrationBuilder.DropIndex(
                name: "ix_users_invitation_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_master_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "invitation_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "master_id",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "User",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Player");
        }
    }
}
