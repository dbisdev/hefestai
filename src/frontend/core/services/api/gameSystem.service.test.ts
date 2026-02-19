/**
 * Game System Service Unit Tests
 * Tests for game system CRUD operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { gameSystemService } from './gameSystem.service';
import { httpClient } from './client';

vi.mock('./client', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
  },
}));

describe('Game System Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getAll', () => {
    it('fetches all active game systems', async () => {
      const mockSystems = [
        { id: '1', name: 'System 1', code: 'SYS1' },
        { id: '2', name: 'System 2', code: 'SYS2' },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockSystems);
      
      const result = await gameSystemService.getAll();
      
      expect(httpClient.get).toHaveBeenCalledWith('/gamesystems');
      expect(result).toEqual(mockSystems);
    });
  });

  describe('getById', () => {
    it('fetches game system by ID', async () => {
      const mockSystem = { id: '1', name: 'Test System', code: 'TEST' };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockSystem);
      
      const result = await gameSystemService.getById('1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/gamesystems/1');
      expect(result).toEqual(mockSystem);
    });
  });

  describe('getByCode', () => {
    it('fetches game system by code', async () => {
      const mockSystem = { id: '1', name: 'Test System', code: 'TEST' };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockSystem);
      
      const result = await gameSystemService.getByCode('TEST');
      
      expect(httpClient.get).toHaveBeenCalledWith('/gamesystems/by-code/TEST');
      expect(result).toEqual(mockSystem);
    });
  });

  describe('create', () => {
    it('creates a new game system', async () => {
      const mockSystem = { id: 'new-id', name: 'New System', code: 'NEW' };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockSystem);
      
      const result = await gameSystemService.create({
        name: 'New System',
        code: 'NEW',
        description: 'A new game system',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/gamesystems', {
        name: 'New System',
        code: 'NEW',
        description: 'A new game system',
      });
      expect(result).toEqual(mockSystem);
    });
  });

  describe('update', () => {
    it('updates a game system', async () => {
      const mockSystem = { id: '1', name: 'Updated System' };
      vi.mocked(httpClient.put).mockResolvedValueOnce(mockSystem);
      
      const result = await gameSystemService.update('1', {
        name: 'Updated System',
        description: 'Updated description',
      });
      
      expect(httpClient.put).toHaveBeenCalledWith('/gamesystems/1', {
        name: 'Updated System',
        description: 'Updated description',
      });
      expect(result).toEqual(mockSystem);
    });
  });

  describe('updateStatus', () => {
    it('activates a game system', async () => {
      const mockSystem = { id: '1', isActive: true };
      vi.mocked(httpClient.patch).mockResolvedValueOnce(mockSystem);
      
      const result = await gameSystemService.updateStatus('1', { isActive: true });
      
      expect(httpClient.patch).toHaveBeenCalledWith('/gamesystems/1/status', {
        isActive: true,
      });
      expect(result).toEqual(mockSystem);
    });

    it('deactivates a game system', async () => {
      const mockSystem = { id: '1', isActive: false };
      vi.mocked(httpClient.patch).mockResolvedValueOnce(mockSystem);
      
      const result = await gameSystemService.updateStatus('1', { isActive: false });
      
      expect(httpClient.patch).toHaveBeenCalledWith('/gamesystems/1/status', {
        isActive: false,
      });
      expect(result).toEqual(mockSystem);
    });
  });
});
