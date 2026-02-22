/**
 * Campaign Detail Modal Component
 * Mobile-friendly modal for displaying campaign details
 * Used on mobile view instead of the side panel
 * Cyberpunk terminal aesthetics matching the rest of the application
 */

import React, { useEffect, useRef } from 'react';
import { Button } from '@shared/components/ui';
import type { Campaign, GameSystem, CampaignMember } from '@core/types';
import { CampaignRole } from '@core/types';

interface CampaignDetailModalProps {
  campaign: Campaign;
  gameSystems: GameSystem[];
  campaignMembers: Record<string, CampaignMember[]>;
  activeCampaign: Campaign | null;
  operationInProgress: string | null;
  showEditForm: boolean;
  isLoadingGameSystems: boolean;
  onClose: () => void;
  onActivate: (campaignId: string) => void;
  onEdit: (campaign: Campaign) => void;
  onLeave: (campaign: Campaign) => void;
  onSettings: (campaign: Campaign) => void;
}

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });
};

const getRoleInfo = (role?: CampaignRole): { label: string; color: string; icon: string } => {
  if (role === CampaignRole.Master) {
    return { label: 'MASTER', color: 'text-cyan-400', icon: 'star' };
  }
  return { label: 'JUGADOR', color: 'text-yellow-400', icon: 'person' };
};

const getGameSystemName = (gameSystemId: string, gameSystems: GameSystem[]): string => {
  const system = gameSystems.find(gs => gs.id === gameSystemId);
  return system?.name || 'Sistema desconocido';
};

export const CampaignDetailModal: React.FC<CampaignDetailModalProps> = ({
  campaign,
  gameSystems,
  campaignMembers,
  activeCampaign,
  operationInProgress,
  showEditForm,
  isLoadingGameSystems,
  onClose,
  onActivate,
  onEdit,
  onLeave,
  onSettings,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);
  const roleInfo = getRoleInfo(campaign.userRole);
  const isActive = activeCampaign?.id === campaign.id;
  const isOperating = operationInProgress === campaign.id;
  const members = campaignMembers[campaign.id];

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
      aria-labelledby="campaign-detail-title"
    >
      <div
        ref={modalRef}
        className="w-full max-w-md max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2 id="campaign-detail-title" className="text-xs uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">auto_stories</span>
            CAMPAÑA_SELECCIONADA
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
            <div className="flex items-center gap-2 flex-wrap">
              <span className={`font-mono text-sm ${roleInfo.color}`}>
                <span className="material-icons text-xs align-middle mr-1">{roleInfo.icon}</span>
                {roleInfo.label}
              </span>
              <span className={`px-2 py-0.5 border ${
                campaign.isActive
                  ? 'border-green-500/40 text-green-400'
                  : 'border-yellow-500/40 text-yellow-400'
              }`}>
                {campaign.isActive ? 'ONLINE' : 'OFFLINE'}
              </span>
              {isActive && (
                <span className="px-2 py-0.5 border border-primary/60 text-primary bg-primary/10">
                  ACTIVA
                </span>
              )}
            </div>

            <h3 className="text-primary font-bold text-lg">{campaign.name}</h3>

            <div className="flex justify-between">
              <span className="text-primary/40">ID:</span>
              <span className="text-primary font-mono">{campaign.id.substring(0, 8)}...</span>
            </div>
            <div className="flex justify-between">
              <span className="text-primary/40">Sistema:</span>
              <span className="text-primary/70 truncate max-w-[180px]">
                {isLoadingGameSystems ? '...' : getGameSystemName(campaign.gameSystemId, gameSystems)}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-primary/40">Creada:</span>
              <span className="text-primary/70">{formatDate(campaign.createdAt)}</span>
            </div>

            {campaign.description && (
              <div className="pt-2 mt-2 border-t border-primary/20">
                <span className="text-primary/40 uppercase text-[10px]">Descripción:</span>
                <p className="text-primary/70 text-xs mt-1">{campaign.description}</p>
              </div>
            )}

            {members && members.length > 0 && (
              <div className="pt-2 mt-2 border-t border-primary/20">
                <span className="text-primary/40 text-xs uppercase">Miembros ({members.length}):</span>
                <div className="mt-1 space-y-1 max-h-24">
                  {[...members]
                    .sort((a, b) => {
                      if (a.role === CampaignRole.Master && b.role !== CampaignRole.Master) return -1;
                      if (a.role !== CampaignRole.Master && b.role === CampaignRole.Master) return 1;
                      return a.displayName.localeCompare(b.displayName);
                    })
                    .map((member) => (
                      <div key={member.id} className="flex items-center gap-2 text-xs">
                        <span className={`material-icons text-xs ${member.role === CampaignRole.Master ? 'text-cyan-400' : 'text-yellow-400'}`}>
                          {member.role === CampaignRole.Master ? 'star' : 'person'}
                        </span>
                        <span className="text-primary/70 truncate">{member.displayName}</span>
                      </div>
                    ))}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="p-4 border-t border-primary/20 space-y-2 flex-shrink-0">
          {activeCampaign?.id !== campaign.id && (
            <Button
              onClick={() => {
                onActivate(campaign.id);
                onClose();
              }}
              variant="primary"
              size="sm"
              className="w-full"
              disabled={isOperating}
            >
              <span className="material-icons text-sm mr-2">play_arrow</span>
              ACTIVAR Y ENTRAR
            </Button>
          )}

          {campaign.userRole === CampaignRole.Master && (
            <Button
              onClick={() => {
                onEdit(campaign);
                onClose();
              }}
              variant="secondary"
              size="sm"
              className="w-full"
              disabled={showEditForm}
            >
              <span className="material-icons text-sm mr-2">edit</span>
              EDITAR
            </Button>
          )}

          {/* {campaign.userRole === CampaignRole.Master && (
            <Button
              onClick={() => {
                onSettings(campaign);
                onClose();
              }}
              variant="secondary"
              size="sm"
              className="w-full"
            >
              <span className="material-icons text-sm mr-2">settings</span>
              CONFIGURACIÓN AVANZADA
            </Button>
          )} */}

          <Button
            onClick={() => {
              onLeave(campaign);
              onClose();
            }}
            variant="danger"
            size="sm"
            className="w-full"
            disabled={isOperating}
          >
            <span className="material-icons text-sm mr-2">
              {campaign.userRole === CampaignRole.Master ? 'delete' : 'logout'}
            </span>
            {campaign.userRole === CampaignRole.Master ? 'ELIMINAR' : 'ABANDONAR'}
          </Button>
        </div>
      </div>
    </div>
  );
};
