/**
 * Admin Service Unit Tests
 * Tests for admin user and campaign management operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { adminUserService, adminCampaignService } from './admin.service';
import { httpClient } from './client';
import { AdminUserRole } from '@core/types';

vi.mock('./client', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Admin User Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getAll', () => {
    it('fetches all active users', async () => {
      const mockUsers = [
        { id: '1', email: 'user1@example.com', role: 'Player' },
        { id: '2', email: 'user2@example.com', role: 'Master' },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockUsers);
      
      const result = await adminUserService.getAll();
      
      expect(httpClient.get).toHaveBeenCalledWith('/admin/users');
      expect(result).toEqual(mockUsers);
    });

    it('includes inactive users when requested', async () => {
      const mockUsers = [
        { id: '1', email: 'active@example.com' },
        { id: '2', email: 'inactive@example.com', isActive: false },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockUsers);
      
      const result = await adminUserService.getAll(true);
      
      expect(httpClient.get).toHaveBeenCalledWith('/admin/users?includeInactive=true');
      expect(result).toEqual(mockUsers);
    });
  });

  describe('getById', () => {
    it('fetches user by ID', async () => {
      const mockUser = {
        id: 'user-1',
        email: 'test@example.com',
        role: 'Player',
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockUser);
      
      const result = await adminUserService.getById('user-1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/admin/users/user-1');
      expect(result).toEqual(mockUser);
    });
  });

  describe('create', () => {
    it('creates a new user', async () => {
      const mockCreatedUser = {
        id: 'new-user',
        email: 'new@example.com',
        role: AdminUserRole.Player,
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockCreatedUser);
      
      const result = await adminUserService.create({
        email: 'new@example.com',
        password: 'Password123',
        displayName: 'New User',
        role: AdminUserRole.Player,
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/admin/users', {
        email: 'new@example.com',
        password: 'Password123',
        displayName: 'New User',
        role: AdminUserRole.Player,
      });
      expect(result).toEqual(mockCreatedUser);
    });
  });

  describe('update', () => {
    it('updates a user', async () => {
      const mockUpdatedUser = {
        id: 'user-1',
        displayName: 'Updated Name',
      };
      vi.mocked(httpClient.put).mockResolvedValueOnce(mockUpdatedUser);
      
      const result = await adminUserService.update('user-1', {
        displayName: 'Updated Name',
        role: AdminUserRole.Master,
      });
      
      expect(httpClient.put).toHaveBeenCalledWith('/admin/users/user-1', {
        displayName: 'Updated Name',
        role: AdminUserRole.Master,
      });
      expect(result).toEqual(mockUpdatedUser);
    });
  });

  describe('delete', () => {
    it('deletes a user (soft delete)', async () => {
      vi.mocked(httpClient.delete).mockResolvedValueOnce(undefined);
      
      await adminUserService.delete('user-1');
      
      expect(httpClient.delete).toHaveBeenCalledWith('/admin/users/user-1');
    });
  });
});

describe('Admin Campaign Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getAll', () => {
    it('fetches all active campaigns', async () => {
      const mockCampaigns = [
        { id: '1', name: 'Campaign 1' },
        { id: '2', name: 'Campaign 2' },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockCampaigns);
      
      const result = await adminCampaignService.getAll();
      
      expect(httpClient.get).toHaveBeenCalledWith('/admin/campaigns');
      expect(result).toEqual(mockCampaigns);
    });

    it('includes inactive campaigns when requested', async () => {
      const mockCampaigns = [
        { id: '1', name: 'Active Campaign' },
        { id: '2', name: 'Inactive Campaign', isActive: false },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockCampaigns);
      
      const result = await adminCampaignService.getAll(true);
      
      expect(httpClient.get).toHaveBeenCalledWith('/admin/campaigns?includeInactive=true');
      expect(result).toEqual(mockCampaigns);
    });
  });

  describe('getById', () => {
    it('fetches campaign by ID', async () => {
      const mockCampaign = {
        id: 'campaign-1',
        name: 'Test Campaign',
        ownerId: 'owner-1',
      };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockCampaign);
      
      const result = await adminCampaignService.getById('campaign-1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/admin/campaigns/campaign-1');
      expect(result).toEqual(mockCampaign);
    });
  });

  describe('update', () => {
    it('updates a campaign', async () => {
      const mockUpdatedCampaign = {
        id: 'campaign-1',
        name: 'Updated Campaign',
      };
      vi.mocked(httpClient.put).mockResolvedValueOnce(mockUpdatedCampaign);
      
      const result = await adminCampaignService.update('campaign-1', {
        name: 'Updated Campaign',
        description: 'New description',
      });
      
      expect(httpClient.put).toHaveBeenCalledWith('/admin/campaigns/campaign-1', {
        name: 'Updated Campaign',
        description: 'New description',
      });
      expect(result).toEqual(mockUpdatedCampaign);
    });
  });

  describe('delete', () => {
    it('deletes a campaign (soft delete)', async () => {
      vi.mocked(httpClient.delete).mockResolvedValueOnce(undefined);
      
      await adminCampaignService.delete('campaign-1');
      
      expect(httpClient.delete).toHaveBeenCalledWith('/admin/campaigns/campaign-1');
    });
  });
});
