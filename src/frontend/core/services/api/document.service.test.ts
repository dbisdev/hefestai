/**
 * Document Service Unit Tests
 * Tests for document and RAG search operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { documentService } from './document.service';
import { httpClient } from './client';
import { tokenService } from '@core/services/storage/token.service';

vi.mock('./client', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

vi.mock('@core/services/storage/token.service', () => ({
  tokenService: {
    getAccessToken: vi.fn(),
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
    it('uploads a manual with FormData', async () => {
      vi.mocked(tokenService.getAccessToken).mockReturnValue('test-token');
      
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          manualId: 'new-manual',
          title: 'Uploaded Manual',
          pageCount: 10,
          chunkCount: 50,
          processingTimeMs: 1000,
        }),
      });
      global.fetch = mockFetch;
      
      const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
      const result = await documentService.uploadManual({
        gameSystemId: 'system-1',
        file,
        title: 'Custom Title',
        sourceType: 'Rulebook',
        version: '1.0',
      });
      
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/documents/game-systems/system-1/manuals'),
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            Authorization: 'Bearer test-token',
          }),
        })
      );
      expect(result.manualId).toBe('new-manual');
    });

    it('uploads without auth token when not available', async () => {
      vi.mocked(tokenService.getAccessToken).mockReturnValue(null);
      
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({ manualId: 'new-manual' }),
      });
      global.fetch = mockFetch;
      
      const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
      await documentService.uploadManual({
        gameSystemId: 'system-1',
        file,
      });
      
      const callArgs = mockFetch.mock.calls[0][1];
      expect(callArgs.headers).not.toHaveProperty('Authorization');
    });

    it('throws on upload failure', async () => {
      vi.mocked(tokenService.getAccessToken).mockReturnValue('token');
      
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 400,
        text: async () => 'Bad request',
      });
      global.fetch = mockFetch;
      
      const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
      
      await expect(documentService.uploadManual({
        gameSystemId: 'system-1',
        file,
      })).rejects.toThrow();
    });
  });

  describe('generateMissingEmbeddings', () => {
    it('generates missing embeddings with defaults', async () => {
      const mockResult = {
        totalProcessed: 10,
        successCount: 8,
        failureCount: 2,
        errors: [],
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResult);
      
      const result = await documentService.generateMissingEmbeddings();
      
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
      
      const result = await documentService.generateMissingEmbeddings({
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
