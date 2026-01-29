/**
 * Entity Types for WorldBuilder content
 * Aligned with backend LoreEntity and LoreEntityDto
 * Single Responsibility: Only entity-related type definitions
 */

/**
 * Entity category for gallery filtering
 * Maps to entityType field in backend
 */
export type EntityCategory = 
  | 'solar_system' 
  | 'character' 
  | 'npc' 
  | 'enemy' 
  | 'vehicle'
  | 'mission'
  | 'encounter';

/**
 * Visibility level for lore entities
 * Matches backend VisibilityLevel enum
 */
export enum VisibilityLevel {
  Draft = 0,
  Private = 1,
  Campaign = 2,
  Public = 3
}

/**
 * Ownership type for lore entities
 * Matches backend OwnershipType enum
 */
export enum OwnershipType {
  Master = 0,
  Player = 1,
  Shared = 2
}

/**
 * LoreEntity interface matching backend LoreEntityDto
 */
export interface LoreEntity {
  id: string;
  campaignId: string;
  ownerId: string;
  entityType: EntityCategory;
  name: string;
  description?: string;
  ownershipType: OwnershipType;
  visibility: VisibilityLevel;
  isTemplate: boolean;
  imageUrl?: string;
  attributes?: Record<string, unknown>;
  metadata?: Record<string, unknown>;
  createdAt: string;
  updatedAt?: string;
}

/**
 * DTO for creating a new lore entity
 * Matches backend CreateLoreEntityRequest
 */
export interface CreateLoreEntityInput {
  entityType: EntityCategory;
  name: string;
  description?: string;
  ownershipType?: OwnershipType;
  visibility?: VisibilityLevel;
  isTemplate?: boolean;
  imageUrl?: string;
  attributes?: Record<string, unknown>;
  metadata?: Record<string, unknown>;
}

/**
 * DTO for updating a lore entity
 * Matches backend UpdateLoreEntityRequest
 */
export interface UpdateLoreEntityInput {
  name: string;
  description?: string;
  visibility?: VisibilityLevel;
  imageUrl?: string;
  attributes?: Record<string, unknown>;
  metadata?: Record<string, unknown>;
}

/**
 * Request to change entity visibility
 */
export interface ChangeVisibilityInput {
  visibility: VisibilityLevel;
}

// ============================================
// Generator-specific data types (stored in attributes)
// ============================================

/**
 * Character data structure
 * Used for player characters
 */
export interface CharacterData {
  name: string;
  bio: string;
  stats: {
    STR: number;
    INT: number;
    DEX: number;
  };
}

/**
 * Solar system data structure
 */
export interface SystemData {
  name: string;
  description: string;
  planets: string[];
}

/**
 * Vehicle data structure
 */
export interface VehicleData {
  name: string;
  specs: string;
  stats: {
    SPEED: number;
    ARMOR: number;
    CARGO: number;
  };
}

/**
 * NPC (Non-Player Character) data structure
 * Used for humanoid or friendly NPCs in campaigns
 */
export interface NpcData {
  name: string;
  occupation: string;
  personality: string;
  background: string;
  stats: {
    CHA: number;  // Charisma - social influence
    INT: number;  // Intelligence
    WIS: number;  // Wisdom - perception, insight
  };
}

/**
 * Enemy data structure
 * Used for hostile creatures, aliens, or antagonists
 */
export interface EnemyData {
  name: string;
  species: string;
  threatLevel: string;
  abilities: string;
  weakness: string;
  stats: {
    HP: number;   // Health Points
    ATK: number;  // Attack power
    DEF: number;  // Defense
    SPD: number;  // Speed/Initiative
  };
}

/**
 * Mission data structure
 * Used for quests, objectives, and campaign missions
 */
export interface MissionData {
  name: string;
  briefing: string;
  objective: string;
  rewards: string;
  difficulty: 'EASY' | 'MEDIUM' | 'HARD' | 'EXTREME';
  estimatedDuration: string;
}

/**
 * Encounter data structure
 * Used for combat encounters or random events
 */
export interface EncounterData {
  name: string;
  description: string;
  environment: string;
  participants: string[];
  difficulty: 'EASY' | 'MEDIUM' | 'HARD' | 'EXTREME';
  loot: string;
}

// ============================================
// Legacy aliases for backwards compatibility
// TODO: Remove after migration is complete
// ============================================

/** @deprecated Use LoreEntity instead */
export type Entity = LoreEntity;

/** @deprecated Use LoreEntity instead */
export type EntityDto = LoreEntity;

/** @deprecated Use CreateLoreEntityInput instead */
export type CreateEntityInput = CreateLoreEntityInput;

/** @deprecated Use UpdateLoreEntityInput instead */
export type UpdateEntityInput = UpdateLoreEntityInput & { id: string };
