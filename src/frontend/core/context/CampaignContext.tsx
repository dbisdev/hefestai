/**
 * Campaign Context
 * Single Responsibility: Global campaign state management
 * Manages the active campaign and provides campaign data to the application
 */

import React, { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react';
import { campaignService } from '../services/api';
import { useAuth } from './AuthContext';
import type { Campaign, CampaignDetail, CampaignRole, UpdateCampaignInput, UpdateCampaignStatusInput } from '../types';

interface CampaignState {
  /** List of all campaigns the user is a member of */
  campaigns: Campaign[];
  /** Currently active/selected campaign */
  activeCampaign: CampaignDetail | null;
  /** Loading state for campaign operations */
  isLoading: boolean;
  /** Error message if any */
  error: string | null;
}

interface CampaignContextValue extends CampaignState {
  /** Fetch all campaigns for the current user */
  fetchCampaigns: () => Promise<void>;
  /** Select a campaign as active */
  selectCampaign: (campaignId: string) => Promise<void>;
  /** Clear the active campaign selection */
  clearActiveCampaign: () => void;
  /** Create a new campaign */
  createCampaign: (name: string, description?: string, gameSystemId?: string) => Promise<CampaignDetail>;
  /** Join a campaign by code */
  joinCampaign: (joinCode: string) => Promise<Campaign>;
  /** Leave a campaign */
  leaveCampaign: (campaignId: string) => Promise<void>;
  /** Delete a campaign */
  deleteCampaign: (campaignId: string) => Promise<void>;
  /** Update campaign details (Master only) */
  updateCampaign: (campaignId: string, input: UpdateCampaignInput) => Promise<CampaignDetail>;
  /** Update campaign status - activate/deactivate (Master only) */
  updateCampaignStatus: (campaignId: string, isActive: boolean) => Promise<void>;
  /** Regenerate campaign join code (Master only) */
  regenerateJoinCode: (campaignId: string) => Promise<string>;
  /** Clear any error message */
  clearError: () => void;
  /** Check if user is master of active campaign */
  isActiveCampaignMaster: boolean;
  /** Get active campaign ID (convenience) */
  activeCampaignId: string | null;
}

const CampaignContext = createContext<CampaignContextValue | null>(null);

interface CampaignProviderProps {
  children: React.ReactNode;
}

// Default game system ID for new campaigns (should be fetched from backend in production)
const DEFAULT_GAME_SYSTEM_ID = '00000000-0000-0000-0000-000000000001';

export const CampaignProvider: React.FC<CampaignProviderProps> = ({ children }) => {
  const { user, isAuthenticated, isLoading: isAuthLoading } = useAuth();
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [activeCampaign, setActiveCampaign] = useState<CampaignDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Fetch all campaigns the user is a member of
   */
  const fetchCampaigns = useCallback(async () => {
    if (!isAuthenticated) {
      setCampaigns([]);
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const data = await campaignService.getAll();
      setCampaigns(data);
      
      // Auto-select first campaign if none selected and user has campaigns
      if (!activeCampaign && data.length > 0) {
        const details = await campaignService.getById(data[0].id);
        setActiveCampaign(details);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al cargar campañas';
      setError(message);
      console.error('Failed to fetch campaigns:', err);
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated, activeCampaign]);

  /**
   * Select a campaign as the active campaign
   */
  const selectCampaign = useCallback(async (campaignId: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const details = await campaignService.getById(campaignId);
      setActiveCampaign(details);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al seleccionar campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Clear the active campaign selection
   */
  const clearActiveCampaign = useCallback(() => {
    setActiveCampaign(null);
  }, []);

  /**
   * Create a new campaign
   */
  const createCampaign = useCallback(async (
    name: string, 
    description?: string, 
    gameSystemId: string = DEFAULT_GAME_SYSTEM_ID
  ): Promise<CampaignDetail> => {
    setIsLoading(true);
    setError(null);
    try {
      const newCampaign = await campaignService.create({
        name,
        description,
        gameSystemId,
      });
      
      // Refresh campaigns list and set new campaign as active
      const updatedCampaigns = await campaignService.getAll();
      setCampaigns(updatedCampaigns);
      setActiveCampaign(newCampaign);
      
      return newCampaign;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al crear campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Join a campaign using a join code
   */
  const joinCampaign = useCallback(async (joinCode: string): Promise<Campaign> => {
    setIsLoading(true);
    setError(null);
    try {
      const campaign = await campaignService.joinByCode({ joinCode });
      
      // Refresh campaigns list
      const updatedCampaigns = await campaignService.getAll();
      setCampaigns(updatedCampaigns);
      
      // Select the joined campaign as active
      const details = await campaignService.getById(campaign.id);
      setActiveCampaign(details);
      
      return campaign;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al unirse a la campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Leave a campaign
   */
  const leaveCampaign = useCallback(async (campaignId: string) => {
    setIsLoading(true);
    setError(null);
    try {
      await campaignService.leave(campaignId);
      
      // Clear active campaign if it was the one being left
      if (activeCampaign?.id === campaignId) {
        setActiveCampaign(null);
      }
      
      // Refresh campaigns list
      const updatedCampaigns = await campaignService.getAll();
      setCampaigns(updatedCampaigns);
      
      // Auto-select another campaign if available
      if (updatedCampaigns.length > 0 && !activeCampaign) {
        const details = await campaignService.getById(updatedCampaigns[0].id);
        setActiveCampaign(details);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al abandonar campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [activeCampaign]);

  /**
   * Delete a campaign (owner only)
   */
  const deleteCampaign = useCallback(async (campaignId: string) => {
    setIsLoading(true);
    setError(null);
    try {
      await campaignService.delete(campaignId);
      
      // Clear active campaign if it was the one being deleted
      if (activeCampaign?.id === campaignId) {
        setActiveCampaign(null);
      }
      
      // Refresh campaigns list
      const updatedCampaigns = await campaignService.getAll();
      setCampaigns(updatedCampaigns);
      
      // Auto-select another campaign if available
      if (updatedCampaigns.length > 0 && activeCampaign?.id === campaignId) {
        const details = await campaignService.getById(updatedCampaigns[0].id);
        setActiveCampaign(details);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al eliminar campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [activeCampaign]);

  /**
   * Update campaign details (Master only)
   */
  const updateCampaign = useCallback(async (
    campaignId: string, 
    input: UpdateCampaignInput
  ): Promise<CampaignDetail> => {
    setIsLoading(true);
    setError(null);
    try {
      const updatedCampaign = await campaignService.update(campaignId, input);
      
      // Update active campaign if it's the one being updated
      if (activeCampaign?.id === campaignId) {
        setActiveCampaign(updatedCampaign);
      }
      
      // Refresh campaigns list
      const updatedCampaigns = await campaignService.getAll();
      setCampaigns(updatedCampaigns);
      
      return updatedCampaign;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al actualizar campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [activeCampaign]);

  /**
   * Update campaign status - activate or deactivate (Master only)
   */
  const updateCampaignStatus = useCallback(async (
    campaignId: string, 
    isActive: boolean
  ): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await campaignService.updateStatus(campaignId, { isActive });
      
      // Refresh campaign details if it's the active one
      if (activeCampaign?.id === campaignId) {
        const details = await campaignService.getById(campaignId);
        setActiveCampaign(details);
      }
      
      // Refresh campaigns list
      const updatedCampaigns = await campaignService.getAll();
      setCampaigns(updatedCampaigns);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al cambiar estado de campaña';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [activeCampaign]);

  /**
   * Regenerate campaign join code (Master only)
   */
  const regenerateJoinCode = useCallback(async (campaignId: string): Promise<string> => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await campaignService.regenerateJoinCode(campaignId);
      
      // Refresh campaign details to get new join code
      if (activeCampaign?.id === campaignId) {
        const details = await campaignService.getById(campaignId);
        setActiveCampaign(details);
      }
      
      return response.joinCode;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al regenerar código';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [activeCampaign]);

  /**
   * Clear error message
   */
  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Fetch campaigns when user authenticates (wait for auth to finish loading first)
  useEffect(() => {
    // Don't fetch while auth is still loading - wait for verification to complete
    if (isAuthLoading) {
      return;
    }
    
    if (isAuthenticated) {
      fetchCampaigns();
    } else {
      setCampaigns([]);
      setActiveCampaign(null);
    }
  }, [isAuthenticated, isAuthLoading]);

  // Derived values
  const isActiveCampaignMaster = activeCampaign?.userRole === 1; // CampaignRole.Master = 1
  const activeCampaignId = activeCampaign?.id ?? null;

  const value = useMemo<CampaignContextValue>(() => ({
    campaigns,
    activeCampaign,
    isLoading,
    error,
    fetchCampaigns,
    selectCampaign,
    clearActiveCampaign,
    createCampaign,
    joinCampaign,
    leaveCampaign,
    deleteCampaign,
    updateCampaign,
    updateCampaignStatus,
    regenerateJoinCode,
    clearError,
    isActiveCampaignMaster,
    activeCampaignId,
  }), [
    campaigns,
    activeCampaign,
    isLoading,
    error,
    fetchCampaigns,
    selectCampaign,
    clearActiveCampaign,
    createCampaign,
    joinCampaign,
    leaveCampaign,
    deleteCampaign,
    updateCampaign,
    updateCampaignStatus,
    regenerateJoinCode,
    clearError,
    isActiveCampaignMaster,
    activeCampaignId,
  ]);

  return (
    <CampaignContext.Provider value={value}>
      {children}
    </CampaignContext.Provider>
  );
};

/**
 * Hook to access campaign context
 * @throws Error if used outside CampaignProvider
 */
export function useCampaign(): CampaignContextValue {
  const context = useContext(CampaignContext);
  if (!context) {
    throw new Error('useCampaign must be used within a CampaignProvider');
  }
  return context;
}

/**
 * Hook to get active campaign (convenience wrapper)
 */
export function useActiveCampaign(): CampaignDetail | null {
  const { activeCampaign } = useCampaign();
  return activeCampaign;
}

/**
 * Hook to get active campaign ID (convenience wrapper)
 */
export function useActiveCampaignId(): string | null {
  const { activeCampaignId } = useCampaign();
  return activeCampaignId;
}
