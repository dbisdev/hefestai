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
  ownerId: string;
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
 * Input payload for creating a new game system
 */
export interface CreateGameSystemInput {
  code: string;
  name: string;
  publisher?: string;
  version?: string;
  description?: string;
  supportedEntityTypes?: string[];
}

/**
 * Input payload for updating an existing game system
 */
export interface UpdateGameSystemInput {
  name: string;
  publisher?: string;
  version?: string;
  description?: string;
  supportedEntityTypes?: string[];
}

/**
 * Input payload for updating game system status
 */
export interface UpdateGameSystemStatusInput {
  isActive: boolean;
}
