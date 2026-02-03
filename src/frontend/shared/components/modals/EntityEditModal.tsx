/**
 * Entity Edit Modal Component
 * Allows Masters to edit entity details (name, description, visibility)
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import type { LoreEntity } from '@core/types';
import { VisibilityLevel } from '@core/types';

interface EntityEditModalProps {
  /** Entity to edit */
  entity: LoreEntity;
  /** Callback when modal is closed */
  onClose: () => void;
  /** Callback when entity is successfully saved */
  onSave: (updatedEntity: LoreEntity) => void;
}

/**
 * Modal component for editing entity details
 * Supports editing name, description, visibility, and image URL
 */
export const EntityEditModal: React.FC<EntityEditModalProps> = ({
  entity,
  onClose,
  onSave,
}) => {
  const { activeCampaignId } = useCampaign();
  const modalRef = useRef<HTMLDivElement>(null);
  
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Form state
  const [formData, setFormData] = useState({
    name: entity.name,
    description: entity.description || '',
    visibility: entity.visibility,
    imageUrl: entity.imageUrl || '',
  });

  // Focus trap - focus first input on mount
  useEffect(() => {
    const firstInput = modalRef.current?.querySelector('input') as HTMLInputElement;
    firstInput?.focus();
  }, []);

  // Close on Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  /**
   * Handle form field changes
   */
  const handleChange = useCallback((
    field: keyof typeof formData,
    value: string | VisibilityLevel
  ) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setError(null);
  }, []);

  /**
   * Handle form submission
   */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!activeCampaignId) {
      setError('No hay campaña activa');
      return;
    }

    if (!formData.name.trim()) {
      setError('El nombre es requerido');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const updatedEntity = await entityService.update(activeCampaignId, entity.id, {
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        visibility: formData.visibility,
        imageUrl: formData.imageUrl.trim() || undefined,
        attributes: entity.attributes, // Keep existing attributes
        metadata: entity.metadata, // Keep existing metadata
      });

      onSave(updatedEntity);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al guardar';
      setError(message);
    } finally {
      setIsSaving(false);
    }
  };

  /**
   * Visibility options
   */
  const visibilityOptions = [
    { value: VisibilityLevel.Draft, label: 'BORRADOR', description: 'Solo tú puedes verlo' },
    { value: VisibilityLevel.Private, label: 'PRIVADO', description: 'Solo el creador' },
    { value: VisibilityLevel.Campaign, label: 'CAMPAÑA', description: 'Miembros de la campaña' },
    { value: VisibilityLevel.Public, label: 'PÚBLICO', description: 'Todos pueden verlo' },
  ];

  return (
    <div 
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div 
        ref={modalRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-modal-title"
        className="w-full max-w-lg bg-surface-dark border border-primary shadow-2xl animate-glitch-in"
      >
        {/* Header */}
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center">
          <h2 id="edit-modal-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">edit</span>
            EDITAR_ENTIDAD
          </h2>
          <button
            onClick={onClose}
            className="material-icons text-sm hover:rotate-90 transition-transform"
            aria-label="Cerrar"
          >
            close
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 font-mono">
          {/* Error Message */}
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-3 text-danger text-xs">
              <span className="material-icons text-sm mr-1 align-middle">error</span>
              {error}
            </div>
          )}

          {/* Name Field */}
          <div>
            <label 
              htmlFor="entity-name"
              className="block text-xs text-primary/60 uppercase mb-1"
            >
              Nombre *
            </label>
            <input
              id="entity-name"
              type="text"
              value={formData.name}
              onChange={(e) => handleChange('name', e.target.value)}
              placeholder="Nombre de la entidad"
              disabled={isSaving}
              className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 disabled:opacity-50"
              required
            />
          </div>

          {/* Description Field */}
          <div>
            <label 
              htmlFor="entity-description"
              className="block text-xs text-primary/60 uppercase mb-1"
            >
              Descripción
            </label>
            <textarea
              id="entity-description"
              value={formData.description}
              onChange={(e) => handleChange('description', e.target.value)}
              placeholder="Descripción de la entidad (opcional)"
              rows={4}
              disabled={isSaving}
              className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 resize-none disabled:opacity-50"
            />
          </div>

          {/* Image URL Field */}
          <div>
            <label 
              htmlFor="entity-image"
              className="block text-xs text-primary/60 uppercase mb-1"
            >
              URL de Imagen
            </label>
            <input
              id="entity-image"
              type="url"
              value={formData.imageUrl}
              onChange={(e) => handleChange('imageUrl', e.target.value)}
              placeholder="https://..."
              disabled={isSaving}
              className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 disabled:opacity-50"
            />
          </div>

          {/* Visibility Field */}
          <div>
            <label className="block text-xs text-primary/60 uppercase mb-2">
              Visibilidad
            </label>
            <div className="grid grid-cols-2 gap-2">
              {visibilityOptions.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  onClick={() => handleChange('visibility', option.value)}
                  disabled={isSaving}
                  className={`p-2 border text-left transition-colors ${
                    formData.visibility === option.value
                      ? 'border-primary bg-primary/20 text-primary'
                      : 'border-primary/30 hover:border-primary/60 text-primary/60'
                  } disabled:opacity-50`}
                >
                  <span className="text-xs font-bold block">{option.label}</span>
                  <span className="text-[9px] text-primary/40">{option.description}</span>
                </button>
              ))}
            </div>
          </div>

          {/* Entity Type (read-only info) */}
          <div className="bg-black/40 border border-primary/10 p-3">
            <p className="text-[9px] text-primary/40 uppercase mb-1">Tipo de Entidad</p>
            <p className="text-sm text-primary uppercase font-bold">
              {entity.entityType.replace('_', ' ')}
            </p>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-3 pt-4 border-t border-primary/20">
            <button
              type="button"
              onClick={onClose}
              disabled={isSaving}
              className="flex-1 py-3 border border-primary/40 text-primary/80 text-xs uppercase tracking-widest hover:bg-primary/10 transition-colors disabled:opacity-50"
            >
              CANCELAR
            </button>
            <button
              type="submit"
              disabled={isSaving}
              className="flex-1 py-3 bg-primary text-black text-xs uppercase tracking-widest font-bold hover:bg-primary/80 transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
            >
              {isSaving ? (
                <>
                  <span className="material-icons text-sm animate-spin">sync</span>
                  GUARDANDO...
                </>
              ) : (
                <>
                  <span className="material-icons text-sm">save</span>
                  GUARDAR
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
