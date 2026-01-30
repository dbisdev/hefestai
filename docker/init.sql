-- =============================================================================
-- LOREMASTER DATABASE SCHEMA v3
-- Tabletop RPG Lore Management with AI Generation
-- =============================================================================
-- Changes from v2:
--   1. User table now supports password-based authentication
--   2. Added refresh_token, refresh_token_expiry_time for JWT refresh
--   3. Added is_active, last_login_at for session management
--   4. Added invitation_code for Masters to invite Players
--   5. Added master_id for legacy Master-Player relationship
--   6. external_id is now optional (for future OAuth)
-- =============================================================================

-- Initialize database extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";      -- pgvector for semantic search/RAG
CREATE EXTENSION IF NOT EXISTS "pg_trgm";     -- trigram for fuzzy text search

-- Grants
GRANT ALL PRIVILEGES ON DATABASE loremaster TO postgres;

-- =============================================================================
-- ENUM TYPES
-- =============================================================================

CREATE TYPE user_role AS ENUM ('admin', 'master', 'player');
CREATE TYPE campaign_role AS ENUM ('master', 'player');
CREATE TYPE ownership_type AS ENUM ('master', 'player', 'shared');
CREATE TYPE visibility_level AS ENUM ('draft', 'private', 'campaign', 'public');
CREATE TYPE generation_request_type AS ENUM ('rag_dice_roll', 'ai_narrative', 'pdf_import', 'ocr_import');
CREATE TYPE generation_status AS ENUM ('pending', 'processing', 'completed', 'failed');
CREATE TYPE import_type AS ENUM ('pdf', 'ocr');
CREATE TYPE rag_source_type AS ENUM ('rulebook', 'supplement', 'custom');

-- =============================================================================
-- TABLE: user
-- User accounts with role-based access and password authentication
-- =============================================================================

CREATE TABLE "user" (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    role user_role NOT NULL DEFAULT 'player',
    avatar_url TEXT,
    
    -- External authentication support (for future OAuth integration)
    external_id VARCHAR(255),
    
    -- Password-based authentication fields
    refresh_token VARCHAR(500),
    refresh_token_expiry_time TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_login_at TIMESTAMPTZ,
    
    -- Master-Player relationship (legacy)
    invitation_code VARCHAR(20),
    master_id UUID,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    
    CONSTRAINT uq_user_email UNIQUE (email),
    CONSTRAINT uq_user_external_id UNIQUE (external_id),
    CONSTRAINT uq_user_invitation_code UNIQUE (invitation_code),
    CONSTRAINT fk_user_master FOREIGN KEY (master_id) 
        REFERENCES "user"(id) ON DELETE SET NULL
);

COMMENT ON COLUMN "user".external_id IS 'Optional external OAuth provider ID (Google, GitHub, etc.)';
COMMENT ON COLUMN "user".invitation_code IS 'Auto-generated code for Masters to invite Players';
COMMENT ON COLUMN "user".master_id IS 'Reference to the Master user who invited this Player';

-- Auth lookups
CREATE INDEX ix_user_email ON "user"(email) WHERE deleted_at IS NULL;
CREATE INDEX ix_user_external_id ON "user"(external_id) WHERE external_id IS NOT NULL AND deleted_at IS NULL;
CREATE INDEX ix_user_role ON "user"(role) WHERE deleted_at IS NULL;
CREATE INDEX ix_user_deleted_at ON "user"(deleted_at) WHERE deleted_at IS NOT NULL;
-- Invitation code lookup (for joining as a Player)
CREATE INDEX ix_user_invitation_code ON "user"(invitation_code) WHERE invitation_code IS NOT NULL AND deleted_at IS NULL AND is_active = true;
-- Master's players lookup
CREATE INDEX ix_user_master_id ON "user"(master_id) WHERE master_id IS NOT NULL AND deleted_at IS NULL;
-- Active users
CREATE INDEX ix_user_is_active ON "user"(is_active) WHERE deleted_at IS NULL;

-- =============================================================================
-- TABLE: game_system
-- Supported tabletop RPG systems
-- =============================================================================

CREATE TABLE game_system (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL,
    name VARCHAR(100) NOT NULL,
    publisher VARCHAR(100),
    version VARCHAR(50),
    description TEXT,
    supported_entity_types VARCHAR(50)[] NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT uq_game_system_code UNIQUE (code)
);

CREATE INDEX ix_game_system_code ON game_system(code);
CREATE INDEX ix_game_system_is_active ON game_system(is_active) WHERE is_active = true;

-- =============================================================================
-- TABLE: campaign
-- Game campaigns owned by Masters
-- =============================================================================

CREATE TABLE campaign (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID NOT NULL,
    game_system_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    join_code VARCHAR(20) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    settings JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    
    CONSTRAINT fk_campaign_owner FOREIGN KEY (owner_id) 
        REFERENCES "user"(id) ON DELETE RESTRICT,
    CONSTRAINT fk_campaign_game_system FOREIGN KEY (game_system_id) 
        REFERENCES game_system(id) ON DELETE RESTRICT,
    CONSTRAINT uq_campaign_join_code UNIQUE (join_code)
);

COMMENT ON COLUMN campaign.settings IS 'Game-system-specific settings (house rules, era, etc.). JSONB justified: structure varies by game system.';

-- Join by code (primary use case)
CREATE INDEX ix_campaign_join_code ON campaign(join_code) WHERE deleted_at IS NULL;
-- Owner's campaigns
CREATE INDEX ix_campaign_owner_id ON campaign(owner_id) WHERE deleted_at IS NULL;
-- Filter by game system
CREATE INDEX ix_campaign_game_system_id ON campaign(game_system_id) WHERE deleted_at IS NULL;
-- Active campaigns only
CREATE INDEX ix_campaign_is_active ON campaign(is_active) WHERE deleted_at IS NULL AND is_active = true;
-- Soft delete filter
CREATE INDEX ix_campaign_deleted_at ON campaign(deleted_at) WHERE deleted_at IS NOT NULL;
-- Settings queries (rare, but useful)
CREATE INDEX ix_campaign_settings ON campaign USING GIN (settings jsonb_path_ops) WHERE settings IS NOT NULL;

-- =============================================================================
-- TABLE: campaign_member
-- User participation in campaigns
-- =============================================================================

CREATE TABLE campaign_member (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campaign_id UUID NOT NULL,
    user_id UUID NOT NULL,
    role campaign_role NOT NULL DEFAULT 'player',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_campaign_member_campaign FOREIGN KEY (campaign_id) 
        REFERENCES campaign(id) ON DELETE CASCADE,
    CONSTRAINT fk_campaign_member_user FOREIGN KEY (user_id) 
        REFERENCES "user"(id) ON DELETE CASCADE,
    CONSTRAINT uq_campaign_member UNIQUE (campaign_id, user_id)
);

-- User's memberships (my campaigns)
CREATE INDEX ix_campaign_member_user_id ON campaign_member(user_id);
-- Campaign's members (who's in this campaign)
CREATE INDEX ix_campaign_member_campaign_id ON campaign_member(campaign_id);
-- Filter by role
CREATE INDEX ix_campaign_member_role ON campaign_member(role);

-- =============================================================================
-- TABLE: generation_request
-- AI/RAG generation requests for traceability
-- =============================================================================

CREATE TABLE generation_request (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    campaign_id UUID,
    request_type generation_request_type NOT NULL,
    target_entity_type VARCHAR(50) NOT NULL,
    status generation_status NOT NULL DEFAULT 'pending',
    input_prompt TEXT,
    input_parameters JSONB,
    error_message TEXT,
    processing_started_at TIMESTAMPTZ,
    processing_completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_generation_request_user FOREIGN KEY (user_id) 
        REFERENCES "user"(id) ON DELETE CASCADE,
    CONSTRAINT fk_generation_request_campaign FOREIGN KEY (campaign_id) 
        REFERENCES campaign(id) ON DELETE SET NULL
);

COMMENT ON COLUMN generation_request.input_parameters IS 'Structured input (dice configs, table refs, OCR hints). JSONB justified: structure varies by request_type.';

-- User's generation history
CREATE INDEX ix_generation_request_user_created ON generation_request(user_id, created_at DESC);
-- Campaign context
CREATE INDEX ix_generation_request_campaign_id ON generation_request(campaign_id) WHERE campaign_id IS NOT NULL;
-- Processing queue (pending/processing jobs)
CREATE INDEX ix_generation_request_status_queue ON generation_request(status, created_at) 
    WHERE status IN ('pending', 'processing');
-- Filter by type
CREATE INDEX ix_generation_request_type ON generation_request(request_type);
-- Filter by target
CREATE INDEX ix_generation_request_target ON generation_request(target_entity_type);
-- Input parameters queries
CREATE INDEX ix_generation_request_input_params ON generation_request USING GIN (input_parameters jsonb_path_ops) 
    WHERE input_parameters IS NOT NULL;

-- =============================================================================
-- TABLE: lore_entity
-- Polymorphic base for all lore content (characters, NPCs, locations, etc.)
-- =============================================================================
-- 
-- OWNERSHIP MODEL:
--   ownership_type = 'master'  -> Master-created content, master has full control
--   ownership_type = 'player'  -> Player's character, player has full control
--   ownership_type = 'shared'  -> Collaborative content, campaign members can edit
--
-- VISIBILITY MODEL:
--   visibility = 'draft'    -> Only owner can see (work in progress)
--   visibility = 'private'  -> Owner + campaign master can see
--   visibility = 'campaign' -> All campaign members can see
--   visibility = 'public'   -> Anyone can see (future: public gallery)
--
-- ACCESS CONTROL (enforced in application):
--   CanRead  = owner OR (visibility >= 'campaign' AND member) OR visibility = 'public'
--   CanWrite = owner OR (ownership_type != 'player' AND is_master)
-- =============================================================================

CREATE TABLE lore_entity (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campaign_id UUID NOT NULL,
    owner_id UUID NOT NULL,
    generation_request_id UUID,
    entity_type VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    ownership_type ownership_type NOT NULL DEFAULT 'master',
    visibility visibility_level NOT NULL DEFAULT 'campaign',
    is_template BOOLEAN NOT NULL DEFAULT false,
    image_url TEXT,
    attributes JSONB,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    
    CONSTRAINT fk_lore_entity_campaign FOREIGN KEY (campaign_id) 
        REFERENCES campaign(id) ON DELETE CASCADE,
    CONSTRAINT fk_lore_entity_owner FOREIGN KEY (owner_id) 
        REFERENCES "user"(id) ON DELETE RESTRICT,
    CONSTRAINT fk_lore_entity_generation_request FOREIGN KEY (generation_request_id) 
        REFERENCES generation_request(id) ON DELETE SET NULL
);

COMMENT ON COLUMN lore_entity.ownership_type IS 'Who controls this entity: master (GM content), player (PC), shared (collaborative)';
COMMENT ON COLUMN lore_entity.visibility IS 'Who can see: draft (owner only), private (owner+master), campaign (members), public (all)';
COMMENT ON COLUMN lore_entity.attributes IS 'Game-system-specific mechanical attributes. JSONB justified: structure varies by game system AND entity type.';
COMMENT ON COLUMN lore_entity.metadata IS 'Non-mechanical flexible data (tags, notes). JSONB justified: user-defined custom fields.';

-- PRIMARY QUERY: List entities in campaign by type
CREATE INDEX ix_lore_entity_campaign_type ON lore_entity(campaign_id, entity_type) 
    WHERE deleted_at IS NULL;

-- ACCESS CONTROL: Filter by visibility within campaign
CREATE INDEX ix_lore_entity_campaign_visibility ON lore_entity(campaign_id, visibility) 
    WHERE deleted_at IS NULL;

-- MY ENTITIES: Owner's entities across campaigns
CREATE INDEX ix_lore_entity_owner_id ON lore_entity(owner_id) 
    WHERE deleted_at IS NULL;

-- PLAYER CHARACTERS: Quick lookup for player-owned characters
CREATE INDEX ix_lore_entity_player_characters ON lore_entity(campaign_id, owner_id) 
    WHERE entity_type = 'character' AND ownership_type = 'player' AND deleted_at IS NULL;

-- TEMPLATES: Find reusable templates
CREATE INDEX ix_lore_entity_templates ON lore_entity(campaign_id, entity_type) 
    WHERE is_template = true AND deleted_at IS NULL;

-- GENERATION TRACKING: Entities from AI generation
CREATE INDEX ix_lore_entity_generation ON lore_entity(generation_request_id) 
    WHERE generation_request_id IS NOT NULL AND deleted_at IS NULL;

-- NAME SEARCH: Fuzzy/partial match using trigrams
CREATE INDEX ix_lore_entity_name_trgm ON lore_entity USING GIN (name gin_trgm_ops) 
    WHERE deleted_at IS NULL;

-- FULL-TEXT SEARCH: Search name and description
CREATE INDEX ix_lore_entity_fts ON lore_entity USING GIN (
    to_tsvector('english', coalesce(name, '') || ' ' || coalesce(description, ''))
) WHERE deleted_at IS NULL;

-- ATTRIBUTE QUERIES: Query JSONB attributes (e.g., find characters with Force > 3)
CREATE INDEX ix_lore_entity_attributes ON lore_entity USING GIN (attributes jsonb_path_ops) 
    WHERE attributes IS NOT NULL AND deleted_at IS NULL;

-- METADATA QUERIES: Query JSONB metadata (e.g., find by tag)
CREATE INDEX ix_lore_entity_metadata ON lore_entity USING GIN (metadata jsonb_path_ops) 
    WHERE metadata IS NOT NULL AND deleted_at IS NULL;

-- SOFT DELETE: Filter deleted entities
CREATE INDEX ix_lore_entity_deleted_at ON lore_entity(deleted_at) 
    WHERE deleted_at IS NOT NULL;

-- =============================================================================
-- TABLE: lore_entity_relationship
-- Relationships between lore entities
-- =============================================================================

CREATE TABLE lore_entity_relationship (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_entity_id UUID NOT NULL,
    target_entity_id UUID NOT NULL,
    relationship_type VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_ler_source FOREIGN KEY (source_entity_id) 
        REFERENCES lore_entity(id) ON DELETE CASCADE,
    CONSTRAINT fk_ler_target FOREIGN KEY (target_entity_id) 
        REFERENCES lore_entity(id) ON DELETE CASCADE,
    CONSTRAINT uq_lore_entity_relationship UNIQUE (source_entity_id, target_entity_id, relationship_type),
    CONSTRAINT chk_no_self_relationship CHECK (source_entity_id != target_entity_id)
);

-- Graph traversal: outgoing relationships
CREATE INDEX ix_ler_source ON lore_entity_relationship(source_entity_id);
-- Graph traversal: incoming relationships
CREATE INDEX ix_ler_target ON lore_entity_relationship(target_entity_id);
-- Filter by relationship type
CREATE INDEX ix_ler_type ON lore_entity_relationship(relationship_type);

-- =============================================================================
-- TABLE: rag_source
-- RAG document sources for generation
-- =============================================================================

CREATE TABLE rag_source (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_system_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    source_type rag_source_type NOT NULL,
    version VARCHAR(50),
    content_hash VARCHAR(64),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_rag_source_game_system FOREIGN KEY (game_system_id) 
        REFERENCES game_system(id) ON DELETE CASCADE
);

-- Active sources by game system (main query)
CREATE INDEX ix_rag_source_game_system_active ON rag_source(game_system_id) 
    WHERE is_active = true;
-- Filter by source type
CREATE INDEX ix_rag_source_type ON rag_source(source_type);
-- Detect content changes
CREATE INDEX ix_rag_source_hash ON rag_source(content_hash) WHERE content_hash IS NOT NULL;

-- =============================================================================
-- TABLE: generation_result
-- AI/RAG generation outputs
-- =============================================================================

CREATE TABLE generation_result (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    generation_request_id UUID NOT NULL,
    result_type VARCHAR(50) NOT NULL,
    sequence_order INTEGER NOT NULL DEFAULT 1,
    raw_output JSONB,
    structured_output JSONB,
    model_name VARCHAR(100),
    model_parameters JSONB,
    token_usage JSONB,
    confidence_score DECIMAL(5,4),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_generation_result_request FOREIGN KEY (generation_request_id) 
        REFERENCES generation_request(id) ON DELETE CASCADE,
    CONSTRAINT chk_confidence_score CHECK (confidence_score IS NULL OR (confidence_score >= 0 AND confidence_score <= 1))
);

COMMENT ON COLUMN generation_result.raw_output IS 'Raw AI/RAG output. JSONB justified: AI models return structured JSON (tool calls, reasoning chains).';
COMMENT ON COLUMN generation_result.structured_output IS 'Parsed/normalized output. JSONB justified: format depends on AI model and target entity type.';
COMMENT ON COLUMN generation_result.model_parameters IS 'AI config (temperature, tokens). JSONB justified: varies by model provider.';
COMMENT ON COLUMN generation_result.token_usage IS 'Token metrics. JSONB justified: format varies by provider (OpenAI vs Google vs Anthropic).';

-- Results for a request
CREATE INDEX ix_generation_result_request ON generation_result(generation_request_id);
-- Filter by result type
CREATE INDEX ix_generation_result_type ON generation_result(result_type);
-- Analytics: by model
CREATE INDEX ix_generation_result_model ON generation_result(model_name) WHERE model_name IS NOT NULL;
-- Query raw output
CREATE INDEX ix_generation_result_raw ON generation_result USING GIN (raw_output jsonb_path_ops) 
    WHERE raw_output IS NOT NULL;
-- Query structured output
CREATE INDEX ix_generation_result_structured ON generation_result USING GIN (structured_output jsonb_path_ops) 
    WHERE structured_output IS NOT NULL;

-- =============================================================================
-- TABLE: generation_result_source
-- Links generation results to RAG sources used (traceability)
-- =============================================================================

CREATE TABLE generation_result_source (
    generation_result_id UUID NOT NULL,
    rag_source_id UUID NOT NULL,
    relevance_score DECIMAL(5,4),
    excerpt TEXT,
    
    CONSTRAINT pk_generation_result_source PRIMARY KEY (generation_result_id, rag_source_id),
    CONSTRAINT fk_grs_result FOREIGN KEY (generation_result_id) 
        REFERENCES generation_result(id) ON DELETE CASCADE,
    CONSTRAINT fk_grs_rag FOREIGN KEY (rag_source_id) 
        REFERENCES rag_source(id) ON DELETE CASCADE,
    CONSTRAINT chk_relevance_score CHECK (relevance_score IS NULL OR (relevance_score >= 0 AND relevance_score <= 1))
);

-- Find results that used a specific source
CREATE INDEX ix_grs_rag_source ON generation_result_source(rag_source_id);

-- =============================================================================
-- TABLE: lore_entity_import
-- PDF/OCR import history
-- =============================================================================

CREATE TABLE lore_entity_import (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lore_entity_id UUID NOT NULL,
    user_id UUID NOT NULL,
    import_type import_type NOT NULL,
    source_filename VARCHAR(255) NOT NULL,
    source_file_url VARCHAR(500),
    file_hash VARCHAR(64),
    extraction_result JSONB,
    field_mapping JSONB,
    processing_status generation_status NOT NULL DEFAULT 'pending',
    error_details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_lei_entity FOREIGN KEY (lore_entity_id) 
        REFERENCES lore_entity(id) ON DELETE CASCADE,
    CONSTRAINT fk_lei_user FOREIGN KEY (user_id) 
        REFERENCES "user"(id) ON DELETE CASCADE
);

COMMENT ON COLUMN lore_entity_import.extraction_result IS 'Raw OCR/PDF extraction. JSONB justified: structure depends on source document format.';
COMMENT ON COLUMN lore_entity_import.field_mapping IS 'Field-to-attribute mapping. JSONB justified: user-defined variable mapping.';

-- Imports for an entity
CREATE INDEX ix_lei_entity ON lore_entity_import(lore_entity_id);
-- User's imports
CREATE INDEX ix_lei_user ON lore_entity_import(user_id);
-- Processing queue
CREATE INDEX ix_lei_status_queue ON lore_entity_import(processing_status, created_at) 
    WHERE processing_status IN ('pending', 'processing');
-- Detect duplicate files
CREATE INDEX ix_lei_file_hash ON lore_entity_import(file_hash) WHERE file_hash IS NOT NULL;
-- Query extraction results
CREATE INDEX ix_lei_extraction ON lore_entity_import USING GIN (extraction_result jsonb_path_ops) 
    WHERE extraction_result IS NOT NULL;

-- =============================================================================
-- TRIGGER FUNCTIONS
-- =============================================================================

-- Function to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply updated_at triggers to all tables with updated_at
CREATE TRIGGER tr_user_updated_at
    BEFORE UPDATE ON "user"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_game_system_updated_at
    BEFORE UPDATE ON game_system
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_campaign_updated_at
    BEFORE UPDATE ON campaign
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_campaign_member_updated_at
    BEFORE UPDATE ON campaign_member
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_generation_request_updated_at
    BEFORE UPDATE ON generation_request
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_lore_entity_updated_at
    BEFORE UPDATE ON lore_entity
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_rag_source_updated_at
    BEFORE UPDATE ON rag_source
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_lore_entity_import_updated_at
    BEFORE UPDATE ON lore_entity_import
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- SEED DATA: Game Systems
-- =============================================================================

INSERT INTO game_system (id, code, name, publisher, supported_entity_types, is_active) VALUES
    (gen_random_uuid(), 'star_wars_d6', 'Star Wars D6', 'West End Games', 
     ARRAY['character', 'npc_humanoid', 'npc_alien', 'npc_droid', 'starship', 'vehicle', 'planet', 'star_system', 'encounter', 'mission', 'location', 'organization', 'item'], true),
    (gen_random_uuid(), 'dnd_5e', 'Dungeons & Dragons 5th Edition', 'Wizards of the Coast', 
     ARRAY['character', 'npc', 'monster', 'location', 'item', 'spell', 'encounter', 'quest', 'organization', 'settlement'], true),
    (gen_random_uuid(), 'call_of_cthulhu_7e', 'Call of Cthulhu 7th Edition', 'Chaosium', 
     ARRAY['character', 'npc', 'creature', 'location', 'item', 'tome', 'spell', 'scenario', 'organization'], true),
    (gen_random_uuid(), 'pathfinder_2e', 'Pathfinder 2nd Edition', 'Paizo', 
     ARRAY['character', 'npc', 'monster', 'location', 'item', 'spell', 'encounter', 'quest', 'organization', 'settlement', 'hazard'], true)
ON CONFLICT DO NOTHING;

-- =============================================================================
-- EF Core migrations history table
-- =============================================================================

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) NOT NULL PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

-- Mark migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250129000002_UserPasswordAuth', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
