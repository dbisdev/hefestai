/**
 * Entity Service Unit Tests
 * Tests for LoreEntity CRUD operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { entityService } from './entity.service';
import { httpClient } from './client';

vi.mock('./client', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Entity Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getByCampaignPaginated', () => {
    it('fetches paginated entities with default options', async () => {
      const mockResult = {
        items: [{ id: '1', name: 'Entity 1' }],
        totalCount: 1,
        pageNumber: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await entityService.getByCampaignPaginated('campaign-1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/campaigns/campaign-1/entities');
      expect(result).toEqual(mockResult);
    });

    it('includes query parameters for filters', async () => {
      const mockResult = { items: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      await entityService.getByCampaignPaginated('campaign-1', {
        entityType: 'character',
        visibility: 2,
        search: 'test',
        page: 1,
        pageSize: 10,
      });
      
      const callUrl = vi.mocked(httpClient.get).mock.calls[0][0];
      expect(callUrl).toContain('entityType=character');
      expect(callUrl).toContain('visibility=2');
      expect(callUrl).toContain('search=test');
      expect(callUrl).toContain('page=1');
      expect(callUrl).toContain('pageSize=10');
    });

    it('limits pageSize to 100', async () => {
      const mockResult = { items: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      await entityService.getByCampaignPaginated('campaign-1', {
        pageSize: 200,
      });
      
      const callUrl = vi.mocked(httpClient.get).mock.calls[0][0];
      expect(callUrl).toContain('pageSize=100');
    });
  });

  describe('getByCampaign', () => {
    it('fetches all entities for a campaign', async () => {
      const mockResult = {
        items: [{ id: '1' }, { id: '2' }],
        totalCount: 2,
        pageNumber: null,
        totalPages: null,
        hasNextPage: false,
        hasPreviousPage: false,
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      const result = await entityService.getByCampaign('campaign-1');
      
      expect(result).toEqual(mockResult.items);
    });

    it('passes entity type filter', async () => {
      const mockResult = { items: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      await entityService.getByCampaign('campaign-1', 'character');
      
      const callUrl = vi.mocked(httpClient.get).mock.calls[0][0];
      expect(callUrl).toContain('entityType=character');
    });

    it('passes visibility filter', async () => {
      const mockResult = { items: [], totalCount: 0 };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockResult);
      
      await entityService.getByCampaign('campaign-1', undefined, 2);
      
      const callUrl = vi.mocked(httpClient.get).mock.calls[0][0];
      expect(callUrl).toContain('visibility=2');
    });
  });

  describe('getById', () => {
    it('fetches entity by ID', async () => {
      const mockEntity = { id: 'entity-1', name: 'Test Entity' };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockEntity);
      
      const result = await entityService.getById('campaign-1', 'entity-1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/campaigns/campaign-1/entities/entity-1');
      expect(result).toEqual(mockEntity);
    });
  });

  describe('create', () => {
    it('creates a new entity', async () => {
      const mockEntity = { id: 'new-id', name: 'New Entity' };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockEntity);
      
      const result = await entityService.create('campaign-1', {
        name: 'New Entity',
        entityType: 'character',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/campaigns/campaign-1/entities', {
        name: 'New Entity',
        entityType: 'character',
      });
      expect(result).toEqual(mockEntity);
    });
  });

  describe('update', () => {
    it('updates an entity', async () => {
      const mockEntity = { id: 'entity-1', name: 'Updated' };
      vi.mocked(httpClient.put).mockResolvedValueOnce(mockEntity);
      
      const result = await entityService.update('campaign-1', 'entity-1', {
        name: 'Updated',
      });
      
      expect(httpClient.put).toHaveBeenCalledWith('/campaigns/campaign-1/entities/entity-1', {
        name: 'Updated',
      });
      expect(result).toEqual(mockEntity);
    });
  });

  describe('delete', () => {
    it('deletes an entity', async () => {
      vi.mocked(httpClient.delete).mockResolvedValueOnce(undefined);
      
      await entityService.delete('campaign-1', 'entity-1');
      
      expect(httpClient.delete).toHaveBeenCalledWith('/campaigns/campaign-1/entities/entity-1');
    });
  });

  describe('changeVisibility', () => {
    it('changes entity visibility', async () => {
      const mockEntity = { id: 'entity-1', visibility: 1 };
      vi.mocked(httpClient.patch).mockResolvedValueOnce(mockEntity);
      
      const result = await entityService.changeVisibility('campaign-1', 'entity-1', {
        visibility: 1,
      });
      
      expect(httpClient.patch).toHaveBeenCalledWith(
        '/campaigns/campaign-1/entities/entity-1/visibility',
        { visibility: 1 }
      );
      expect(result).toEqual(mockEntity);
    });
  });

  describe('transferOwnership', () => {
    it('transfers entity ownership', async () => {
      const mockEntity = { id: 'entity-1', ownerId: 'new-owner' };
      vi.mocked(httpClient.patch).mockResolvedValueOnce(mockEntity);
      
      const result = await entityService.transferOwnership('campaign-1', 'entity-1', {
        newOwnerId: 'new-owner',
      });
      
      expect(httpClient.patch).toHaveBeenCalledWith(
        '/campaigns/campaign-1/entities/entity-1/owner',
        { newOwnerId: 'new-owner' }
      );
      expect(result).toEqual(mockEntity);
    });
  });

  describe('deprecated methods', () => {
    it('getAll returns empty array with warning', async () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      
      const result = await entityService.getAll();
      
      expect(result).toEqual([]);
      expect(consoleSpy).toHaveBeenCalledWith(
        'entityService.getAll() is deprecated. Use getByCampaign(campaignId) instead.'
      );
      
      consoleSpy.mockRestore();
    });

    it('getByIdLegacy throws error', async () => {
      await expect(entityService.getByIdLegacy('entity-1')).rejects.toThrow(
        'Entity operations require a campaign context'
      );
    });
  });
});
