/**
 * Admin Document Service
 * Single Responsibility: Admin-only document and embedding operations.
 * Separated from documentService to respect the principle that admin
 * operations (role-restricted, backfill/maintenance) live in their own
 * service rather than being mixed with user-facing document operations.
 */

import { httpClient } from './client';
import { EMBEDDING_BATCH_SIZE_DEFAULT, EMBEDDING_MAX_DOCUMENTS_DEFAULT } from '@core/config/constants';
import type {
  GenerateMissingEmbeddingsParams,
  GenerateMissingEmbeddingsResult,
} from '@core/types';

/**
 * Admin service for document embedding maintenance operations.
 * All methods require Admin role.
 */
export const adminDocumentService = {
  /**
   * Generate embeddings for documents that don't have them yet.
   * Intended for backfilling embeddings after migrations or manual imports.
   * Backend endpoint: POST /api/documents/embeddings/generate
   *
   * @param params - Batch-processing parameters (batchSize, maxDocuments, gameSystemId).
   * @returns Result with success/failure counts and error messages.
   */
  async generateMissingEmbeddings(
    params: GenerateMissingEmbeddingsParams = {},
  ): Promise<GenerateMissingEmbeddingsResult> {
    return httpClient.post<GenerateMissingEmbeddingsResult>('/documents/embeddings/generate', {
      batchSize: params.batchSize ?? EMBEDDING_BATCH_SIZE_DEFAULT,
      maxDocuments: params.maxDocuments ?? EMBEDDING_MAX_DOCUMENTS_DEFAULT,
      gameSystemId: params.gameSystemId,
    });
  },
};
