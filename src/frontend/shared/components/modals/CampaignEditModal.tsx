/**
 * Campaign Edit Modal Component
 * Modal for editing existing campaigns with status management and danger zone
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useState, useEffect, useRef } from 'react';
import type { Campaign, UpdateCampaignInput, CampaignMember } from '@core/types';
import { CampaignRole } from '@core/types';

interface CampaignEditModalProps {
  campaign: Campaign;
  gameSystemName: string;
  members?: CampaignMember[];
  isLoading: boolean;
  isTogglingStatus?: boolean;
  isRegeneratingCode?: boolean;
  isDeleting?: boolean;
  onClose: () => void;
  onSave: (data: UpdateCampaignInput) => Promise<void>;
  onToggleStatus?: () => Promise<void>;
  onConfirmRegenerate?: () => void;
  onConfirmDelete?: () => void;
}

export const CampaignEditModal: React.FC<CampaignEditModalProps> = ({
  campaign,
  gameSystemName,
  members,
  isLoading,
  isTogglingStatus = false,
  isRegeneratingCode = false,
  isDeleting = false,
  onClose,
  onSave,
  onToggleStatus,
  onConfirmRegenerate,
  onConfirmDelete,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);
  const [name, setName] = useState(campaign.name);
  const [description, setDescription] = useState(campaign.description || '');
  const [error, setError] = useState<string | null>(null);
  const [codeCopied, setCodeCopied] = useState(false);

  const isAnyOperationInProgress = isLoading || isTogglingStatus || isRegeneratingCode || isDeleting;

  useEffect(() => {
    setName(campaign.name);
    setDescription(campaign.description || '');
    setError(null);
  }, [campaign]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isAnyOperationInProgress) {
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
  }, [onClose, isAnyOperationInProgress]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError('El nombre es requerido');
      return;
    }

    try {
      await onSave({
        name: name.trim(),
        description: description.trim() || undefined,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al guardar los cambios');
    }
  };

  const handleCopyCode = () => {
    if (campaign.joinCode) {
      navigator.clipboard.writeText(campaign.joinCode);
      setCodeCopied(true);
      setTimeout(() => setCodeCopied(false), 2000);
    }
  };

  const sortedMembers = members
    ? [...members].sort((a, b) => {
        if (a.role === CampaignRole.Master && b.role !== CampaignRole.Master) return -1;
        if (a.role !== CampaignRole.Master && b.role === CampaignRole.Master) return 1;
        return a.displayName.localeCompare(b.displayName);
      })
    : [];

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('es-ES', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  return (
    <div
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="campaign-edit-title"
    >
      <div
        ref={modalRef}
        className="w-full max-w-lg max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="campaign-edit-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">edit</span>
            EDITAR_CAMPAÑA
          </h2>
          {!isAnyOperationInProgress && (
            <button
              onClick={onClose}
              className="material-icons text-sm hover:rotate-90 transition-transform"
              aria-label="Cerrar"
            >
              close
            </button>
          )}
        </div>

        <div className="p-4 space-y-4 font-mono overflow-y-auto flex-1 custom-scrollbar">
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-3 text-danger text-xs flex items-center gap-2">
              <span className="material-icons text-sm">error</span>
              {error}
            </div>
          )}

          {/* Datos de la Campaña */}
          <div className="space-y-3">
            <h3 className="text-[10px] text-primary/60 uppercase tracking-widest flex items-center gap-1">
              <span className="material-icons text-xs">edit</span>
              Datos de la Campaña
            </h3>

            <div>
              <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
                Nombre *
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Nombre de la campaña"
                className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-primary focus:outline-none placeholder:text-primary/30"
                disabled={isAnyOperationInProgress}
              />
            </div>

            <div>
              <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
                Sistema de Juego
              </label>
              <input
                type="text"
                value={gameSystemName}
                className="w-full bg-black/20 border border-primary/20 text-primary/50 p-2 text-sm cursor-not-allowed"
                disabled
              />
            </div>

            <div>
              <label className="block text-primary/70 text-sm uppercase mb-1 tracking-wider">
                Descripción
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Descripción de la campaña..."
                rows={2}
                className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-primary focus:outline-none placeholder:text-primary/30 resize-none"
                disabled={isAnyOperationInProgress}
              />
            </div>
          </div>

          {/* Estado y Acciones */}
          <div className="border-t border-primary/20 pt-4 space-y-3">
            <h3 className="text-xs text-primary/60 uppercase tracking-widest flex items-center gap-1">
              <span className="material-icons text-xs">settings</span>
              Estado y Acciones
            </h3>

            <div className="grid grid-cols-2 gap-2">
              {/* Estado */}
              <div className="bg-black/40 border border-primary/20 p-2">
                <p className="text-sm text-primary/40 uppercase mb-1">Estado</p>
                <div className="flex items-center justify-between">
                  <span className={`text-md font-bold uppercase ${campaign.isActive ? 'text-green-400' : 'text-yellow-400'}`}>
                    {campaign.isActive ? 'ACTIVA' : 'INACTIVA'}
                  </span>
                  {onToggleStatus && (
                    <button
                      onClick={onToggleStatus}
                      disabled={isAnyOperationInProgress}
                      className={`cursor-pointer px-2 py-0.5 text-[9px] uppercase border transition-colors ${
                        campaign.isActive
                          ? 'border-yellow-500/60 text-yellow-500 hover:bg-yellow-500/20'
                          : 'border-green-500/60 text-green-500 hover:bg-green-500/20'
                      } disabled:opacity-50`}
                    >
                      {isTogglingStatus ? '...' : campaign.isActive ? 'Desactivar' : 'Activar'}
                    </button>
                  )}
                </div>
              </div>

              {/* Código de Acceso */}
              <div className="bg-black/40 border border-primary/20 p-2">
                <p className="text-sm text-primary/40 uppercase mb-1">Código</p>
                <div className="flex items-center gap-1">
                  <span className="text-primary font-mono font-bold flex-1 text-md tracking-wider truncate">
                    {campaign.joinCode || '------'}
                  </span>
                  <button
                    onClick={handleCopyCode}
                    disabled={!campaign.joinCode || codeCopied}
                    className={`cursor-pointer material-icons text-sm transition-colors ${
                      codeCopied ? 'text-green-400' : 'text-primary/60 hover:text-primary'
                    } disabled:opacity-30`}
                    title="Copiar código"
                  >
                    {codeCopied ? 'check' : 'content_copy'}
                  </button>
                  {onConfirmRegenerate && (
                    <button
                      onClick={onConfirmRegenerate}
                      disabled={isAnyOperationInProgress}
                      className="cursor-pointer material-icons text-cyan-500/60 hover:text-cyan-500 text-sm disabled:opacity-30"
                      title="Regenerar código"
                    >
                      {isRegeneratingCode ? (
                        <span className="animate-spin">sync</span>
                      ) : (
                        'refresh'
                      )}
                    </button>
                  )}
                </div>
              </div>

              {/* Creada */}
              <div className="bg-black/40 border border-primary/20 p-2">
                <p className="text-sm text-primary/40 uppercase mb-1">Creada</p>
                <span className="text-primary/70 text-md">
                  {formatDate(campaign.createdAt)}
                </span>
              </div>

              {/* Miembros count */}
              <div className="bg-black/40 border border-primary/20 p-2">
                <p className="text-sm text-primary/40 uppercase mb-1">Miembros</p>
                <span className="text-primary font-bold text-md">
                  {members?.length || 0}
                </span>
              </div>
            </div>
          </div>

          {/* Lista de Miembros */}
          {sortedMembers.length > 0 && (
            <div className="border-t border-primary/20 pt-4 space-y-2">
              <h3 className="text-xs text-primary/60 uppercase tracking-widest flex items-center gap-1">
                <span className="material-icons text-xs">group</span>
                Miembros ({sortedMembers.length})
              </h3>
              <div className="custom-scrollbar">
                {sortedMembers.map((member) => (
                  <div key={member.id} className="flex items-center gap-2 text-sm bg-black/20 p-1.5">
                    <span className={`material-icons text-xs ${
                      member.role === CampaignRole.Master ? 'text-cyan-400' : 'text-yellow-400'
                    }`}>
                      {member.role === CampaignRole.Master ? 'star' : 'person'}
                    </span>
                    <span className="text-primary/70 truncate">{member.displayName}</span>
                    <span className={`ml-auto text-xs uppercase ${
                      member.role === CampaignRole.Master ? 'text-cyan-400/60' : 'text-yellow-400/60'
                    }`}>
                      {member.role === CampaignRole.Master ? 'Master' : 'Jugador'}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Zona de Peligro */}
          {onConfirmDelete && (
            <div className="border-t border-danger/30 pt-4">
              <h3 className="text-xs text-danger/60 uppercase tracking-widest flex items-center gap-1 mb-2">
                <span className="material-icons text-xs">warning</span>
                Zona de Peligro
              </h3>
              <button
                onClick={onConfirmDelete}
                disabled={isAnyOperationInProgress}
                className="cursor-pointer w-full py-2 border border-danger/60 text-danger text-sm uppercase tracking-widest hover:bg-danger hover:text-white transition-all disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {isDeleting ? (
                  <>
                    <span className="material-icons text-sm animate-spin">sync</span>
                    Eliminando...
                  </>
                ) : (
                  <>
                    <span className="material-icons text-sm">delete_forever</span>
                    Eliminar Campaña
                  </>
                )}
              </button>
            </div>
          )}
        </div>

        <div className="p-4 border-t border-primary/20 flex justify-end gap-3 flex-shrink-0">
          <button
            type="button"
            onClick={onClose}
            disabled={isAnyOperationInProgress}
            className="cursor-pointer px-4 py-2 border border-primary/40 text-primary/80 text-sm uppercase tracking-widest hover:bg-primary/10 transition-colors disabled:opacity-50"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={isAnyOperationInProgress}
            className="cursor-pointer px-4 py-2 bg-primary text-black text-sm uppercase tracking-widest font-bold hover:bg-primary/80 transition-colors disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <span className="material-icons text-sm animate-spin">sync</span>
                Guardando...
              </>
            ) : (
              <>
                <span className="material-icons text-sm">save</span>
                Guardar
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
