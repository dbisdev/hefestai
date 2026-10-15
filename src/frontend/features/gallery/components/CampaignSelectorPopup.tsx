/**
 * CampaignSelectorPopup Component
 * Floating dropdown for switching between campaigns and navigating
 * to campaign/game-system settings.
 *
 * Accessibility:
 * - role="dialog" with aria-modal and aria-label
 * - tabIndex={-1} for programmatic focus
 * - Close button and Escape handled by parent via forwardRef
 */

import React, { forwardRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { CampaignRole } from '@core/types';
import type { Campaign } from '@core/types';

export interface CampaignSelectorPopupProps {
  /** All campaigns the user belongs to */
  campaigns: Campaign[];
  /** Currently active campaign */
  activeCampaign: Campaign | null;
  /** Whether the current user is a master in the active campaign */
  isMaster: boolean;
  /** Called when a campaign is selected */
  onSelectCampaign: (campaignId: string) => void;
  /** Called when "new campaign" / "manage campaigns" is requested */
  onManageCampaigns: () => void;
  /** Called when the popup should close */
  onClose: () => void;
}

/**
 * Floating campaign switcher popup positioned near the sidebar.
 * Parent controls visibility via conditional rendering.
 */
export const CampaignSelectorPopup = forwardRef<HTMLDivElement, CampaignSelectorPopupProps>(
  ({ campaigns, activeCampaign, isMaster, onSelectCampaign, onManageCampaigns, onClose }, ref) => {
    const navigate = useNavigate();

    const handleConfigureCampaign = () => {
      onClose();
      if (activeCampaign) navigate(`/campaigns/${activeCampaign.id}`);
    };

    const handleGameSystems = () => {
      onClose();
      navigate('/game-systems');
    };

    return (
      <div
        ref={ref}
        role="dialog"
        aria-label="Selector de campaña"
        aria-modal="true"
        tabIndex={-1}
        className="absolute top-0 left-16 md:left-64 z-50 bg-surface-dark border border-primary p-4 shadow-2xl animate-glitch-in font-mono min-w-[250px]"
      >
        {/* Close button */}
        <button
          onClick={onClose}
          className="absolute top-2 right-2 text-primary/60 hover:text-primary"
          aria-label="Cerrar selector de campaña"
        >
          <span className="material-icons text-sm">close</span>
        </button>

        <h4 className="text-primary text-[10px] uppercase font-bold mb-3 flex items-center gap-2">
          <span className="material-icons text-sm">public</span>
          Seleccionar Campaña
        </h4>

        {campaigns.length === 0 ? (
          <div className="text-center py-4">
            <p className="text-[10px] text-primary/60 mb-3">No perteneces a ninguna campaña</p>
            <button
              onClick={onManageCampaigns}
              className="w-full bg-primary/20 border border-primary py-2 text-[10px] text-primary uppercase font-bold hover:bg-primary hover:text-black transition-colors"
            >
              CREAR/UNIRSE
            </button>
          </div>
        ) : (
          <>
            {/* Campaign list */}
            <div className="space-y-2 max-h-48 overflow-y-auto mb-3">
              {campaigns.map(campaign => (
                <button
                  key={campaign.id}
                  onClick={() => onSelectCampaign(campaign.id)}
                  className={`w-full text-left p-2 border transition-all ${
                    activeCampaign?.id === campaign.id
                      ? 'border-primary bg-primary/20 text-primary'
                      : 'border-primary/30 hover:border-primary text-white/80 hover:text-primary'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-[11px] font-bold uppercase truncate">{campaign.name}</span>
                    <span
                      className={`text-[8px] px-1 border ${
                        campaign.userRole === CampaignRole.Master
                          ? 'border-primary text-primary'
                          : 'border-yellow-500 text-yellow-500'
                      }`}
                    >
                      {campaign.userRole === CampaignRole.Master ? 'MASTER' : 'PLAYER'}
                    </span>
                  </div>
                </button>
              ))}
            </div>

            {/* Campaign settings button — Master only */}
            {isMaster && activeCampaign && (
              <button
                onClick={handleConfigureCampaign}
                className="w-full bg-cyan-500/10 border border-cyan-500/50 py-2 text-[10px] text-cyan-500 uppercase font-bold hover:bg-cyan-500/30 transition-colors mb-2 flex items-center justify-center gap-2"
              >
                <span className="material-icons text-xs">settings</span>
                CONFIGURAR CAMPAÑA
              </button>
            )}

            {/* Game systems button — Master only */}
            {isMaster && (
              <button
                onClick={handleGameSystems}
                className="w-full bg-purple-500/10 border border-purple-500/50 py-2 text-[10px] text-purple-400 uppercase font-bold hover:bg-purple-500/30 transition-colors mb-2 flex items-center justify-center gap-2"
              >
                <span className="material-icons text-xs">sports_esports</span>
                SISTEMAS DE JUEGO
              </button>
            )}

            <button
              onClick={onManageCampaigns}
              className="w-full bg-primary/10 border border-primary/50 py-2 text-[10px] text-primary/80 uppercase font-bold hover:bg-primary hover:text-black transition-colors"
            >
              + NUEVA CAMPAÑA
            </button>
          </>
        )}
      </div>
    );
  }
);

CampaignSelectorPopup.displayName = 'CampaignSelectorPopup';
