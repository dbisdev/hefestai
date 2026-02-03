/**
 * Document Service
 * Single Responsibility: Document and RAG search operations
 */

import { httpClient } from './client';
import { tokenService } from '../storage/token.service';

/**
 * Document data transfer object
 */
export interface DocumentDto {
  id: string;
  title: string;
  content: string;
  source?: string;
  metadata?: string;
  hasEmbedding: boolean;
  embeddingDimensions?: number;
  ownerId: string;
  projectId?: string;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Search result with similarity score
 */
export interface DocumentSearchResult {
  document: DocumentDto;
  similarityScore: number;
}

/**
 * Parameters for semantic search
 */
export interface SemanticSearchParams {
  /** The search query text */
  query: string;
  /** Maximum number of results (default: 5) */
  limit?: number;
  /** Minimum similarity threshold 0.0-1.0 (default: 0.7) */
  threshold?: number;
  /** Optional project ID filter */
  projectId?: string;
  /** Optional game system ID filter (for RAG on manuals) */
  gameSystemId?: string;
  /** Whether to generate a RAG answer from results */
  generateAnswer?: boolean;
  /** Optional system prompt for answer generation */
  systemPrompt?: string;
}

/**
 * Result of semantic search
 */
export interface SemanticSearchResult {
  documents: DocumentSearchResult[];
  generatedAnswer?: string;
}

/**
 * Manual document DTO
 */
export interface ManualDto {
  id: string;
  gameSystemId: string;
  title: string;
  pageCount: number;
  chunkCount: number;
  sourceType: 'Rulebook' | 'Supplement' | 'Custom';
  version?: string;
  createdAt: string;
}

/**
 * RAG source type enum
 */
export type RagSourceType = 'Rulebook' | 'Supplement' | 'Custom';

/**
 * Result of manual upload operation
 */
export interface UploadManualResult {
  manualId: string;
  title: string;
  pageCount: number;
  chunkCount: number;
  processingTimeMs: number;
}

/**
 * Parameters for uploading a manual
 */
export interface UploadManualParams {
  /** The game system ID this manual belongs to */
  gameSystemId: string;
  /** The PDF file to upload */
  file: File;
  /** Optional title (extracted from PDF if not provided) */
  title?: string;
  /** Type of RAG source */
  sourceType?: RagSourceType;
  /** Optional version identifier */
  version?: string;
}

export const documentService = {
  /**
   * Perform semantic search across documents.
   * Uses vector similarity to find relevant content.
   * 
   * @param params - Search parameters
   * @returns Search results with optional AI-generated answer
   */
  async semanticSearch(params: SemanticSearchParams): Promise<SemanticSearchResult> {
    const response = await httpClient.post<SemanticSearchResult>('/documents/search', {
      query: params.query,
      limit: params.limit ?? 5,
      threshold: params.threshold ?? 0.7,
      projectId: params.projectId,
      gameSystemId: params.gameSystemId,
      generateAnswer: params.generateAnswer ?? false,
      systemPrompt: params.systemPrompt,
    });

    return response;
  },

  /**
   * Get a manual by ID
   * 
   * @param gameSystemId - The game system ID
   * @param manualId - The manual document ID
   * @returns The manual document details
   */
  async getManual(gameSystemId: string, manualId: string): Promise<ManualDto> {
    return httpClient.get<ManualDto>(`/documents/game-systems/${gameSystemId}/manuals/${manualId}`);
  },

  /**
   * Upload a PDF manual for RAG processing.
   * The PDF will be chunked and indexed for semantic search.
   * 
   * @param params - Upload parameters including file and metadata
   * @returns Result containing manual ID and processing statistics
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

    // Use fetch directly for multipart/form-data
    const token = tokenService.getAccessToken();
    const response = await fetch(`/api/documents/game-systems/${params.gameSystemId}/manuals`, {
      method: 'POST',
      headers: {
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      },
      body: formData,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || `Upload failed with status ${response.status}`);
    }

    return response.json();
  },
};
