/**
 * useCharacterSheetPdf Hook
 * React hook for character sheet PDF export and import functionality
 * 
 * Single Responsibility: Manage PDF export/import state and operations
 */

import { useState, useCallback } from 'react';
import { CharacterSheetPdfService } from '@core/services/pdf';
import type { LoreEntity, FieldDefinition } from '@core/types';

/**
 * PDF operation state
 */
interface PdfState {
  /** Is an export operation in progress */
  isExporting: boolean;
  /** Is an import operation in progress */
  isImporting: boolean;
  /** Error message if any */
  error: string | null;
  /** Last successful operation result */
  lastResult: 'export' | 'import' | null;
}

/**
 * PDF export options for the hook
 */
interface ExportOptions {
  /** Optional custom filename */
  filename?: string;
  /** Field definitions from template for display name mapping */
  fieldDefinitions?: FieldDefinition[];
}

/**
 * Import result data
 */
interface ImportResult {
  name: string;
  description?: string;
  entityType: string;
  attributes: Record<string, unknown>;
  metadata?: Record<string, unknown>;
}

/**
 * Hook return type
 */
interface UseCharacterSheetPdfReturn {
  /** Current state */
  state: PdfState;
  /** Export entity to PDF and trigger download */
  exportToPdf: (entity: LoreEntity, options?: ExportOptions) => Promise<void>;
  /** Import entity data from PDF file */
  importFromPdf: (file: File) => Promise<ImportResult | null>;
  /** Clear any error state */
  clearError: () => void;
}

/**
 * useCharacterSheetPdf
 * Hook for managing character sheet PDF operations
 * 
 * @example
 * ```tsx
 * const { state, exportToPdf, importFromPdf } = useCharacterSheetPdf();
 * 
 * // Export
 * await exportToPdf(entity, 'my-character');
 * 
 * // Import
 * const data = await importFromPdf(file);
 * if (data) {
 *   // Use imported data to create entity
 * }
 * ```
 */
export function useCharacterSheetPdf(): UseCharacterSheetPdfReturn {
  const [state, setState] = useState<PdfState>({
    isExporting: false,
    isImporting: false,
    error: null,
    lastResult: null,
  });

  /**
   * Export entity to PDF and trigger download
   * @param entity - The entity to export
   * @param options - Export options including filename and field definitions
   */
  const exportToPdf = useCallback(async (entity: LoreEntity, options: ExportOptions = {}): Promise<void> => {
    const { filename, fieldDefinitions } = options;
    setState(prev => ({ ...prev, isExporting: true, error: null }));
    
    try {
      const blob = await CharacterSheetPdfService.exportToPdf(entity, {
        includeImage: true,
        format: 'a4',
        orientation: 'portrait',
        fieldDefinitions,
      });
      
      const exportFilename = filename || `${entity.name.replace(/\s+/g, '_')}_character_sheet`;
      CharacterSheetPdfService.downloadPdf(blob, exportFilename);
      
      setState(prev => ({ ...prev, isExporting: false, lastResult: 'export' }));
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Error al exportar PDF';
      setState(prev => ({ ...prev, isExporting: false, error: message }));
      throw error;
    }
  }, []);

  /**
   * Import entity data from PDF file
   */
  const importFromPdf = useCallback(async (file: File): Promise<ImportResult | null> => {
    setState(prev => ({ ...prev, isImporting: true, error: null }));
    
    try {
      // Validate file type
      if (!file.name.toLowerCase().endsWith('.pdf')) {
        throw new Error('El archivo debe ser un PDF');
      }
      
      const data = await CharacterSheetPdfService.importFromPdf(file);
      
      if (!data) {
        throw new Error('No se encontraron datos de personaje en el PDF');
      }
      
      setState(prev => ({ ...prev, isImporting: false, lastResult: 'import' }));
      
      return {
        name: data.entity.name,
        description: data.entity.description,
        entityType: data.entity.entityType,
        attributes: data.entity.attributes,
        metadata: data.entity.metadata,
      };
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Error al importar PDF';
      setState(prev => ({ ...prev, isImporting: false, error: message }));
      return null;
    }
  }, []);

  /**
   * Clear error state
   */
  const clearError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }));
  }, []);

  return {
    state,
    exportToPdf,
    importFromPdf,
    clearError,
  };
}

export default useCharacterSheetPdf;
