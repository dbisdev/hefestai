/**
 * CampaignContext Unit Tests
 * Tests for global campaign state management
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { ReactNode } from 'react';
import { CampaignProvider, useCampaign, useActiveCampaign, useActiveCampaignId } from './CampaignContext';
import { campaignService } from '@core/services/api';
import { useAuth } from './AuthContext';
import type { Campaign, CampaignDetail, User } from '@core/types';

vi.mock('@core/services/api', () => ({
  campaignService: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    joinByCode: vi.fn(),
    leave: vi.fn(),
    updateStatus: vi.fn(),
    regenerateJoinCode: vi.fn(),
  },
}));

vi.mock('./AuthContext', () => ({
  useAuth: vi.fn(),
}));

const mockUser: User = {
  id: 'user-123',
  username: 'testuser',
  email: 'test@example.com',
  role: 'MASTER',
};

const mockCampaign: Campaign = {
  id: 'campaign-1',
  name: 'Test Campaign',
  joinCode: 'ABC123',
  gameSystemId: 'system-1',
  isActive: true,
  createdAt: '2024-01-01T00:00:00.000Z',
};

const mockCampaignDetail: CampaignDetail = {
  ...mockCampaign,
  description: 'Test description',
  userRole: 1,
  ownerId: 'user-123',
  memberCount: 1,
};

const wrapper = ({ children }: { children: ReactNode }) => (
  <CampaignProvider>{children}</CampaignProvider>
);

describe('CampaignContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuth).mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
      isLoading: false,
      error: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      clearError: vi.fn(),
      isMaster: true,
      isPlayer: false,
      isAdmin: false,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('CampaignProvider', () => {
    it('initializes with empty campaigns', () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([]);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      expect(result.current.campaigns).toEqual([]);
      expect(result.current.activeCampaign).toBeNull();
    });

    it('fetches campaigns when authenticated', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(campaignService.getAll).toHaveBeenCalled();
        expect(result.current.campaigns).toEqual([mockCampaign]);
      });
    });

    it('does not fetch campaigns when not authenticated', async () => {
      vi.mocked(useAuth).mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      renderHook(() => useCampaign(), { wrapper });
      
      expect(campaignService.getAll).not.toHaveBeenCalled();
    });

    it('waits for auth to finish loading', async () => {
      vi.mocked(useAuth).mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      renderHook(() => useCampaign(), { wrapper });
      
      expect(campaignService.getAll).not.toHaveBeenCalled();
    });

    it('auto-selects first campaign', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.activeCampaign).toEqual(mockCampaignDetail);
      });
    });
  });

  describe('fetchCampaigns', () => {
    it('fetches campaigns manually', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign, { ...mockCampaign, id: 'campaign-2' }]);
      
      await act(async () => {
        await result.current.fetchCampaigns();
      });
      
      expect(result.current.campaigns).toHaveLength(2);
    });
  });

  describe('selectCampaign', () => {
    it('selects a campaign as active', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      const newCampaign: CampaignDetail = {
        ...mockCampaignDetail,
        id: 'campaign-2',
        name: 'Another Campaign',
      };
      vi.mocked(campaignService.getById).mockResolvedValue(newCampaign);
      
      await act(async () => {
        await result.current.selectCampaign('campaign-2');
      });
      
      expect(result.current.activeCampaign).toEqual(newCampaign);
    });

    it('sets error on failure', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([]);
      vi.mocked(campaignService.getById).mockRejectedValue(new Error('Not found'));
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      await act(async () => {
        try {
          await result.current.selectCampaign('invalid-id');
        } catch (e) {
          // Expected
        }
      });
      
      expect(result.current.error).toBe('Not found');
    });
  });

  describe('clearActiveCampaign', () => {
    it('clears active campaign', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.activeCampaign).toEqual(mockCampaignDetail);
      });
      
      act(() => {
        result.current.clearActiveCampaign();
      });
      
      expect(result.current.activeCampaign).toBeNull();
    });
  });

  describe('createCampaign', () => {
    it('creates a campaign and sets as active', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([]);
      vi.mocked(campaignService.create).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      
      let createdCampaign;
      await act(async () => {
        createdCampaign = await result.current.createCampaign(
          'New Campaign',
          'Description',
          'system-1'
        );
      });
      
      expect(campaignService.create).toHaveBeenCalledWith({
        name: 'New Campaign',
        description: 'Description',
        gameSystemId: 'system-1',
      });
      expect(createdCampaign).toEqual(mockCampaignDetail);
      expect(result.current.activeCampaign).toEqual(mockCampaignDetail);
    });
  });

  describe('joinCampaign', () => {
    it('joins campaign by code', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([]);
      vi.mocked(campaignService.joinByCode).mockResolvedValue(mockCampaign);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      
      await act(async () => {
        await result.current.joinCampaign('JOIN-CODE');
      });
      
      expect(campaignService.joinByCode).toHaveBeenCalledWith({ joinCode: 'JOIN-CODE' });
      expect(result.current.activeCampaign).toEqual(mockCampaignDetail);
    });
  });

  describe('leaveCampaign', () => {
    it('leaves a campaign', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      vi.mocked(campaignService.leave).mockResolvedValue(undefined);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.activeCampaign).toEqual(mockCampaignDetail);
      });
      
      vi.mocked(campaignService.getAll).mockResolvedValue([]);
      
      await act(async () => {
        await result.current.leaveCampaign('campaign-1');
      });
      
      expect(campaignService.leave).toHaveBeenCalledWith('campaign-1');
      expect(result.current.activeCampaign).toBeNull();
    });
  });

  describe('deleteCampaign', () => {
    it('deletes a campaign', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      vi.mocked(campaignService.delete).mockResolvedValue(undefined);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.activeCampaign).toEqual(mockCampaignDetail);
      });
      
      vi.mocked(campaignService.getAll).mockResolvedValue([]);
      
      await act(async () => {
        await result.current.deleteCampaign('campaign-1');
      });
      
      expect(campaignService.delete).toHaveBeenCalledWith('campaign-1');
      expect(result.current.activeCampaign).toBeNull();
    });
  });

  describe('updateCampaign', () => {
    it('updates campaign details', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const updatedCampaign: CampaignDetail = {
        ...mockCampaignDetail,
        name: 'Updated Name',
      };
      vi.mocked(campaignService.update).mockResolvedValue(updatedCampaign);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      
      await act(async () => {
        await result.current.updateCampaign('campaign-1', { name: 'Updated Name' });
      });
      
      expect(campaignService.update).toHaveBeenCalledWith('campaign-1', { name: 'Updated Name' });
      expect(result.current.activeCampaign).toEqual(updatedCampaign);
    });
  });

  describe('updateCampaignStatus', () => {
    it('updates campaign status', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      vi.mocked(campaignService.updateStatus).mockResolvedValue(mockCampaign);
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue({
        ...mockCampaignDetail,
        isActive: false,
      });
      
      await act(async () => {
        await result.current.updateCampaignStatus('campaign-1', false);
      });
      
      expect(campaignService.updateStatus).toHaveBeenCalledWith('campaign-1', { isActive: false });
    });
  });

  describe('regenerateJoinCode', () => {
    it('regenerates join code', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      vi.mocked(campaignService.regenerateJoinCode).mockResolvedValue({ joinCode: 'NEW-CODE' });
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      let newCode;
      await act(async () => {
        newCode = await result.current.regenerateJoinCode('campaign-1');
      });
      
      expect(newCode).toBe('NEW-CODE');
      expect(campaignService.regenerateJoinCode).toHaveBeenCalledWith('campaign-1');
    });
  });

  describe('clearError', () => {
    it('clears error state', async () => {
      vi.mocked(campaignService.getAll).mockRejectedValue(new Error('Error'));
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.error).toBe('Error');
      });
      
      act(() => {
        result.current.clearError();
      });
      
      expect(result.current.error).toBeNull();
    });
  });

  describe('isActiveCampaignMaster', () => {
    it('returns true when user is master of active campaign', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue({
        ...mockCampaignDetail,
        userRole: 1,
      });
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.isActiveCampaignMaster).toBe(true);
      });
    });

    it('returns false when user is not master', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue({
        ...mockCampaignDetail,
        userRole: 0,
      });
      
      const { result } = renderHook(() => useCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.isActiveCampaignMaster).toBe(false);
      });
    });
  });

  describe('useActiveCampaign', () => {
    it('returns active campaign', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useActiveCampaign(), { wrapper });
      
      await waitFor(() => {
        expect(result.current).toEqual(mockCampaignDetail);
      });
    });
  });

  describe('useActiveCampaignId', () => {
    it('returns active campaign ID', async () => {
      vi.mocked(campaignService.getAll).mockResolvedValue([mockCampaign]);
      vi.mocked(campaignService.getById).mockResolvedValue(mockCampaignDetail);
      
      const { result } = renderHook(() => useActiveCampaignId(), { wrapper });
      
      await waitFor(() => {
        expect(result.current).toBe('campaign-1');
      });
    });
  });

  describe('useCampaign error handling', () => {
    it('throws when used outside provider', () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      
      expect(() => {
        renderHook(() => useCampaign());
      }).toThrow('useCampaign must be used within a CampaignProvider');
      
      consoleError.mockRestore();
    });
  });
});
