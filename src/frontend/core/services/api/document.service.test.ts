/**
 * Document Service Unit Tests
 * Tests for document and RAG search operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { documentService } from './document.service';
import { adminDocumentService } from './adminDocument.service';
import { httpClient } from './client';

vi.mock('./client', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    postForm: vi.fn(),
  },
}));

describe('Document Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('semanticSearch', () => {
    it('performs semantic search with default parameters', async () => {
      const mockResult = {
        documents: [{ document: { id: '1' }, similarityScore: 0.9 }],
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await documentService.semanticSearch({ query: 'test query' });
      
      expect(httpClient.post).toHaveBeenCalledWith('/documents/search', {
        query: 'test query',
        limit: 5,
        threshold: 0.7,
        projectId: undefined,
        gameSystemId: undefined,
        generateAnswer: false,
        systemPrompt: undefined,
        masterId: undefined,
      });
      expect(result).toEqual(mockResult);
    });

    it('performs semantic search with all parameters', async () => {
      const mockResult = {
        documents: [],
        generatedAnswer: 'AI answer',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await documentService.semanticSearch({
        query: 'test',
        limit: 10,
        threshold: 0.8,
        projectId: 'project-1',
        gameSystemId: 'system-1',
        generateAnswer: true,
        systemPrompt: 'You are a helpful assistant',
        masterId: 'master-1',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/documents/search', {
        query: 'test',
        limit: 10,
        threshold: 0.8,
        projectId: 'project-1',
        gameSystemId: 'system-1',
        generateAnswer: true,
        systemPrompt: 'You are a helpful assistant',
        masterId: 'master-1',
      });
      expect(result).toEqual(mockResult);
    });
  });

  describe('getManual', () => {
    it('fetches a manual by ID', async () => {
      const mockManual = {
        id: 'manual-1',
        gameSystemId: 'system-1',
        title: 'Test Manual',
        pageCount: 100,
        chunkCount: 50,
        sourceType: 'Rulebook' as const,
        createdAt: '2024-01-01',
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockManual);
      
      const result = await documentService.getManual('system-1', 'manual-1');
      
      expect(httpClient.get).toHaveBeenCalledWith(
        '/documents/game-systems/system-1/manuals/manual-1'
      );
      expect(result).toEqual(mockManual);
    });
  });

  describe('getManualsByGameSystem', () => {
    it('fetches all manuals for a game system', async () => {
      const mockManuals = [
        { id: '1', title: 'Manual 1', chunkCount: 10 },
        { id: '2', title: 'Manual 2', chunkCount: 20 },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockManuals);
      
      const result = await documentService.getManualsByGameSystem('system-1');
      
      expect(httpClient.get).toHaveBeenCalledWith(
        '/documents/game-systems/system-1/manuals'
      );
      expect(result).toEqual(mockManuals);
    });
  });

  describe('uploadManual', () => {
    it('uploads a manual with FormData via httpClient.postForm', async () => {
      const mockResult = {
        manualId: 'new-manual',
        title: 'Uploaded Manual',
        pageCount: 10,
        chunkCount: 50,
        processingTimeMs: 1000,
      };
      vi.mocked(httpClient.postForm).mockResolvedValueOnce(mockResult);

      const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
      const result = await documentService.uploadManual({
        gameSystemId: 'system-1',
        file,
        title: 'Custom Title',
        sourceType: 'Rulebook',
        version: '1.0',
      });

      expect(httpClient.postForm).toHaveBeenCalledWith(
        '/documents/game-systems/system-1/manuals',
        expect.any(FormData),
      );

      // Verify FormData fields were appended
      const formData: FormData = vi.mocked(httpClient.postForm).mock.calls[0][1];
      expect(formData.get('File')).toEqual(file);
      expect(formData.get('Title')).toBe('Custom Title');
      expect(formData.get('SourceType')).toBe('Rulebook');
      expect(formData.get('Version')).toBe('1.0');

      expect(result).toEqual(mockResult);
    });

    it('uploads with only required fields (no optional metadata)', async () => {
      vi.mocked(httpClient.postForm).mockResolvedValueOnce({ manualId: 'new-manual' });

      const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
      await documentService.uploadManual({ gameSystemId: 'system-1', file });

      const formData: FormData = vi.mocked(httpClient.postForm).mock.calls[0][1];
      expect(formData.get('File')).toEqual(file);
      expect(formData.get('Title')).toBeNull();
      expect(formData.get('SourceType')).toBeNull();
      expect(formData.get('Version')).toBeNull();
    });

    it('propagates error when httpClient.postForm rejects', async () => {
      vi.mocked(httpClient.postForm).mockRejectedValueOnce(new Error('Upload failed'));

      const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });

      await expect(
        documentService.uploadManual({ gameSystemId: 'system-1', file }),
      ).rejects.toThrow('Upload failed');
    });
  });

  describe('generateMissingEmbeddings', () => {
    // generateMissingEmbeddings was moved to adminDocumentService (admin-only operation).
    // These tests target the correct service.
    it('generates missing embeddings with defaults', async () => {
      const mockResult = {
        totalProcessed: 10,
        successCount: 8,
        failureCount: 2,
        errors: [],
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await adminDocumentService.generateMissingEmbeddings();
      
      expect(httpClient.post).toHaveBeenCalledWith('/documents/embeddings/generate', {
        batchSize: 10,
        maxDocuments: 100,
        gameSystemId: undefined,
      });
      expect(result).toEqual(mockResult);
    });

    it('generates missing embeddings with custom parameters', async () => {
      const mockResult = {
        totalProcessed: 5,
        successCount: 5,
        failureCount: 0,
        errors: [],
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await adminDocumentService.generateMissingEmbeddings({
        batchSize: 5,
        maxDocuments: 50,
        gameSystemId: 'system-1',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/documents/embeddings/generate', {
        batchSize: 5,
        maxDocuments: 50,
        gameSystemId: 'system-1',
      });
      expect(result).toEqual(mockResult);
    });
  });

  describe('checkDocumentAvailability', () => {
    it('checks document availability without masterId', async () => {
      const mockResult = {
        hasDocuments: true,
        gameSystemId: 'system-1',
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await documentService.checkDocumentAvailability('system-1');
      
      expect(httpClient.get).toHaveBeenCalledWith(
        '/documents/game-systems/system-1/available'
      );
      expect(result).toEqual(mockResult);
    });

    it('checks document availability with masterId', async () => {
      const mockResult = {
        hasDocuments: true,
        gameSystemId: 'system-1',
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await documentService.checkDocumentAvailability('system-1', 'master-1');
      
      expect(httpClient.get).toHaveBeenCalledWith(
        '/documents/game-systems/system-1/available?masterId=master-1'
      );
      expect(result).toEqual(mockResult);
    });
  });
});
