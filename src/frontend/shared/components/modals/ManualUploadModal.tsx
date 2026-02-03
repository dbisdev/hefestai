/**
 * Manual Upload Modal Component
 * Allows Masters to upload PDF manuals for RAG processing.
 * Manuals are chunked, embedded, and made searchable via semantic search.
 * 
 * Accessibility Features:
 * - Full keyboard navigation (Tab, Escape)
 * - ARIA labels and roles for screen readers
 * - Focus management for modal dialog
 */

import React, { useState, useRef, useEffect, useCallback } from 'react';
import { documentService, UploadManualResult, RagSourceType } from '@core/services/api';

interface ManualUploadModalProps {
  /** Callback to close the modal */
  onClose: () => void;
  /** Game system ID this manual belongs to */
  gameSystemId: string;
  /** Optional game system name for display */
  gameSystemName?: string;
  /** Callback when upload is successful */
  onSuccess?: (result: UploadManualResult) => void;
}

/**
 * ManualUploadModal Component
 * 
 * Provides a form for uploading PDF manuals that will be processed
 * for RAG-based semantic search functionality.
 */
export const ManualUploadModal: React.FC<ManualUploadModalProps> = ({
  onClose,
  gameSystemId,
  gameSystemName,
  onSuccess,
}) => {
  // Form state
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [title, setTitle] = useState('');
  const [sourceType, setSourceType] = useState<RagSourceType>('Rulebook');
  const [version, setVersion] = useState('');
  
  // UI state
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [uploadResult, setUploadResult] = useState<UploadManualResult | null>(null);
  const [uploadProgress, setUploadProgress] = useState(0);

  // Refs
  const modalRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  /**
   * Focus trap - focus modal on mount
   */
  useEffect(() => {
    modalRef.current?.focus();
  }, []);

  /**
   * Handle escape key to close modal
   */
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isUploading) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose, isUploading]);

  /**
   * Handle file selection
   */
  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.type !== 'application/pdf') {
        setError('Solo se permiten archivos PDF');
        return;
      }
      if (file.size > 50 * 1024 * 1024) { // 50MB limit
        setError('El archivo no puede superar 50MB');
        return;
      }
      setSelectedFile(file);
      setError(null);
      // Auto-fill title from filename if empty
      if (!title) {
        const nameWithoutExtension = file.name.replace(/\.pdf$/i, '');
        setTitle(nameWithoutExtension);
      }
    }
  }, [title]);

  /**
   * Handle drag and drop
   */
  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    if (file) {
      if (file.type !== 'application/pdf') {
        setError('Solo se permiten archivos PDF');
        return;
      }
      setSelectedFile(file);
      setError(null);
      if (!title) {
        const nameWithoutExtension = file.name.replace(/\.pdf$/i, '');
        setTitle(nameWithoutExtension);
      }
    }
  }, [title]);

  /**
   * Handle form submission
   */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedFile) {
      setError('Selecciona un archivo PDF');
      return;
    }

    setIsUploading(true);
    setError(null);
    setUploadProgress(10);

    try {
      // Simulate progress (actual upload doesn't provide progress)
      const progressInterval = setInterval(() => {
        setUploadProgress(prev => Math.min(prev + 10, 90));
      }, 500);

      const result = await documentService.uploadManual({
        gameSystemId,
        file: selectedFile,
        title: title.trim() || undefined,
        sourceType,
        version: version.trim() || undefined,
      });

      clearInterval(progressInterval);
      setUploadProgress(100);
      setUploadResult(result);
      
      if (onSuccess) {
        onSuccess(result);
      }
    } catch (err) {
      setUploadProgress(0);
      setError(err instanceof Error ? err.message : 'Error al subir el manual');
    } finally {
      setIsUploading(false);
    }
  };

  /**
   * Format file size for display
   */
  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const sourceTypeOptions: { value: RagSourceType; label: string; description: string }[] = [
    { value: 'Rulebook', label: 'LIBRO DE REGLAS', description: 'Manual principal del sistema' },
    { value: 'Supplement', label: 'SUPLEMENTO', description: 'Expansión o módulo adicional' },
    { value: 'Custom', label: 'PERSONALIZADO', description: 'Contenido propio o homebrew' },
  ];

  return (
    <div 
      className="fixed inset-0 bg-black/90 backdrop-blur-sm z-[100] flex items-center justify-center p-4"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isUploading) onClose();
      }}
    >
      <div 
        ref={modalRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="upload-modal-title"
        tabIndex={-1}
        className="w-full max-w-xl bg-surface-dark border border-primary shadow-2xl animate-glitch-in focus:outline-none"
      >
        {/* Header */}
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center">
          <h2 id="upload-modal-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">upload_file</span>
            CARGAR MANUAL RAG
          </h2>
          {!isUploading && (
            <button
              onClick={onClose}
              className="material-icons text-sm hover:rotate-90 transition-transform"
              aria-label="Cerrar"
            >
              close
            </button>
          )}
        </div>

        {uploadResult ? (
          /* Success State */
          <div className="p-6 space-y-4">
            <div className="text-center">
              <span className="material-icons text-6xl text-green-400 mb-4 block">check_circle</span>
              <h3 className="text-lg text-primary font-bold uppercase mb-2">Manual Cargado</h3>
              <p className="text-sm text-primary/60">{uploadResult.title}</p>
            </div>
            
            <div className="grid grid-cols-2 gap-4 text-center">
              <div className="bg-black/40 border border-primary/20 p-3">
                <p className="text-xs text-primary/40 uppercase">Páginas</p>
                <p className="text-2xl font-bold text-primary">{uploadResult.pageCount}</p>
              </div>
              <div className="bg-black/40 border border-primary/20 p-3">
                <p className="text-xs text-primary/40 uppercase">Fragmentos</p>
                <p className="text-2xl font-bold text-primary">{uploadResult.chunkCount}</p>
              </div>
            </div>
            
            <p className="text-xs text-primary/40 text-center">
              Procesado en {(uploadResult.processingTimeMs / 1000).toFixed(1)}s
            </p>
            
            <button
              onClick={onClose}
              className="w-full py-3 bg-primary text-black font-bold uppercase text-sm hover:bg-primary/80 transition-colors"
            >
              CERRAR
            </button>
          </div>
        ) : (
          /* Upload Form */
          <form onSubmit={handleSubmit} className="p-6 space-y-4 font-mono">
            {/* Game System Info */}
            <div className="bg-black/40 border border-primary/20 p-3 flex items-center gap-3">
              <span className="material-icons text-primary/60">sports_esports</span>
              <div>
                <p className="text-xs text-primary/40 uppercase">Sistema de Juego</p>
                <p className="text-sm text-primary font-bold">{gameSystemName || 'Sistema Activo'}</p>
              </div>
            </div>

            {/* Error Message */}
            {error && (
              <div className="bg-danger/20 border border-danger/50 p-3 text-danger text-xs flex items-center gap-2">
                <span className="material-icons text-sm">error</span>
                {error}
              </div>
            )}

            {/* File Drop Zone */}
            <div
              onDrop={handleDrop}
              onDragOver={(e) => e.preventDefault()}
              onClick={() => fileInputRef.current?.click()}
              className={`border-2 border-dashed p-6 text-center cursor-pointer transition-colors ${
                selectedFile 
                  ? 'border-primary/60 bg-primary/10' 
                  : 'border-primary/30 hover:border-primary/60 hover:bg-primary/5'
              }`}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept=".pdf,application/pdf"
                onChange={handleFileSelect}
                className="hidden"
                disabled={isUploading}
              />
              {selectedFile ? (
                <div className="space-y-2">
                  <span className="material-icons text-4xl text-primary">description</span>
                  <p className="text-sm text-primary font-bold">{selectedFile.name}</p>
                  <p className="text-xs text-primary/60">{formatFileSize(selectedFile.size)}</p>
                </div>
              ) : (
                <div className="space-y-2">
                  <span className="material-icons text-4xl text-primary/40">cloud_upload</span>
                  <p className="text-sm text-primary/60">
                    Arrastra un PDF aquí o <span className="text-primary underline">haz clic para seleccionar</span>
                  </p>
                  <p className="text-xs text-primary/40">Máximo 50MB</p>
                </div>
              )}
            </div>

            {/* Title Field */}
            <div>
              <label className="block text-xs text-primary/60 uppercase mb-1">
                Título del Manual (opcional)
              </label>
              <input
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Se extraerá del PDF si está vacío"
                disabled={isUploading}
                className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 disabled:opacity-50"
              />
            </div>

            {/* Source Type */}
            <div>
              <label className="block text-xs text-primary/60 uppercase mb-2">
                Tipo de Fuente
              </label>
              <div className="grid grid-cols-3 gap-2">
                {sourceTypeOptions.map((option) => (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => setSourceType(option.value)}
                    disabled={isUploading}
                    className={`p-2 border text-left transition-colors ${
                      sourceType === option.value
                        ? 'border-primary bg-primary/20 text-primary'
                        : 'border-primary/30 hover:border-primary/60 text-primary/60'
                    } disabled:opacity-50`}
                  >
                    <span className="text-[10px] font-bold block">{option.label}</span>
                    <span className="text-[8px] text-primary/40 hidden sm:block">{option.description}</span>
                  </button>
                ))}
              </div>
            </div>

            {/* Version Field */}
            <div>
              <label className="block text-xs text-primary/60 uppercase mb-1">
                Versión (opcional)
              </label>
              <input
                type="text"
                value={version}
                onChange={(e) => setVersion(e.target.value)}
                placeholder="ej: 1.0, 2024, Core"
                disabled={isUploading}
                className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 disabled:opacity-50"
              />
            </div>

            {/* Progress Bar */}
            {isUploading && (
              <div className="space-y-2">
                <div className="h-2 bg-black/60 border border-primary/30">
                  <div 
                    className="h-full bg-primary transition-all duration-300"
                    style={{ width: `${uploadProgress}%` }}
                  />
                </div>
                <p className="text-xs text-primary/60 text-center animate-pulse">
                  {uploadProgress < 30 && 'Subiendo archivo...'}
                  {uploadProgress >= 30 && uploadProgress < 60 && 'Extrayendo texto...'}
                  {uploadProgress >= 60 && uploadProgress < 90 && 'Generando embeddings...'}
                  {uploadProgress >= 90 && 'Indexando fragmentos...'}
                </p>
              </div>
            )}

            {/* Action Buttons */}
            <div className="flex gap-3 pt-4 border-t border-primary/20">
              <button
                type="button"
                onClick={onClose}
                disabled={isUploading}
                className="flex-1 py-3 border border-primary/40 text-primary/80 text-xs uppercase tracking-widest hover:bg-primary/10 transition-colors disabled:opacity-50"
              >
                CANCELAR
              </button>
              <button
                type="submit"
                disabled={!selectedFile || isUploading}
                className="flex-1 py-3 bg-primary text-black text-xs uppercase tracking-widest font-bold hover:bg-primary/80 transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {isUploading ? (
                  <>
                    <span className="material-icons text-sm animate-spin">sync</span>
                    PROCESANDO...
                  </>
                ) : (
                  <>
                    <span className="material-icons text-sm">upload</span>
                    CARGAR MANUAL
                  </>
                )}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default ManualUploadModal;
