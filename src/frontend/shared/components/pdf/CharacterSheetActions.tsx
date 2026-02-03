/**
 * CharacterSheetActions Component
 * Provides PDF export and import functionality for character entities
 * 
 * Single Responsibility: UI for character sheet PDF operations
 */

import React, { useRef, useCallback } from 'react';
import { useCharacterSheetPdf } from '@core/hooks';
import type { LoreEntity, CreateLoreEntityInput } from '@core/types';

interface CharacterSheetActionsProps {
  /** Entity to export (for export button) */
  entity?: LoreEntity;
  /** Callback when import is successful */
  onImportSuccess?: (data: CreateLoreEntityInput) => void;
  /** Whether to show export button */
  showExport?: boolean;
  /** Whether to show import button */
  showImport?: boolean;
  /** Custom class name */
  className?: string;
}

/**
 * CharacterSheetActions
 * Renders export/import buttons for character sheet PDFs
 * 
 * @example
 * ```tsx
 * // Export only
 * <CharacterSheetActions entity={selectedEntity} showExport showImport={false} />
 * 
 * // Import only
 * <CharacterSheetActions showExport={false} showImport onImportSuccess={handleImport} />
 * 
 * // Both
 * <CharacterSheetActions entity={entity} showExport showImport onImportSuccess={handleImport} />
 * ```
 */
export const CharacterSheetActions: React.FC<CharacterSheetActionsProps> = ({
  entity,
  onImportSuccess,
  showExport = true,
  showImport = true,
  className = '',
}) => {
  const { state, exportToPdf, importFromPdf, clearError } = useCharacterSheetPdf();
  const fileInputRef = useRef<HTMLInputElement>(null);

  /**
   * Handle export button click
   */
  const handleExport = useCallback(async () => {
    if (!entity) return;
    
    try {
      await exportToPdf(entity);
    } catch (error) {
      console.error('Export failed:', error);
    }
  }, [entity, exportToPdf]);

  /**
   * Handle import button click - opens file picker
   */
  const handleImportClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  /**
   * Handle file selection for import
   */
  const handleFileChange = useCallback(async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    
    const result = await importFromPdf(file);
    
    if (result && onImportSuccess) {
      // Convert import result to CreateLoreEntityInput
      const entityInput: CreateLoreEntityInput = {
        entityType: result.entityType as CreateLoreEntityInput['entityType'],
        name: result.name,
        description: result.description,
        attributes: result.attributes,
        metadata: result.metadata,
      };
      
      onImportSuccess(entityInput);
    }
    
    // Reset file input
    event.target.value = '';
  }, [importFromPdf, onImportSuccess]);

  /**
   * Dismiss error notification
   */
  const handleDismissError = useCallback(() => {
    clearError();
  }, [clearError]);

  const isLoading = state.isExporting || state.isImporting;

  return (
    <div className={`flex flex-col gap-2 ${className}`}>
      {/* Error notification */}
      {state.error && (
        <div className="flex items-center justify-between bg-danger/20 border border-danger/50 p-2 text-[10px] text-danger">
          <span className="flex items-center gap-1">
            <span className="material-icons text-sm">error</span>
            {state.error}
          </span>
          <button 
            onClick={handleDismissError}
            className="text-danger hover:text-danger/80"
            aria-label="Cerrar error"
          >
            <span className="material-icons text-sm">close</span>
          </button>
        </div>
      )}

      {/* Success notification */}
      {state.lastResult && !state.error && (
        <div className="flex items-center gap-1 bg-primary/20 border border-primary/50 p-2 text-[10px] text-primary">
          <span className="material-icons text-sm">check_circle</span>
          {state.lastResult === 'export' ? 'PDF exportado correctamente' : 'PDF importado correctamente'}
        </div>
      )}

      {/* Action buttons */}
      <div className="flex gap-2">
        {/* Export button */}
        {showExport && entity && (
          <button
            onClick={handleExport}
            disabled={isLoading}
            className="flex-1 flex items-center justify-center gap-2 py-2 border border-primary/50 text-primary text-[10px] uppercase font-bold hover:bg-primary/20 transition-colors disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-primary"
            aria-label="Exportar ficha de personaje a PDF"
          >
            {state.isExporting ? (
              <>
                <span className="material-icons text-sm animate-spin">sync</span>
                EXPORTANDO...
              </>
            ) : (
              <>
                <span className="material-icons text-sm">picture_as_pdf</span>
                EXPORTAR PDF
              </>
            )}
          </button>
        )}

        {/* Import button */}
        {showImport && (
          <>
            <button
              onClick={handleImportClick}
              disabled={isLoading}
              className="flex-1 flex items-center justify-center gap-2 py-2 border border-accent/50 text-accent text-[10px] uppercase font-bold hover:bg-accent/20 transition-colors disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-accent"
              aria-label="Importar ficha de personaje desde PDF"
            >
              {state.isImporting ? (
                <>
                  <span className="material-icons text-sm animate-spin">sync</span>
                  IMPORTANDO...
                </>
              ) : (
                <>
                  <span className="material-icons text-sm">upload_file</span>
                  IMPORTAR PDF
                </>
              )}
            </button>
            
            {/* Hidden file input */}
            <input
              ref={fileInputRef}
              type="file"
              accept=".pdf"
              onChange={handleFileChange}
              className="hidden"
              aria-hidden="true"
            />
          </>
        )}
      </div>
    </div>
  );
};

export default CharacterSheetActions;
