/**
 * Campaign List Page
 * Displays all campaigns the user is a member of with options to:
 * - View campaign details
 * - Edit campaigns (Master only)
 * - Create new campaigns
 * - Join existing campaigns
 * 
 * Follows the same layout and style as GameSystemsPage for consistency.
 * Cyberpunk terminal aesthetics with campaign management features.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button, TerminalLog } from '@shared/components/ui';
import { CampaignDetailModal } from '@shared/components/modals';
import { useAuth, useCampaign } from '@core/context';
import { gameSystemService, campaignService } from '@core/services/api';
import { useTerminalLog } from '@core/hooks/useTerminalLog';
import type { Campaign, GameSystem, UpdateCampaignInput, CampaignMember } from '@core/types';
import { CampaignRole } from '@core/types';

export const CampaignListPage: React.FC = () => {
  const navigate = useNavigate();
  const { isMaster } = useAuth();
  const { 
    campaigns, 
    activeCampaign,
    selectCampaign, 
    leaveCampaign,
    updateCampaign,
    updateCampaignStatus,
    isLoading,
    error,
    clearError
  } = useCampaign();

  // Local state
  const [gameSystems, setGameSystems] = useState<GameSystem[]>([]);
  const [isLoadingGameSystems, setIsLoadingGameSystems] = useState(true);
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null);
  const [filter, setFilter] = useState<'all' | 'master' | 'player'>('all');
  const [operationInProgress, setOperationInProgress] = useState<string | null>(null);
  const [showMobileModal, setShowMobileModal] = useState(false);
  
  // Campaign members state - maps campaignId to members array
  const [campaignMembers, setCampaignMembers] = useState<Record<string, CampaignMember[]>>({});
  
  // Edit form state
  const [showEditForm, setShowEditForm] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [editForm, setEditForm] = useState<UpdateCampaignInput>({
    name: '',
    description: '',
  });
  
  const { logs, addLog } = useTerminalLog({
    maxLogs: 10,
    initialLogs: [
      '> Campaign registry system online...',
      '> [SUCCESS] Connection established.',
      '> Awaiting commands...'
    ]
  });

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
   * Load members for all campaigns
   */
  useEffect(() => {
    const loadAllCampaignMembers = async () => {
      if (campaigns.length === 0) return;

      try {
        const membersMap: Record<string, CampaignMember[]> = {};
        
        // Fetch members for all campaigns in parallel
        const results = await Promise.allSettled(
          campaigns.map(async (campaign) => {
            const members = await campaignService.getMembers(campaign.id);
            return { campaignId: campaign.id, members };
          })
        );

        results.forEach((result) => {
          if (result.status === 'fulfilled') {
            membersMap[result.value.campaignId] = result.value.members;
          }
        });

        setCampaignMembers(membersMap);
        addLog(`[SUCCESS] Miembros de campañas cargados.`);
      } catch (err) {
        console.error('Failed to load campaign members:', err);
      }
    };

    loadAllCampaignMembers();
  }, [campaigns, addLog]);

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
   * Populates the edit form with the selected campaign's data and shows the edit form
   */
  const handleStartEdit = (campaign: Campaign) => {
    setEditForm({
      name: campaign.name,
      description: campaign.description || '',
    });
    setShowEditForm(true);
    addLog(`EDITANDO: ${campaign.name.toUpperCase()}`);
  };

  /**
   * Validates the edit form
   */
  const validateEditForm = (): boolean => {
    if (!editForm.name.trim()) {
      addLog('ERROR: NOMBRE REQUERIDO');
      return false;
    }
    return true;
  };

  /**
   * Handles updating an existing campaign
   */
  const handleUpdate = async () => {
    if (!selectedCampaign) {
      addLog('ERROR: NO HAY CAMPAÑA SELECCIONADA');
      return;
    }
    
    if (!validateEditForm()) return;

    setIsUpdating(true);
    addLog(`ACTUALIZANDO ${selectedCampaign.name.toUpperCase()}...`);

    try {
      const request: UpdateCampaignInput = {
        name: editForm.name.trim(),
        description: editForm.description?.trim() || undefined,
      };

      await updateCampaign(selectedCampaign.id, request);
      
      // Update selected campaign locally
      setSelectedCampaign(prev => prev ? { ...prev, ...request } : null);
      addLog(`[SUCCESS] Campaña actualizada: ${request.name.toUpperCase()}`);
      
      // Reset edit form and close
      setShowEditForm(false);
      setEditForm({ name: '', description: '' });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL ACTUALIZAR CAMPAÑA';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsUpdating(false);
    }
  };

  /**
   * Handle selecting a campaign as active and navigate to gallery
   */
  const handleActivateCampaign = async (campaignId: string) => {
    setOperationInProgress(campaignId);
    addLog(`ACTIVANDO CAMPAÑA...`);
    try {
      await selectCampaign(campaignId);
      addLog('[SUCCESS] Campaña activada.');
      navigate('/gallery');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al activar';
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
      
      // Clear selection if the deleted campaign was selected
      if (selectedCampaign?.id === campaign.id) {
        setSelectedCampaign(null);
        setShowEditForm(false);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error en operación';
      addLog(`ERROR: ${message}`);
    } finally {
      setOperationInProgress(null);
    }
  };

  /**
   * Handle toggling campaign active status
   */
  const handleToggleStatus = async (campaign: Campaign) => {
    const newStatus = !campaign.isActive;
    const statusText = newStatus ? 'ACTIVANDO' : 'PAUSANDO';
    
    addLog(`${statusText} ${campaign.name.toUpperCase()}...`);

    try {
      await updateCampaignStatus(campaign.id, newStatus);
      addLog(`[SUCCESS] ${campaign.name.toUpperCase()} ${newStatus ? 'ACTIVADA' : 'PAUSADA'}`);
      
      // Update selected campaign if it's the one being toggled
      if (selectedCampaign?.id === campaign.id) {
        setSelectedCampaign(prev => prev ? { ...prev, isActive: newStatus } : null);
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL CAMBIAR ESTADO';
      addLog(`ERROR: ${message}`);
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
      return { label: 'MASTER', color: 'text-cyan-400', icon: 'star' };
    }
    return { label: 'JUGADOR', color: 'text-yellow-400', icon: 'person' };
  };

  /**
   * Render members summary for a campaign
   * Shows icons for each member type (Master/Player) with their names on hover
   */
  const renderMembersSummary = (campaignId: string) => {
    const members = campaignMembers[campaignId];
    
    if (!members || members.length === 0) {
      return null;
    }

    const masters = members.filter(m => m.role === CampaignRole.Master);
    const players = members.filter(m => m.role === CampaignRole.Player);

    return (
      <div className="flex items-center gap-3 mt-2 pt-2 border-t border-primary/10">
        {/* Masters */}
        {masters.length > 0 && (
          <div className="flex items-center gap-1" title={masters.map(m => m.displayName).join(', ')}>
            <span className="material-icons text-cyan-400 text-xs">star</span>
            <span className="text-cyan-400 text-xs font-mono">{masters.length}</span>
          </div>
        )}
        
        {/* Players */}
        {players.length > 0 && (
          <div className="flex items-center gap-1" title={players.map(m => m.displayName).join(', ')}>
            <span className="material-icons text-yellow-400 text-xs">person</span>
            <span className="text-yellow-400 text-xs font-mono">{players.length}</span>
          </div>
        )}

        {/* Total count */}
        <span className="text-primary/30 text-xs">
          ({members.length} {members.length === 1 ? 'miembro' : 'miembros'})
        </span>
      </div>
    );
  };

  return (
    <TerminalLayout
      title="CAMPAÑAS"
      subtitle="Gestión de campañas"
      icon="auto_stories"
      hideCampaignSelector={true}
    >
      <div className="flex flex-col md:flex-row h-full gap-6">
        {/* Main Content Section */}
        <div className="flex-1 flex flex-col gap-6 overflow-hidden">
          {/* Header */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                  Mis Campañas
                </h1>
                <p className="text-primary/40 text-xs mt-1 hidden md:block">
                  Gestiona tus campañas de rol
                </p>
              </div>
              <div className="flex items-center gap-2">
                {/* Only Masters can create new campaigns */}
                {isMaster && (
                  <Button 
                    onClick={() => navigate('/campaigns/new')}
                    variant="primary"
                    size="sm"
                  >
                    + NUEVA CAMPAÑA
                  </Button>
                )}
                {/* Players see a "Join Campaign" button instead */}
                {!isMaster && (
                  <Button 
                    onClick={() => navigate('/campaigns/invitations')}
                    variant="secondary"
                    size="sm"
                  >
                    UNIRSE A CAMPAÑA
                  </Button>
                )}
              </div>
            </div>
          </div>

          {/* Edit Form */}
          {showEditForm && selectedCampaign && (
            <div className="border border-yellow-500/30 bg-black/60 p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-sm text-yellow-500/60 uppercase tracking-widest flex items-center gap-2">
                  <span className="material-icons text-xs">edit</span>
                  Editar Campaña: <span className="text-yellow-400 font-mono ml-1">{selectedCampaign.name}</span>
                </h2>
                <button
                  onClick={() => setShowEditForm(false)}
                  className="text-primary/40 hover:text-primary transition-colors"
                >
                  <span className="material-icons text-sm">close</span>
                </button>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Name */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Nombre *
                  </label>
                  <input
                    type="text"
                    value={editForm.name}
                    onChange={(e) => setEditForm(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="Nombre de la campaña"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none placeholder:text-primary/20"
                    disabled={isUpdating}
                  />
                </div>

                {/* Game System (read-only) */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Sistema de Juego (no editable)
                  </label>
                  <input
                    type="text"
                    value={getGameSystemName(selectedCampaign.gameSystemId)}
                    className="w-full bg-black/20 border border-primary/20 text-primary/50 p-3 text-sm cursor-not-allowed"
                    disabled
                  />
                </div>

                {/* Description */}
                <div className="md:col-span-2">
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Descripción
                  </label>
                  <textarea
                    value={editForm.description || ''}
                    onChange={(e) => setEditForm(prev => ({ ...prev, description: e.target.value }))}
                    placeholder="Descripción de la campaña..."
                    rows={2}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none placeholder:text-primary/20 resize-none"
                    disabled={isUpdating}
                  />
                </div>
              </div>

              {/* Update Buttons */}
              <div className="mt-4 flex justify-end gap-2">
                <Button 
                  onClick={() => setShowEditForm(false)} 
                  variant="secondary"
                  disabled={isUpdating}
                >
                  CANCELAR
                </Button>
                <Button 
                  onClick={handleUpdate} 
                  disabled={isUpdating}
                  className="min-w-[200px]"
                >
                  {isUpdating ? 'ACTUALIZANDO...' : 'GUARDAR CAMBIOS'}
                </Button>
              </div>
            </div>
          )}

          {/* Error Display */}
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-3 flex items-center justify-between">
              <div className="flex items-center gap-2 text-danger text-sm">
                <span className="material-icons text-sm">error</span>
                {error}
              </div>
              <button type="button" onClick={clearError} className="text-danger/60 hover:text-danger cursor-pointer">
                <span className="material-icons text-sm">close</span>
              </button>
            </div>
          )}

          {/* Campaign List */}
          <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
            {/* List Header with Filter Tabs */}
            <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="material-icons text-sm">auto_stories</span>
                Campañas Disponibles
              </span>
              
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
                    className={`px-3 py-1 text-[9px] uppercase tracking-widest transition-all flex items-center gap-1 ${
                      filter === tab.id
                        ? 'bg-primary text-black font-bold'
                        : 'text-primary/60 hover:text-primary hover:bg-primary/5'
                    }`}
                  >
                    {tab.label}
                    <span className={`${filter === tab.id ? 'text-black/60' : 'text-primary/40'}`}>
                      ({tab.count})
                    </span>
                  </button>
                ))}
              </div>
            </div>
            
            <div className="flex-1 overflow-y-auto p-4">
              {isLoading && campaigns.length === 0 ? (
                <div className="flex items-center justify-center h-full text-primary/40">
                  <span className="animate-pulse">CARGANDO CAMPAÑAS...</span>
                </div>
              ) : filteredCampaigns.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-primary/40">
                  <span className="material-icons text-4xl mb-2">folder_off</span>
                  <p className="text-sm uppercase">
                    {filter === 'all' ? 'No hay campañas' : `No hay campañas como ${filter === 'master' ? 'Master' : 'Jugador'}`}
                  </p>
                  <p className="text-sm mt-1">
                    {isMaster ? 'Crea una nueva campaña o únete a una existente' : 'Únete a una campaña usando un código de invitación'}
                  </p>
                </div>
              ) : (
                <div className="grid gap-3 lg:grid-cols-2">
                  {filteredCampaigns.map((campaign) => {
                    const roleInfo = getRoleInfo(campaign.userRole);
                    const isActive = activeCampaign?.id === campaign.id;
                    const isSelected = selectedCampaign?.id === campaign.id;
                    const isOperating = operationInProgress === campaign.id;

                    return (
                      <div
                        key={campaign.id}
                        className={`border p-4 cursor-pointer transition-all ${
                          isSelected 
                            ? 'border-cyan-500 bg-cyan-500/10' 
                            : 'border-primary/20 bg-black/40 hover:border-primary/40'
                        } ${isOperating ? 'opacity-50' : ''}`}
                        onClick={() => {
                          setSelectedCampaign(campaign);
                          if (window.innerWidth < 768) {
                            setShowMobileModal(true);
                          }
                        }}
                      >
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-2 flex-wrap">
                              <span className={`font-mono text-sm ${roleInfo.color}`}>
                                <span className="material-icons text-xs align-middle mr-1">{roleInfo.icon}</span>
                                {roleInfo.label}
                              </span>
                              <span className={`text-xs px-2 py-0.5 border ${
                                campaign.isActive
                                  ? 'border-green-500/40 text-green-400' 
                                  : 'border-yellow-500/40 text-yellow-400'
                              }`}>
                                {campaign.isActive ? 'ONLINE' : 'OFFLINE'}
                              </span>
                              {isActive && (
                                <span className="text-xs px-2 py-0.5 border border-primary/60 text-primary bg-primary/10">
                                  ACTIVA
                                </span>
                              )}
                            </div>
                            <h3 className="text-primary font-bold mt-1 text-lg">{campaign.name}</h3>
                            <p className="text-primary/40 text-xs mt-1">
                              {isLoadingGameSystems ? '...' : getGameSystemName(campaign.gameSystemId)}
                              <span className="mx-2">•</span>
                              Creada: {formatDate(campaign.createdAt)}
                            </p>
                          </div>
                          
                          {/* Toggle Status Button (Master only) */}
                          {campaign.userRole === CampaignRole.Master && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                handleToggleStatus(campaign);
                              }}
                              className={`material-icons text-sm transition-colors ${
                                campaign.isActive
                                  ? 'text-green-500/60 hover:text-yellow-500'
                                  : 'text-yellow-500/60 hover:text-green-500'
                              }`}
                              title={campaign.isActive ? 'Pausar campaña' : 'Activar campaña'}
                            >
                              {campaign.isActive ? 'toggle_on' : 'toggle_off'}
                            </button>
                          )}
                        </div>
                        
                        {campaign.description && (
                          <p className="text-primary/50 text-xs mt-2 line-clamp-2">
                            {campaign.description}
                          </p>
                        )}

                        {/* Members Summary */}
                        {renderMembersSummary(campaign.id)}

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
        </div>

        {/* Terminal Log & Details Section */}
        <div className="w-full md:w-80 flex flex-col border border-primary/30 bg-black/80">

          
          {/* Selected Campaign Details - Hidden on mobile */}
          {selectedCampaign && (
            <div className="hidden md:block flex-1">
              <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
                <span className="material-icons text-sm">terminal</span>
                Campaña Seleccionada
              </div>

            <div className="flex-1 border-t border-primary/30 p-4">
              {/* <h3 className="text-xs text-primary/60 uppercase tracking-widest mb-2">
                Campaña Seleccionada
              </h3> */}
              <div className="space-y-2 text-xs">
                <div className="flex justify-between">
                  <span className="text-primary/40">ID:</span>
                  <span className="text-primary font-mono">{selectedCampaign.id.substring(0, 8)}...</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-primary/40">Nombre:</span>
                  <span className="text-primary">{selectedCampaign.name}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-primary/40">Rol:</span>
                  <span className={getRoleInfo(selectedCampaign.userRole).color}>
                    {getRoleInfo(selectedCampaign.userRole).label}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-primary/40">Sistema:</span>
                  <span className="text-primary/70 truncate max-w-[120px]">
                    {isLoadingGameSystems ? '...' : getGameSystemName(selectedCampaign.gameSystemId)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-primary/40">Estado:</span>
                  <span className={selectedCampaign.isActive ? 'text-green-400' : 'text-yellow-400'}>
                    {selectedCampaign.isActive ? 'ONLINE' : 'OFFLINE'}
                  </span>
                </div>
                
                {/* Members in selected campaign */}
                {campaignMembers[selectedCampaign.id] && (
                  <div className="pt-2 mt-2 border-t border-primary/20">
                    <span className="text-primary/40 text-xs uppercase">Miembros:</span>
                    <div className="mt-1 space-y-1 max-h-24 overflow-y-auto">
                      {[...campaignMembers[selectedCampaign.id]]
                        .sort((a, b) => {
                          // Masters first, then sort by name
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
              
              {/* Action Buttons */}
              <div className="mt-4 space-y-2">
                {/* Activate Button */}
                {activeCampaign?.id !== selectedCampaign.id && (
                  <Button
                    onClick={() => handleActivateCampaign(selectedCampaign.id)}
                    variant="primary"
                    size="sm"
                    className="w-full"
                    disabled={operationInProgress === selectedCampaign.id}
                  >
                    <span className="material-icons text-sm mr-2">play_arrow</span>
                    ACTIVAR Y ENTRAR
                  </Button>
                )}
                
                {/* Edit Button (Master only) */}
                {selectedCampaign.userRole === CampaignRole.Master && (
                  <Button
                    onClick={() => handleStartEdit(selectedCampaign)}
                    variant="secondary"
                    size="sm"
                    className="w-full"
                    disabled={showEditForm}
                  >
                    <span className="material-icons text-sm mr-2">edit</span>
                    EDITAR
                  </Button>
                )}
                
                {/* Settings Button (Master only) */}
                {selectedCampaign.userRole === CampaignRole.Master && (
                  <Button
                    onClick={async () => {
                      await selectCampaign(selectedCampaign.id);
                      navigate(`/campaigns/${selectedCampaign.id}`);
                    }}
                    variant="secondary"
                    size="sm"
                    className="w-full"
                  >
                    <span className="material-icons text-sm mr-2">settings</span>
                    CONFIGURACIÓN AVANZADA
                  </Button>
                )}
                
                {/* Leave/Delete Button */}
                <Button
                  onClick={() => handleLeaveCampaign(selectedCampaign)}
                  variant="danger"
                  size="sm"
                  className="w-full"
                  disabled={operationInProgress === selectedCampaign.id}
                >
                  <span className="material-icons text-sm mr-2">
                    {selectedCampaign.userRole === CampaignRole.Master ? 'delete' : 'logout'}
                  </span>
                  {selectedCampaign.userRole === CampaignRole.Master ? 'ELIMINAR' : 'ABANDONAR'}
                </Button>
              </div>
            </div>
            </div>
          )}
          
          


          {/* System Log */}
          <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">terminal</span>
            System Log
          </div>
          <TerminalLog logs={logs} maxLogs={10} className="h-24 md:h-32" />
          




        </div>

        {/* Mobile Modal for Campaign Details */}
        {showMobileModal && selectedCampaign && (
          <CampaignDetailModal
            campaign={selectedCampaign}
            gameSystems={gameSystems}
            campaignMembers={campaignMembers}
            activeCampaign={activeCampaign}
            operationInProgress={operationInProgress}
            showEditForm={showEditForm}
            isLoadingGameSystems={isLoadingGameSystems}
            onClose={() => setShowMobileModal(false)}
            onActivate={handleActivateCampaign}
            onEdit={handleStartEdit}
            onLeave={handleLeaveCampaign}
            onSettings={async (campaign) => {
              await selectCampaign(campaign.id);
              navigate(`/campaigns/${campaign.id}`);
            }}
          />
        )}
      </div>
    </TerminalLayout>
  );
};

export default CampaignListPage;
