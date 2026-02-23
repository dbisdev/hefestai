/**
 * Campaign Create Modal Component
 * Modal for creating new campaigns
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useState, useEffect, useRef } from 'react';
import type { GameSystem } from '@core/types';

interface CampaignCreateModalProps {
  gameSystems: GameSystem[];
  isLoadingGameSystems: boolean;
  isLoading: boolean;
  onClose: () => void;
  onCreate: (name: string, description: string | undefined, gameSystemId: string) => Promise<void>;
}

export const CampaignCreateModal: React.FC<CampaignCreateModalProps> = ({
  gameSystems,
  isLoadingGameSystems,
  isLoading,
  onClose,
  onCreate,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [gameSystemId, setGameSystemId] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (gameSystems.length > 0 && !gameSystemId) {
      setGameSystemId(gameSystems[0].id);
    }
  }, [gameSystems, gameSystemId]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isLoading) {
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
  }, [onClose, isLoading]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError('El nombre es requerido');
      return;
    }

    if (!gameSystemId) {
      setError('Debes seleccionar un sistema de juego');
      return;
    }

    try {
      await onCreate(name.trim(), description.trim() || undefined, gameSystemId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al crear la campaña');
    }
  };

  return (
    <div
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="campaign-create-title"
    >
      <div
        ref={modalRef}
        className="w-full max-w-md max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="campaign-create-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">auto_stories</span>
            NUEVA_CAMPAÑA
          </h2>
          {!isLoading && (
            <button
              onClick={onClose}
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
              <span className="material-icons text-xs mr-1 align-middle">edit</span>
              Nombre *
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Nombre de la campaña"
              className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/30"
              disabled={isLoading}
              autoFocus
            />
          </div>

          <div>
            <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
              <span className="material-icons text-sm mr-1 align-middle">sports_esports</span>
              Sistema de Juego *
            </label>
            {isLoadingGameSystems ? (
              <div className="w-full bg-black/40 border border-primary/30 p-3 text-sm text-primary/40">
                Cargando sistemas...
              </div>
            ) : gameSystems.length === 0 ? (
              <div className="w-full bg-black/40 border border-danger/30 p-3 text-sm text-danger">
                No hay sistemas disponibles
              </div>
            ) : (
              <select
                value={gameSystemId}
                onChange={(e) => setGameSystemId(e.target.value)}
                className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none"
                disabled={isLoading}
              >
                {gameSystems.map((gs) => (
                  <option key={gs.id} value={gs.id}>
                    {gs.name}
                  </option>
                ))}
              </select>
            )}
          </div>

          <div>
            <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
              <span className="material-icons text-xs mr-1 align-middle">description</span>
              Descripción
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Descripción de la campaña..."
              rows={3}
              className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/30 resize-none"
              disabled={isLoading}
            />
          </div>
        </form>

        <div className="p-4 border-t border-primary/20 flex justify-end gap-3 flex-shrink-0">
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="cursor-pointer px-4 py-2 border border-primary/40 text-primary/80 text-sm uppercase tracking-widest hover:bg-primary/10 transition-colors disabled:opacity-50"
          >
            Cancelar
          </button>
          
          <button
            type="button"
            onClick={handleSubmit}
            disabled={isLoading || isLoadingGameSystems || gameSystems.length === 0}
            className="cursor-pointer px-4 py-2 bg-primary text-black text-sm uppercase tracking-widest font-bold hover:bg-primary/80 transition-colors disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <span className="material-icons text-sm animate-spin">sync</span>
                Creando...
              </>
            ) : (
              <>
                <span className="material-icons text-sm">add</span>
                Crear
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
