/**
 * Entity Service
 * Single Responsibility: LoreEntity CRUD operations (campaign-scoped)
 * All entity operations require a campaignId context
 */

import { httpClient } from './client';
import type {
  LoreEntity,
  CreateLoreEntityInput,
  UpdateLoreEntityInput,
  ChangeVisibilityInput,
  TransferOwnershipInput,
  EntityCategory,
} from '../../types';

/**
 * Paginated result from the backend
 */
export interface PaginatedEntitiesResult {
  items: LoreEntity[];
  totalCount: number;
  pageNumber: number | null;
  totalPages: number | null;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Options for fetching entities with pagination and filtering
 */
export interface GetEntitiesOptions {
  entityType?: EntityCategory;
  visibility?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Build the base URL for campaign-scoped entity endpoints
 */
function buildEntityUrl(campaignId: string, entityId?: string): string {
  const base = `/campaigns/${campaignId}/entities`;
  return entityId ? `${base}/${entityId}` : base;
}

export const entityService = {
  /**
   * Get visible entities in a campaign with optional pagination and filtering
   * @param campaignId - The campaign ID to fetch entities from
   * @param options - Optional filters, search, and pagination parameters
   * @returns Paginated result with entities and metadata
   */
  async getByCampaignPaginated(
    campaignId: string,
    options: GetEntitiesOptions = {}
  ): Promise<PaginatedEntitiesResult> {
    const params = new URLSearchParams();
    
    if (options.entityType) params.append('entityType', options.entityType);
    if (options.visibility !== undefined) params.append('visibility', options.visibility.toString());
    if (options.search) params.append('search', options.search);
    if (options.page !== undefined) params.append('page', options.page.toString());
    if (options.pageSize !== undefined) params.append('pageSize', options.pageSize.toString());
    
    const queryString = params.toString();
    const url = buildEntityUrl(campaignId) + (queryString ? `?${queryString}` : '');
    
    return httpClient.get<PaginatedEntitiesResult>(url);
  },

  /**
   * Get all visible entities in a campaign for the current user (unpaginated)
   * @param campaignId - The campaign ID to fetch entities from
   * @param entityType - Optional filter by entity type
   * @param visibility - Optional filter by visibility level
   */
  async getByCampaign(
    campaignId: string,
    entityType?: EntityCategory,
    visibility?: number
  ): Promise<LoreEntity[]> {
    // Use paginated endpoint without page number to get all results
    const result = await this.getByCampaignPaginated(campaignId, { entityType, visibility });
    return result.items;
  },

  /**
   * Get a single entity by ID within a campaign
   */
  async getById(campaignId: string, entityId: string): Promise<LoreEntity> {
    return httpClient.get<LoreEntity>(buildEntityUrl(campaignId, entityId));
  },

  /**
   * Create a new entity in a campaign
   */
  async create(campaignId: string, input: CreateLoreEntityInput): Promise<LoreEntity> {
    return httpClient.post<LoreEntity>(buildEntityUrl(campaignId), input);
  },

  /**
   * Update an existing entity
   */
  async update(
    campaignId: string,
    entityId: string,
    input: UpdateLoreEntityInput
  ): Promise<LoreEntity> {
    return httpClient.put<LoreEntity>(buildEntityUrl(campaignId, entityId), input);
  },

  /**
   * Delete an entity (soft delete)
   */
  async delete(campaignId: string, entityId: string): Promise<void> {
    await httpClient.delete(buildEntityUrl(campaignId, entityId));
  },

  /**
   * Change entity visibility
   */
  async changeVisibility(
    campaignId: string,
    entityId: string,
    input: ChangeVisibilityInput
  ): Promise<LoreEntity> {
    return httpClient.patch<LoreEntity>(
      `${buildEntityUrl(campaignId, entityId)}/visibility`,
      input
    );
  },

  /**
   * Transfer entity ownership to another campaign member.
   * Only the current owner or campaign master can transfer ownership.
   * @param campaignId - The campaign ID
   * @param entityId - The entity ID to transfer
   * @param input - Transfer ownership request with new owner ID
   * @returns Updated entity with new owner
   */
  async transferOwnership(
    campaignId: string,
    entityId: string,
    input: TransferOwnershipInput
  ): Promise<LoreEntity> {
    return httpClient.patch<LoreEntity>(
      `${buildEntityUrl(campaignId, entityId)}/owner`,
      input
    );
  },

  // ============================================
  // Legacy methods for backwards compatibility
  // These will fail without a campaign context
  // TODO: Remove after migration is complete
  // ============================================

  /**
   * @deprecated Use getByCampaign instead
   * This method will fail - entities require campaign context
   */
  async getAll(): Promise<LoreEntity[]> {
    console.warn('entityService.getAll() is deprecated. Use getByCampaign(campaignId) instead.');
    // Return empty array to prevent crashes during migration
    return [];
  },

  /**
   * @deprecated Use getById(campaignId, entityId) instead
   */
  async getByIdLegacy(id: string): Promise<LoreEntity> {
    console.warn('entityService.getByIdLegacy() is deprecated. Use getById(campaignId, entityId) instead.');
    throw new Error('Entity operations require a campaign context. Please select a campaign first.');
  },
};
