/**
 * Game System Form Component
 * Single Responsibility: Unified form for create/edit game systems
 * DRY: Single form for both operations
 */

import React, { useState, useEffect } from 'react';
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
          code: formData.code.trim().toUpperCase(),
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
        className="bg-surface-dark border border-primary/30 rounded-lg max-w-lg w-full p-6 relative"
        role="dialog"
        aria-modal="true"
        aria-labelledby="form-title"
      >
        <div className="absolute top-0 left-0 w-4 h-4 border-t-2 border-l-2 border-primary rounded-tl-lg" />
        <div className="absolute top-0 right-0 w-4 h-4 border-t-2 border-r-2 border-primary rounded-tr-lg" />
        <div className="absolute bottom-0 left-0 w-4 h-4 border-b-2 border-l-2 border-primary rounded-bl-lg" />
        <div className="absolute bottom-0 right-0 w-4 h-4 border-b-2 border-r-2 border-primary rounded-br-lg" />

        <h2
          id="form-title"
          className="text-primary text-lg font-display uppercase tracking-wider mb-4 flex items-center gap-2"
        >
          <span className="material-icons">sports_esports</span>
          {mode === 'create' ? 'Nuevo Sistema' : 'Editar Sistema'}
        </h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Código"
              value={formData.code}
              onChange={(e) => handleChange('code', e.target.value)}
              placeholder="Ej: DND5E"
              disabled={mode === 'edit'}
              icon="tag"
            />
            <Input
              label="Nombre"
              value={formData.name}
              onChange={(e) => handleChange('name', e.target.value)}
              placeholder="Ej: Dungeons & Dragons 5e"
              icon="sports_esports"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Editorial"
              value={formData.publisher}
              onChange={(e) => handleChange('publisher', e.target.value)}
              placeholder="Ej: Wizards of the Coast"
              icon="business"
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
            <label className="block text-primary/70 text-[10px] uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">category</span>
              Tipos de Entidad (separados por coma)
            </label>
            <input
              type="text"
              value={formData.entityTypesInput}
              onChange={(e) => handleChange('entityTypesInput', e.target.value)}
              placeholder="character, npc, vehicle, mission"
              className="w-full bg-black/40 border border-primary/30 rounded p-2 text-primary 
                focus:outline-none focus:border-primary transition-colors text-sm
                placeholder:text-primary/30"
            />
          </div>

          <div>
            <label className="block text-primary/70 text-[10px] uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">description</span>
              Descripción
            </label>
            <textarea
              value={formData.description}
              onChange={(e) => handleChange('description', e.target.value)}
              placeholder="Descripción del sistema de juego..."
              rows={3}
              className="w-full bg-black/40 border border-primary/30 rounded p-2 text-primary 
                focus:outline-none focus:border-primary transition-colors text-sm
                placeholder:text-primary/30 resize-none"
            />
          </div>

          {error && (
            <div className="bg-danger/20 border border-danger/50 p-2 rounded text-danger text-xs">
              {error}
            </div>
          )}

          <div className="flex justify-end gap-3 pt-4 border-t border-primary/10">
            <Button variant="ghost" size="sm" type="button" onClick={onCancel} disabled={isLoading}>
              Cancelar
            </Button>
            <Button variant="primary" size="sm" type="submit" isLoading={isLoading}>
              {mode === 'create' ? 'Crear' : 'Guardar'}
            </Button>
          </div>
        </form>

        <button
          type="button"
          onClick={onCancel}
          className="absolute top-2 right-2 text-primary/40 hover:text-primary transition-colors cursor-pointer"
          aria-label="Cerrar"
        >
          <span className="material-icons text-sm">close</span>
        </button>
      </div>
    </div>
  );
};
