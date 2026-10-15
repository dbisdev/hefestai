/**
 * Admin System Operations Page
 * Provides system-wide administrative operations like embedding regeneration.
 * Cyberpunk terminal aesthetics consistent with other admin pages.
 */

import React, { useState } from 'react';
import { AdminLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useTerminalLog } from '@core/hooks/useTerminalLog';
import { adminDocumentService } from '@core/services/api';
import { extractErrorMessage } from '@core/utils/error';
import { 
  EMBEDDING_BATCH_SIZE_DEFAULT,
  EMBEDDING_MAX_DOCUMENTS_DEFAULT,
  EMBEDDING_BATCH_SIZE_MAX,
  EMBEDDING_MAX_DOCUMENTS_MAX,
  TERMINAL_LOG_ADMIN,
} from '@core/config/constants';
import type { GenerateMissingEmbeddingsResult } from '@core/types';
import { AdminGuard, AdminTerminalPanel } from '../components';

/**
 * Admin System Page Component
 * Provides UI for system-wide administrative operations (Admin only)
 * - Generate missing embeddings for RAG documents
 * - Future: Database maintenance, cache clearing, etc.
 */
export const AdminSystemPage: React.FC = () => {
  const { logs, addLog } = useTerminalLog({
    maxLogs: TERMINAL_LOG_ADMIN,
    initialLogs: [
      '> System operations panel online...',
      '> [SUCCESS] Admin protocols established.',
      '> Awaiting commands...'
    ]
  });
 
  // UI state
  const [isGeneratingEmbeddings, setIsGeneratingEmbeddings] = useState(false);
  const [embeddingsResult, setEmbeddingsResult] = useState<GenerateMissingEmbeddingsResult | null>(null);
  
  // Configuration state
  const [batchSize, setBatchSize] = useState(EMBEDDING_BATCH_SIZE_DEFAULT);
  const [maxDocuments, setMaxDocuments] = useState(EMBEDDING_MAX_DOCUMENTS_DEFAULT);

  /**
   * Handles generating missing embeddings for documents.
   */
  const handleGenerateEmbeddings = async () => {
    setIsGeneratingEmbeddings(true);
    setEmbeddingsResult(null);
    addLog('INICIANDO GENERACION DE EMBEDDINGS...');
    addLog(`Configuracion: batch=${batchSize}, max=${maxDocuments}`);

    try {
      const result = await adminDocumentService.generateMissingEmbeddings({
        batchSize,
        maxDocuments,
      });
      
      setEmbeddingsResult(result);
      
      addLog(`[SUCCESS] Procesados: ${result.totalProcessed} documentos`);
      addLog(`[SUCCESS] Exitosos: ${result.successCount}`);
      
      if (result.failureCount > 0) {
        addLog(`[WARNING] Fallidos: ${result.failureCount}`);
        result.errors.forEach(err => addLog(`ERROR: ${err}`));
      }
      
      if (result.totalProcessed === 0) {
        addLog('[INFO] No hay documentos pendientes de embedding');
      }
    } catch (error: unknown) {
      const message = extractErrorMessage(error, 'Error al generar embeddings');
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsGeneratingEmbeddings(false);
    }
  };

  return (
    <AdminGuard>
      <AdminLayout>
        <div className="flex flex-col lg:flex-row h-full gap-6">
          {/* Left Column - Operations */}
          <div className="w-full lg:w-1/2 flex flex-col gap-4 overflow-hidden">
            {/* Header */}
            <div className="border border-primary/30 bg-black/60 p-4">
              <div className="flex items-center justify-between mb-2">
                <div>
                  <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                    RAG
                  </h1>
                  <p className="text-primary/40 text-xs mt-1">
                    Operaciones administrativas del sistema
                  </p>
                </div>              
              </div>
            </div>

            {/* Embeddings Generation Card */}
            <div className="border border-purple-500/30 bg-black/60 p-6">
              <h2 className="text-sm text-purple-500/60 uppercase tracking-widest mb-4 flex items-center gap-2">
                <span className="material-icons text-sm">memory</span>
                Generar Embeddings Faltantes
              </h2>
              
              <p className="text-xs text-primary/60 mb-4">
                Genera embeddings vectoriales para documentos RAG que no los tienen.
                Util despues de migraciones o importaciones manuales.
              </p>

              {/* Configuration */}
              <div className="grid grid-cols-2 gap-4 mb-4">
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Batch Size
                  </label>
                  <input
                    type="number"
                    value={batchSize}
                    onChange={(e) => setBatchSize(Math.max(1, parseInt(e.target.value) || 1))}
                    min={1}
                    max={EMBEDDING_BATCH_SIZE_MAX}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-purple-500 focus:outline-none"
                    disabled={isGeneratingEmbeddings}
                  />
                  <p className="text-[10px] text-primary/30 mt-1">Documentos por lote (1-{EMBEDDING_BATCH_SIZE_MAX})</p>
                </div>
                
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Max Documentos
                  </label>
                  <input
                    type="number"
                    value={maxDocuments}
                    onChange={(e) => setMaxDocuments(Math.max(1, parseInt(e.target.value) || 1))}
                    min={1}
                    max={EMBEDDING_MAX_DOCUMENTS_MAX}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-purple-500 focus:outline-none"
                    disabled={isGeneratingEmbeddings}
                  />
                  <p className="text-[10px] text-primary/30 mt-1">Limite total (1-{EMBEDDING_MAX_DOCUMENTS_MAX})</p>
                </div>
              </div>

              {/* Action Button */}
              <Button 
                onClick={handleGenerateEmbeddings} 
                disabled={isGeneratingEmbeddings}
                className="w-full"
              >
                {isGeneratingEmbeddings ? (
                  <span className="flex items-center justify-center gap-2">
                    <span className="material-icons text-sm animate-spin">sync</span>
                    GENERANDO EMBEDDINGS...
                  </span>
                ) : (
                  <span className="flex items-center justify-center gap-2">
                    <span className="material-icons text-sm">play_arrow</span>
                    EJECUTAR GENERACION
                  </span>
                )}
              </Button>

              {/* Results */}
              {embeddingsResult && (
                <div className="mt-4 p-3 border border-primary/20 bg-black/40">
                  <h3 className="text-xs text-primary/60 uppercase mb-2">Resultado</h3>
                  <div className="grid grid-cols-3 gap-2 text-center">
                    <div>
                      <p className="text-2xl text-primary font-bold">{embeddingsResult.totalProcessed}</p>
                      <p className="text-[10px] text-primary/40 uppercase">Procesados</p>
                    </div>
                    <div>
                      <p className="text-2xl text-green-400 font-bold">{embeddingsResult.successCount}</p>
                      <p className="text-[10px] text-green-400/60 uppercase">Exitosos</p>
                    </div>
                    <div>
                      <p className={`text-2xl font-bold ${embeddingsResult.failureCount > 0 ? 'text-danger' : 'text-primary/40'}`}>
                        {embeddingsResult.failureCount}
                      </p>
                      <p className="text-[10px] text-primary/40 uppercase">Fallidos</p>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Future Operations Placeholder */}
            <div className="border border-primary/20 bg-black/40 p-6 flex-1">
              <h2 className="text-sm text-primary/30 uppercase tracking-widest mb-4 flex items-center gap-2">
                <span className="material-icons text-sm">construction</span>
                Operaciones Adicionales
              </h2>
              <p className="text-xs text-primary/20">
                Futuras operaciones de mantenimiento del sistema apareceran aqui.
              </p>
            </div>
          </div>

          {/* Right Column - Terminal Log (fills available space) */}
          <AdminTerminalPanel logs={logs} maxLogs={TERMINAL_LOG_ADMIN} fill />
        </div>
      </AdminLayout>
    </AdminGuard>
  );
};
