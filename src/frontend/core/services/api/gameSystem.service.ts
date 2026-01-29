/**
 * Game System Service
 * Single Responsibility: Game System read operations
 * Fetches game systems from the backend API
 */

import { httpClient } from './client';
import type { GameSystem } from '../../types';

export const gameSystemService = {
  /**
   * Get all active game systems
   * Backend endpoint: GET /api/GameSystems
   */
  async getAll(): Promise<GameSystem[]> {
    return httpClient.get<GameSystem[]>('/gamesystems');
  },

  /**
   * Get a specific game system by ID
   * Backend endpoint: GET /api/GameSystems/{id}
   */
  async getById(id: string): Promise<GameSystem> {
    return httpClient.get<GameSystem>(`/gamesystems/${id}`);
  },

  /**
   * Get a specific game system by code
   * Backend endpoint: GET /api/GameSystems/by-code/{code}
   */
  async getByCode(code: string): Promise<GameSystem> {
    return httpClient.get<GameSystem>(`/gamesystems/by-code/${code}`);
  },
};
