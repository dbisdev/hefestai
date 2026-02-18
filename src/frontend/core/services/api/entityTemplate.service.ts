/**
 * Entity Template Service
 * Single Responsibility: Entity Template CRUD and extraction operations
 * Provides API interface for template management (Admin only)
 */

import { httpClient } from './client';
import type {
  EntityTemplate,
  GetTemplatesResult,
  ExtractTemplatesResult,
  ConfirmTemplateResult,
  ConfirmTemplateRequest,
  CreateTemplateRequest,
  CreateTemplateResult,
  UpdateTemplateRequest,
  UpdateTemplateResult,
  DeleteTemplateResult,
  FieldDefinition,
  TemplateStatus,
} from '@core/types';

/**
 * Service for managing entity templates.
 * Templates define the schema (fields) for creating entities of specific types.
 * Most operations require Master or Admin role.
 */
export const entityTemplateService = {
  /**
   * Get all templates for a game system
   * Backend endpoint: GET /api/game-systems/{gameSystemId}/templates
   * @param gameSystemId - The game system ID
   * @param status - Optional status filter
   * @param confirmedOnly - If true, only return confirmed templates
   */
  async getByGameSystem(
    gameSystemId: string,
    status?: TemplateStatus,
    confirmedOnly?: boolean
  ): Promise<GetTemplatesResult> {
    const params = new URLSearchParams();
    if (status !== undefined) {
      params.append('status', status.toString());
    }
    if (confirmedOnly) {
      params.append('confirmedOnly', 'true');
    }
    const query = params.toString();
    const url = `/game-systems/${gameSystemId}/templates${query ? `?${query}` : ''}`;
    return httpClient.get<GetTemplatesResult>(url);
  },

  /**
   * Get a specific template by ID
   * Backend endpoint: GET /api/game-systems/{gameSystemId}/templates/{templateId}
   * @param gameSystemId - The game system ID
   * @param templateId - The template ID
   */
  async getById(gameSystemId: string, templateId: string): Promise<EntityTemplate> {
    return httpClient.get<EntityTemplate>(`/game-systems/${gameSystemId}/templates/${templateId}`);
  },

  /**
   * Get the confirmed template for a specific entity type.
   * Useful for getting field definitions to display entity data.
   * @param gameSystemId - The game system ID
   * @param entityTypeName - The entity type name (e.g., "character", "npc")
   * @returns The confirmed template or null if not found
   */
  async getConfirmedByEntityType(
    gameSystemId: string,
    entityTypeName: string
  ): Promise<EntityTemplate | null> {
    try {
      // Get all confirmed templates
      const result = await this.getByGameSystem(gameSystemId, undefined, true);
      
      // Normalize the entity type name for comparison
      const normalizedName = entityTypeName.toLowerCase().replace(/\s+/g, '_');
      
      // Find the matching template by entity type name
      const templateSummary = result.templates.find(
        t => t.entityTypeName.toLowerCase() === normalizedName
      );
      
      if (!templateSummary) {
        return null;
      }
      
      // Fetch full template with field definitions
      return await this.getById(gameSystemId, templateSummary.id);
    } catch (error) {
      console.error(`Failed to get template for ${entityTypeName}:`, error);
      return null;
    }
  },

  /**
   * Get field definitions for a specific entity type.
   * Convenience method that returns just the fields array.
   * @param gameSystemId - The game system ID
   * @param entityTypeName - The entity type name (e.g., "character", "npc")
   * @returns Array of field definitions or empty array if not found
   */
  async getFieldDefinitions(
    gameSystemId: string,
    entityTypeName: string
  ): Promise<FieldDefinition[]> {
    const template = await this.getConfirmedByEntityType(gameSystemId, entityTypeName);
    return template?.fields ?? [];
  },

  /**
   * Extract templates from uploaded manuals using RAG
   * Backend endpoint: POST /api/game-systems/{gameSystemId}/templates/extract
   * @param gameSystemId - The game system ID
   * @param sourceDocumentId - Optional specific document to analyze
   */
  async extractFromManual(
    gameSystemId: string,
    sourceDocumentId?: string
  ): Promise<ExtractTemplatesResult> {
    const body = sourceDocumentId ? { sourceDocumentId } : {};
    return httpClient.post<ExtractTemplatesResult>(
      `/game-systems/${gameSystemId}/templates/extract`,
      body
    );
  },

  /**
   * Create a new template manually
   * Backend endpoint: POST /api/game-systems/{gameSystemId}/templates
   * @param gameSystemId - The game system ID
   * @param request - Template creation data
   */
  async create(
    gameSystemId: string,
    request: CreateTemplateRequest
  ): Promise<CreateTemplateResult> {
    return httpClient.post<CreateTemplateResult>(
      `/game-systems/${gameSystemId}/templates`,
      request
    );
  },

  /**
   * Update an existing template
   * Backend endpoint: PUT /api/game-systems/{gameSystemId}/templates/{templateId}
   * @param gameSystemId - The game system ID
   * @param templateId - The template ID
   * @param request - Template update data
   */
  async update(
    gameSystemId: string,
    templateId: string,
    request: UpdateTemplateRequest
  ): Promise<UpdateTemplateResult> {
    return httpClient.put<UpdateTemplateResult>(
      `/game-systems/${gameSystemId}/templates/${templateId}`,
      request
    );
  },

  /**
   * Confirm a template, making it available for entity creation
   * Backend endpoint: POST /api/game-systems/{gameSystemId}/templates/{templateId}/confirm
   * @param gameSystemId - The game system ID
   * @param templateId - The template ID
   * @param request - Optional confirmation notes
   */
  async confirm(
    gameSystemId: string,
    templateId: string,
    request?: ConfirmTemplateRequest
  ): Promise<ConfirmTemplateResult> {
    return httpClient.post<ConfirmTemplateResult>(
      `/game-systems/${gameSystemId}/templates/${templateId}/confirm`,
      request ?? {}
    );
  },

  /**
   * Delete a template
   * Backend endpoint: DELETE /api/game-systems/{gameSystemId}/templates/{templateId}
   * @param gameSystemId - The game system ID
   * @param templateId - The template ID
   * @param force - If true, delete even if entities are using this template
   */
  async delete(
    gameSystemId: string,
    templateId: string,
    force?: boolean
  ): Promise<DeleteTemplateResult> {
    const params = force ? '?force=true' : '';
    return httpClient.delete<DeleteTemplateResult>(
      `/game-systems/${gameSystemId}/templates/${templateId}${params}`
    );
  },

  /**
   * Confirm all templates for a game system in one operation
   * Confirms all templates that are in Draft or PendingReview status
   * @param gameSystemId - The game system ID
   */
  async confirmAll(gameSystemId: string): Promise<{ confirmed: number; failed: number }> {
    // First get all templates
    const result = await this.getByGameSystem(gameSystemId);
    
    let confirmed = 0;
    let failed = 0;
    
    // Filter templates that can be confirmed (Draft or PendingReview)
    const confirmableTemplates = result.templates.filter(
      t => t.status === TemplateStatus.Draft || t.status === TemplateStatus.PendingReview
    );
    
    // Confirm each one
    for (const template of confirmableTemplates) {
      try {
        await this.confirm(gameSystemId, template.id, { notes: 'Bulk confirmation' });
        confirmed++;
      } catch {
        failed++;
      }
    }
    
    return { confirmed, failed };
  },
};
