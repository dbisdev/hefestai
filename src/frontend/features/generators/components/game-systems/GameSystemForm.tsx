/**
 * Game System Form Component
 * Single Responsibility: Unified form for create/edit game systems
 * DRY: Single form for both operations
 */

import React, { useState, useEffect, useRef } from 'react';
import { Button, Input } from '@shared/components/ui';
import type { GameSystem, CreateGameSystemRequest, UpdateGameSystemRequest } from '@core/types';

interface GameSystemFormProps {
  mode: 'create' | 'edit';
  initialData?: GameSystem | null;
  onSubmit: (data: CreateGameSystemRequest | UpdateGameSystemRequest) => Promise<GameSystem | null>;
  onCancel: () => void;
  isLoading: boolean;
}

export const GameSystemForm: React.FC<GameSystemFormProps> = ({
  mode,
  initialData,
  onSubmit,
  onCancel,
  isLoading,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);
  const [formData, setFormData] = useState({
    code: '',
    name: '',
    publisher: '',
    version: '',
    description: '',
    entityTypesInput: '',
  });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (mode === 'edit' && initialData) {
      setFormData({
        code: initialData.code,
        name: initialData.name,
        publisher: initialData.publisher || '',
        version: initialData.version || '',
        description: initialData.description || '',
        entityTypesInput: initialData.supportedEntityTypes.join(', '),
      });
    } else {
      setFormData({
        code: '',
        name: '',
        publisher: '',
        version: '',
        description: '',
        entityTypesInput: '',
      });
    }
    setError(null);
  }, [mode, initialData]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isLoading) {
        onCancel();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    modalRef.current?.focus();
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onCancel, isLoading]);

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!formData.code.trim() || !formData.name.trim()) {
      setError('Código y nombre son requeridos');
      return;
    }

    const supportedEntityTypes = formData.entityTypesInput
      .split(',')
      .map((t) => t.trim())
      .filter((t) => t.length > 0);

    const data = mode === 'create'
      ? {
          code: formData.code.trim().toLowerCase(),
          name: formData.name.trim(),
          publisher: formData.publisher.trim() || undefined,
          version: formData.version.trim() || undefined,
          description: formData.description.trim() || undefined,
          supportedEntityTypes,
        }
      : {
          name: formData.name.trim(),
          publisher: formData.publisher.trim() || undefined,
          version: formData.version.trim() || undefined,
          description: formData.description.trim() || undefined,
          supportedEntityTypes,
        };

    const result = await onSubmit(data as CreateGameSystemRequest | UpdateGameSystemRequest);
    if (!result) {
      setError('Error al guardar el sistema');
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div
        ref={modalRef}
        className="w-full max-w-lg max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        role="dialog"
        aria-modal="true"
        aria-labelledby="form-title"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="form-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">sports_esports</span>
            {mode === 'create' ? 'NUEVO_SISTEMA' : 'EDITAR_SISTEMA'}
          </h2>
          {!isLoading && (
            <button
              onClick={onCancel}
              className="material-icons text-sm hover:rotate-90 transition-transform"
              aria-label="Cerrar"
            >
              close
            </button>
          )}
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4 font-mono overflow-y-auto flex-1 custom-scrollbar">
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-3 text-danger text-xs flex items-center gap-2">
              <span className="material-icons text-sm">error</span>
              {error}
            </div>
          )}

         <div>
            <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">sports_esports</span>
              Nombre
            </label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => handleChange('name', e.target.value)}
              placeholder="Ej: Dungeons & Dragons 5e"
              className="w-full bg-black/40 border border-primary/30 p-2 text-primary 
                focus:outline-none focus:border-primary transition-colors text-sm
                placeholder:text-primary/30"
            />
          </div>

          <div>
            <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">business</span>
              Editorial
            </label>
            <input
              type="text"
              value={formData.publisher}
              onChange={(e) => handleChange('publisher', e.target.value)}
              placeholder="Ej: Wizards of the Coast"
              className="w-full bg-black/40 border border-primary/30 p-2 text-primary 
                focus:outline-none focus:border-primary transition-colors text-sm
                placeholder:text-primary/30"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Código"
              value={formData.code}
              onChange={(e) => handleChange('code', e.target.value)}
              placeholder="Ej: dark-trophy"
              disabled={mode === 'edit'}
              icon="tag"
            />
            <Input
              label="Versión"
              value={formData.version}
              onChange={(e) => handleChange('version', e.target.value)}
              placeholder="Ej: 5.0"
              icon="info"
            />
          </div>

          

          <div>
            <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">category</span>
              Tipos de Entidad (separados por coma)
            </label>
            <input
              type="text"
              value={formData.entityTypesInput}
              onChange={(e) => handleChange('entityTypesInput', e.target.value)}
              placeholder="character, npc, vehicle, mission"
              className="w-full bg-black/40 border border-primary/30 p-2 text-primary 
                focus:outline-none focus:border-primary transition-colors text-sm
                placeholder:text-primary/30"
            />
          </div>

          <div>
            <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">description</span>
              Descripción (Opcional)
            </label>
            <textarea
              value={formData.description}
              onChange={(e) => handleChange('description', e.target.value)}
              placeholder="Descripción del sistema de juego..."
              rows={3}
              className="w-full bg-black/40 border border-primary/30 p-2 text-primary 
                focus:outline-none focus:border-primary transition-colors text-sm
                placeholder:text-primary/30 resize-none"
            />
          </div>
        </form>

        <div className="p-4 border-t border-primary/20 flex justify-end gap-3 flex-shrink-0">         

          <button
            type="button"
            onClick={onCancel}
            disabled={isLoading}
            className="cursor-pointer px-4 py-2 border border-primary/40 text-primary/80 text-sm uppercase tracking-widest hover:bg-primary/10 transition-colors disabled:opacity-50"
          >
            Cancelar
          </button>

          <Button variant="primary" size="md" onClick={handleSubmit} isLoading={isLoading}>
            {mode === 'create' ? 'Crear' : 'Guardar'}
          </Button>
        </div>
      </div>
    </div>
  );
};
