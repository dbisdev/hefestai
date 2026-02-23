/**
 * Entity Edit Modal Component
 * Allows editing entity details (name, description, visibility, attributes)
 * Visibility editing restricted to entity owner or campaign master
 * Ownership transfer available to entity owner or campaign master
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { entityService, campaignService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { buildLabelMapFromFields, getDisplayLabel, categorizeAttributes, isNestedObject } from '@core/utils';
import { ImageSourceSelector, type ImageSourceMode } from '@shared/components/ui';
import type { LoreEntity, DynamicStats, FieldDefinition, CampaignMember } from '@core/types';
import { VisibilityLevel, CampaignRole } from '@core/types';

interface EntityEditModalProps {
  /** Entity to edit */
  entity: LoreEntity;
  /** Whether the current user can edit visibility (owner or campaign master) */
  canEditVisibility: boolean;
  /** Whether the current user can edit ownership (owner or campaign master) */
  canEditOwnership?: boolean;
  /** Current user ID for ownership comparison */
  currentUserId?: string;
  /** Field definitions from template for display name mapping */
  fieldDefinitions?: FieldDefinition[];
  /** Callback when modal is closed */
  onClose: () => void;
  /** Callback when entity is successfully saved */
  onSave: (updatedEntity: LoreEntity) => void;
}

/**
 * Modal component for editing entity details
 * Supports editing name, description, visibility, image URL, and dynamic attributes
 */
export const EntityEditModal: React.FC<EntityEditModalProps> = ({
  entity,
  canEditVisibility,
  canEditOwnership = false,
  currentUserId,
  fieldDefinitions,
  onClose,
  onSave,
}) => {
  const { activeCampaignId } = useCampaign();
  const modalRef = useRef<HTMLDivElement>(null);
  
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Campaign members for ownership transfer
  const [campaignMembers, setCampaignMembers] = useState<CampaignMember[]>([]);
  const [isLoadingMembers, setIsLoadingMembers] = useState(false);
  const [selectedOwnerId, setSelectedOwnerId] = useState<string>(entity.ownerId);
  
  // Build label map from field definitions
  const labelMap = fieldDefinitions ? buildLabelMapFromFields(fieldDefinitions) : undefined;
  
  // Form state for basic fields
  const [formData, setFormData] = useState({
    name: entity.name,
    description: entity.description || '',
    visibility: entity.visibility,
    imageUrl: entity.imageUrl || '',
  });
  
  // Image upload state
  const [imageMode, setImageMode] = useState<ImageSourceMode>('none');
  const [uploadedImageData, setUploadedImageData] = useState<string | null>(null);
  
  // State for editable attributes (deep copy to avoid mutating original)
  const [attributes, setAttributes] = useState<DynamicStats>(
    entity.attributes ? JSON.parse(JSON.stringify(entity.attributes)) : {}
  );

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
   * Fetch campaign members for ownership transfer dropdown
   * Only fetches if user can edit ownership
   */
  useEffect(() => {
    const fetchMembers = async () => {
      if (!canEditOwnership || !activeCampaignId) {
        return;
      }
      
      setIsLoadingMembers(true);
      try {
        const members = await campaignService.getMembers(activeCampaignId);
        setCampaignMembers(members);
      } catch (err) {
        console.error('Failed to fetch campaign members:', err);
        setCampaignMembers([]);
      } finally {
        setIsLoadingMembers(false);
      }
    };
    
    fetchMembers();
  }, [canEditOwnership, activeCampaignId]);

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
   * Handle attribute value change (for numeric and string values)
   */
  const handleAttributeChange = useCallback((key: string, value: string | number) => {
    setAttributes(prev => ({ ...prev, [key]: value }));
    setError(null);
  }, []);

  /**
   * Handle nested attribute value change (for objects like SKILLS)
   */
  const handleNestedAttributeChange = useCallback((
    parentKey: string, 
    childKey: string, 
    value: string | number
  ) => {
    setAttributes(prev => {
      const parent = prev[parentKey];
      if (isNestedObject(parent)) {
        return {
          ...prev,
          [parentKey]: {
            ...parent,
            [childKey]: value,
          },
        };
      }
      return prev;
    });
    setError(null);
  }, []);

  /**
   * Handle form submission
   * Transfers ownership if changed, then updates entity details if user still has permission.
   * Note: If a player transfers ownership to someone else, they lose edit permission,
   * so we skip the entity update in that case.
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
      let updatedEntity: LoreEntity;
      const ownershipChanged = canEditOwnership && selectedOwnerId !== entity.ownerId;
      
      // Check if user will still have edit permission after ownership transfer
      // Masters can always edit (non-player-owned entities), owners can edit their own
      const currentUserIsOwner = currentUserId === entity.ownerId;
      const willTransferToOther = ownershipChanged && selectedOwnerId !== currentUserId;
      
      // If ownership changed, transfer ownership FIRST
      if (ownershipChanged) {
        updatedEntity = await entityService.transferOwnership(
          activeCampaignId,
          entity.id,
          { newOwnerId: selectedOwnerId }
        );
        
        // If current user was the owner (not master) and transferred to someone else,
        // they lose edit permission - just return the transferred entity
        if (currentUserIsOwner && willTransferToOther) {
          onSave(updatedEntity);
          return;
        }
      }

      // Update the entity details
      const finalImageUrl = imageMode === 'upload' && uploadedImageData
        ? `data:image/webp;base64,${uploadedImageData}`
        : formData.imageUrl.trim() || undefined;

      updatedEntity = await entityService.update(activeCampaignId, entity.id, {
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        visibility: formData.visibility,
        imageUrl: finalImageUrl,
        attributes: Object.keys(attributes).length > 0 ? attributes : undefined,
        metadata: entity.metadata,
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

  /**
   * Categorize attributes by type for proper rendering
   */
  const { numeric: numericAttrs, string: stringAttrs, nested: nestedAttrs } = categorizeAttributes(attributes);
  const hasAttributes = numericAttrs.length > 0 || stringAttrs.length > 0 || nestedAttrs.length > 0;

  return (
    <div 
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
    >
      <div 
        ref={modalRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-modal-title"
        className="w-full max-w-2xl max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col"
      >
        {/* Header */}
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
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

        {/* Form - Scrollable */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 font-mono overflow-y-auto flex-1 custom-scrollbar">
          {/* Error Message */}
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-3 text-danger text-xs">
              <span className="material-icons text-sm mr-1 align-middle">error</span>
              {error}
            </div>
          )}

          {/* Image Section */}
          <div className="space-y-4">
            <p className="text-[9px] text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
              // IMAGEN
            </p>
            
            {/* Current Image Preview */}
            <div className="relative w-full aspect-square border border-primary/30 p-1 bg-black shadow-[0_0_15px_rgba(37,244,106,0.1)]">
              <img 
                src={entity.imageUrl || 'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop'} 
                alt={`Imagen de ${entity.name}`} 
                className="w-full h-full object-cover" 
              />
              <div className="absolute top-2 left-2 px-1 bg-primary/80 text-black text-[8px] font-bold">CURRENT_IMAGE</div>
              <div className="absolute bottom-2 right-2 flex gap-1">
                {[...Array(3)].map((_, i) => <div key={i} className="w-1.5 h-1.5 bg-primary/40 animate-pulse" style={{ animationDelay: `${i*0.1}s` }} />)}
              </div>
            </div>

            {/* Image Source Selector */}
            <ImageSourceSelector
              mode={imageMode}
              onModeChange={setImageMode}
              onImageUpload={setUploadedImageData}
              uploadedImage={uploadedImageData}
              disabled={isSaving}
            />
          </div>

          {/* Basic Info Section */}
          <div className="space-y-4">
            <p className="text-[9px] text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
              // INFORMACIÓN_BÁSICA
            </p>

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
                rows={3}
                disabled={isSaving}
                className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 resize-none disabled:opacity-50"
              />
            </div>
          </div>

          {/* Visibility Field - Only shown if user can edit */}
          {canEditVisibility ? (
            <div className="space-y-2">
              <p className="text-xs text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
                // VISIBILIDAD
              </p>
              <div className="grid grid-cols-2 gap-2">
                {visibilityOptions.map((option) => (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => handleChange('visibility', option.value)}
                    disabled={isSaving}
                    className={`cursor-pointer p-2 border text-left transition-colors ${
                      formData.visibility === option.value
                        ? 'border-primary bg-primary/20 text-primary'
                        : 'border-primary/30 hover:border-primary/60 text-primary/60'
                    } disabled:opacity-50`}
                  >
                    <span className="text-sm font-bold block">{option.label}</span>
                    <span className="text-xs text-primary/40">{option.description}</span>
                  </button>
                ))}
              </div>
            </div>
          ) : (
            <div className="bg-black/40 border border-primary/10 p-3">
              <p className="text-xs text-primary/40 uppercase mb-1">Visibilidad (solo lectura)</p>
              <p className="text-sm text-primary uppercase font-bold">
                {visibilityOptions.find(o => o.value === formData.visibility)?.label || 'DESCONOCIDO'}
              </p>
              <p className="text-xs text-primary/30 mt-1">
                Solo el propietario o el master pueden cambiar la visibilidad
              </p>
            </div>
          )}

          {/* Ownership Transfer Section - Only shown if user can edit ownership */}
          {canEditOwnership ? (
            <div className="space-y-2">
              <p className="text-xs text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
                // PROPIETARIO
              </p>
              {isLoadingMembers ? (
                <div className="flex items-center gap-2 p-3 bg-black/40 border border-primary/20">
                  <span className="material-icons text-sm text-primary/60 animate-spin">sync</span>
                  <span className="text-xs text-primary/60">Cargando miembros...</span>
                </div>
              ) : (
                <div className="relative">
                  <select
                    id="entity-owner"
                    value={selectedOwnerId}
                    onChange={(e) => setSelectedOwnerId(e.target.value)}
                    disabled={isSaving}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none disabled:opacity-50 appearance-none cursor-pointer"
                  >
                    {campaignMembers.map((member) => (
                      <option key={member.userId} value={member.userId}>
                        {member.displayName} ({member.role === CampaignRole.Master ? 'MASTER' : 'PLAYER'})
                        {member.userId === entity.ownerId ? ' - Actual' : ''}
                      </option>
                    ))}
                  </select>
                  <span className="material-icons absolute right-3 top-1/2 -translate-y-1/2 text-primary/60 pointer-events-none text-sm">
                    expand_more
                  </span>
                </div>
              )}
              {selectedOwnerId !== entity.ownerId && (
                <div className="bg-yellow-500/10 border border-yellow-500/30 p-2 text-[10px] text-yellow-500 flex items-center gap-2">
                  <span className="material-icons text-sm">warning</span>
                  La propiedad se transferira al guardar
                </div>
              )}
            </div>
          ) : (
            <div className="bg-black/40 border border-primary/10 p-3">
              <p className="text-xs text-primary/40 uppercase mb-1">Propietario (solo lectura)</p>
              <p className="text-sm text-primary uppercase font-bold">
                {entity.ownerName || 'Desconocido'}
              </p>
              <p className="text-xs text-primary/30 mt-1">
                Solo el propietario o el master pueden transferir la propiedad
              </p>
            </div>
          )}

          {/* Dynamic Attributes Section */}
          {hasAttributes && (
            <div className="space-y-3">
              <p className="text-xs text-primary/40 uppercase tracking-[0.2em] font-bold border-b border-primary/20 pb-1">
                // ATRIBUTOS
              </p>

              {/* Numeric Attributes Grid */}
              {numericAttrs.length > 0 && (
                <div className="grid grid-cols-3 gap-2">
                  {numericAttrs.map(([key, value]) => (
                    <div key={key} className="bg-black/40 border border-primary/20 p-2">
                      <label 
                        htmlFor={`attr-${key}`}
                        className="block text-xs text-primary/40 uppercase mb-1 truncate"
                        title={key}
                      >
                        {getDisplayLabel(key, labelMap)}
                      </label>
                      <input
                        id={`attr-${key}`}
                        type="number"
                        value={value}
                        onChange={(e) => handleAttributeChange(key, parseFloat(e.target.value) || 0)}
                        disabled={isSaving}
                        className="w-full bg-black/60 border border-primary/30 text-primary p-2 text-sm text-center font-bold focus:border-primary focus:outline-none disabled:opacity-50"
                      />
                    </div>
                  ))}
                </div>
              )}

              {/* String Attributes */}
              {stringAttrs.map(([key, value]) => (
                <div key={key} className="bg-black/40 border border-primary/20 p-3">
                  <label 
                    htmlFor={`attr-${key}`}
                    className="block text-xs text-primary/40 uppercase mb-1"
                    title={key}
                  >
                    {getDisplayLabel(key, labelMap)}
                  </label>
                  <textarea
                    id={`attr-${key}`}
                    value={value}
                    onChange={(e) => handleAttributeChange(key, e.target.value)}
                    rows={2}
                    disabled={isSaving}
                    className="w-full bg-black/60 border border-primary/30 text-primary p-2 text-sm focus:border-primary focus:outline-none resize-none disabled:opacity-50"
                  />
                </div>
              ))}

              {/* Nested Attributes (like SKILLS) */}
              {nestedAttrs.map(([parentKey, nestedObj]) => (
                <div key={parentKey} className="bg-black/40 border border-primary/20 p-3">
                  <p 
                    className="text-xs text-primary/40 uppercase mb-2 flex items-center gap-1"
                    title={parentKey}
                  >
                    <span className="material-icons text-xs">folder_open</span>
                    {getDisplayLabel(parentKey, labelMap)}
                  </p>
                  <div className="grid grid-cols-2 gap-2">
                    {Object.entries(nestedObj).map(([childKey, childValue]) => (
                      <div key={childKey} className="flex items-center gap-2">
                        <label 
                          htmlFor={`attr-${parentKey}-${childKey}`}
                          className="text-xs text-primary/60 uppercase flex-1 truncate"
                          title={childKey}
                        >
                          {getDisplayLabel(childKey, labelMap)}
                        </label>
                        {typeof childValue === 'number' ? (
                          <input
                            id={`attr-${parentKey}-${childKey}`}
                            type="number"
                            value={childValue}
                            onChange={(e) => handleNestedAttributeChange(
                              parentKey, 
                              childKey, 
                              parseFloat(e.target.value) || 0
                            )}
                            disabled={isSaving}
                            className="w-16 bg-black/60 border border-primary/30 text-primary p-1 text-sm text-center font-bold focus:border-primary focus:outline-none disabled:opacity-50"
                          />
                        ) : (
                          <input
                            id={`attr-${parentKey}-${childKey}`}
                            type="text"
                            value={String(childValue)}
                            onChange={(e) => handleNestedAttributeChange(
                              parentKey, 
                              childKey, 
                              e.target.value
                            )}
                            disabled={isSaving}
                            className="flex-1 bg-black/60 border border-primary/30 text-primary p-1 text-sm focus:border-primary focus:outline-none disabled:opacity-50"
                          />
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Entity Type (read-only info) */}
          <div className="bg-black/40 border border-primary/10 p-3">
            <p className="text-xs text-primary/40 uppercase mb-1">Tipo de Entidad</p>
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
              className="cursor-pointer flex-1 py-3 border border-primary/40 text-primary/80 text-xs uppercase tracking-widest hover:bg-primary/10 transition-colors disabled:opacity-50"
            >
              CANCELAR
            </button>
            <button
              type="submit"
              disabled={isSaving}
              className="cursor-pointer flex-1 py-3 bg-primary text-black text-xs uppercase tracking-widest font-bold hover:bg-primary/80 transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
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
