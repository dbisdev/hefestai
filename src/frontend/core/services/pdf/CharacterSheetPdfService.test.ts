/**
 * CharacterSheetPdfService Unit Tests
 * Tests PDF export and import functionality
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { CharacterSheetPdfService } from './CharacterSheetPdfService';
import type { LoreEntity, EntityCategory, OwnershipType, VisibilityLevel } from '@core/types';

/**
 * Create a mock LoreEntity for testing
 */
const createMockEntity = (overrides: Partial<LoreEntity> = {}): LoreEntity => ({
  id: 'entity-123',
  campaignId: 'campaign-123',
  ownerId: 'user-123',
  entityType: 'character' as EntityCategory,
  name: 'Test Character',
  description: 'A brave adventurer from the outer rim',
  ownershipType: 0 as OwnershipType,
  visibility: 2 as VisibilityLevel,
  isTemplate: false,
  imageUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
  attributes: {
    STR: 15,
    INT: 12,
    DEX: 18,
    bio: 'A long background story',
  },
  metadata: {
    generatedBy: 'ai',
    version: '1.0',
  },
  createdAt: '2024-01-01T00:00:00.000Z',
  updatedAt: '2024-01-02T00:00:00.000Z',
  ...overrides,
});

/**
 * Helper to create a mock File with arrayBuffer support
 */
const createMockFile = (content: string, name: string): File => {
  const blob = new Blob([content], { type: 'application/pdf' });
  const file = new File([blob], name, { type: 'application/pdf' });
  
  // Add arrayBuffer method for jsdom compatibility
  if (!file.arrayBuffer) {
    (file as unknown as Record<string, unknown>).arrayBuffer = async () => {
      return new Promise<ArrayBuffer>((resolve) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.readAsArrayBuffer(blob);
      });
    };
  }
  
  return file;
};

/**
 * Helper to read blob as text for assertions
 */
const blobToText = async (blob: Blob): Promise<string> => {
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.readAsText(blob);
  });
};

describe('CharacterSheetPdfService', () => {
  // Mock URL methods
  let createObjectURLMock: ReturnType<typeof vi.fn>;
  let revokeObjectURLMock: ReturnType<typeof vi.fn>;
  
  beforeEach(() => {
    createObjectURLMock = vi.fn(() => 'blob:mock-url');
    revokeObjectURLMock = vi.fn();
    global.URL.createObjectURL = createObjectURLMock as unknown as typeof URL.createObjectURL;
    global.URL.revokeObjectURL = revokeObjectURLMock as unknown as typeof URL.revokeObjectURL;
  });
  
  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('exportToPdf', () => {
    it('should export an entity to PDF blob', async () => {
      const entity = createMockEntity();
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      
      expect(blob).toBeInstanceOf(Blob);
      expect(blob.type).toBe('application/pdf');
    });

    it('should create PDF with correct options', async () => {
      const entity = createMockEntity();
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity, {
        includeImage: true,
        format: 'a4',
        orientation: 'portrait',
      });
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should handle entity without image', async () => {
      const entity = createMockEntity({ imageUrl: undefined });
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity, {
        includeImage: true,
      });
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should handle entity without attributes', async () => {
      const entity = createMockEntity({ attributes: undefined });
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should handle entity without metadata', async () => {
      const entity = createMockEntity({ metadata: undefined });
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should handle entity without description', async () => {
      const entity = createMockEntity({ description: undefined });
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should handle different entity types', async () => {
      const entityTypes: EntityCategory[] = ['character', 'actor', 'monster', 'vehicle', 'mission', 'encounter', 'solar_system', 'location', 'item'];
      
      for (const entityType of entityTypes) {
        const entity = createMockEntity({ entityType });
        const blob = await CharacterSheetPdfService.exportToPdf(entity);
        expect(blob).toBeInstanceOf(Blob);
      }
    });

    it('should support letter format', async () => {
      const entity = createMockEntity();
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity, {
        format: 'letter',
      });
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should support landscape orientation', async () => {
      const entity = createMockEntity();
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity, {
        orientation: 'landscape',
      });
      
      expect(blob).toBeInstanceOf(Blob);
    });

    it('should embed entity data as JSON in the PDF', async () => {
      const entity = createMockEntity();
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      
      // Read the PDF content as text to check for embedded data
      const text = await blobToText(blob);
      
      // The PDF should contain the HEFESTAI_DATA marker
      expect(text).toContain('HEFESTAI_DATA');
      expect(text).toContain('END_HEFESTAI_DATA');
    });

    it('should include entity name in embedded data', async () => {
      const entity = createMockEntity({ name: 'Unique Test Name' });
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      const text = await blobToText(blob);
      
      expect(text).toContain('Unique Test Name');
    });
  });

  describe('downloadPdf', () => {
    it('should trigger download with correct filename', () => {
      const mockLink = {
        href: '',
        download: '',
        click: vi.fn(),
      };
      
      const createElementSpy = vi.spyOn(document, 'createElement').mockReturnValue(mockLink as unknown as HTMLAnchorElement);
      const appendChildSpy = vi.spyOn(document.body, 'appendChild').mockImplementation(() => mockLink as unknown as Node);
      const removeChildSpy = vi.spyOn(document.body, 'removeChild').mockImplementation(() => mockLink as unknown as Node);
      
      const blob = new Blob(['test'], { type: 'application/pdf' });
      
      CharacterSheetPdfService.downloadPdf(blob, 'test-filename');
      
      expect(createElementSpy).toHaveBeenCalledWith('a');
      expect(mockLink.download).toBe('test-filename.pdf');
      expect(mockLink.click).toHaveBeenCalled();
      expect(createObjectURLMock).toHaveBeenCalledWith(blob);
      expect(revokeObjectURLMock).toHaveBeenCalledWith('blob:mock-url');
      
      createElementSpy.mockRestore();
      appendChildSpy.mockRestore();
      removeChildSpy.mockRestore();
    });

    it('should not double-add .pdf extension', () => {
      const mockLink = {
        href: '',
        download: '',
        click: vi.fn(),
      };
      
      const createElementSpy = vi.spyOn(document, 'createElement').mockReturnValue(mockLink as unknown as HTMLAnchorElement);
      const appendChildSpy = vi.spyOn(document.body, 'appendChild').mockImplementation(() => mockLink as unknown as Node);
      const removeChildSpy = vi.spyOn(document.body, 'removeChild').mockImplementation(() => mockLink as unknown as Node);
      
      const blob = new Blob(['test'], { type: 'application/pdf' });
      
      CharacterSheetPdfService.downloadPdf(blob, 'test-filename.pdf');
      
      expect(mockLink.download).toBe('test-filename.pdf');
      
      createElementSpy.mockRestore();
      appendChildSpy.mockRestore();
      removeChildSpy.mockRestore();
    });
  });

  describe('importFromPdf', () => {
    it('should return null for PDF without embedded data', async () => {
      const pdfWithoutData = createMockFile(
        '%PDF-1.4 Some PDF content without Hefestai data',
        'test.pdf'
      );
      
      const result = await CharacterSheetPdfService.importFromPdf(pdfWithoutData);
      
      expect(result).toBeNull();
    });

    it('should extract entity data from valid PDF with embedded JSON', async () => {
      // Create a mock PDF file with embedded Hefestai data
      const mockData = {
        version: '1.0.0',
        exportedAt: '2024-01-01T00:00:00.000Z',
        entity: {
          name: 'Imported Character',
          description: 'An imported character',
          entityType: 'character',
          attributes: { STR: 10 },
          metadata: { source: 'import' },
        },
      };
      
      const pdfContent = `%PDF-1.4 HEFESTAI_DATA:${JSON.stringify(mockData)}:END_HEFESTAI_DATA`;
      const pdfWithData = createMockFile(pdfContent, 'test.pdf');
      
      const result = await CharacterSheetPdfService.importFromPdf(pdfWithData);
      
      expect(result).not.toBeNull();
      expect(result?.entity.name).toBe('Imported Character');
      expect(result?.entity.entityType).toBe('character');
      expect(result?.entity.attributes).toEqual({ STR: 10 });
      expect(result?.version).toBe('1.0.0');
    });

    it('should return null for invalid JSON in PDF', async () => {
      const pdfContent = '%PDF-1.4 HEFESTAI_DATA:invalid-json:END_HEFESTAI_DATA';
      const pdfWithBadData = createMockFile(pdfContent, 'test.pdf');
      
      const result = await CharacterSheetPdfService.importFromPdf(pdfWithBadData);
      
      expect(result).toBeNull();
    });

    it('should return null for malformed data structure', async () => {
      // Missing required fields
      const mockData = {
        version: '1.0.0',
        // Missing entity field
      };
      
      const pdfContent = `%PDF-1.4 HEFESTAI_DATA:${JSON.stringify(mockData)}:END_HEFESTAI_DATA`;
      const pdfWithBadData = createMockFile(pdfContent, 'test.pdf');
      
      const result = await CharacterSheetPdfService.importFromPdf(pdfWithBadData);
      
      expect(result).toBeNull();
    });

    it('should handle file read errors gracefully', async () => {
      // Create a mock file that will fail to read
      const badFile = {
        arrayBuffer: vi.fn().mockRejectedValue(new Error('Read error')),
      } as unknown as File;
      
      const result = await CharacterSheetPdfService.importFromPdf(badFile);
      
      expect(result).toBeNull();
    });
  });

  describe('round-trip export/import', () => {
    it('should successfully export and re-import entity data', async () => {
      const originalEntity = createMockEntity({
        name: 'Round Trip Test',
        description: 'Testing export then import',
        entityType: 'actor',
        attributes: {
          CHA: 14,
          INT: 16,
          WIS: 12,
        },
        metadata: {
          faction: 'rebels',
          level: 5,
        },
      });
      
      // Export to PDF
      const blob = await CharacterSheetPdfService.exportToPdf(originalEntity);
      
      // Convert blob to File for import with arrayBuffer support
      const pdfFile = createMockFile(await blobToText(blob), 'test-export.pdf');
      
      // Import from PDF
      const importedData = await CharacterSheetPdfService.importFromPdf(pdfFile);
      
      expect(importedData).not.toBeNull();
      expect(importedData?.entity.name).toBe(originalEntity.name);
      expect(importedData?.entity.description).toBe(originalEntity.description);
      expect(importedData?.entity.entityType).toBe(originalEntity.entityType);
      expect(importedData?.entity.attributes).toEqual(originalEntity.attributes);
      expect(importedData?.entity.metadata).toEqual(originalEntity.metadata);
    });

    it('should preserve numeric attributes through export/import', async () => {
      const entity = createMockEntity({
        attributes: {
          STR: 18,
          INT: 10,
          DEX: 15,
          HP: 100,
          ATK: 25,
        },
      });
      
      const blob = await CharacterSheetPdfService.exportToPdf(entity);
      const pdfFile = createMockFile(await blobToText(blob), 'stats-test.pdf');
      const importedData = await CharacterSheetPdfService.importFromPdf(pdfFile);
      
      expect(importedData?.entity.attributes).toEqual(entity.attributes);
      expect(typeof importedData?.entity.attributes?.STR).toBe('number');
    });
  });
});
