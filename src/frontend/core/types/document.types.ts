/**
 * Document Types
 * Types for document and RAG (Retrieval-Augmented Generation) operations.
 * Matches backend DTOs for the documents API.
 */

/**
 * Full document DTO returned by the backend.
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
 * A single semantic search hit with its similarity score.
 */
export interface DocumentSearchResult {
  document: DocumentDto;
  similarityScore: number;
}

/**
 * Parameters for a semantic (vector-similarity) search request.
 */
export interface SemanticSearchParams {
  /** The search query text. */
  query: string;
  /** Maximum number of results (default: 5). */
  limit?: number;
  /** Minimum similarity threshold 0.0–1.0 (default: 0.7). */
  threshold?: number;
  /** Optional project ID filter. */
  projectId?: string;
  /** Optional game system ID filter (for RAG on manuals). */
  gameSystemId?: string;
  /** Whether to generate a RAG answer from results. */
  generateAnswer?: boolean;
  /** Optional system prompt for answer generation. */
  systemPrompt?: string;
  /** Optional Master ID to include their documents (for Players in a campaign). */
  masterId?: string;
}

/**
 * Result of a semantic search request.
 */
export interface SemanticSearchResult {
  documents: DocumentSearchResult[];
  generatedAnswer?: string;
}

/**
 * Source type for a RAG manual document.
 */
export type RagSourceType = 'Rulebook' | 'Supplement' | 'Custom';

/**
 * Full manual DTO (includes page count).
 */
export interface ManualDto {
  id: string;
  gameSystemId: string;
  title: string;
  pageCount: number;
  chunkCount: number;
  sourceType: RagSourceType;
  version?: string;
  createdAt: string;
}

/**
 * Manual summary DTO for list views (omits page count).
 */
export interface ManualSummaryDto {
  id: string;
  gameSystemId: string;
  title: string;
  chunkCount: number;
  sourceType: RagSourceType;
  version?: string;
  createdAt: string;
}

/**
 * Result of uploading a PDF manual for RAG processing.
 */
export interface UploadManualResult {
  manualId: string;
  title: string;
  pageCount: number;
  chunkCount: number;
  processingTimeMs: number;
}

/**
 * Parameters for uploading a PDF manual.
 */
export interface UploadManualParams {
  /** The game system ID this manual belongs to. */
  gameSystemId: string;
  /** The PDF file to upload. */
  file: File;
  /** Optional title (extracted from PDF if not provided). */
  title?: string;
  /** Type of RAG source. */
  sourceType?: RagSourceType;
  /** Optional version identifier. */
  version?: string;
}

/**
 * Result of the availability check for RAG documents.
 */
export interface DocumentAvailabilityResult {
  /** Whether documents are available for RAG search. */
  hasDocuments: boolean;
  /** The game system ID that was checked. */
  gameSystemId: string;
}

/**
 * Parameters for the admin operation that backfills missing embeddings.
 */
export interface GenerateMissingEmbeddingsParams {
  /** Number of documents to process per batch (default: 10). */
  batchSize?: number;
  /** Maximum total documents to process (default: 100). */
  maxDocuments?: number;
  /** Optional game system ID to filter documents. */
  gameSystemId?: string;
}

/**
 * Result of the generate-missing-embeddings admin operation.
 */
export interface GenerateMissingEmbeddingsResult {
  totalProcessed: number;
  successCount: number;
  failureCount: number;
  errors: string[];
}
