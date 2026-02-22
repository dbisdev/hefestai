/**
 * Invitations Page
 * Allows users to join campaigns via join code and manage invitation codes
 * Layout: 3 columns - Join Campaign (left) + Campaign Codes (center) + System Log (right)
 */

import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button, TerminalLog } from '@shared/components/ui';
import { useAuth, useCampaign } from '@core/context';
import { useTerminalLog } from '@core/hooks/useTerminalLog';
import { CampaignRole, CampaignDetail } from '@core/types';
import { campaignService } from '@core/services/api';

/**
 * Invitations Page Component
 * Provides UI for joining campaigns and managing invitation codes
 */
export const InvitationsPage: React.FC = () => {
  const navigate = useNavigate();
  const { campaignId } = useParams();
  const { isMaster, user } = useAuth();
  const { 
    joinCampaign, 
    campaigns,
    activeCampaign,
    isLoading: isContextLoading, 
    error: contextError,
    clearError 
  } = useCampaign();

  // Join campaign form state
  const [joinCode, setJoinCode] = useState('');
  const [isJoining, setIsJoining] = useState(false);
  const [joinError, setJoinError] = useState<string | null>(null);
  const [joinSuccess, setJoinSuccess] = useState<string | null>(null);

  // Track which campaign code was recently copied
  const [copiedCampaignId, setCopiedCampaignId] = useState<string | null>(null);

  // Detailed campaign data for master campaigns (includes joinCode and gameSystem)
  const [masterCampaignDetails, setMasterCampaignDetails] = useState<CampaignDetail[]>([]);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);

  // Terminal logs
  const { logs, addLog } = useTerminalLog({
    maxLogs: 12,
    initialLogs: [
      'Sistema de invitaciones activo...',
      '[SUCCESS] Conexion establecida.',
      'Esperando comandos...'
    ]
  });

  // Clear context error on unmount
  useEffect(() => {
    return () => {
      clearError();
    };
  }, [clearError]);

  // Get campaigns where user is Master
  const masterCampaigns = campaigns.filter(c => c.userRole === CampaignRole.Master);
  
  // Get campaigns where user is Player
  const playerCampaigns = campaigns.filter(c => c.userRole === CampaignRole.Player);

  /**
   * Fetch detailed information for master campaigns (joinCode, gameSystem, etc.)
   */
  useEffect(() => {
    const fetchMasterCampaignDetails = async () => {
      if (masterCampaigns.length === 0) {
        setMasterCampaignDetails([]);
        return;
      }

      setIsLoadingDetails(true);
      try {
        const details = await Promise.all(
          masterCampaigns.map(campaign => campaignService.getById(campaign.id))
        );
        setMasterCampaignDetails(details);
        addLog(`[SUCCESS] ${details.length} CAMPAÑAS CARGADAS`);
      } catch (err) {
        console.error('Failed to fetch campaign details:', err);
        addLog('ERROR: NO SE PUDIERON CARGAR LOS DETALLES');
      } finally {
        setIsLoadingDetails(false);
      }
    };

    fetchMasterCampaignDetails();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [masterCampaigns.length]);

  /**
   * Validates the join code format
   */
  const validateJoinCode = (code: string): boolean => {
    // Join codes are 8 characters, alphanumeric, uppercase
    const codeRegex = /^[A-Z0-9]{8}$/;
    return codeRegex.test(code.toUpperCase().trim());
  };

  /**
   * Handles joining a campaign with the provided code
   */
  const handleJoinCampaign = async () => {
    const normalizedCode = joinCode.toUpperCase().trim();
    
    // Clear previous states
    setJoinError(null);
    setJoinSuccess(null);

    // Validate code format
    if (!normalizedCode) {
      setJoinError('Ingresa un codigo de campaña');
      addLog('ERROR: CODIGO REQUERIDO');
      return;
    }

    if (!validateJoinCode(normalizedCode)) {
      setJoinError('El codigo debe tener 8 caracteres alfanumericos');
      addLog('ERROR: FORMATO DE CODIGO INVALIDO');
      return;
    }

    setIsJoining(true);
    addLog(`PROCESANDO CODIGO: ${normalizedCode}...`);

    try {
      const campaign = await joinCampaign(normalizedCode);
      setJoinSuccess(`Te has unido a la campaña: ${campaign.name}`);
      addLog(`[SUCCESS] UNIDO A: ${campaign.name.toUpperCase()}`);
      setJoinCode('');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al unirse a la campaña';
      setJoinError(message);
      addLog(`ERROR: ${message.toUpperCase()}`);
    } finally {
      setIsJoining(false);
    }
  };

  /**
   * Handles key press in join code input (Enter to submit)
   */
  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !isJoining) {
      handleJoinCampaign();
    }
  };

  /**
   * Copies a campaign invitation code to clipboard
   */
  const handleCopyCampaignCode = async (campaignId: string, code: string, campaignName: string) => {
    try {
      await navigator.clipboard.writeText(code);
      setCopiedCampaignId(campaignId);
      addLog(`[SUCCESS] CODIGO DE "${campaignName.toUpperCase()}" COPIADO`);
      // Reset copied state after 2 seconds
      setTimeout(() => setCopiedCampaignId(null), 2000);
    } catch {
      addLog('ERROR: NO SE PUDO COPIAR AL PORTAPAPELES');
    }
  };

  return (
    <TerminalLayout
      title="INVITACIONES"
      subtitle="Unirse a campañas y codigos de invitacion"
      icon="mail"
      hideCampaignSelector={true}
    >
      <div className="h-full flex flex-col lg:flex-row gap-6 overflow-hidden">
        {/* Main Content Area */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto">
          {/* Header */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                  Invitaciones
                </h1>
                <p className="text-primary/50 text-xs mt-1 hidden md:block">
                  Gestiona codigos de acceso a campañas
                </p>
              </div>
              <Button
                onClick={() => navigate('/campaigns')}
                variant="secondary"
                size="sm"
              >
                VER MIS CAMPAÑAS
              </Button>
            </div>
          </div>

          {/* Two Column Layout for Invitations */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 flex-1 min-h-0">
            {/* LEFT COLUMN - Join Campaign */}
            <div className="border border-cyan-500/40 bg-black/60 p-6 flex flex-col">
              <h2 className="text-md text-cyan-500/80 uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">login</span>
                Unirse a una Campaña
              </h2>
              
              <p className="text-primary/80 text-sm mb-6">
                Ingresa el codigo de 8 caracteres que te proporcionó el Master de la campaña.
              </p>

            {/* Code Input */}
            <div className="mb-4">
              <label className="block text-md text-primary/80 uppercase mb-2">
                Codigo de Campaña
              </label>
              <input
                type="text"
                value={joinCode}
                onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                onKeyPress={handleKeyPress}
                placeholder="________"
                maxLength={8}
                className="w-full bg-black/40 border border-cyan-500/50 text-cyan-400 p-4 focus:border-cyan-500 focus:outline-none placeholder:text-primary/20 font-mono uppercase tracking-[0.4em] text-center text-2xl h-16"
                disabled={isJoining || isContextLoading}
              />
            </div>

            {/* Action Button */}
            <Button
              onClick={handleJoinCampaign}
              disabled={isJoining || isContextLoading || !joinCode.trim()}
              className="w-full h-12"
            >
              {isJoining ? 'PROCESANDO...' : 'UNIRSE A CAMPAÑA'}
            </Button>

            {/* Success/Error Messages */}
            {joinSuccess && (
              <div className="mt-4 p-3 border border-green-500/40 bg-green-500/10">
                <p className="text-green-400 text-sm flex items-center gap-2">
                  <span className="material-icons text-sm">check_circle</span>
                  {joinSuccess}
                </p>
              </div>
            )}

            {(joinError || contextError) && (
              <div className="mt-4 p-3 border border-danger/40 bg-danger/10">
                <p className="text-danger text-sm flex items-center gap-2">
                  <span className="material-icons text-sm">error</span>
                  {joinError || contextError}
                </p>
              </div>
            )}

            {/* Info Note */}
            <div className="mt-auto pt-6">
              <div className="p-3 border border-primary/20 bg-black/40">
                <p className="text-primary/80 text-xs flex items-start gap-2">
                  <span className="material-icons text-sm mt-0.5">info</span>
                  <span>
                    El codigo de campaña lo proporciona el Master. Se encuentra en la 
                    configuración de cada campaña.
                  </span>
                </p>
              </div>
            </div>
          </div>

          {/* RIGHT COLUMN - Campaign Codes List or Stats */}
            <div className="border border-yellow-500/40 bg-black/60 p-6 flex flex-col">
              {isMaster ? (
                <>
                  <h2 className="text-md text-yellow-500/80 uppercase tracking-widest mb-2 flex items-center gap-2">
                    <span className="material-icons text-sm">key</span>
                    Codigos de Mis Campañas
                  </h2>
                  
                  <p className="text-primary/80 text-sm mb-4">
                    Comparte estos codigos con jugadores para que se unan a tus campañas.
                  </p>

                  {/* Campaign Codes List */}
                  <div className="flex-1 overflow-y-auto space-y-3 min-h-0">
                    {isLoadingDetails ? (
                      <div className="flex flex-col items-center justify-center h-full text-center py-8">
                        <span className="material-icons text-4xl text-primary/40 animate-spin mb-2">sync</span>
                        <p className="text-primary/40 text-sm">Cargando campañas...</p>
                      </div>
                    ) : masterCampaignDetails.length > 0 ? (
                      masterCampaignDetails.map((campaign) => (
                        <div 
                          key={campaign.id}
                          className="border border-yellow-500/20 bg-black/40 p-3"
                        >
                          <div className="flex items-center justify-between gap-3">
                            <div className="flex-1 min-w-0">
                              <h3 className="text-yellow-400 text-sm font-medium truncate">
                                {campaign.name}
                              </h3>
                              <p className="text-primary/40 text-xs mt-0.5 truncate">
                                {campaign.gameSystem?.name || 'Sin sistema'}
                              </p>
                            </div>
                            <div className="flex items-center gap-2 flex-shrink-0">
                              <span className="font-mono text-yellow-400 tracking-widest text-sm bg-black/60 px-3 h-9 flex items-center border border-yellow-500/30">
                                {campaign.joinCode || '--------'}
                              </span>
                              <button
                                onClick={() => handleCopyCampaignCode(
                                  campaign.id, 
                                  campaign.joinCode || '', 
                                  campaign.name
                                )}
                                disabled={!campaign.joinCode}
                                className="h-9 w-9 flex items-center justify-center border border-yellow-500/30 bg-black/60 text-yellow-400 hover:bg-yellow-500/10 hover:border-yellow-500/50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                                title="Copiar codigo"
                              >
                                <span className="material-icons text-sm">
                                  {copiedCampaignId === campaign.id ? 'check' : 'content_copy'}
                                </span>
                              </button>
                            </div>
                          </div>
                        </div>
                      ))
                    ) : (
                      <div className="flex flex-col items-center justify-center h-full text-center py-8">
                        <span className="material-icons text-4xl text-primary/20 mb-2">folder_off</span>
                        <p className="text-primary/40 text-sm">No tienes campañas como Master</p>
                        <Button
                          onClick={() => navigate('/campaigns/new')}
                          variant="primary"
                          size="sm"
                          className="mt-4"
                        >
                          <span className="material-icons text-sm mr-1">add</span>
                          CREAR CAMPAÑA
                        </Button>
                      </div>
                    )}
                  </div>

                  {/* Info Note for Masters */}
                  {masterCampaignDetails.length > 0 && (
                    <div className="mt-4 pt-4">
                      <div className="p-3 border border-primary/20 bg-black/40">
                        <p className="text-primary/80 text-xs flex items-start gap-2">
                          <span className="material-icons text-sm mt-0.5">info</span>
                          <span>
                            Los jugadores usan estos codigos para unirse a tus campañas.
                            Cada campaña tiene su propio codigo unico.
                          </span>
                        </p>
                      </div>
                    </div>
                  )}
                </>
              ) : (
                /* Stats Panel for non-Masters (Players) */
                <>
                  <h2 className="text-sm text-yellow-500/60 uppercase tracking-widest mb-2 flex items-center gap-2">
                    <span className="material-icons text-sm">bar_chart</span>
                    Tus Campañas
                  </h2>
                  
                  <p className="text-primary/60 text-xs mb-4">
                    Resumen de tu participacion en campañas.
                  </p>

                  {/* Stats Display */}
                  <div className="space-y-3 mb-6">
                    <div className="flex justify-between items-center p-3 border border-primary/20 bg-black/40">
                      <span className="text-primary/60 text-sm">Total Campañas</span>
                      <span className="text-primary font-mono text-xl">{campaigns.length}</span>
                    </div>
                    <div className="flex justify-between items-center p-3 border border-yellow-500/20 bg-black/40">
                      <span className="text-primary/60 text-sm">Como Jugador</span>
                      <span className="text-yellow-400 font-mono text-xl">{playerCampaigns.length}</span>
                    </div>
                  </div>

                  {/* Campaign List for Players */}
                  {playerCampaigns.length > 0 && (
                    <div className="flex-1 overflow-y-auto space-y-2 min-h-0 mb-4">
                      <label className="block text-xs text-primary/40 uppercase mb-2">
                        Tus Campañas Activas
                      </label>
                      {playerCampaigns.map((campaign) => (
                        <div 
                          key={campaign.id}
                          className="border border-primary/20 bg-black/40 p-2 text-sm"
                        >
                          <span className="text-primary/80">{campaign.name}</span>
                          <span className="text-primary/40 text-xs ml-2">
                            ({campaign.gameSystem?.name || 'Sin sistema'})
                          </span>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Action Buttons */}
                  <div className="mt-auto space-y-3">
                    <Button
                      onClick={() => navigate('/campaigns')}
                      variant="secondary"
                      className="w-full h-12"
                    >
                      <span className="material-icons text-sm mr-2">list</span>
                      VER CAMPAÑAS
                    </Button>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>

        {/* System Log - Fixed Width Right Sidebar */}
        <div className="w-full lg:w-80 flex flex-col border border-primary/30 bg-black/80">
          <TerminalLog logs={logs} maxLogs={12} className="flex-1 h-24 md:h-32" />
        </div>

      </div>
    </TerminalLayout>
  );
};
