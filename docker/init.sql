-- Initialize database extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";  -- pgvector for semantic search

-- Grants
GRANT ALL PRIVILEGES ON DATABASE loremaster TO postgres;

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(256) NOT NULL,
    password_hash VARCHAR(512) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'User',
    display_name VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    refresh_token VARCHAR(512),
    refresh_token_expiry_time TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),
    updated_at TIMESTAMP WITH TIME ZONE,
    updated_by VARCHAR(256)
);

-- Indexes
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON users(email);
CREATE INDEX IF NOT EXISTS ix_users_role ON users(role);
CREATE INDEX IF NOT EXISTS ix_users_is_active ON users(is_active);
CREATE INDEX IF NOT EXISTS ix_users_refresh_token ON users(refresh_token) WHERE refresh_token IS NOT NULL;

-- EF Core migrations history table
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) NOT NULL PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

-- Mark initial migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241216000000_InitialCreate', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
