/**
 * Entity Service
 * Single Responsibility: Entity CRUD operations
 */

import { httpClient } from './client';
import type { Entity, EntityDto, CreateEntityInput, UpdateEntityInput, EntityCategory } from '../../types';

/**
 * Map API DTO to Entity
 */
function mapEntityFromDto(dto: EntityDto): Entity {
  return {
    id: dto.id,
    name: dto.name,
    type: dto.type,
    meta: dto.meta,
    image: dto.image,
    category: dto.category as EntityCategory,
    description: dto.description,
    stats: dto.stats,
    creatorId: dto.creatorId,
  };
}

export const entityService = {
  /**
   * Get all visible entities for the current user
   */
  async getAll(): Promise<Entity[]> {
    const data = await httpClient.get<EntityDto[]>('/entities');
    return data.map(mapEntityFromDto);
  },

  /**
   * Get entities by category
   */
  async getByCategory(category: EntityCategory): Promise<Entity[]> {
    const data = await httpClient.get<EntityDto[]>(`/entities?category=${category}`);
    return data.map(mapEntityFromDto);
  },

  /**
   * Get a single entity by ID
   */
  async getById(id: string): Promise<Entity> {
    const data = await httpClient.get<EntityDto>(`/entities/${id}`);
    return mapEntityFromDto(data);
  },

  /**
   * Create a new entity
   */
  async create(input: CreateEntityInput): Promise<Entity> {
    const data = await httpClient.post<EntityDto>('/entities', input);
    return mapEntityFromDto(data);
  },

  /**
   * Update an existing entity
   */
  async update(input: UpdateEntityInput): Promise<Entity> {
    const { id, ...updateData } = input;
    const data = await httpClient.put<EntityDto>(`/entities/${id}`, updateData);
    return mapEntityFromDto(data);
  },

  /**
   * Save entity (create or update)
   */
  async save(entity: Omit<Entity, 'id' | 'creatorId'> & { id?: string }): Promise<Entity> {
    if (entity.id) {
      return this.update({ ...entity, id: entity.id });
    }
    return this.create(entity as CreateEntityInput);
  },

  /**
   * Delete an entity
   */
  async delete(id: string): Promise<void> {
    await httpClient.delete(`/entities/${id}`);
  },
};
