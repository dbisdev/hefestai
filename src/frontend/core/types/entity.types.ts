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
  /** Display name of the entity owner */
  ownerName?: string;
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
  /**
   * Optional ID of the generation request that created this entity's content.
   * Links the saved entity to its AI generation history for traceability.
   */
  generationRequestId?: string;
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

/**
 * Request to transfer entity ownership
 * Matches backend TransferOwnershipRequest
 */
export interface TransferOwnershipInput {
  /** The ID of the new owner (must be a campaign member) */
  newOwnerId: string;
  /** Optional ownership type for the transfer */
  ownershipType?: OwnershipType;
}

// ============================================
// Generator-specific data types (stored in attributes)
// ============================================

/**
 * Dynamic stats object from AI generation.
 * Stats vary by game system, so we use a flexible Record type.
 * Values can be numbers, strings, or nested objects (like SKILLS).
 */
export type DynamicStats = Record<string, unknown>;

/**
 * Character data structure
 * Used for player characters
 */
export interface CharacterData {
  name: string;
  bio: string;
  /** Dynamic stats that vary by game system */
  stats: DynamicStats;
  morphology?: string;
  role?: string;
  style?: string;
}

/**
 * Planet data structure for solar systems
 * All fields optional to handle variable AI responses
 */
export interface PlanetData {
  /** Orbital position from the star (1 = closest) */
  orbital_position?: number;
  /** Planet type (Terrestrial, Gas Giant, Ice Planet, etc.) */
  type?: string;
  /** Planet name (can be null if unnamed) */
  name?: string | null;
  /** Planet diameter in km */
  size?: number;
  /** Surface gravity relative to Earth (1.0 = Earth gravity) */
  gravity?: number;
  /** Atmospheric composition */
  atmosphere?: string;
  /** Surface temperature classification */
  temperature?: string;
  /** Notable geological/environmental features */
  features?: string;
  /** Available natural resources */
  resources?: string;
}

/**
 * Solar system stats structure
 */
export interface SystemStats {
  /** Stellar classification (e.g., "GV", "M", "O") */
  star_type?: string;
  /** Array of planets in the system */
  planets?: PlanetData[];
}

/**
 * Solar system data structure
 */
export interface SystemData {
  /** System name */
  name: string;
  /** System description/lore */
  description: string;
  /** System statistics and planet data */
  stats?: SystemStats;
}

/**
 * Vehicle data structure
 */
export interface VehicleData {
  name: string;
  specs: string;
  /** Dynamic stats that vary by game system */
  stats: DynamicStats;
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
  /** Dynamic stats that vary by game system */
  stats: DynamicStats;
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
  /** Dynamic stats that vary by game system */
  stats: DynamicStats;
}

/**
 * Mission stats structure
 * Contains nested mission attributes from AI generation
 * All fields optional to handle variable AI responses
 */
export interface MissionStats {
  /** Primary mission objective */
  objective?: string;
  /** Mission rewards (credits, items, reputation, etc.) */
  rewards?: string;
  /** Difficulty level */
  difficulty?: string;
  /** Estimated time to complete */
  estimatedDuration?: string;
}

/**
 * Mission data structure
 * Used for quests, objectives, and campaign missions
 * Matches AI response format: { name, description, stats: { ... } }
 */
export interface MissionData {
  /** Mission codename/title */
  name: string;
  /** Mission briefing/description (AI returns 'description', not 'briefing') */
  description?: string;
  /** Nested stats object containing mission details */
  stats?: MissionStats;
}

/**
 * Encounter participant structure
 * AI may return participants as objects with details
 */
export interface EncounterParticipant {
  /** Enemy/NPC type (e.g., "Xenomorph Drone") */
  type?: string;
  /** Number of this participant type */
  count?: number;
  /** Additional notes about this participant */
  notes?: string;
}

/**
 * Encounter stats structure
 * Contains nested encounter attributes from AI generation
 * All fields optional to handle variable AI responses
 */
export interface EncounterStats {
  /** Combat environment description */
  environment?: string;
  /** List of enemies/NPCs in the encounter - can be strings or objects */
  participants?: (string | EncounterParticipant)[];
  /** Difficulty level */
  difficulty?: string;
  /** Potential loot/rewards from the encounter */
  loot?: string;
}

/**
 * Encounter data structure
 * Used for combat encounters or random events
 * Matches AI response format: { name, description, stats: { ... } }
 */
export interface EncounterData {
  /** Encounter name/title */
  name: string;
  /** Encounter narrative description */
  description?: string;
  /** Nested stats object containing encounter details */
  stats?: EncounterStats;
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
