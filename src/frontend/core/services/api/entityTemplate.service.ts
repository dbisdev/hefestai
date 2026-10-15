/**
 * Entity Template Service
 * Single Responsibility: Entity Template CRUD and extraction operations
 * Provides API interface for template management (Admin only)
 */

import { httpClient, ApiRequestError } from './client';
import type {
  EntityTemplate,
  GetTemplatesResult,
  ExtractTemplatesResult,
  ConfirmTemplateResult,
  ConfirmTemplateInput,
  CreateTemplateInput,
  CreateTemplateResult,
  UpdateTemplateInput,
  UpdateTemplateResult,
  DeleteTemplateResult,
  FieldDefinition,
} from '@core/types';
import { TemplateStatus } from '@core/types';

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
   * @param confirmedOnly - If true, only return confirmed templates (regardless of owner)
   * @param includeAll - If true, return all templates regardless of owner and status (for system owners)
   */
  async getByGameSystem(
    gameSystemId: string,
    status?: TemplateStatus,
    confirmedOnly?: boolean,
    includeAll?: boolean
  ): Promise<GetTemplatesResult> {
    const params = new URLSearchParams();
    if (status !== undefined) {
      params.append('status', status.toString());
    }
    if (confirmedOnly) {
      params.append('confirmedOnly', 'true');
    }
    if (includeAll) {
      params.append('includeAll', 'true');
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
   * @throws {ApiRequestError} When the network request fails (non-404 errors are propagated)
   */
  async getConfirmedByEntityType(
    gameSystemId: string,
    entityTypeName: string
  ): Promise<EntityTemplate | null> {
    // Get all confirmed templates — let network/auth errors propagate to the caller
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
    try {
      return await this.getById(gameSystemId, templateSummary.id);
    } catch (error) {
      // A 404 on the detail endpoint means the template was deleted between
      // the list and the detail request — treat as "not found", not as an error.
      if (error instanceof ApiRequestError && error.status === 404) {
        return null;
      }
      throw error;
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
    // Only send body if sourceDocumentId is provided
    if (sourceDocumentId) {
      return httpClient.post<ExtractTemplatesResult>(
        `/game-systems/${gameSystemId}/templates/extract`,
        { sourceDocumentId },
        { timeout: 120000 } // 2 minutes timeout for extraction
      );
    }
    // Send empty object for request with no body
    return httpClient.post<ExtractTemplatesResult>(
      `/game-systems/${gameSystemId}/templates/extract`,
      {},
      { timeout: 120000 } // 2 minutes timeout for extraction
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
    request: UpdateTemplateInput
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
    request?: ConfirmTemplateInput
  ): Promise<ConfirmTemplateResult> {
    return httpClient.post<ConfirmTemplateResult>(
      `/game-systems/${gameSystemId}/templates/${templateId}/confirm`,
      request ?? {}
    );
  },

  /**
   * Revert a confirmed template back to draft for modifications
   * Backend endpoint: POST /api/game-systems/{gameSystemId}/templates/{templateId}/revert-to-draft
   * @param gameSystemId - The game system ID
   * @param templateId - The template ID
   */
  async revertToDraft(
    gameSystemId: string,
    templateId: string
  ): Promise<void> {
    await httpClient.post(
      `/game-systems/${gameSystemId}/templates/${templateId}/revert-to-draft`,
      {}
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
   * Confirm all templates for a game system in one operation.
   * Confirms all templates that are in Draft or PendingReview status.
   * Individual failures are collected rather than aborting the whole batch.
   * @param gameSystemId - The game system ID
   * @returns Counts of confirmed/failed templates and the IDs that failed
   * @throws {ApiRequestError} When the initial template list request fails
   */
  async confirmAll(
    gameSystemId: string
  ): Promise<{ confirmed: number; failed: number; failedIds: string[] }> {
    // Get all templates — let this propagate; without the list we can do nothing
    const result = await this.getByGameSystem(gameSystemId);

    let confirmed = 0;
    const failedIds: string[] = [];

    // Filter templates that can be confirmed (Draft or PendingReview)
    const confirmableTemplates = result.templates.filter(
      t => t.status === TemplateStatus.Draft || t.status === TemplateStatus.PendingReview
    );

    // Confirm each one, collecting failures rather than swallowing them silently
    for (const template of confirmableTemplates) {
      try {
        await this.confirm(gameSystemId, template.id, { notes: 'Bulk confirmation' });
        confirmed++;
      } catch {
        failedIds.push(template.id);
      }
    }

    return { confirmed, failed: failedIds.length, failedIds };
  },
};
