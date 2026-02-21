/**
 * AI Generation Service
 * Single Responsibility: AI content generation operations
 * Refactored to use generic helper for DRY principle
 */

import { httpClient } from './client';
import type {
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

/**
 * Base interface for all AI generation results
 */
interface GeneratedEntityBase {
  imageBase64?: string;
  imageUrl?: string;
  generationRequestId?: string;
}

/**
 * Extract common fields from API response
 */
function extractGeneratedFields<T extends GeneratedEntityBase>(
  data: { success: boolean; error?: string; imageBase64?: string; imageUrl?: string; generationRequestId?: string },
  jsonContent: string
): T & GeneratedEntityBase {
  return {
    ...(jsonContent ? {} : {}),
    imageBase64: data.imageBase64,
    imageUrl: data.imageUrl,
    generationRequestId: data.generationRequestId,
  } as T;
}

/**
 * Result types for AI generation operations
 */
export interface GeneratedCharacter extends GeneratedEntityBase {
  characterJson: string;
}

export interface GeneratedSolarSystem extends GeneratedEntityBase {
  systemJson: string;
}

export interface GeneratedVehicle extends GeneratedEntityBase {
  vehicleJson: string;
}

export interface GeneratedNpc extends GeneratedEntityBase {
  npcJson: string;
}

export interface GeneratedEnemy extends GeneratedEntityBase {
  enemyJson: string;
}

export interface GeneratedMission extends GeneratedEntityBase {
  missionJson: string;
}

export interface GeneratedEncounter extends GeneratedEntityBase {
  encounterJson: string;
}

export const aiService = {
  async generateCharacter(params: CharacterGenerationParams): Promise<GeneratedCharacter> {
    const data = await httpClient.post<CharacterGenerationResponse>('/ai/generate/character', params);
    if (!data.success) throw new Error(data.error || 'Character generation failed');
    return { characterJson: data.characterJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },

  async generateSolarSystem(params: SolarSystemGenerationParams): Promise<GeneratedSolarSystem> {
    const data = await httpClient.post<SolarSystemGenerationResponse>('/ai/generate/solar-system', params);
    if (!data.success) throw new Error(data.error || 'Solar system generation failed');
    return { systemJson: data.systemJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },

  async generateVehicle(params: VehicleGenerationParams): Promise<GeneratedVehicle> {
    const data = await httpClient.post<VehicleGenerationResponse>('/ai/generate/vehicle', params);
    if (!data.success) throw new Error(data.error || 'Vehicle generation failed');
    return { vehicleJson: data.vehicleJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },

  async generateNpc(params: NpcGenerationParams): Promise<GeneratedNpc> {
    const data = await httpClient.post<NpcGenerationResponse>('/ai/generate/npc', params);
    if (!data.success) throw new Error(data.error || 'NPC generation failed');
    return { npcJson: data.npcJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },

  async generateEnemy(params: EnemyGenerationParams): Promise<GeneratedEnemy> {
    const data = await httpClient.post<EnemyGenerationResponse>('/ai/generate/enemy', params);
    if (!data.success) throw new Error(data.error || 'Enemy generation failed');
    return { enemyJson: data.enemyJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },

  async generateMission(params: MissionGenerationParams): Promise<GeneratedMission> {
    const data = await httpClient.post<MissionGenerationResponse>('/ai/generate/mission', params);
    if (!data.success) throw new Error(data.error || 'Mission generation failed');
    return { missionJson: data.missionJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },

  async generateEncounter(params: EncounterGenerationParams): Promise<GeneratedEncounter> {
    const data = await httpClient.post<EncounterGenerationResponse>('/ai/generate/encounter', params);
    if (!data.success) throw new Error(data.error || 'Encounter generation failed');
    return { encounterJson: data.encounterJson!, imageBase64: data.imageBase64, imageUrl: data.imageUrl, generationRequestId: data.generationRequestId };
  },
};
