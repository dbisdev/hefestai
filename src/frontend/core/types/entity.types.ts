/**
 * Entity Types for WorldBuilder content
 * Single Responsibility: Only entity-related type definitions
 */

export type EntityCategory = 'PLANETS' | 'CHARACTERS' | 'VEHICLES';

export interface Entity {
  id: string;
  name: string;
  type: string;
  meta: string;
  image: string;
  category: EntityCategory;
  description?: string;
  stats?: Record<string, unknown>;
  creatorId: string;
}

export interface EntityDto {
  id: string;
  name: string;
  type: string;
  meta: string;
  image: string;
  category: string;
  description?: string;
  stats?: Record<string, unknown>;
  creatorId: string;
  createdAt: string;
}

export interface CreateEntityInput {
  name: string;
  type: string;
  meta: string;
  image: string;
  category: EntityCategory;
  description?: string;
  stats?: Record<string, unknown>;
}

export interface UpdateEntityInput extends Partial<CreateEntityInput> {
  id: string;
}

// Generator-specific types
export interface CharacterData {
  name: string;
  bio: string;
  stats: {
    STR: number;
    INT: number;
    DEX: number;
  };
}

export interface SystemData {
  name: string;
  description: string;
  planets: string[];
}

export interface VehicleData {
  name: string;
  specs: string;
  stats: {
    SPEED: number;
    ARMOR: number;
    CARGO: number;
  };
}
