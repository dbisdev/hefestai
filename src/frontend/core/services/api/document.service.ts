/**
 * Document Service
 * Single Responsibility: User-facing document and RAG search operations.
 *
 * Admin-only operations (e.g. generating missing embeddings) are handled
 * by `adminDocument.service.ts` to keep this service focused on the
 * operations available to all authenticated users.
 */

import { httpClient } from './client';
import type {
  SemanticSearchParams,
  SemanticSearchResult,
  ManualDto,
  ManualSummaryDto,
  UploadManualParams,
  UploadManualResult,
  DocumentAvailabilityResult,
} from '@core/types';

export const documentService = {
  /**
   * Perform semantic search across documents.
   * Uses vector similarity to find relevant content.
   * Backend endpoint: POST /api/documents/search
   *
   * @param params - Search parameters (query, filters, RAG options).
   * @returns Search results with optional AI-generated answer.
   */
  async semanticSearch(params: SemanticSearchParams): Promise<SemanticSearchResult> {
    return httpClient.post<SemanticSearchResult>('/documents/search', {
      query: params.query,
      limit: params.limit ?? 5,
      threshold: params.threshold ?? 0.7,
      projectId: params.projectId,
      gameSystemId: params.gameSystemId,
      generateAnswer: params.generateAnswer ?? false,
      systemPrompt: params.systemPrompt,
      masterId: params.masterId,
    });
  },

  /**
   * Get a single manual by ID.
   * Backend endpoint: GET /api/documents/game-systems/{gameSystemId}/manuals/{manualId}
   *
   * @param gameSystemId - The game system the manual belongs to.
   * @param manualId - The manual document ID.
   * @returns Full manual details including page count.
   */
  async getManual(gameSystemId: string, manualId: string): Promise<ManualDto> {
    return httpClient.get<ManualDto>(`/documents/game-systems/${gameSystemId}/manuals/${manualId}`);
  },

  /**
   * Get all manuals for a game system.
   * Backend endpoint: GET /api/documents/game-systems/{gameSystemId}/manuals
   *
   * @param gameSystemId - The game system ID.
   * @returns Summary list of manuals with chunk counts.
   */
  async getManualsByGameSystem(gameSystemId: string): Promise<ManualSummaryDto[]> {
    return httpClient.get<ManualSummaryDto[]>(`/documents/game-systems/${gameSystemId}/manuals`);
  },

  /**
   * Upload a PDF manual for RAG processing.
   * The PDF will be chunked and indexed for semantic search.
   * Backend endpoint: POST /api/documents/game-systems/{gameSystemId}/manuals
   *
   * @param params - Upload parameters including the PDF file and metadata.
   * @returns Result containing manual ID and processing statistics.
   */
  async uploadManual(params: UploadManualParams): Promise<UploadManualResult> {
    const formData = new FormData();
    formData.append('File', params.file);

    if (params.title) {
      formData.append('Title', params.title);
    }
    if (params.sourceType) {
      formData.append('SourceType', params.sourceType);
    }
    if (params.version) {
      formData.append('Version', params.version);
    }

    return httpClient.postForm<UploadManualResult>(
      `/documents/game-systems/${params.gameSystemId}/manuals`,
      formData,
    );
  },

  /**
   * Check if a game system has documents available for RAG search.
   * Accessible by all authenticated users:
   * - Players see Admin-shared documents only (unless masterId is provided).
   * - Masters see Admin-shared + their own documents.
   * Backend endpoint: GET /api/documents/game-systems/{gameSystemId}/available
   *
   * @param gameSystemId - The game system ID to check.
   * @param masterId - Optional Master ID to include their documents (for Players in a campaign).
   * @returns Document availability status.
   */
  async checkDocumentAvailability(
    gameSystemId: string,
    masterId?: string,
  ): Promise<DocumentAvailabilityResult> {
    const query = masterId ? `?masterId=${masterId}` : '';
    return httpClient.get<DocumentAvailabilityResult>(
      `/documents/game-systems/${gameSystemId}/available${query}`,
    );
  },
};
