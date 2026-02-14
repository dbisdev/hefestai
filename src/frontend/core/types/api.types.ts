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
 * Parameters for character generation with optional RAG enhancement.
 */
export interface CharacterGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  species: string;
  role: string;
  morphology: string;
  attire: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from character generation including RAG metadata.
 */
export interface CharacterGenerationResponse {
  success: boolean;
  characterJson?: string;
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
 * Parameters for solar system generation with optional RAG enhancement.
 */
export interface SolarSystemGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  spectralClass: string;
  planetCount: number;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from solar system generation including RAG metadata.
 */
export interface SolarSystemGenerationResponse {
  success: boolean;
  systemJson?: string;
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
 * Parameters for vehicle generation with optional RAG enhancement.
 */
export interface VehicleGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  type: string;
  class: string;
  engine: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from vehicle generation including RAG metadata.
 */
export interface VehicleGenerationResponse {
  success: boolean;
  vehicleJson?: string;
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
 * Parameters for NPC generation with optional RAG enhancement.
 */
export interface NpcGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  species: string;
  occupation: string;
  personality: string;
  setting: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from NPC generation including RAG metadata.
 */
export interface NpcGenerationResponse {
  success: boolean;
  npcJson?: string;
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
 * Parameters for enemy generation with optional RAG enhancement.
 */
export interface EnemyGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  species: string;
  threatLevel: string;
  behavior: string;
  environment: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from enemy generation including RAG metadata.
 */
export interface EnemyGenerationResponse {
  success: boolean;
  enemyJson?: string;
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
 * Parameters for mission generation with optional RAG enhancement.
 */
export interface MissionGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  missionType: string;
  difficulty: string;
  environment: string;
  factionInvolved: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from mission generation including RAG metadata.
 */
export interface MissionGenerationResponse {
  success: boolean;
  missionJson?: string;
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
 * Parameters for encounter generation with optional RAG enhancement.
 */
export interface EncounterGenerationParams {
  /** Optional game system ID for RAG-enhanced generation using manual lore */
  gameSystemId?: string;
  encounterType: string;
  difficulty: string;
  environment: string;
  enemyCount: string;
  /** Whether to generate an AI image for this entity */
  generateImage?: boolean;
}

/**
 * Response from encounter generation including RAG metadata.
 */
export interface EncounterGenerationResponse {
  success: boolean;
  encounterJson?: string;
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

// HTTP client types
export interface RequestConfig extends RequestInit {
  skipAuth?: boolean;
  timeout?: number;
}

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
