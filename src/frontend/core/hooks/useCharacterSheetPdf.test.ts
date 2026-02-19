/**
 * useCharacterSheetPdf Hook Unit Tests
 * Tests for PDF export/import functionality
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import type { LoreEntity, EntityCategory, OwnershipType, VisibilityLevel } from '@core/types';
import { FieldType } from '@core/types';

vi.mock('@core/services/pdf', () => ({
  CharacterSheetPdfService: {
    exportToPdf: vi.fn(),
    downloadPdf: vi.fn(),
    importFromPdf: vi.fn(),
  },
}));

const createMockEntity = (overrides: Partial<LoreEntity> = {}): LoreEntity => ({
  id: 'entity-123',
  campaignId: 'campaign-123',
  ownerId: 'user-123',
  entityType: 'character' as EntityCategory,
  name: 'Test Character',
  description: 'A test character',
  ownershipType: 0 as OwnershipType,
  visibility: 2 as VisibilityLevel,
  isTemplate: false,
  imageUrl: 'https://example.com/image.png',
  attributes: { STR: 10 },
  metadata: {},
  createdAt: '2024-01-01T00:00:00.000Z',
  updatedAt: '2024-01-01T00:00:00.000Z',
  ...overrides,
});

describe('useCharacterSheetPdf', () => {
  let CharacterSheetPdfService: typeof import('@core/services/pdf').CharacterSheetPdfService;

  beforeEach(async () => {
    vi.clearAllMocks();
    vi.resetModules();
    CharacterSheetPdfService = (await import('@core/services/pdf')).CharacterSheetPdfService;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('initial state', () => {
    it('initializes with default state', async () => {
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      
      expect(result.current.state.isExporting).toBe(false);
      expect(result.current.state.isImporting).toBe(false);
      expect(result.current.state.error).toBeNull();
      expect(result.current.state.lastResult).toBeNull();
    });
  });

  describe('exportToPdf', () => {
    it('exports entity to PDF successfully', async () => {
      const mockBlob = new Blob(['pdf content'], { type: 'application/pdf' });
      vi.mocked(CharacterSheetPdfService.exportToPdf).mockResolvedValue(mockBlob);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const entity = createMockEntity();
      
      await act(async () => {
        await result.current.exportToPdf(entity);
      });
      
      expect(CharacterSheetPdfService.exportToPdf).toHaveBeenCalledWith(entity, {
        includeImage: true,
        format: 'a4',
        orientation: 'portrait',
        fieldDefinitions: undefined,
      });
      expect(CharacterSheetPdfService.downloadPdf).toHaveBeenCalled();
      expect(result.current.state.isExporting).toBe(false);
      expect(result.current.state.lastResult).toBe('export');
      expect(result.current.state.error).toBeNull();
    });

    it('uses custom filename', async () => {
      const mockBlob = new Blob(['pdf content'], { type: 'application/pdf' });
      vi.mocked(CharacterSheetPdfService.exportToPdf).mockResolvedValue(mockBlob);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const entity = createMockEntity();
      
      await act(async () => {
        await result.current.exportToPdf(entity, { filename: 'custom-name' });
      });
      
      expect(CharacterSheetPdfService.downloadPdf).toHaveBeenCalledWith(mockBlob, 'custom-name');
    });

    it('uses entity name as default filename', async () => {
      const mockBlob = new Blob(['pdf content'], { type: 'application/pdf' });
      vi.mocked(CharacterSheetPdfService.exportToPdf).mockResolvedValue(mockBlob);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const entity = createMockEntity({ name: 'My Character' });
      
      await act(async () => {
        await result.current.exportToPdf(entity);
      });
      
      expect(CharacterSheetPdfService.downloadPdf).toHaveBeenCalledWith(
        mockBlob,
        'My_Character_character_sheet'
      );
    });

    it('passes field definitions to service', async () => {
      const mockBlob = new Blob(['pdf content'], { type: 'application/pdf' });
      vi.mocked(CharacterSheetPdfService.exportToPdf).mockResolvedValue(mockBlob);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const entity = createMockEntity();
      const fieldDefinitions = [{ 
        name: 'STR', 
        displayName: 'Strength', 
        fieldType: FieldType.Number,
        isRequired: false,
        order: 0,
      }];
      
      await act(async () => {
        await result.current.exportToPdf(entity, { fieldDefinitions });
      });
      
      expect(CharacterSheetPdfService.exportToPdf).toHaveBeenCalledWith(
        entity,
        expect.objectContaining({ fieldDefinitions })
      );
    });

    it('handles export errors', async () => {
      const error = new Error('Export failed');
      vi.mocked(CharacterSheetPdfService.exportToPdf).mockRejectedValue(error);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const entity = createMockEntity();
      
      await act(async () => {
        try {
          await result.current.exportToPdf(entity);
        } catch (e) {
          expect(e).toBe(error);
        }
      });
      
      expect(result.current.state.isExporting).toBe(false);
      expect(result.current.state.error).toBe('Export failed');
    });
  });

  describe('importFromPdf', () => {
    it('imports PDF successfully', async () => {
      const mockImportData = {
        version: '1.0',
        exportedAt: '2024-01-01T00:00:00.000Z',
        entity: {
          name: 'Imported Character',
          description: 'A description',
          entityType: 'character',
          attributes: { STR: 15 },
          metadata: { source: 'import' },
        },
      };
      vi.mocked(CharacterSheetPdfService.importFromPdf).mockResolvedValue(mockImportData);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const file = new File([''], 'test.pdf', { type: 'application/pdf' });
      
      let importResult;
      await act(async () => {
        importResult = await result.current.importFromPdf(file);
      });
      
      expect(importResult).toEqual({
        name: 'Imported Character',
        description: 'A description',
        entityType: 'character',
        attributes: { STR: 15 },
        metadata: { source: 'import' },
      });
      expect(result.current.state.isImporting).toBe(false);
      expect(result.current.state.lastResult).toBe('import');
    });

    it('rejects non-PDF files', async () => {
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const file = new File([''], 'test.txt', { type: 'text/plain' });
      
      let importResult;
      await act(async () => {
        importResult = await result.current.importFromPdf(file);
      });
      
      expect(importResult).toBeNull();
      expect(result.current.state.error).toBe('El archivo debe ser un PDF');
    });

    it('returns null when no data found in PDF', async () => {
      vi.mocked(CharacterSheetPdfService.importFromPdf).mockResolvedValue(null);
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const file = new File([''], 'test.pdf', { type: 'application/pdf' });
      
      let importResult;
      await act(async () => {
        importResult = await result.current.importFromPdf(file);
      });
      
      expect(importResult).toBeNull();
      expect(result.current.state.error).toBe('No se encontraron datos de personaje en el PDF');
    });

    it('handles import errors', async () => {
      vi.mocked(CharacterSheetPdfService.importFromPdf).mockRejectedValue(new Error('Import failed'));
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const file = new File([''], 'test.pdf', { type: 'application/pdf' });
      
      let importResult;
      await act(async () => {
        importResult = await result.current.importFromPdf(file);
      });
      
      expect(importResult).toBeNull();
      expect(result.current.state.error).toBe('Import failed');
    });
  });

  describe('clearError', () => {
    it('clears error state', async () => {
      vi.mocked(CharacterSheetPdfService.importFromPdf).mockRejectedValue(new Error('Error'));
      
      const { useCharacterSheetPdf } = await import('./useCharacterSheetPdf');
      const { result } = renderHook(() => useCharacterSheetPdf());
      const file = new File([''], 'test.txt', { type: 'text/plain' });
      
      await act(async () => {
        await result.current.importFromPdf(file);
      });
      
      expect(result.current.state.error).not.toBeNull();
      
      act(() => {
        result.current.clearError();
      });
      
      expect(result.current.state.error).toBeNull();
    });
  });
});
