/**
 * EntityDetailPanel Component
 * Inspector panel that shows full details for the selected entity.
 *
 * Responsibilities:
 * - Render entity image or SolarSystemVisualization
 * - Display entity metadata (type, owner, visibility, description)
 * - Expose action buttons: View, Edit, Export PDF, Delete
 * - Reset scroll position when the selected entity changes
 *
 * Uses forwardRef so GalleryPage can manage focus on the panel element
 * (keyboard navigation trap on mobile).
 *
 * Accessibility:
 * - role="complementary" with a dynamic aria-label
 * - tabIndex={-1} allows programmatic focus
 * - All action buttons have aria-label
 */

import React, { useRef, useEffect, forwardRef } from 'react';
import { SolarSystemVisualization } from '@shared/components/visualization';
import { useCharacterSheetPdf } from '@core/hooks';
import type { LoreEntity, FieldDefinition, PlanetData, VisibilityLevel } from '@core/types';
import { logger } from '@core/utils';

const FALLBACK_IMAGE =
  'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop';

export interface EntityDetailPanelProps {
  /** Currently selected entity, or null when none is selected */
  entity: LoreEntity | null;
  /** Whether the current user is a campaign master */
  isMaster: boolean;
  /** Authenticated user id */
  currentUserId?: string;
  /** Field definitions for label mapping in export / modals */
  fieldDefinitions?: FieldDefinition[];
  /** Called when the panel close button is pressed */
  onClose: () => void;
  /** Called when the delete action is triggered */
  onDelete: (id: string) => void;
  /** Called when visibility is changed via the panel */
  onVisibilityChange: (entityId: string, visibility: VisibilityLevel) => void;
  /** Called when the edit action is triggered */
  onEdit: () => void;
  /** Called when the "view more" action is triggered */
  onView: () => void;
}

/** Maps a numeric visibility level to a display label */
function visibilityLabel(visibility: number): string {
  if (visibility === 0) return 'BORRADOR';
  if (visibility === 1) return 'PRIVADO';
  if (visibility === 2) return 'CAMPAÑA';
  return 'PUBLICO';
}

/**
 * Desktop entity inspector panel with PDF export and delete capabilities.
 * Accepts a ref to allow the parent to manage programmatic focus.
 */
export const EntityDetailPanel = forwardRef<HTMLElement, EntityDetailPanelProps>(
  (
    { entity, isMaster, currentUserId, fieldDefinitions, onClose, onDelete, onVisibilityChange: _onVisibilityChange, onEdit, onView },
    ref
  ) => {
    const hasEntity = entity !== null;
    const imageUrl = entity?.imageUrl || FALLBACK_IMAGE;
    const scrollContainerRef = useRef<HTMLDivElement>(null);

    // Solar-system-specific data
    const isSolarSystem = entity?.entityType === 'solar_system';
    const attributes = entity?.attributes ?? {};
    const planets = (attributes?.planets as PlanetData[] | undefined) ?? [];
    const spectralClass = attributes?.spectralClass as string | undefined;

    const { state: pdfState, exportToPdf } = useCharacterSheetPdf();

    // Reset scroll when the selected entity changes
    useEffect(() => {
      if (hasEntity && scrollContainerRef.current) {
        scrollContainerRef.current.scrollTop = 0;
      }
    }, [entity?.id, hasEntity]);

    const handleExportPdf = async () => {
      if (!entity) return;
      try {
        await exportToPdf(entity, { fieldDefinitions });
      } catch (error) {
        logger.error('[EntityDetailPanel] PDF export failed:', error);
      }
    };

    return (
      <aside
        ref={ref}
        role="complementary"
        aria-label={hasEntity ? `Detalles de ${entity.name}` : 'Inspector de entidad'}
        tabIndex={-1}
        className={`flex flex-col border relative focus:outline-none overflow-hidden flex-1 min-h-0 ${
          hasEntity
            ? 'border-primary bg-surface-dark/95 backdrop-blur-md animate-glitch-in'
            : 'border-primary/30 bg-surface-dark/60'
        }`}
      >
        {/* Panel header */}
        <div
          className={`font-bold p-2 text-xs flex justify-between items-center ${
            hasEntity
              ? 'bg-primary text-black shadow-[0_4px_10px_rgba(0,0,0,0.5)]'
              : 'bg-primary/20 text-primary/60'
          }`}
        >
          <span className="tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">analytics</span> &gt; INSPECTOR_ENTIDAD
          </span>
          {hasEntity && (
            <button
              aria-label="Cerrar panel de detalles"
              onClick={onClose}
              className="material-icons text-sm cursor-pointer hover:rotate-90 transition-transform focus:outline-none focus:ring-2 focus:ring-black"
            >
              close
            </button>
          )}
        </div>

        {hasEntity ? (
          <div
            ref={scrollContainerRef}
            className="p-6 flex flex-col gap-6 flex-1 min-h-0 overflow-y-auto custom-scrollbar font-mono"
          >
            {/* Entity visualisation */}
            {isSolarSystem ? (
              <SolarSystemVisualization
                spectralClass={spectralClass}
                planets={planets}
                size="md"
                showPlanetList={false}
                backgroundImage={entity?.imageUrl}
              />
            ) : (
              <div className="relative w-full aspect-square border-2 border-primary/30 p-1 bg-black shadow-[0_0_15px_rgba(37,244,106,0.1)]">
                <img
                  src={imageUrl}
                  alt={`Imagen de ${entity.name}`}
                  className="w-full h-full object-cover filter transition-all duration-700"
                />
                <div className="absolute top-2 left-2 px-1 bg-primary/80 text-black text-[8px] font-bold">
                  ANALYSIS_LIVE
                </div>
                <div className="absolute bottom-2 right-2 flex gap-1">
                  {[...Array(3)].map((_, i) => (
                    <div
                      key={i}
                      className="w-1.5 h-1.5 bg-primary/40 animate-pulse"
                      style={{ animationDelay: `${i * 0.1}s` }}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* Metadata */}
            <div className="space-y-4">
              <div>
                <h2 className="text-3xl font-display text-primary text-glow leading-none uppercase">
                  {entity.name}
                </h2>
                <div className="h-0.5 w-full bg-gradient-to-r from-primary via-primary/20 to-transparent mt-1" />
              </div>

              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="bg-black/40 border border-primary/10 p-2">
                  <span className="text-primary/40 block text-xs">TIPO</span>
                  <span className="text-primary font-bold uppercase">
                    {entity.entityType.replace('_', ' ')}
                  </span>
                </div>
                <div className="bg-black/40 border border-primary/10 p-2">
                  <span className="text-primary/40 block text-xs">PROPIETARIO</span>
                  <span
                    className="text-primary font-bold truncate block"
                    title={entity.ownerName || 'Desconocido'}
                  >
                    {entity.ownerName || 'Desconocido'}
                  </span>
                </div>
              </div>

              <div className="bg-black/40 border border-primary/10 p-2 text-sm">
                <span className="text-primary/40 block text-xs">VISIBILIDAD</span>
                <span className="text-primary font-bold uppercase">
                  {visibilityLabel(entity.visibility)}
                </span>
              </div>

              <div className="bg-black/60 p-4 border border-primary/20 text-sm text-primary/80 leading-relaxed shadow-inner">
                <p className="text-xs text-primary/40 mb-2 uppercase tracking-[0.2em] font-bold">
                  // BITÁCORA_NÚCLEO
                </p>
                <p>
                  {entity.description ||
                    'Sin descripción adicional en el núcleo de datos. Acceso a metadatos restringido.'}
                </p>
              </div>
            </div>

            {/* Action buttons */}
            <div className="mt-auto flex flex-col gap-2 pt-4 border-t border-primary/10">
              <button
                onClick={onView}
                aria-label={`Ver detalles de ${entity.name}`}
                className="cursor-pointer w-full py-3 border border-primary/60 text-primary text-xs hover:bg-primary/20 transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <span className="material-icons text-sm">visibility</span> MÁS_DATOS
              </button>

              {(isMaster || currentUserId === entity.ownerId) &&
                entity.entityType !== 'solar_system' && (
                  <button
                    onClick={onEdit}
                    aria-label={`Editar ${entity.name}`}
                    className="cursor-pointer w-full py-3 border border-primary/60 text-primary text-xs hover:bg-primary/20 transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-primary"
                  >
                    <span className="material-icons text-sm">edit</span> EDITAR_ENTIDAD
                  </button>
                )}

              <button
                onClick={handleExportPdf}
                disabled={pdfState.isExporting}
                aria-label={`Exportar ${entity.name} a PDF`}
                className="cursor-pointer w-full py-3 border border-cyan-500/60 text-cyan-500 text-xs hover:bg-cyan-500/20 transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-cyan-500 disabled:opacity-50"
              >
                {pdfState.isExporting ? (
                  <>
                    <span className="material-icons text-sm animate-spin">sync</span> EXPORTANDO...
                  </>
                ) : (
                  <>
                    <span className="material-icons text-sm">picture_as_pdf</span> EXPORTAR_FICHA
                  </>
                )}
              </button>

              {(isMaster || currentUserId === entity.ownerId) && (
                <button
                  onClick={() => onDelete(entity.id)}
                  aria-label={`Eliminar ${entity.name}`}
                  className="cursor-pointer w-full py-3 border border-danger/60 text-danger text-xs hover:bg-danger hover:text-white transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-danger"
                >
                  <span className="material-icons text-sm">delete_forever</span> PURGAR_REGISTRO
                </button>
              )}
            </div>
          </div>
        ) : (
          /* Empty state */
          <div className="flex-1 min-h-0 flex flex-col items-center justify-center p-6 text-center overflow-hidden">
            <span className="material-icons text-5xl text-primary/20 mb-4">touch_app</span>
            <p className="text-primary/40 text-xs uppercase tracking-widest mb-2">
              Sin entidad seleccionada
            </p>
            <p className="text-primary/30 text-[10px]">
              Seleccione una entidad de la galería para previsualizar
            </p>
          </div>
        )}

        {/* Decorative side accents */}
        <div className="absolute -left-1 top-1/4 w-1 h-20 bg-primary/20" />
        <div className="absolute -right-1 bottom-1/4 w-1 h-20 bg-primary/20" />
      </aside>
    );
  }
);

EntityDetailPanel.displayName = 'EntityDetailPanel';
