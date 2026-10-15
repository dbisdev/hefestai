/**
 * PdfImportDialog Component
 * Floating dialog that allows Masters to import entity data from a PDF file.
 *
 * Manages a hidden file input and delegates the actual import logic to the
 * handlers provided by the parent (GalleryPage).
 *
 * Accessibility:
 * - role="dialog" with aria-modal and aria-label
 * - tabIndex={-1} for programmatic focus
 * - Hidden file input is aria-hidden
 */

import React, { forwardRef, RefObject } from 'react';

export interface PdfImportDialogProps {
  /** Ref to the hidden file input managed by the parent */
  fileInputRef: RefObject<HTMLInputElement | null>;
  /** Whether an import is currently in progress */
  isImporting: boolean;
  /** Error message from the last failed import, if any */
  error: string | null;
  /** Called when the "select file" button is clicked */
  onImportClick: () => void;
  /** Called when the file input value changes */
  onFileChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  /** Called when the dialog should close */
  onClose: () => void;
}

/**
 * Floating PDF import dialog for Masters.
 * Parent controls visibility via conditional rendering.
 */
export const PdfImportDialog = forwardRef<HTMLDivElement, PdfImportDialogProps>(
  ({ fileInputRef, isImporting, error, onImportClick, onFileChange, onClose }, ref) => (
    <div
      ref={ref}
      role="dialog"
      aria-label="Importar ficha desde PDF"
      aria-modal="true"
      tabIndex={-1}
      className="absolute top-0 right-0 z-50 bg-surface-dark border border-cyan-500 p-4 shadow-2xl animate-glitch-in font-mono min-w-[280px]"
    >
      {/* Close button */}
      <button
        onClick={onClose}
        className="absolute top-2 right-2 text-cyan-500/60 hover:text-cyan-500"
        aria-label="Cerrar diálogo de importación"
      >
        <span className="material-icons text-sm">close</span>
      </button>

      <h4 className="text-cyan-500 text-[10px] uppercase font-bold mb-2 flex items-center gap-2">
        <span className="material-icons text-sm">upload_file</span>
        Importar Ficha PDF
      </h4>

      <p className="text-[9px] text-cyan-500/60 leading-tight mb-4">
        Selecciona un archivo PDF exportado desde Hefestai para importar los datos de la entidad a
        esta campaña.
      </p>

      {/* Error banner */}
      {error && (
        <div className="bg-danger/20 border border-danger/50 p-2 mb-4 text-[10px] text-danger flex items-center gap-2">
          <span className="material-icons text-sm">error</span>
          {error}
        </div>
      )}

      {/* Import button */}
      <button
        onClick={onImportClick}
        disabled={isImporting}
        className="w-full bg-cyan-500/10 border border-cyan-500/50 py-3 text-[10px] text-cyan-500 uppercase font-bold hover:bg-cyan-500/20 transition-colors mb-3 flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {isImporting ? (
          <>
            <span className="material-icons text-sm animate-spin">sync</span>
            PROCESANDO...
          </>
        ) : (
          <>
            <span className="material-icons text-sm">folder_open</span>
            SELECCIONAR ARCHIVO
          </>
        )}
      </button>

      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept=".pdf"
        onChange={onFileChange}
        className="hidden"
        aria-hidden="true"
      />

      <div className="border-t border-cyan-500/20 pt-3">
        <button
          onClick={onClose}
          className="w-full bg-transparent border border-cyan-500/30 py-1 text-[10px] text-cyan-500/60 uppercase font-bold hover:border-cyan-500/50 hover:text-cyan-500 transition-colors"
        >
          CANCELAR
        </button>
      </div>
    </div>
  )
);

PdfImportDialog.displayName = 'PdfImportDialog';
