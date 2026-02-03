/**
 * Game System Service
 * Single Responsibility: Game System CRUD operations
 * Provides API interface for game system management
 */

import { httpClient } from './client';
import type { 
  GameSystem, 
  CreateGameSystemRequest, 
  UpdateGameSystemRequest,
  UpdateGameSystemStatusRequest 
} from '../../types';

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

  /**
   * Create a new game system (requires Master or Admin role)
   * Backend endpoint: POST /api/GameSystems
   */
  async create(request: CreateGameSystemRequest): Promise<GameSystem> {
    return httpClient.post<GameSystem>('/gamesystems', request);
  },

  /**
   * Update an existing game system (requires Master or Admin role)
   * Backend endpoint: PUT /api/GameSystems/{id}
   */
  async update(id: string, request: UpdateGameSystemRequest): Promise<GameSystem> {
    return httpClient.put<GameSystem>(`/gamesystems/${id}`, request);
  },

  /**
   * Activate or deactivate a game system (requires Master or Admin role)
   * Backend endpoint: PATCH /api/GameSystems/{id}/status
   */
  async updateStatus(id: string, request: UpdateGameSystemStatusRequest): Promise<GameSystem> {
    return httpClient.patch<GameSystem>(`/gamesystems/${id}/status`, request);
  },
};
