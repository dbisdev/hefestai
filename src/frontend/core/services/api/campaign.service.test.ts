/**
 * Campaign Service Unit Tests
 * Tests for campaign CRUD and membership operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { campaignService } from './campaign.service';
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

describe('Campaign Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getAll', () => {
    it('fetches all campaigns', async () => {
      const mockCampaigns = [
        { id: '1', name: 'Campaign 1' },
        { id: '2', name: 'Campaign 2' },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockCampaigns);
      
      const result = await campaignService.getAll();
      
      expect(httpClient.get).toHaveBeenCalledWith('/campaigns');
      expect(result).toEqual(mockCampaigns);
    });
  });

  describe('getById', () => {
    it('fetches campaign by ID', async () => {
      const mockCampaign = { id: '1', name: 'Test Campaign' };
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockCampaign);
      
      const result = await campaignService.getById('1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/campaigns/1');
      expect(result).toEqual(mockCampaign);
    });
  });

  describe('create', () => {
    it('creates a new campaign', async () => {
      const mockCreatedCampaign = {
        id: 'new-id',
        name: 'New Campaign',
        description: 'Description',
      };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockCreatedCampaign);
      
      const result = await campaignService.create({
        name: 'New Campaign',
        description: 'Description',
        gameSystemId: 'system-1',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/campaigns', {
        name: 'New Campaign',
        description: 'Description',
        gameSystemId: 'system-1',
      });
      expect(result).toEqual(mockCreatedCampaign);
    });
  });

  describe('update', () => {
    it('updates campaign details', async () => {
      const mockUpdatedCampaign = {
        id: '1',
        name: 'Updated Campaign',
      };
      vi.mocked(httpClient.put).mockResolvedValueOnce(mockUpdatedCampaign);
      
      const result = await campaignService.update('1', {
        name: 'Updated Campaign',
        description: 'Updated description',
      });
      
      expect(httpClient.put).toHaveBeenCalledWith('/campaigns/1', {
        name: 'Updated Campaign',
        description: 'Updated description',
      });
      expect(result).toEqual(mockUpdatedCampaign);
    });
  });

  describe('delete', () => {
    it('deletes a campaign', async () => {
      vi.mocked(httpClient.delete).mockResolvedValueOnce(undefined);
      
      await campaignService.delete('1');
      
      expect(httpClient.delete).toHaveBeenCalledWith('/campaigns/1');
    });
  });

  describe('joinByCode', () => {
    it('joins campaign with code', async () => {
      const mockCampaign = { id: '1', name: 'Joined Campaign' };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockCampaign);
      
      const result = await campaignService.joinByCode({ joinCode: 'ABC123' });
      
      expect(httpClient.post).toHaveBeenCalledWith('/campaigns/join', { joinCode: 'ABC123' });
      expect(result).toEqual(mockCampaign);
    });
  });

  describe('leave', () => {
    it('leaves a campaign', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(undefined);
      
      await campaignService.leave('1');
      
      expect(httpClient.post).toHaveBeenCalledWith('/campaigns/1/leave', {});
    });
  });

  describe('getMembers', () => {
    it('fetches campaign members', async () => {
      const mockMembers = [
        { id: 'member-1', userId: 'user-1', role: 1 },
        { id: 'member-2', userId: 'user-2', role: 0 },
      ];
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockMembers);
      
      const result = await campaignService.getMembers('1');
      
      expect(httpClient.get).toHaveBeenCalledWith('/campaigns/1/members');
      expect(result).toEqual(mockMembers);
    });
  });

  describe('updateMemberRole', () => {
    it('updates member role', async () => {
      const mockUpdatedMember = { id: 'member-1', role: 1 };
      vi.mocked(httpClient.patch).mockResolvedValueOnce(mockUpdatedMember);
      
      const result = await campaignService.updateMemberRole('1', 'member-1', { role: 1 });
      
      expect(httpClient.patch).toHaveBeenCalledWith(
        '/campaigns/1/members/member-1/role',
        { role: 1 }
      );
      expect(result).toEqual(mockUpdatedMember);
    });
  });

  describe('removeMember', () => {
    it('removes a member from campaign', async () => {
      vi.mocked(httpClient.delete).mockResolvedValueOnce(undefined);
      
      await campaignService.removeMember('1', 'member-1');
      
      expect(httpClient.delete).toHaveBeenCalledWith('/campaigns/1/members/member-1');
    });
  });

  describe('regenerateJoinCode', () => {
    it('regenerates join code', async () => {
      const mockResponse = { joinCode: 'NEW-CODE' };
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockResponse);
      
      const result = await campaignService.regenerateJoinCode('1');
      
      expect(httpClient.post).toHaveBeenCalledWith('/campaigns/1/regenerate-code', {});
      expect(result).toEqual(mockResponse);
    });
  });

  describe('updateStatus', () => {
    it('updates campaign status', async () => {
      const mockCampaign = { id: '1', isActive: false };
      vi.mocked(httpClient.patch).mockResolvedValueOnce(mockCampaign);
      
      const result = await campaignService.updateStatus('1', { isActive: false });
      
      expect(httpClient.patch).toHaveBeenCalledWith('/campaigns/1/status', { isActive: false });
      expect(result).toEqual(mockCampaign);
    });
  });
});
