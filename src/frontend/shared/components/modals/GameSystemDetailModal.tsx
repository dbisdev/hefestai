/**
 * Game System Detail Modal Component
 * Mobile-friendly modal for displaying game system details
 * Used on mobile view instead of the side panel
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useEffect, useRef } from 'react';
import { Button } from '@shared/components/ui';
import type { GameSystem, EntityTemplateSummary, Campaign } from '@core/types';

interface GameSystemDetailModalProps {
  system: GameSystem;
  isOwned: boolean;
  isLoadingData: boolean;
  confirmedTemplates: EntityTemplateSummary[];
  campaignsUsingSystem: Campaign[];
  hasDocuments: boolean;
  onClose: () => void;
  onEdit: () => void;
  onUploadManual: () => void;
  onExtractEntities: () => void;
}

export const GameSystemDetailModal: React.FC<GameSystemDetailModalProps> = ({
  system,
  isOwned,
  isLoadingData,
  confirmedTemplates,
  campaignsUsingSystem,
  hasDocuments,
  onClose,
  onEdit,
  onUploadManual,
  onExtractEntities,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    document.body.style.overflow = 'hidden';
    modalRef.current?.focus();

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
    };
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="game-system-detail-title"
    >
      <div
        ref={modalRef}
        className="w-full max-w-md max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="game-system-detail-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">sports_esports</span>
            SISTEMA_SELECCIONADO
          </h2>
          <button
            onClick={onClose}
            className="material-icons text-sm hover:rotate-90 transition-transform"
            aria-label="Cerrar"
          >
            close
          </button>
        </div>

        <div className="p-4 space-y-4 font-mono overflow-y-auto flex-1 custom-scrollbar">
          <div className="space-y-2 text-xs">
            <h3 className="text-primary font-bold text-lg">{system.name}</h3>

            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-cyan-400 font-mono">{system.code}</span>
              <span className={`px-2 py-0.5 border ${
                system.isActive
                  ? 'border-green-500/40 text-green-400'
                  : 'border-yellow-500/40 text-yellow-400'
              }`}>
                {system.isActive ? 'ACTIVO' : 'INACTIVO'}
              </span>
            </div>

            <div className="flex justify-between">
              <span className="text-primary/40">ID:</span>
              <span className="text-primary font-mono">{system.id.substring(0, 8)}...</span>
            </div>
            {system.publisher && (
              <div className="flex justify-between">
                <span className="text-primary/40">Editorial:</span>
                <span className="text-primary/70">{system.publisher}</span>
              </div>
            )}
            {system.version && (
              <div className="flex justify-between">
                <span className="text-primary/40">Versión:</span>
                <span className="text-primary/70">{system.version}</span>
              </div>
            )}
          </div>

          <div className="space-y-3">
            <div>
              <div className="text-xs text-primary/40 uppercase tracking-wider mb-1">
                Entidades Confirmadas ({confirmedTemplates.length})
              </div>
              {isLoadingData ? (
                <div className="text-xs text-primary/40">Cargando...</div>
              ) : confirmedTemplates.length > 0 ? (
                <ul className="text-xs space-y-1 max-h-24 overflow-y-auto">
                  {confirmedTemplates.map((t) => (
                    <li key={t.id} className="text-primary/70 flex items-center gap-1">
                      <span className="material-icons text-[10px]">check_circle</span>
                      {t.displayName}
                    </li>
                  ))}
                </ul>
              ) : (
                <div className="text-xs text-primary/40">Sin entidades confirmadas</div>
              )}
            </div>

            <div>
              <div className="text-xs text-primary/40 uppercase tracking-wider mb-1">
                Campañas ({campaignsUsingSystem.length})
              </div>
              {isLoadingData ? (
                <div className="text-xs text-primary/40">Cargando...</div>
              ) : campaignsUsingSystem.length > 0 ? (
                <ul className="text-xs space-y-1 max-h-24 overflow-y-auto">
                  {campaignsUsingSystem.map((c) => (
                    <li key={c.id} className="text-cyan-400/70 flex items-center gap-1">
                      <span className="material-icons text-[10px]">campaign</span>
                      {c.name}
                    </li>
                  ))}
                </ul>
              ) : (
                <div className="text-xs text-primary/40">Sin campañas activas</div>
              )}
            </div>
          </div>
        </div>

        {isOwned && (
          <div className="p-4 border-t border-primary/20 space-y-2 flex-shrink-0">
            <Button
              onClick={() => {
                onEdit();
                onClose();
              }}
              variant="secondary"
              size="sm"
              fullWidth
              icon="edit"
            >
              Editar
            </Button>
            <Button
              onClick={() => {
                onUploadManual();
                onClose();
              }}
              variant="primary"
              size="sm"
              fullWidth
              icon="upload_file"
            >
              Cargar Manual RAG
            </Button>
            <Button
              onClick={() => {
                onExtractEntities();
                onClose();
              }}
              variant="secondary"
              size="sm"
              fullWidth
              icon="auto_awesome"
              className={!hasDocuments ? 'opacity-50' : ''}
            >
              Extraer Entidades
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};
