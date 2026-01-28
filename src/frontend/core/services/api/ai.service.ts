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
} from '../../types';

export interface GeneratedCharacter {
  characterJson: string;
  imageBase64?: string;
  imageUrl?: string;
}

export interface GeneratedSolarSystem {
  systemJson: string;
  imageBase64?: string;
  imageUrl?: string;
}

export interface GeneratedVehicle {
  vehicleJson: string;
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
    };
  },
};
