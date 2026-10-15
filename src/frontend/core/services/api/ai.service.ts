/**
 * AI Generation Service
 * Single Responsibility: AI content generation operations
 * Uses a generic helper to avoid repeating the same POST → validate → extract
 * pattern across all seven generation methods (DRY principle).
 */

import { httpClient, ApiRequestError } from './client';
import type {
  GenerationResponseBase,
  CharacterGenerationParams,
  CharacterGenerationResponse,
  SolarSystemGenerationParams,
  SolarSystemGenerationResponse,
  VehicleGenerationParams,
  VehicleGenerationResponse,
  NpcGenerationParams,
  NpcGenerationResponse,
  EnemyGenerationParams,
  EnemyGenerationResponse,
  MissionGenerationParams,
  MissionGenerationResponse,
  EncounterGenerationParams,
  EncounterGenerationResponse,
} from '@core/types';

// ---------------------------------------------------------------------------
// Shared base for all Generated* result types
// ---------------------------------------------------------------------------

/**
 * Fields present on every successful AI generation result.
 */
export interface GeneratedEntityBase {
  imageBase64?: string;
  imageUrl?: string;
  generationRequestId?: string;
}

export interface GeneratedCharacter extends GeneratedEntityBase { characterJson: string; }
export interface GeneratedSolarSystem extends GeneratedEntityBase { systemJson: string; }
export interface GeneratedVehicle extends GeneratedEntityBase { vehicleJson: string; }
export interface GeneratedNpc extends GeneratedEntityBase { npcJson: string; }
export interface GeneratedEnemy extends GeneratedEntityBase { enemyJson: string; }
export interface GeneratedMission extends GeneratedEntityBase { missionJson: string; }
export interface GeneratedEncounter extends GeneratedEntityBase { encounterJson: string; }

// ---------------------------------------------------------------------------
// Private helpers
// ---------------------------------------------------------------------------

/**
 * Throws a typed ApiRequestError for a failed AI generation.
 * HTTP 422 Unprocessable Entity reflects that the request was valid but the
 * AI pipeline could not produce a result.
 */
function throwGenerationError(message: string | undefined, fallback: string): never {
  throw new ApiRequestError(message || fallback, 422, 'GENERATION_FAILED');
}

/**
 * Sends a POST request to an AI generation endpoint, validates the response,
 * and returns the raw response data.
 *
 * Centralises the POST → success-check pattern shared by all generation methods.
 */
async function callGenerationEndpoint<TResponse extends GenerationResponseBase>(
  endpoint: string,
  params: unknown,
  errorFallback: string,
): Promise<TResponse> {
  const data = await httpClient.post<TResponse>(endpoint, params);
  if (!data.success) throwGenerationError(data.error, errorFallback);
  return data;
}

/**
 * Extracts the shared image/id fields from a raw generation response.
 */
function extractBase(data: GenerationResponseBase): GeneratedEntityBase {
  return {
    imageBase64: data.imageBase64,
    imageUrl: data.imageUrl,
    generationRequestId: data.generationRequestId,
  };
}

// ---------------------------------------------------------------------------
// Public service
// ---------------------------------------------------------------------------

export const aiService = {
  /** Generates a character entity, optionally with RAG and AI image. */
  async generateCharacter(params: CharacterGenerationParams): Promise<GeneratedCharacter> {
    const data = await callGenerationEndpoint<CharacterGenerationResponse>(
      '/ai/generate/character', params, 'Character generation failed',
    );
    return { ...extractBase(data), characterJson: data.characterJson! };
  },

  /** Generates a solar system entity, optionally with RAG and AI image. */
  async generateSolarSystem(params: SolarSystemGenerationParams): Promise<GeneratedSolarSystem> {
    const data = await callGenerationEndpoint<SolarSystemGenerationResponse>(
      '/ai/generate/solar-system', params, 'Solar system generation failed',
    );
    return { ...extractBase(data), systemJson: data.systemJson! };
  },

  /** Generates a vehicle entity, optionally with RAG and AI image. */
  async generateVehicle(params: VehicleGenerationParams): Promise<GeneratedVehicle> {
    const data = await callGenerationEndpoint<VehicleGenerationResponse>(
      '/ai/generate/vehicle', params, 'Vehicle generation failed',
    );
    return { ...extractBase(data), vehicleJson: data.vehicleJson! };
  },

  /** Generates an NPC entity, optionally with RAG and AI image. */
  async generateNpc(params: NpcGenerationParams): Promise<GeneratedNpc> {
    const data = await callGenerationEndpoint<NpcGenerationResponse>(
      '/ai/generate/npc', params, 'NPC generation failed',
    );
    return { ...extractBase(data), npcJson: data.npcJson! };
  },

  /** Generates an enemy entity, optionally with RAG and AI image. */
  async generateEnemy(params: EnemyGenerationParams): Promise<GeneratedEnemy> {
    const data = await callGenerationEndpoint<EnemyGenerationResponse>(
      '/ai/generate/enemy', params, 'Enemy generation failed',
    );
    return { ...extractBase(data), enemyJson: data.enemyJson! };
  },

  /** Generates a mission entity, optionally with RAG and AI image. */
  async generateMission(params: MissionGenerationParams): Promise<GeneratedMission> {
    const data = await callGenerationEndpoint<MissionGenerationResponse>(
      '/ai/generate/mission', params, 'Mission generation failed',
    );
    return { ...extractBase(data), missionJson: data.missionJson! };
  },

  /** Generates an encounter entity, optionally with RAG and AI image. */
  async generateEncounter(params: EncounterGenerationParams): Promise<GeneratedEncounter> {
    const data = await callGenerationEndpoint<EncounterGenerationResponse>(
      '/ai/generate/encounter', params, 'Encounter generation failed',
    );
    return { ...extractBase(data), encounterJson: data.encounterJson! };
  },
};
