/**
 * Mock for CampaignContext
 * Provides a configurable mock for testing components that use useCampaign
 */
import React, { ReactNode } from 'react';
import { vi } from 'vitest';

// Default mock values
export const defaultMockCampaignContext = {
  campaigns: [],
  activeCampaign: null,
  activeCampaignId: null,
  isLoading: false,
  error: null,
  isActiveCampaignMaster: false,
  fetchCampaigns: vi.fn().mockResolvedValue(undefined),
  selectCampaign: vi.fn().mockResolvedValue(undefined),
  clearActiveCampaign: vi.fn(),
  createCampaign: vi.fn().mockResolvedValue({}),
  joinCampaign: vi.fn().mockResolvedValue({}),
  leaveCampaign: vi.fn().mockResolvedValue(undefined),
  deleteCampaign: vi.fn().mockResolvedValue(undefined),
  clearError: vi.fn(),
};

// Store for mock values (mutable for test configuration)
let mockCampaignContextValues = { ...defaultMockCampaignContext };

/**
 * Configure mock campaign context values for tests
 */
export const setMockCampaignContext = (overrides: Partial<typeof defaultMockCampaignContext>) => {
  mockCampaignContextValues = { ...defaultMockCampaignContext, ...overrides };
};

/**
 * Reset mock campaign context to defaults
 */
export const resetMockCampaignContext = () => {
  mockCampaignContextValues = { ...defaultMockCampaignContext };
};

/**
 * Mock useCampaign hook
 */
export const useCampaign = () => mockCampaignContextValues;

/**
 * Mock useActiveCampaign hook
 */
export const useActiveCampaign = () => mockCampaignContextValues.activeCampaign;

/**
 * Mock useActiveCampaignId hook
 */
export const useActiveCampaignId = () => mockCampaignContextValues.activeCampaignId;

/**
 * Mock CampaignProvider component
 */
export const CampaignProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  return <>{children}</>;
};
