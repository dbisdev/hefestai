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
}

/**
 * Simplified game system for dropdown/select components
 */
export interface GameSystemOption {
  id: string;
  name: string;
  code: string;
}
