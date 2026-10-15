/**
 * API Types and Response interfaces
 * Single Responsibility: Only API-related type definitions
 */

export interface ApiError {
  message: string;
  code?: string;
  errors?: Record<string, string[]>;
  status?: number;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

// AI Generation Request/Response types

/**
 * Common parameters shared by every AI generation request.
 * Each entity-specific Params interface extends this base.
 */
export interface GenerationParamsBase {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Common fields returned by every AI generation response.
 * Each entity-specific Response interface extends this base.
 */
export interface GenerationResponseBase {
  success: boolean;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
  /** Whether RAG context from game manuals was used for generation */
  ragContextUsed?: boolean;
  /** Number of RAG context chunks used (0 if none) */
  ragSourceCount?: number;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

/**
 * Parameters for character generation with optional RAG enhancement.
 */
export interface CharacterGenerationParams extends GenerationParamsBase {
  species: string;
  role: string;
  morphology: string;
  attire: string;
}

/**
 * Response from character generation including RAG metadata.
 */
export interface CharacterGenerationResponse extends GenerationResponseBase {
  characterJson?: string;
}

/**
 * Parameters for solar system generation with optional RAG enhancement.
 */
export interface SolarSystemGenerationParams extends GenerationParamsBase {
  spectralClass: string;
  planetCount: number;
}

/**
 * Response from solar system generation including RAG metadata.
 */
export interface SolarSystemGenerationResponse extends GenerationResponseBase {
  systemJson?: string;
}

/**
 * Parameters for vehicle generation with optional RAG enhancement.
 */
export interface VehicleGenerationParams extends GenerationParamsBase {
  type: string;
  class: string;
  engine: string;
}

/**
 * Response from vehicle generation including RAG metadata.
 */
export interface VehicleGenerationResponse extends GenerationResponseBase {
  vehicleJson?: string;
}

/**
 * Parameters for NPC generation with optional RAG enhancement.
 */
export interface NpcGenerationParams extends GenerationParamsBase {
  species: string;
  occupation: string;
  personality: string;
  setting: string;
}

/**
 * Response from NPC generation including RAG metadata.
 */
export interface NpcGenerationResponse extends GenerationResponseBase {
  npcJson?: string;
}

/**
 * Parameters for enemy generation with optional RAG enhancement.
 */
export interface EnemyGenerationParams extends GenerationParamsBase {
  species: string;
  threatLevel: string;
  behavior: string;
  environment: string;
}

/**
 * Response from enemy generation including RAG metadata.
 */
export interface EnemyGenerationResponse extends GenerationResponseBase {
  enemyJson?: string;
}

/**
 * Parameters for mission generation with optional RAG enhancement.
 */
export interface MissionGenerationParams extends GenerationParamsBase {
  missionType: string;
  difficulty: string;
  environment: string;
  factionInvolved: string;
}

/**
 * Response from mission generation including RAG metadata.
 */
export interface MissionGenerationResponse extends GenerationResponseBase {
  missionJson?: string;
}

/**
 * Valid enemy count categories for encounter generation.
 * Must match the C# EnemyCountSize enum values (case-insensitive via JsonStringEnumConverter).
 */
export type EnemyCountSize = 'solo' | 'pair' | 'squad' | 'horde';

/**
 * Parameters for encounter generation with optional RAG enhancement.
 */
export interface EncounterGenerationParams extends GenerationParamsBase {
  encounterType: string;
  difficulty: string;
  environment: string;
  /** Typed enemy count category. Must be one of the EnemyCountSize values. */
  enemyCount: EnemyCountSize;
}

/**
 * Response from encounter generation including RAG metadata.
 */
export interface EncounterGenerationResponse extends GenerationResponseBase {
  encounterJson?: string;
}

// HTTP client types
export interface RequestConfig extends RequestInit {
  skipAuth?: boolean;
  timeout?: number;
}

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
