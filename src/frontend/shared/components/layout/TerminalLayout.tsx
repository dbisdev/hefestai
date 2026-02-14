/**
 * Terminal Layout Component
 * Main layout wrapper with standardized header, footer, and terminal styling.
 * 
 * Header Left: Section icon, title, subtitle with user name and campaign selector
 * Header Right: Dice roller, Rules search, Logout, Back to Hub
 * Footer Left: User role, connection status, encryption status
 */

import React, { useState, useCallback } from 'react';
import DiceRoller from '../../../components/DiceRoller';
import RuleQuery from '../../../components/RuleQuery';
import { useAuth, useCampaign } from '@core/context';
import { Screen } from '@core/types';
import { CampaignRole } from '@core/types/campaign.types';

/**
 * Props for the TerminalLayout component
 */
interface TerminalLayoutProps {
  /** Main content to render */
  children: React.ReactNode;
  /** Section title displayed in header */
  title: string;
  /** Section subtitle (system info) */
  subtitle?: string;
  /** Material icon name for the section */
  icon?: string;
  /** Handler for logging out */
  onLogout?: () => void;
  /** Handler for navigation (required for back to hub) */
  onNavigate?: (screen: Screen) => void;
  /** Additional action buttons for header */
  actions?: React.ReactNode;
  /** Optional game system ID for RAG rule queries */
  gameSystemId?: string;
  /** Optional game system name for display */
  gameSystemName?: string;
  /** Hide campaign selector (e.g., for admin pages) */
  hideCampaignSelector?: boolean;
  /** Hide back to hub button */
  hideBackToHub?: boolean;
}

/**
 * TerminalLayout - Standardized layout for all application views
 * 
 * Features:
 * - Consistent header with section info and campaign context
 * - Quick campaign switching for players and masters
 * - Dice roller and rules search access
 * - User role display in footer
 * - Cyberpunk terminal aesthetic
 */
export const TerminalLayout: React.FC<TerminalLayoutProps> = ({ 
  children, 
  title, 
  subtitle,
  icon = 'terminal',
  onLogout, 
  onNavigate,
  actions,
  gameSystemId,
  gameSystemName,
  hideCampaignSelector = true,
  hideBackToHub = false
}) => {
  const [showDice, setShowDice] = useState(false);
  const [showRuleQuery, setShowRuleQuery] = useState(false);
  const [showCampaignSelector, setShowCampaignSelector] = useState(false);

  const { user } = useAuth();
  const { campaigns, activeCampaign, selectCampaign, isLoading: campaignsLoading } = useCampaign();

  /**
   * Handle campaign selection from dropdown
   */
  const handleSelectCampaign = useCallback(async (campaignId: string) => {
    await selectCampaign(campaignId);
    setShowCampaignSelector(false);
  }, [selectCampaign]);

  /**
   * Toggle campaign selector dropdown
   */
  const handleCampaignSelectorToggle = useCallback(() => {
    setShowCampaignSelector(prev => !prev);
  }, []);

  /**
   * Navigate back to hub based on user role
   */
  const handleBackToHub = useCallback(() => {
    if (!onNavigate) return;
    if (user?.role === 'ADMIN') {
      onNavigate(Screen.ADMIN_USERS);
    } else if (user?.role === 'MASTER') {
      onNavigate(Screen.MASTER_HUB);
    } else {
      onNavigate(Screen.GALLERY);
    }
  }, [onNavigate, user?.role]);

  /**
   * Get display text for user role
   */
  const getUserRoleDisplay = (): string => {
    if (!user) return 'INVITADO';
    switch (user.role) {
      case 'ADMIN': return 'ADMINISTRADOR';
      case 'MASTER': return 'MAESTRO DE JUEGO';
      case 'PLAYER': return 'OPERATIVO';
      default: return 'USUARIO';
    }
  };

  /**
   * Check if campaign selector should be shown
   * Only for authenticated non-admin users
   */
  const shouldShowCampaignSelector = !hideCampaignSelector && user && user.role !== 'ADMIN';

  return (
    <div className="flex flex-col h-screen p-4 md:p-8 bg-background-dark font-mono relative">
      {/* Header */}
      <header className="flex justify-between items-center mb-6 pb-2 border-b-2 border-primary/30">
        {/* Left Section: Icon, Title, Subtitle, Campaign */}
        <div className="flex items-center gap-4">
          <div className="w-8 h-8 border border-primary flex items-center justify-center bg-primary/10 shrink-0">
            <span className="material-icons text-primary text-sm">{icon}</span>
          </div>
          <div className="min-w-0">
            <h1 className="text-xl md:text-3xl font-display uppercase tracking-widest text-primary text-glow font-bold truncate">
              {title}
            </h1>
            <div className="flex items-center gap-2 flex-wrap">
              {/* Subtitle with username */}
              <p className="text-[10px] md:text-xs text-primary/60 uppercase tracking-wider">
                <span className="animate-pulse">_</span> {/* USER: {user?.username || 'Usuario'} */}
              </p>
              
              {/* Campaign Selector Button */}
              {shouldShowCampaignSelector && (
                <>
                  <span className="text-primary/30 hidden sm:inline">//</span>
                  <button
                    onClick={handleCampaignSelectorToggle}
                    className="flex items-center gap-1 text-[10px] md:text-xs text-primary/60 hover:text-primary transition-colors uppercase tracking-wider group"
                    aria-expanded={showCampaignSelector}
                    aria-haspopup="listbox"
                    aria-label="Seleccionar campaña"
                  >
                    <span className="material-icons text-xs">public</span>
                    <span className="truncate max-w-[100px] md:max-w-[150px]">
                      {activeCampaign?.name || 'Sin campaña'}
                    </span>
                    <span className="material-icons text-xs group-hover:rotate-180 transition-transform">
                      expand_more
                    </span>
                  </button>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Right Section: Actions */}
        <div className="flex items-center gap-2 md:gap-4">

          <div className="hidden md:flex flex-col text-right text-xs text-primary/60">
            <span>MEM: 64TB [OK]</span>
            <span>NET: ENCRYPTED</span>
          </div>

          {/* Dice Roller Button */}
          <button 
            onClick={() => setShowDice(true)}
            className="flex items-center gap-2 border border-primary/40 px-2 md:px-3 py-1 text-xs uppercase hover:bg-primary/20 transition-all text-primary font-bold"
            aria-label="Lanzar dados"
          >
            <span className="material-icons text-sm">casino</span>
            <span className="hidden sm:inline">DADOS</span>
          </button>

          {/* Rules Search Button */}
          <button 
            onClick={() => setShowRuleQuery(true)}
            className="flex items-center gap-2 border border-primary/40 px-2 md:px-3 py-1 text-xs uppercase hover:bg-primary/20 transition-all text-primary font-bold"
            aria-label="Consultar reglas"
          >
            <span className="material-icons text-sm">auto_stories</span>
            <span className="hidden sm:inline">REGLAS</span>
          </button>

          {/* Additional Actions */}
          {actions}

          {/* Back to Hub Button */}
          {!hideBackToHub && onNavigate && (
            <button 
              onClick={handleBackToHub}
              className="flex items-center gap-2 border  px-2 md:px-3 py-1 text-xs uppercase border-cyan-500/30 hover:border-cyan-500 hover:bg-cyan-500/40 transition-all font-bold text-cyan-500 group-hover:text-cyan-400"
              aria-label="Volver al hub"
            >
              <span className="material-icons text-sm">home</span>
              <span className="hidden md:inline">HUB</span>
            </button>
          )}
          
          {/* Logout Button */}
          {onLogout && (
            <button 
              onClick={onLogout}
              className="flex items-center gap-2 border border-red-500/60 px-2 md:px-3 py-1 text-xs uppercase hover:bg-red-500 hover:text-black transition-colors text-red-500 font-bold"
              aria-label="Cerrar sesión"
            >
              <span className="material-icons text-sm">logout</span>
              <span className="hidden md:inline">LOGOUT</span>
            </button>
          )}
        </div>
      </header>

      {/* Campaign Selector Dropdown */}
      {showCampaignSelector && shouldShowCampaignSelector && (
        <>
          {/* Backdrop */}
          <div 
            className="fixed inset-0 z-40" 
            onClick={() => setShowCampaignSelector(false)}
            aria-hidden="true"
          />
          
          {/* Dropdown Panel */}
          <div 
            className="absolute top-20 left-4 md:left-16 z-50 w-72 md:w-80 bg-surface-dark border border-primary/30 shadow-[0_0_20px_rgba(37,244,106,0.1)]"
            role="listbox"
            aria-label="Lista de campañas"
          >
            <div className="p-3 border-b border-primary/20">
              <h4 className="text-xs text-primary uppercase tracking-widest font-bold">
                Seleccionar Campaña
              </h4>
            </div>
            
            <div className="max-h-60 overflow-y-auto custom-scrollbar">
              {campaignsLoading ? (
                <div className="p-4 text-center text-primary/40 text-xs">
                  Cargando campañas...
                </div>
              ) : campaigns.length === 0 ? (
                <div className="p-4 text-center text-primary/40 text-xs">
                  No perteneces a ninguna campaña
                </div>
              ) : (
                campaigns.map(campaign => (
                  <button
                    key={campaign.id}
                    onClick={() => handleSelectCampaign(campaign.id)}
                    className={`w-full p-3 text-left flex items-center justify-between hover:bg-primary/10 transition-colors ${
                      activeCampaign?.id === campaign.id 
                        ? 'bg-primary/20 border-l-2 border-primary' 
                        : 'border-l-2 border-transparent'
                    }`}
                    role="option"
                    aria-selected={activeCampaign?.id === campaign.id}
                  >
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-primary truncate font-bold">
                        {campaign.name}
                      </p>
                      <p className="text-[10px] text-primary/40 uppercase">
                        {campaign.userRole === CampaignRole.Master ? 'MASTER' : 'PLAYER'}
                        {!campaign.isActive && ' // INACTIVA'}
                      </p>
                    </div>
                    {activeCampaign?.id === campaign.id && (
                      <span className="material-icons text-primary text-sm">check</span>
                    )}
                  </button>
                ))
              )}
            </div>

            {/* Campaign Management Actions */}
            <div className="p-2 border-t border-primary/20 flex gap-2">
              {onNavigate && (
                <>
                  <button
                    onClick={() => {
                      setShowCampaignSelector(false);
                      onNavigate(Screen.CAMPAIGN_GEN);
                    }}
                    className="flex-1 p-2 text-[10px] text-primary/60 hover:text-primary hover:bg-primary/10 transition-colors uppercase tracking-wider text-center"
                  >
                    + NUEVA
                  </button>
                  <button
                    onClick={() => {
                      setShowCampaignSelector(false);
                      onNavigate(Screen.INVITATIONS);
                    }}
                    className="flex-1 p-2 text-[10px] text-primary/60 hover:text-primary hover:bg-primary/10 transition-colors uppercase tracking-wider text-center"
                  >
                    UNIRSE
                  </button>
                  {activeCampaign && user?.role === 'MASTER' && (
                    <button
                      onClick={() => {
                        setShowCampaignSelector(false);
                        onNavigate(Screen.CAMPAIGN_SETTINGS);
                      }}
                      className="flex-1 p-2 text-[10px] text-primary/60 hover:text-primary hover:bg-primary/10 transition-colors uppercase tracking-wider text-center"
                    >
                      CONFIGURAR
                    </button>
                  )}
                </>
              )}
            </div>
          </div>
        </>
      )}

      {/* Main Content */}
      <div className="flex-1 overflow-hidden">
        {children}
      </div>

      {/* Footer */}
      <footer className="mt-4 border-t border-primary/30 pt-2 flex justify-between text-[10px] md:text-xs text-primary/40 uppercase">
        {/* Left: User Role and System Status */}
        <div className="flex gap-3 md:gap-4 items-center">
          <span className="text-primary/60 font-bold">
            {user?.username || 'Usuario'} // {getUserRoleDisplay()}
          </span>
          <span className="hidden sm:inline">CONEXION: SEGURA</span>
          <span className="hidden md:inline">ENCRIPTACION: AES-4096</span>
        </div>
        
        {/* Right: Data Transfer Animation */}
        <div className="flex gap-4 animate-pulse">
          <span className="hidden sm:inline">TRANSFERENCIA DE DATOS...</span>
          <span>[ |||||||||| ] 100%</span>
        </div>
      </footer>

      {/* Dice Roller Modal */}
      {showDice && <DiceRoller onClose={() => setShowDice(false)} />}
      
      {/* Rule Query Modal */}
      {showRuleQuery && (
        <RuleQuery 
          onClose={() => setShowRuleQuery(false)} 
          gameSystemId={gameSystemId || activeCampaign?.gameSystemId}
          gameSystemName={gameSystemName}
          campaignOwnerId={activeCampaign?.ownerId}
        />
      )}
    </div>
  );
};
