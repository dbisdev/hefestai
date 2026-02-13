/**
 * Campaign List Page
 * Displays all campaigns the user is a member of with options to:
 * - View campaign details
 * - Edit campaigns (Master only)
 * - Create new campaigns
 * - Join existing campaigns
 * 
 * Cyberpunk terminal aesthetics with campaign management features.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useCampaign } from '@core/context';
import { gameSystemService } from '@core/services/api';
import type { Campaign, GameSystem } from '@core/types';
import { Screen, CampaignRole } from '@core/types';

interface CampaignListPageProps {
  /** Handler for navigating to other screens */
  onNavigate: (screen: Screen) => void;
  /** Handler for logging out */
  onLogout: () => void;
}

/**
 * CampaignListPage Component
 * 
 * Provides a comprehensive view of all user campaigns with management capabilities.
 * Features:
 * - Campaign cards with status indicators
 * - Quick actions (edit, select, leave)
 * - Create new campaign button
 * - Join campaign functionality
 * - Filter and sort options
 */
export const CampaignListPage: React.FC<CampaignListPageProps> = ({ onNavigate, onLogout }) => {
  const { 
    campaigns, 
    activeCampaign,
    selectCampaign, 
    leaveCampaign,
    fetchCampaigns,
    isLoading,
    error,
    clearError
  } = useCampaign();

  // Local state
  const [gameSystems, setGameSystems] = useState<GameSystem[]>([]);
  const [isLoadingGameSystems, setIsLoadingGameSystems] = useState(true);
  const [selectedCampaignId, setSelectedCampaignId] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'master' | 'player'>('all');
  const [operationInProgress, setOperationInProgress] = useState<string | null>(null);
  const [logs, setLogs] = useState<string[]>([
    '> Campaign registry system online...',
    '> [SUCCESS] Connection established.',
    '> Awaiting commands...'
  ]);

  /**
   * Add a log entry to the terminal display
   */
  const addLog = useCallback((msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-8));
  }, []);

  /**
   * Load game systems for display
   */
  useEffect(() => {
    const loadGameSystems = async () => {
      setIsLoadingGameSystems(true);
      try {
        const systems = await gameSystemService.getAll();
        setGameSystems(systems);
        addLog(`[SUCCESS] ${systems.length} sistema(s) de juego indexado(s).`);
      } catch (err) {
        console.error('Failed to load game systems:', err);
        addLog('WARNING: No se pudieron cargar sistemas de juego.');
      } finally {
        setIsLoadingGameSystems(false);
      }
    };

    loadGameSystems();
  }, [addLog]);

  /**
   * Get game system name by ID
   */
  const getGameSystemName = useCallback((gameSystemId: string): string => {
    const system = gameSystems.find(gs => gs.id === gameSystemId);
    return system?.name || 'Sistema desconocido';
  }, [gameSystems]);

  /**
   * Filter campaigns based on user role
   */
  const filteredCampaigns = campaigns.filter(campaign => {
    if (filter === 'all') return true;
    if (filter === 'master') return campaign.userRole === CampaignRole.Master;
    if (filter === 'player') return campaign.userRole === CampaignRole.Player;
    return true;
  });

  /**
   * Handle selecting a campaign as active
   */
  const handleSelectCampaign = async (campaignId: string) => {
    setOperationInProgress(campaignId);
    addLog(`SELECCIONANDO CAMPAÑA...`);
    try {
      await selectCampaign(campaignId);
      addLog('[SUCCESS] Campaña activada.');
      // Navigate to gallery after selecting
      onNavigate(Screen.GALLERY);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al seleccionar';
      addLog(`ERROR: ${message}`);
    } finally {
      setOperationInProgress(null);
    }
  };

  /**
   * Handle editing a campaign (navigate to settings)
   */
  const handleEditCampaign = async (campaignId: string) => {
    setOperationInProgress(campaignId);
    addLog('CARGANDO CONFIGURACIÓN...');
    try {
      await selectCampaign(campaignId);
      onNavigate(Screen.CAMPAIGN_SETTINGS);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al cargar';
      addLog(`ERROR: ${message}`);
    } finally {
      setOperationInProgress(null);
    }
  };

  /**
   * Handle leaving a campaign
   */
  const handleLeaveCampaign = async (campaign: Campaign) => {
    const isMaster = campaign.userRole === CampaignRole.Master;
    
    const confirmMessage = isMaster 
      ? `¿Eliminar la campaña "${campaign.name}"? Esta acción no se puede deshacer.`
      : `¿Abandonar la campaña "${campaign.name}"?`;
    
    if (!confirm(confirmMessage)) return;
    
    if (isMaster && !confirm('¿Estás ABSOLUTAMENTE seguro? Todos los datos se perderán.')) {
      return;
    }

    setOperationInProgress(campaign.id);
    addLog(isMaster ? 'ELIMINANDO CAMPAÑA...' : 'ABANDONANDO CAMPAÑA...');
    
    try {
      await leaveCampaign(campaign.id);
      addLog(`[SUCCESS] ${isMaster ? 'Campaña eliminada' : 'Has abandonado la campaña'}.`);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error en operación';
      addLog(`ERROR: ${message}`);
    } finally {
      setOperationInProgress(null);
    }
  };

  /**
   * Format date for display
   */
  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('es-ES', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  /**
   * Get role display info
   */
  const getRoleInfo = (role?: CampaignRole): { label: string; color: string; icon: string } => {
    if (role === CampaignRole.Master) {
      return { label: 'MASTER', color: 'text-primary', icon: 'star' };
    }
    return { label: 'JUGADOR', color: 'text-yellow-400', icon: 'person' };
  };

  return (
    <TerminalLayout
      title="CAMPAÑAS"
      subtitle="Registro de Operaciones"
      icon="auto_stories"
      onLogout={onLogout}
      onNavigate={onNavigate}
      hideCampaignSelector={true}
    >
      <div className="h-full flex flex-col lg:flex-row gap-6 p-4">
        {/* Main Content */}
        <div className="flex-1 flex flex-col gap-4 overflow-hidden">
          {/* Header Actions */}
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
            {/* Filter Tabs */}
            <div className="flex border border-primary/30 bg-black/40">
              {[
                { id: 'all', label: 'TODAS', count: campaigns.length },
                { id: 'master', label: 'MASTER', count: campaigns.filter(c => c.userRole === CampaignRole.Master).length },
                { id: 'player', label: 'JUGADOR', count: campaigns.filter(c => c.userRole === CampaignRole.Player).length },
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setFilter(tab.id as typeof filter)}
                  className={`px-4 py-2 text-[10px] uppercase tracking-widest transition-all flex items-center gap-2 ${
                    filter === tab.id
                      ? 'bg-primary text-black font-bold'
                      : 'text-primary/60 hover:text-primary hover:bg-primary/5'
                  }`}
                >
                  {tab.label}
                  <span className={`text-[9px] ${filter === tab.id ? 'text-black/60' : 'text-primary/40'}`}>
                    ({tab.count})
                  </span>
                </button>
              ))}
            </div>

            {/* Action Buttons */}
            <div className="flex gap-2">
              <Button
                onClick={() => onNavigate(Screen.CAMPAIGN_GEN)}
                variant="primary"
                size="sm"
                icon="add"
              >
                NUEVA CAMPAÑA
              </Button>
            </div>
          </div>

          {/* Error Display */}
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-3 flex items-center justify-between">
              <div className="flex items-center gap-2 text-danger text-sm">
                <span className="material-icons text-sm">error</span>
                {error}
              </div>
              <button onClick={clearError} className="text-danger/60 hover:text-danger">
                <span className="material-icons text-sm">close</span>
              </button>
            </div>
          )}

          {/* Campaign List */}
          <div className="flex-1 overflow-y-auto custom-scrollbar pr-2">
            {isLoading && campaigns.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-64 text-primary/60">
                <span className="material-icons text-4xl mb-4 animate-pulse">sync</span>
                <p className="text-sm uppercase tracking-widest">Cargando campañas...</p>
              </div>
            ) : filteredCampaigns.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-64 text-primary/40">
                <span className="material-icons text-6xl mb-4">folder_off</span>
                <p className="text-sm uppercase tracking-widest mb-2">
                  {filter === 'all' ? 'No hay campañas' : `No hay campañas como ${filter === 'master' ? 'Master' : 'Jugador'}`}
                </p>
                <p className="text-xs text-primary/30 mb-6">
                  Crea una nueva campaña o únete a una existente
                </p>
                <Button
                  onClick={() => onNavigate(Screen.CAMPAIGN_GEN)}
                  variant="secondary"
                  size="sm"
                  icon="add"
                >
                  CREAR CAMPAÑA
                </Button>
              </div>
            ) : (
              <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
                {filteredCampaigns.map((campaign) => {
                  const roleInfo = getRoleInfo(campaign.userRole);
                  const isActive = activeCampaign?.id === campaign.id;
                  const isOperating = operationInProgress === campaign.id;

                  return (
                    <div
                      key={campaign.id}
                      className={`group relative border bg-black/40 p-5 transition-all duration-300 hover:bg-black/60 ${
                        isActive 
                          ? 'border-primary shadow-[0_0_10px_rgba(37,244,106,0.1)]' 
                          : 'border-primary/20 hover:border-primary/40'
                      } ${isOperating ? 'opacity-70' : ''}`}
                    >
                      {/* Active Indicator */}
                      {isActive && (
                        <div className="absolute top-0 left-0 w-full h-0.5 bg-gradient-to-r from-primary via-primary/50 to-transparent" />
                      )}

                      {/* Campaign Header */}
                      <div className="flex items-start justify-between mb-3">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-1">
                            <h3 className="text-lg text-primary font-display font-bold uppercase tracking-wider truncate">
                              {campaign.name}
                            </h3>
                            {isActive && (
                              <span className="px-2 py-0.5 bg-primary/20 text-primary text-[8px] uppercase tracking-widest shrink-0">
                                ACTIVA
                              </span>
                            )}
                          </div>
                          <div className="flex items-center gap-3 text-[10px]">
                            <span className={`flex items-center gap-1 ${roleInfo.color}`}>
                              <span className="material-icons text-xs">{roleInfo.icon}</span>
                              {roleInfo.label}
                            </span>
                            <span className={`flex items-center gap-1 ${campaign.isActive ? 'text-green-400' : 'text-yellow-400'}`}>
                              <span className="material-icons text-xs">
                                {campaign.isActive ? 'check_circle' : 'pause_circle'}
                              </span>
                              {campaign.isActive ? 'ONLINE' : 'OFFLINE'}
                            </span>
                          </div>
                        </div>
                      </div>

                      {/* Campaign Description */}
                      {campaign.description && (
                        <p className="text-xs text-primary/50 leading-relaxed mb-4 line-clamp-2">
                          {campaign.description}
                        </p>
                      )}

                      {/* Campaign Meta */}
                      <div className="grid grid-cols-2 gap-3 mb-4 text-[10px]">
                        <div className="bg-black/40 border border-primary/10 p-2">
                          <p className="text-primary/30 uppercase mb-0.5">Sistema</p>
                          <p className="text-primary/70 truncate">
                            {isLoadingGameSystems ? '...' : getGameSystemName(campaign.gameSystemId)}
                          </p>
                        </div>
                        <div className="bg-black/40 border border-primary/10 p-2">
                          <p className="text-primary/30 uppercase mb-0.5">Creada</p>
                          <p className="text-primary/70">{formatDate(campaign.createdAt)}</p>
                        </div>
                      </div>

                      {/* Action Buttons */}
                      <div className="flex gap-2 pt-3 border-t border-primary/10">
                        {!isActive && (
                          <button
                            onClick={() => handleSelectCampaign(campaign.id)}
                            disabled={isOperating}
                            className="flex-1 py-2 border border-primary/40 text-primary text-[10px] uppercase tracking-widest hover:bg-primary hover:text-black transition-all disabled:opacity-50 flex items-center justify-center gap-1"
                          >
                            <span className="material-icons text-sm">play_arrow</span>
                            ACTIVAR
                          </button>
                        )}
                        
                        {campaign.userRole === CampaignRole.Master && (
                          <button
                            onClick={() => handleEditCampaign(campaign.id)}
                            disabled={isOperating}
                            className="flex-1 py-2 border border-cyan-500/40 text-cyan-500 text-[10px] uppercase tracking-widest hover:bg-cyan-500/20 transition-all disabled:opacity-50 flex items-center justify-center gap-1"
                          >
                            <span className="material-icons text-sm">settings</span>
                            CONFIGURAR
                          </button>
                        )}

                        <button
                          onClick={() => handleLeaveCampaign(campaign)}
                          disabled={isOperating}
                          className={`py-2 px-3 border text-[10px] uppercase tracking-widest transition-all disabled:opacity-50 flex items-center justify-center gap-1 ${
                            campaign.userRole === CampaignRole.Master
                              ? 'border-danger/40 text-danger hover:bg-danger/20'
                              : 'border-yellow-500/40 text-yellow-500 hover:bg-yellow-500/20'
                          }`}
                          title={campaign.userRole === CampaignRole.Master ? 'Eliminar' : 'Abandonar'}
                        >
                          <span className="material-icons text-sm">
                            {campaign.userRole === CampaignRole.Master ? 'delete' : 'logout'}
                          </span>
                        </button>
                      </div>

                      {/* Loading Overlay */}
                      {isOperating && (
                        <div className="absolute inset-0 bg-black/60 flex items-center justify-center">
                          <span className="material-icons text-primary animate-spin">sync</span>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* Terminal Log Panel */}
        <div className="w-full lg:w-72 flex flex-col border border-primary/30 bg-black/80 shrink-0">
          <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">terminal</span>
            System Log
          </div>
          <div className="flex-1 p-4 font-mono text-xs text-primary/70 space-y-1 overflow-y-auto min-h-[150px] lg:min-h-0">
            {logs.map((log, i) => (
              <p 
                key={i} 
                className={`${
                  log.includes('ERROR') ? 'text-danger' : 
                  log.includes('SUCCESS') ? 'text-green-400' : 
                  log.includes('WARNING') ? 'text-yellow-400' : ''
                }`}
              >
                {log}
              </p>
            ))}
            <p className="animate-pulse">_</p>
          </div>

          {/* Quick Stats */}
          <div className="border-t border-primary/20 p-4 space-y-3">
            <div className="text-[10px] text-primary/40 uppercase tracking-widest mb-2">
              Estadísticas
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="bg-black/40 border border-primary/10 p-2 text-center">
                <p className="text-lg font-bold text-primary">{campaigns.length}</p>
                <p className="text-[8px] text-primary/40 uppercase">Total</p>
              </div>
              <div className="bg-black/40 border border-primary/10 p-2 text-center">
                <p className="text-lg font-bold text-primary">
                  {campaigns.filter(c => c.userRole === CampaignRole.Master).length}
                </p>
                <p className="text-[8px] text-primary/40 uppercase">Master</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};

export default CampaignListPage;
