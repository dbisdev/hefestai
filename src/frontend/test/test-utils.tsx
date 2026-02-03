/**
 * Test Utilities
 * Common test helpers and custom render function with providers
 */
import React, { ReactElement, ReactNode } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

/**
 * Mock providers wrapper for tests
 * Add your context providers here
 */
interface WrapperProps {
  children: ReactNode;
}

const AllTheProviders = ({ children }: WrapperProps) => {
  return <>{children}</>;
};

/**
 * Custom render function that wraps component with all providers
 */
const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) => {
  return {
    user: userEvent.setup(),
    ...render(ui, { wrapper: AllTheProviders, ...options }),
  };
};

// Re-export everything from testing-library
export * from '@testing-library/react';

// Override render with custom render
export { customRender as render };

/**
 * Create a mock user for testing
 */
export const createMockUser = (overrides = {}) => ({
  id: 'user-123',
  username: 'testuser',
  email: 'test@example.com',
  role: 'PLAYER' as const,
  ...overrides,
});

/**
 * Create a mock campaign for testing
 */
export const createMockCampaign = (overrides = {}) => ({
  id: 'campaign-123',
  name: 'Test Campaign',
  description: 'A test campaign',
  joinCode: 'ABC123',
  userRole: 'Master' as const,
  ...overrides,
});

/**
 * Create a mock entity for testing
 */
export const createMockEntity = (overrides = {}) => ({
  id: 'entity-123',
  name: 'Test Entity',
  description: 'A test entity description',
  entityType: 'character' as const,
  visibility: 2,
  imageUrl: 'https://example.com/image.jpg',
  attributes: {},
  campaignId: 'campaign-123',
  ownerId: 'user-123',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
});

/**
 * Wait for async operations to complete
 */
export const waitForAsync = () => new Promise((resolve) => setTimeout(resolve, 0));
