/**
 * Entity View Modal Component
 * Read-only modal for displaying complete entity details
 * Available to all users who can see the entity (masters and players)
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useEffect, useRef } from 'react';
import type { LoreEntity, FieldDefinition } from '@core/types';
import { VisibilityLevel } from '@core/types';

interface EntityViewModalProps {
  /** Entity to display */
  entity: LoreEntity;
  /** Field definitions from template for display name mapping */
  fieldDefinitions?: FieldDefinition[];
  /** Callback when modal is closed */
  onClose: () => void;
}

/**
 * Map of field identifier (name) to display name.
 */
type LabelMap = Record<string, string>;

/**
 * Builds a label map from field definitions.
 * Maps field.name (identifier) -> field.displayName
 */
const buildLabelMapFromFields = (fields: FieldDefinition[]): LabelMap => {
  const map: LabelMap = {};
  fields.forEach(field => {
    map[field.name] = field.displayName;
    map[field.name.toLowerCase()] = field.displayName;
    map[field.name.toUpperCase()] = field.displayName;
  });
  return map;
};

/**
 * Gets the display label for a field key.
 * Falls back to formatting the raw key if not found in map.
 */
const getDisplayLabel = (key: string, labelMap?: LabelMap): string => {
  if (labelMap?.[key]) {
    return labelMap[key];
  }
  
  if (labelMap) {
    const lowerKey = key.toLowerCase();
    const upperKey = key.toUpperCase();
    if (labelMap[lowerKey]) return labelMap[lowerKey];
    if (labelMap[upperKey]) return labelMap[upperKey];
  }
  
  return key
    .replace(/_/g, ' ')
    .toLowerCase()
    .replace(/\b\w/g, c => c.toUpperCase());
};

/**
 * Checks if a value is a nested object (like SKILLS)
 */
const isNestedObject = (value: unknown): value is Record<string, unknown> => {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
};

/**
 * Visibility level labels
 */
const visibilityLabels: Record<VisibilityLevel, { label: string; description: string }> = {
  [VisibilityLevel.Draft]: { label: 'BORRADOR', description: 'Solo el creador puede verlo' },
  [VisibilityLevel.Private]: { label: 'PRIVADO', description: 'Solo el creador puede verlo' },
  [VisibilityLevel.Campaign]: { label: 'CAMPAÑA', description: 'Miembros de la campaña' },
  [VisibilityLevel.Public]: { label: 'PÚBLICO', description: 'Todos pueden verlo' },
};

/**
 * Modal component for viewing entity details (read-only)
 * Displays name, description, visibility, image, and dynamic attributes
 */
export const EntityViewModal: React.FC<EntityViewModalProps> = ({
  entity,
  fieldDefinitions,
  onClose,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);

  // Build label map from field definitions
  const labelMap = fieldDefinitions ? buildLabelMapFromFields(fieldDefinitions) : undefined;

  // Get attributes from entity
  const attributes = (entity.attributes || {}) as Record<string, unknown>;

  // Separate attributes by type for proper rendering
  const numericAttrs: [string, number][] = [];
  const stringAttrs: [string, string][] = [];
  const nestedAttrs: [string, Record<string, unknown>][] = [];

  Object.entries(attributes).forEach(([key, value]) => {
    if (typeof value === 'number') {
      numericAttrs.push([key, value]);
    } else if (typeof value === 'string') {
      stringAttrs.push([key, value]);
    } else if (isNestedObject(value)) {
      nestedAttrs.push([key, value]);
    }
  });

  const hasAttributes = numericAttrs.length > 0 || stringAttrs.length > 0 || nestedAttrs.length > 0;

  // Handle escape key to close modal
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    
    // Focus modal on mount for accessibility
    modalRef.current?.focus();

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [onClose]);

  // Get visibility info
  const visibilityInfo = visibilityLabels[entity.visibility] || { label: 'DESCONOCIDO', description: '' };

  return (
    <div 
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
    >
      <div 
        ref={modalRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="view-modal-title"
        tabIndex={-1}
        className="w-full max-w-2xl max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
      >
        {/* Header */}
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="view-modal-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">visibility</span>
            DATOS_ENTIDAD
          </h2>
          <button
            onClick={onClose}
            className="material-icons text-sm hover:rotate-90 transition-transform"
            aria-label="Cerrar"
          >
            close
          </button>
        </div>

        {/* Content - Scrollable */}
        <div className="p-6 space-y-4 font-mono overflow-y-auto flex-1 custom-scrollbar">
          {/* Entity Image */}
          {entity.imageUrl && (
            <div className="w-full h-48 overflow-hidden border border-primary/30 mb-4">
              <img 
                src={entity.imageUrl} 
                alt={entity.name}
                className="w-full h-full object-cover"
              />
            </div>
          )}

          {/* Basic Info Section */}
          <div className="space-y-4">
            <p className="text-[9px] text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
              // INFORMACIÓN_BÁSICA
            </p>

            {/* Name */}
            <div className="bg-black/40 border border-primary/20 p-3">
              <span className="text-[9px] text-primary/40 uppercase block mb-1">Nombre</span>
              <span className="text-lg text-primary font-bold">{entity.name}</span>
            </div>

            {/* Type */}
            <div className="bg-black/40 border border-primary/20 p-3">
              <span className="text-[9px] text-primary/40 uppercase block mb-1">Tipo</span>
              <span className="text-sm text-primary uppercase">{entity.entityType.replace('_', ' ')}</span>
            </div>

            {/* Description */}
            {entity.description && (
              <div className="bg-black/40 border border-primary/20 p-3">
                <span className="text-[9px] text-primary/40 uppercase block mb-1">Descripción</span>
                <p className="text-sm text-primary/80 whitespace-pre-wrap">{entity.description}</p>
              </div>
            )}

            {/* Owner */}
            <div className="bg-black/40 border border-primary/20 p-3">
              <span className="text-[9px] text-primary/40 uppercase block mb-1">Propietario</span>
              <span className="text-sm text-primary">{entity.ownerName || 'Desconocido'}</span>
            </div>

            {/* Visibility */}
            <div className="bg-black/40 border border-primary/20 p-3">
              <span className="text-[9px] text-primary/40 uppercase block mb-1">Visibilidad</span>
              <span className="text-sm text-primary font-bold">{visibilityInfo.label}</span>
              <span className="text-[9px] text-primary/40 block mt-1">{visibilityInfo.description}</span>
            </div>
          </div>

          {/* Attributes Section */}
          {hasAttributes && (
            <div className="space-y-4">
              <p className="text-[9px] text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
                // ATRIBUTOS
              </p>

              {/* Numeric Attributes */}
              {numericAttrs.length > 0 && (
                <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                  {numericAttrs.map(([key, value]) => (
                    <div key={key} className="bg-black/40 border border-primary/20 p-3">
                      <span className="text-[9px] text-primary/40 uppercase block mb-1">
                        {getDisplayLabel(key, labelMap)}
                      </span>
                      <span className="text-xl text-primary font-bold font-mono">{value}</span>
                    </div>
                  ))}
                </div>
              )}

              {/* String Attributes */}
              {stringAttrs.length > 0 && (
                <div className="space-y-2">
                  {stringAttrs.map(([key, value]) => (
                    <div key={key} className="bg-black/40 border border-primary/20 p-3">
                      <span className="text-[9px] text-primary/40 uppercase block mb-1">
                        {getDisplayLabel(key, labelMap)}
                      </span>
                      <span className="text-sm text-primary">{value}</span>
                    </div>
                  ))}
                </div>
              )}

              {/* Nested Attributes (like SKILLS) */}
              {nestedAttrs.map(([parentKey, nestedObj]) => (
                <div key={parentKey} className="space-y-2">
                  <p className="text-[10px] text-primary/50 uppercase tracking-wider">
                    {getDisplayLabel(parentKey, labelMap)}
                  </p>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                    {Object.entries(nestedObj).map(([childKey, childValue]) => (
                      <div key={childKey} className="bg-black/40 border border-primary/20 p-2">
                        <span className="text-[9px] text-primary/40 uppercase block mb-1">
                          {getDisplayLabel(childKey, labelMap)}
                        </span>
                        <span className="text-lg text-primary font-bold font-mono">
                          {typeof childValue === 'number' ? childValue : String(childValue)}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Metadata Section */}
          {entity.metadata && Object.keys(entity.metadata).length > 0 && (
            <div className="space-y-2">
              <p className="text-[9px] text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
                // METADATOS
              </p>
              <div className="bg-black/40 border border-primary/20 p-3">
                <pre className="text-[10px] text-primary/60 overflow-x-auto">
                  {JSON.stringify(entity.metadata, null, 2)}
                </pre>
              </div>
            </div>
          )}

          {/* Timestamps */}
          <div className="space-y-2 pt-4 border-t border-primary/10">
            <div className="flex justify-between text-[9px] text-primary/40">
              <span>Creado: {new Date(entity.createdAt).toLocaleString()}</span>
              {entity.updatedAt && (
                <span>Actualizado: {new Date(entity.updatedAt).toLocaleString()}</span>
              )}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-primary/20 flex justify-end flex-shrink-0">
          <button
            onClick={onClose}
            className="px-6 py-2 border border-primary/60 text-primary text-xs uppercase tracking-wider hover:bg-primary/20 transition-colors"
          >
            Cerrar
          </button>
        </div>
      </div>
    </div>
  );
};
