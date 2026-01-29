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
export interface CharacterGenerationParams {
  species: string;
  role: string;
  morphology: string;
  attire: string;
}

export interface CharacterGenerationResponse {
  success: boolean;
  characterJson?: string;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
}

export interface SolarSystemGenerationParams {
  spectralClass: string;
  planetCount: number;
}

export interface SolarSystemGenerationResponse {
  success: boolean;
  systemJson?: string;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
}

export interface VehicleGenerationParams {
  type: string;
  class: string;
  engine: string;
}

export interface VehicleGenerationResponse {
  success: boolean;
  vehicleJson?: string;
  error?: string;
}

// NPC Generation types
export interface NpcGenerationParams {
  species: string;
  occupation: string;
  personality: string;
  setting: string;
}

export interface NpcGenerationResponse {
  success: boolean;
  npcJson?: string;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
}

// Enemy Generation types
export interface EnemyGenerationParams {
  species: string;
  threatLevel: string;
  behavior: string;
  environment: string;
}

export interface EnemyGenerationResponse {
  success: boolean;
  enemyJson?: string;
  imageBase64?: string;
  imageUrl?: string;
  error?: string;
}

// Mission Generation types
export interface MissionGenerationParams {
  missionType: string;
  difficulty: string;
  environment: string;
  factionInvolved: string;
}

export interface MissionGenerationResponse {
  success: boolean;
  missionJson?: string;
  error?: string;
}

// Encounter Generation types
export interface EncounterGenerationParams {
  encounterType: string;
  difficulty: string;
  environment: string;
  enemyCount: string;
}

export interface EncounterGenerationResponse {
  success: boolean;
  encounterJson?: string;
  error?: string;
}

// HTTP client types
export interface RequestConfig extends RequestInit {
  skipAuth?: boolean;
  timeout?: number;
}

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
