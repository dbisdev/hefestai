using System;
using System.Collections.Generic;
using System.Text.Json;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Loremaster.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create extensions first
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            
            // Create PostgreSQL enums in public schema
            migrationBuilder.Sql("DO $$ BEGIN CREATE TYPE public.user_role AS ENUM ('player', 'master', 'admin'); EXCEPTION WHEN duplicate_object THEN null; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN CREATE TYPE public.campaign_role AS ENUM ('player', 'master'); EXCEPTION WHEN duplicate_object THEN null; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN CREATE TYPE public.ownership_type AS ENUM ('master', 'player', 'shared'); EXCEPTION WHEN duplicate_object THEN null; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN CREATE TYPE public.visibility_level AS ENUM ('draft', 'private', 'campaign', 'public'); EXCEPTION WHEN duplicate_object THEN null; END $$;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.campaign_role", "player,master")
                .Annotation("Npgsql:Enum:public.ownership_type", "master,player,shared")
                .Annotation("Npgsql:Enum:public.user_role", "player,master,admin")
                .Annotation("Npgsql:Enum:public.visibility_level", "draft,private,campaign,public")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "game_system",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    publisher = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    supported_entity_types = table.Column<List<string>>(type: "varchar(50)[]", nullable: false, defaultValueSql: "'{}'"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_system", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<UserRole>(type: "user_role", nullable: false, defaultValueSql: "'player'"),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    invitation_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    master_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_master",
                        column: x => x.master_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rag_source",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    game_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_source", x => x.id);
                    table.ForeignKey(
                        name: "FK_rag_source_game_system_game_system_id",
                        column: x => x.game_system_id,
                        principalTable: "game_system",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    join_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    settings = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign", x => x.id);
                    table.ForeignKey(
                        name: "FK_campaign_game_system_game_system_id",
                        column: x => x.game_system_id,
                        principalTable: "game_system",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaign_user_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_projects_user_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign_member",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<CampaignRole>(type: "campaign_role", nullable: false, defaultValueSql: "'player'"),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_campaign_member_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_campaign_member_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    EmbeddingDimensions = table.Column<int>(type: "integer", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    GameSystemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: true),
                    ParentDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_game_system_GameSystemId",
                        column: x => x.GameSystemId,
                        principalTable: "game_system",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_user_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generation_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    input_prompt = table.Column<string>(type: "text", nullable: true),
                    input_parameters = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CampaignMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_generation_request_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_generation_request_campaign_member_CampaignMemberId",
                        column: x => x.CampaignMemberId,
                        principalTable: "campaign_member",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_generation_request_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    entity_type_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    field_definitions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    icon_hint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    review_notes = table.Column<string>(type: "text", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    game_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_entity_template_Documents_source_document_id",
                        column: x => x.source_document_id,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_entity_template_game_system_game_system_id",
                        column: x => x.game_system_id,
                        principalTable: "game_system",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entity_template_user_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generation_result",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    generation_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sequence_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    raw_output = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    structured_output = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model_parameters = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    token_usage = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    confidence_score = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_result", x => x.id);
                    table.CheckConstraint("chk_confidence_score", "confidence_score IS NULL OR (confidence_score >= 0 AND confidence_score <= 1)");
                    table.ForeignKey(
                        name: "FK_generation_result_generation_request_generation_request_id",
                        column: x => x.generation_request_id,
                        principalTable: "generation_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lore_entity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generation_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    ownership_type = table.Column<OwnershipType>(type: "ownership_type", nullable: false, defaultValueSql: "'master'"),
                    visibility = table.Column<VisibilityLevel>(type: "visibility_level", nullable: false, defaultValueSql: "'campaign'"),
                    is_template = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    attributes = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lore_entity", x => x.id);
                    table.ForeignKey(
                        name: "FK_lore_entity_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lore_entity_generation_request_generation_request_id",
                        column: x => x.generation_request_id,
                        principalTable: "generation_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lore_entity_user_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "generation_result_source",
                columns: table => new
                {
                    generation_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rag_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relevance_score = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    excerpt = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_result_source", x => new { x.generation_result_id, x.rag_source_id });
                    table.CheckConstraint("chk_relevance_score", "relevance_score IS NULL OR (relevance_score >= 0 AND relevance_score <= 1)");
                    table.ForeignKey(
                        name: "FK_generation_result_source_generation_result_generation_resul~",
                        column: x => x.generation_result_id,
                        principalTable: "generation_result",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_generation_result_source_rag_source_rag_source_id",
                        column: x => x.rag_source_id,
                        principalTable: "rag_source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lore_entity_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lore_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source_file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    extraction_result = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    field_mapping = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    processing_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lore_entity_import", x => x.id);
                    table.ForeignKey(
                        name: "FK_lore_entity_import_lore_entity_lore_entity_id",
                        column: x => x.lore_entity_id,
                        principalTable: "lore_entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lore_entity_import_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lore_entity_relationship",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lore_entity_relationship", x => x.id);
                    table.CheckConstraint("chk_no_self_relationship", "source_entity_id != target_entity_id");
                    table.ForeignKey(
                        name: "FK_lore_entity_relationship_lore_entity_source_entity_id",
                        column: x => x.source_entity_id,
                        principalTable: "lore_entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lore_entity_relationship_lore_entity_target_entity_id",
                        column: x => x.target_entity_id,
                        principalTable: "lore_entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_deleted_at",
                table: "campaign",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_game_system_id",
                table: "campaign",
                column: "game_system_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_is_active",
                table: "campaign",
                column: "is_active",
                filter: "deleted_at IS NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_join_code",
                table: "campaign",
                column: "join_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_owner_id",
                table: "campaign",
                column: "owner_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_settings",
                table: "campaign",
                column: "settings",
                filter: "settings IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_member_campaign_id",
                table: "campaign_member",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_member_role",
                table: "campaign_member",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_member_user_id",
                table: "campaign_member",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_campaign_member",
                table: "campaign_member",
                columns: new[] { "campaign_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_GameSystemId",
                table: "Documents",
                column: "GameSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OwnerId",
                table: "Documents",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ParentDocumentId",
                table: "Documents",
                column: "ParentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProjectId",
                table: "Documents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceType",
                table: "Documents",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "ix_entity_template_confirmed",
                table: "entity_template",
                columns: new[] { "game_system_id", "owner_id", "entity_type_name" },
                unique: true,
                filter: "status = 'Confirmed'");

            migrationBuilder.CreateIndex(
                name: "ix_entity_template_game_system_id",
                table: "entity_template",
                column: "game_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_entity_template_owner_id",
                table: "entity_template",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_template_source_document_id",
                table: "entity_template",
                column: "source_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_entity_template_status",
                table: "entity_template",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_game_system_is_active",
                table: "game_system",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "uq_game_system_code",
                table: "game_system",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_campaign_id",
                table: "generation_request",
                column: "campaign_id",
                filter: "campaign_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_generation_request_CampaignMemberId",
                table: "generation_request",
                column: "CampaignMemberId");

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_created_at",
                table: "generation_request",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_input_parameters",
                table: "generation_request",
                column: "input_parameters",
                filter: "input_parameters IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_request_type",
                table: "generation_request",
                column: "request_type");

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_status",
                table: "generation_request",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_target_entity_type",
                table: "generation_request",
                column: "target_entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_user_id",
                table: "generation_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_generation_result_model",
                table: "generation_result",
                column: "model_name",
                filter: "model_name IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_generation_result_request",
                table: "generation_result",
                column: "generation_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_generation_result_type",
                table: "generation_result",
                column: "result_type");

            migrationBuilder.CreateIndex(
                name: "ix_generation_result_source_rag",
                table: "generation_result_source",
                column: "rag_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_campaign_visibility",
                table: "lore_entity",
                columns: new[] { "campaign_id", "visibility" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_deleted_at",
                table: "lore_entity",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_generation",
                table: "lore_entity",
                column: "generation_request_id",
                filter: "generation_request_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_owner_id",
                table: "lore_entity",
                column: "owner_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_player_characters",
                table: "lore_entity",
                columns: new[] { "campaign_id", "owner_id" },
                filter: "entity_type = 'character' AND ownership_type = 'player' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_templates",
                table: "lore_entity",
                columns: new[] { "campaign_id", "entity_type" },
                filter: "is_template = true AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_import_entity_id",
                table: "lore_entity_import",
                column: "lore_entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_import_extraction_result",
                table: "lore_entity_import",
                column: "extraction_result",
                filter: "extraction_result IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_import_file_hash",
                table: "lore_entity_import",
                column: "file_hash",
                filter: "file_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_import_status",
                table: "lore_entity_import",
                column: "processing_status");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_import_type",
                table: "lore_entity_import",
                column: "import_type");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_import_user_id",
                table: "lore_entity_import",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_relationship_source",
                table: "lore_entity_relationship",
                column: "source_entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_relationship_target",
                table: "lore_entity_relationship",
                column: "target_entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_lore_entity_relationship_type",
                table: "lore_entity_relationship",
                column: "relationship_type");

            migrationBuilder.CreateIndex(
                name: "uq_lore_entity_relationship",
                table: "lore_entity_relationship",
                columns: new[] { "source_entity_id", "target_entity_id", "relationship_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_owner_id",
                table: "projects",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_owner_status",
                table: "projects",
                columns: new[] { "owner_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_projects_status",
                table: "projects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_rag_source_content_hash",
                table: "rag_source",
                column: "content_hash",
                filter: "content_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rag_source_game_system_id",
                table: "rag_source",
                column: "game_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_rag_source_is_active",
                table: "rag_source",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_rag_source_source_type",
                table: "rag_source",
                column: "source_type");

            migrationBuilder.CreateIndex(
                name: "ix_user_deleted_at",
                table: "user",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_email",
                table: "user",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_external_id",
                table: "user",
                column: "external_id",
                unique: true,
                filter: "external_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_invitation_code",
                table: "user",
                column: "invitation_code",
                unique: true,
                filter: "invitation_code IS NOT NULL AND deleted_at IS NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_user_is_active",
                table: "user",
                column: "is_active",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_master_id",
                table: "user",
                column: "master_id",
                filter: "master_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_role",
                table: "user",
                column: "role",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_template");

            migrationBuilder.DropTable(
                name: "generation_result_source");

            migrationBuilder.DropTable(
                name: "lore_entity_import");

            migrationBuilder.DropTable(
                name: "lore_entity_relationship");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "generation_result");

            migrationBuilder.DropTable(
                name: "rag_source");

            migrationBuilder.DropTable(
                name: "lore_entity");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "generation_request");

            migrationBuilder.DropTable(
                name: "campaign_member");

            migrationBuilder.DropTable(
                name: "campaign");

            migrationBuilder.DropTable(
                name: "game_system");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
