/**
 * Mock for Entity Service
 * Provides configurable mocks for entity API operations
 */
import { vi } from 'vitest';
import type { LoreEntity } from '@core/types';
import { OwnershipType, VisibilityLevel } from '@core/types';

// Default mock entities
export const mockEntities: LoreEntity[] = [
  {
    id: 'entity-1',
    name: 'Test Character',
    description: 'A test character for testing',
    entityType: 'character',
    visibility: VisibilityLevel.Campaign,
    ownershipType: OwnershipType.Master,
    isTemplate: false,
    imageUrl: 'https://example.com/char.jpg',
    attributes: { strength: 10, dexterity: 12 },
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'entity-2',
    name: 'Test NPC',
    description: 'A test NPC',
    entityType: 'npc',
    visibility: VisibilityLevel.Campaign,
    ownershipType: OwnershipType.Master,
    isTemplate: false,
    imageUrl: 'https://example.com/npc.jpg',
    attributes: {},
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'entity-3',
    name: 'Test Enemy',
    description: 'A test enemy',
    entityType: 'enemy',
    visibility: VisibilityLevel.Campaign,
    ownershipType: OwnershipType.Master,
    isTemplate: false,
    imageUrl: undefined,
    attributes: { health: 100 },
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

// Store for mock responses
let mockEntityList = [...mockEntities];
let shouldFail = false;
let failMessage = 'Mock error';

/**
 * Configure mock entity responses
 */
export const setMockEntities = (entities: LoreEntity[]) => {
  mockEntityList = entities;
};

/**
 * Configure mock to fail
 */
export const setMockToFail = (fail: boolean, message = 'Mock error') => {
  shouldFail = fail;
  failMessage = message;
};

/**
 * Reset mock to defaults
 */
export const resetMockEntityService = () => {
  mockEntityList = [...mockEntities];
  shouldFail = false;
  failMessage = 'Mock error';
};

/**
 * Mock entity service
 */
export const entityService = {
  getByCampaign: vi.fn().mockImplementation(async () => {
    if (shouldFail) throw new Error(failMessage);
    return mockEntityList;
  }),
  
  getById: vi.fn().mockImplementation(async (campaignId: string, entityId: string) => {
    if (shouldFail) throw new Error(failMessage);
    return mockEntityList.find(e => e.id === entityId) || null;
  }),
  
  create: vi.fn().mockImplementation(async (campaignId: string, input: Partial<LoreEntity>) => {
    if (shouldFail) throw new Error(failMessage);
    const newEntity: LoreEntity = {
      id: `entity-${Date.now()}`,
      name: input.name || 'New Entity',
      description: input.description || '',
      entityType: input.entityType || 'character',
      visibility: input.visibility ?? VisibilityLevel.Campaign,
      ownershipType: input.ownershipType ?? OwnershipType.Master,
      isTemplate: input.isTemplate ?? false,
      imageUrl: input.imageUrl || undefined,
      attributes: input.attributes || {},
      campaignId,
      ownerId: 'user-123',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    mockEntityList.push(newEntity);
    return newEntity;
  }),
  
  update: vi.fn().mockImplementation(async (campaignId: string, entityId: string, input: Partial<LoreEntity>) => {
    if (shouldFail) throw new Error(failMessage);
    const index = mockEntityList.findIndex(e => e.id === entityId);
    if (index >= 0) {
      mockEntityList[index] = { ...mockEntityList[index], ...input };
      return mockEntityList[index];
    }
    throw new Error('Entity not found');
  }),
  
  delete: vi.fn().mockImplementation(async (campaignId: string, entityId: string) => {
    if (shouldFail) throw new Error(failMessage);
    const index = mockEntityList.findIndex(e => e.id === entityId);
    if (index >= 0) {
      mockEntityList.splice(index, 1);
    }
  }),
};
