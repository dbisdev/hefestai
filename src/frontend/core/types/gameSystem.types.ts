/**
 * Game System Types
 * Aligned with backend GameSystem DTOs
 * Single Responsibility: Only game system-related type definitions
 */

/**
 * Game system DTO matching backend GameSystemDto
 * Represents a tabletop RPG system (e.g., D&D, Pathfinder, etc.)
 */
export interface GameSystem {
  id: string;
  code: string;
  name: string;
  publisher?: string;
  version?: string;
  description?: string;
  supportedEntityTypes: string[];
  isActive?: boolean;
}

/**
 * Simplified game system for dropdown/select components
 */
export interface GameSystemOption {
  id: string;
  name: string;
  code: string;
}

/**
 * Request payload for creating a new game system
 * Matches backend CreateGameSystemRequest
 */
export interface CreateGameSystemRequest {
  code: string;
  name: string;
  publisher?: string;
  version?: string;
  description?: string;
  supportedEntityTypes?: string[];
}

/**
 * Request payload for updating an existing game system
 * Matches backend UpdateGameSystemRequest
 */
export interface UpdateGameSystemRequest {
  name: string;
  publisher?: string;
  version?: string;
  description?: string;
  supportedEntityTypes?: string[];
}

/**
 * Request payload for updating game system status
 * Matches backend UpdateGameSystemStatusRequest
 */
export interface UpdateGameSystemStatusRequest {
  isActive: boolean;
}
