/**
 * Entity Detail Modal Component
 * Mobile-friendly modal for displaying entity details
 * Used on mobile view instead of the side panel in gallery
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useEffect, useRef } from 'react';
import type { LoreEntity, FieldDefinition } from '@core/types';
import { VisibilityLevel } from '@core/types';

interface EntityDetailModalProps {
  entity: LoreEntity;
  isMaster: boolean;
  currentUserId?: string;
  fieldDefinitions?: FieldDefinition[];
  isExportingPdf?: boolean;
  onClose: () => void;
  onDelete: (id: string) => void;
  onVisibilityChange: (entityId: string, visibility: VisibilityLevel) => void;
  onEdit: () => void;
  onView: () => void;
  onExportPdf: () => void;
}

const visibilityLabels: Record<VisibilityLevel, { label: string; color: string }> = {
  [VisibilityLevel.Draft]: { label: 'BORRADOR', color: 'text-gray-400' },
  [VisibilityLevel.Private]: { label: 'PRIVADO', color: 'text-yellow-400' },
  [VisibilityLevel.Campaign]: { label: 'CAMPAÑA', color: 'text-cyan-400' },
  [VisibilityLevel.Public]: { label: 'PUBLICO', color: 'text-green-400' },
};

export const EntityDetailModal: React.FC<EntityDetailModalProps> = ({
  entity,
  isMaster,
  currentUserId,
  fieldDefinitions,
  isExportingPdf = false,
  onClose,
  onDelete,
  onVisibilityChange,
  onEdit,
  onView,
  onExportPdf,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);
  const imageUrl = entity.imageUrl || 'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop';
  const visibilityInfo = visibilityLabels[entity.visibility] || { label: 'DESCONOCIDO', color: 'text-primary/40' };
  const isOwner = currentUserId === entity.ownerId;
  const canEdit = isMaster || isOwner;

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
      aria-labelledby="entity-detail-title"
    >
      <div
        ref={modalRef}
        className="w-full max-w-md max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="entity-detail-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">analytics</span>
            INSPECTOR_ENTIDAD
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
          <div className="relative w-full aspect-square border border-primary/30 p-1 bg-black shadow-[0_0_15px_rgba(37,244,106,0.1)]">
            <img 
              src={imageUrl} 
              alt={`Imagen de ${entity.name}`} 
              className="w-full h-full object-cover" 
            />
            <div className="absolute top-2 left-2 px-1 bg-primary/80 text-black text-[8px] font-bold">
              ANALYSIS_LIVE
            </div>
          </div>

          <div className="space-y-3">
            <div>
              <h3 className="text-xl text-primary font-bold uppercase tracking-wider">{entity.name}</h3>
              <div className="h-0.5 w-full bg-gradient-to-r from-primary via-primary/20 to-transparent mt-1"></div>
            </div>

            <div className="grid grid-cols-2 gap-2 text-xs">
              <div className="bg-black/40 border border-primary/20 p-2">
                <span className="text-primary/40 block text-[10px] uppercase">Tipo</span>
                <span className="text-primary font-bold uppercase">{entity.entityType.replace('_', ' ')}</span>
              </div>
              <div className="bg-black/40 border border-primary/20 p-2">
                <span className="text-primary/40 block text-[10px] uppercase">Propietario</span>
                <span className="text-primary font-bold truncate block" title={entity.ownerName || 'Desconocido'}>
                  {entity.ownerName || 'Desconocido'}
                </span>
              </div>
            </div>

            <div className="bg-black/40 border border-primary/20 p-2 text-xs">
              <span className="text-primary/40 block text-[10px] uppercase mb-1">Visibilidad</span>
              <div className="flex items-center justify-between">
                <span className={`font-bold uppercase ${visibilityInfo.color}`}>{visibilityInfo.label}</span>
              </div>
            </div>

            {entity.description && (
              <div className="bg-black/40 border border-primary/20 p-3">
                <p className="text-[10px] text-primary/40 uppercase tracking-wider mb-1">// Descripción</p>
                <p className="text-xs text-primary/80">{entity.description}</p>
              </div>
            )}
          </div>
        </div>

        <div className="p-4 border-t border-primary/20 space-y-2 flex-shrink-0">
          <button
            onClick={() => { onView(); onClose(); }}
            className="w-full py-2.5 border border-primary/60 text-primary text-xs hover:bg-primary/20 transition-all font-bold uppercase tracking-wider flex items-center justify-center gap-2"
          >
            <span className="material-icons text-sm">visibility</span>
            Ver Detalles
          </button>

          {canEdit && (
            <button
              onClick={() => { onEdit(); onClose(); }}
              className="w-full py-2.5 border border-primary/60 text-primary text-xs hover:bg-primary/20 transition-all font-bold uppercase tracking-wider flex items-center justify-center gap-2"
            >
              <span className="material-icons text-sm">edit</span>
              Editar
            </button>
          )}

          <button
            onClick={onExportPdf}
            disabled={isExportingPdf}
            className="w-full py-2.5 border border-cyan-500/60 text-cyan-400 text-xs hover:bg-cyan-500/20 transition-all font-bold uppercase tracking-wider flex items-center justify-center gap-2 disabled:opacity-50"
          >
            {isExportingPdf ? (
              <>
                <span className="material-icons text-sm animate-spin">sync</span>
                Exportando...
              </>
            ) : (
              <>
                <span className="material-icons text-sm">picture_as_pdf</span>
                Exportar Ficha
              </>
            )}
          </button>

          {canEdit && (
            <button
              onClick={() => { onDelete(entity.id); onClose(); }}
              className="w-full py-2.5 border border-danger/60 text-danger text-xs hover:bg-danger hover:text-white transition-all font-bold uppercase tracking-wider flex items-center justify-center gap-2"
            >
              <span className="material-icons text-sm">delete_forever</span>
              Eliminar
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
