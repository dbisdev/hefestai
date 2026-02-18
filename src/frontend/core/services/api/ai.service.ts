/**
 * AI Generation Service
 * Single Responsibility: AI content generation operations
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

export interface GeneratedCharacter {
  characterJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export interface GeneratedSolarSystem {
  systemJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export interface GeneratedVehicle {
  vehicleJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export interface GeneratedNpc {
  npcJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export interface GeneratedEnemy {
  enemyJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export interface GeneratedMission {
  missionJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export interface GeneratedEncounter {
  encounterJson: string;
  imageBase64?: string;
  imageUrl?: string;
  /** Unique identifier for this generation request. Use when saving to link entity to generation history. */
  generationRequestId?: string;
}

export const aiService = {
  /**
   * Generate a character with AI
   */
  async generateCharacter(params: CharacterGenerationParams): Promise<GeneratedCharacter> {
    const data = await httpClient.post<CharacterGenerationResponse>(
      '/ai/generate/character',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'Character generation failed');
    }

    return {
      characterJson: data.characterJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },

  /**
   * Generate a solar system with AI
   */
  async generateSolarSystem(params: SolarSystemGenerationParams): Promise<GeneratedSolarSystem> {
    const data = await httpClient.post<SolarSystemGenerationResponse>(
      '/ai/generate/solar-system',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'Solar system generation failed');
    }

    return {
      systemJson: data.systemJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },

  /**
   * Generate a vehicle with AI
   */
  async generateVehicle(params: VehicleGenerationParams): Promise<GeneratedVehicle> {
    const data = await httpClient.post<VehicleGenerationResponse>(
      '/ai/generate/vehicle',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'Vehicle generation failed');
    }

    return {
      vehicleJson: data.vehicleJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },

  /**
   * Generate an NPC (Non-Player Character) with AI
   */
  async generateNpc(params: NpcGenerationParams): Promise<GeneratedNpc> {
    const data = await httpClient.post<NpcGenerationResponse>(
      '/ai/generate/npc',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'NPC generation failed');
    }

    return {
      npcJson: data.npcJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },

  /**
   * Generate an enemy/hostile creature with AI
   */
  async generateEnemy(params: EnemyGenerationParams): Promise<GeneratedEnemy> {
    const data = await httpClient.post<EnemyGenerationResponse>(
      '/ai/generate/enemy',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'Enemy generation failed');
    }

    return {
      enemyJson: data.enemyJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },

  /**
   * Generate a mission/quest with AI
   */
  async generateMission(params: MissionGenerationParams): Promise<GeneratedMission> {
    const data = await httpClient.post<MissionGenerationResponse>(
      '/ai/generate/mission',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'Mission generation failed');
    }

    return {
      missionJson: data.missionJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },

  /**
   * Generate a combat encounter with AI
   */
  async generateEncounter(params: EncounterGenerationParams): Promise<GeneratedEncounter> {
    const data = await httpClient.post<EncounterGenerationResponse>(
      '/ai/generate/encounter',
      params
    );

    if (!data.success) {
      throw new Error(data.error || 'Encounter generation failed');
    }

    return {
      encounterJson: data.encounterJson!,
      imageBase64: data.imageBase64,
      imageUrl: data.imageUrl,
      generationRequestId: data.generationRequestId,
    };
  },
};
