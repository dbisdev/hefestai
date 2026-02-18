/**
 * Campaign Generator Page
 * Allows users to create new campaigns or join existing ones
 * Cyberpunk terminal aesthetics with campaign management features
 */

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useAuth, useCampaign } from '@core/context';
import { gameSystemService } from '@core/services/api';
import type { GameSystem } from '@core/types';

interface CampaignGeneratorPageProps {
  onBack: () => void;
}

/**
 * Campaign Generator Page Component
 * Provides UI for creating new campaigns or joining existing ones via invite code
 */
export const CampaignGeneratorPage: React.FC<CampaignGeneratorPageProps> = ({ onBack }) => {
  const navigate = useNavigate();
  const { isMaster } = useAuth();
  const { createCampaign, joinCampaign, isLoading } = useCampaign();
  
  const [logs, setLogs] = useState([
    '> Campaign management system online...',
    '> [SUCCESS] Command protocols established.',
    '> Awaiting campaign parameters...'
  ]);
  const [isCreating, setIsCreating] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  // Players can only join campaigns, so default to 'join' tab for them
  const [activeTab, setActiveTab] = useState<'create' | 'join'>(isMaster ? 'create' : 'join');

  // Game systems state
  const [gameSystems, setGameSystems] = useState<GameSystem[]>([]);
  const [isLoadingGameSystems, setIsLoadingGameSystems] = useState(true);

  // Create campaign form state
  const [createForm, setCreateForm] = useState({
    name: '',
    description: '',
    gameSystemId: '',
  });

  // Join campaign form state
  const [joinCode, setJoinCode] = useState('');

  /**
   * Load game systems on component mount
   */
  useEffect(() => {
    const loadGameSystems = async () => {
      setIsLoadingGameSystems(true);
      try {
        const systems = await gameSystemService.getAll();
        setGameSystems(systems);
        
        // Auto-select first game system if available
        if (systems.length > 0 && !createForm.gameSystemId) {
          setCreateForm(prev => ({ ...prev, gameSystemId: systems[0].id }));
        }
        
        addLog(`[SUCCESS] ${systems.length} sistema(s) de juego cargado(s).`);
      } catch (error) {
        console.error('Failed to load game systems:', error);
        addLog('ERROR: No se pudieron cargar los sistemas de juego.');
      } finally {
        setIsLoadingGameSystems(false);
      }
    };

    loadGameSystems();
  }, []);

  /**
   * Adds a log entry to the terminal display
   */
  const addLog = (msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-6));
  };

  /**
   * Handles creating a new campaign
   */
  const handleCreate = async () => {
    if (!createForm.name.trim()) {
      addLog('ERROR: NOMBRE DE CAMPAÑA REQUERIDO');
      return;
    }

    if (!createForm.gameSystemId) {
      addLog('ERROR: SISTEMA DE JUEGO REQUERIDO');
      return;
    }

    setIsCreating(true);
    addLog('INICIALIZANDO NUEVA CAMPAÑA...');

    try {
      addLog('ESTABLECIENDO PARAMETROS DE COMANDO...');
      
      const campaign = await createCampaign(
        createForm.name.trim(),
        createForm.description.trim() || undefined,
        createForm.gameSystemId
      );

      addLog(`CAMPAÑA CREADA: ${campaign.name.toUpperCase()}`);
      addLog(`CODIGO DE ACCESO: ${campaign.joinCode || 'GENERANDO...'}`);
      addLog('REDIRIGIENDO A GALERIA...');
      
      setTimeout(onBack, 1500);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'CREACION FALLIDA';
      addLog(`ERROR_CRITICO: ${message}`);
      console.error(error);
    } finally {
      setIsCreating(false);
    }
  };

  /**
   * Handles joining an existing campaign
   */
  const handleJoin = async () => {
    if (!joinCode.trim()) {
      addLog('ERROR: CODIGO DE ACCESO REQUERIDO');
      return;
    }

    setIsJoining(true);
    addLog('VALIDANDO CODIGO DE ACCESO...');

    try {
      addLog('ESTABLECIENDO ENLACE CON CAMPAÑA...');
      
      const campaign = await joinCampaign(joinCode.trim().toUpperCase());

      addLog(`CONEXION ESTABLECIDA: ${campaign.name.toUpperCase()}`);
      addLog('SINCRONIZANDO DATOS DE CAMPAÑA...');
      addLog('REDIRIGIENDO A GALERIA...');
      
      setTimeout(onBack, 1500);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ACCESO DENEGADO';
      addLog(`ERROR_CRITICO: ${message}`);
      console.error(error);
    } finally {
      setIsJoining(false);
    }
  };

  /**
   * Get the selected game system details for preview
   */
  const getSelectedGameSystem = (): GameSystem | undefined => {
    return gameSystems.find(gs => gs.id === createForm.gameSystemId);
  };

  const isProcessing = isCreating || isJoining || isLoading;

  return (
    <TerminalLayout 
      title="CONTROL DE CAMPAÑAS" 
      subtitle="Sistema de Gestion de Campañas"
      icon="campaign"
      hideCampaignSelector={true}
    >
      <div className="flex flex-col lg:flex-row gap-8 h-full font-mono">
        {/* Form Panel */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          {/* Tab Selector - Only show Create tab for Masters */}
          <div className="flex border border-primary/30">
            {isMaster && (
              <button
                onClick={() => setActiveTab('create')}
                disabled={isProcessing}
                className={`flex-1 py-3 text-[10px] uppercase tracking-widest transition-all flex items-center justify-center gap-2 ${
                  activeTab === 'create'
                    ? 'bg-primary text-black font-bold'
                    : 'text-primary/60 hover:text-primary hover:bg-primary/5'
                }`}
              >
                <span className="material-icons text-sm">add_circle</span>
                Crear Campaña
              </button>
            )}
            <button
              onClick={() => setActiveTab('join')}
              disabled={isProcessing}
              className={`flex-1 py-3 text-[10px] uppercase tracking-widest transition-all flex items-center justify-center gap-2 ${
                activeTab === 'join'
                  ? 'bg-primary text-black font-bold'
                  : 'text-primary/60 hover:text-primary hover:bg-primary/5'
              }`}
            >
              <span className="material-icons text-sm">link</span>
              Unirse a Campaña
            </button>
          </div>

          {/* Create Campaign Form - Only for Masters */}
          {activeTab === 'create' && isMaster && (
            <div className="space-y-6 animate-fadeIn">
              <div className="border border-primary/20 bg-surface-dark/30 p-4">
                <div className="flex items-center gap-2 mb-4">
                  <span className="material-icons text-primary">campaign</span>
                  <span className="text-[10px] text-primary/60 uppercase tracking-widest">Nueva Campaña</span>
                </div>
                
                <p className="text-[11px] text-white/60 leading-relaxed">
                  Crear una nueva campaña te convierte en el <span className="text-primary font-bold">MASTER</span>. 
                  Podras invitar jugadores usando un codigo de acceso unico.
                </p>
              </div>

              {/* Game System Selector */}
              <div>
                <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                  <span className="material-icons text-sm">sports_esports</span> Sistema de Juego *
                </label>
                {isLoadingGameSystems ? (
                  <div className="w-full bg-surface-dark border border-primary/30 h-10 px-4 flex items-center">
                    <span className="text-primary/50 text-sm animate-pulse">Cargando sistemas...</span>
                  </div>
                ) : gameSystems.length === 0 ? (
                  <div className="w-full bg-surface-dark border border-danger/30 h-10 px-4 flex items-center">
                    <span className="text-danger/70 text-sm">No hay sistemas disponibles</span>
                  </div>
                ) : (
                  <select
                    value={createForm.gameSystemId}
                    onChange={(e) => setCreateForm({ ...createForm, gameSystemId: e.target.value })}
                    disabled={isProcessing}
                    className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm disabled:opacity-50"
                  >
                    <option value="">Seleccionar sistema de juego...</option>
                    {gameSystems.map((system) => (
                      <option key={system.id} value={system.id}>
                        {system.name} {system.version ? `(${system.version})` : ''}
                      </option>
                    ))}
                  </select>
                )}
                {getSelectedGameSystem()?.description && (
                  <p className="text-[9px] text-primary/40 mt-2">
                    {getSelectedGameSystem()?.description}
                  </p>
                )}
              </div>

              {/* Campaign Name */}
              <div>
                <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                  <span className="material-icons text-sm">badge</span> Nombre de Campaña *
                </label>
                <input
                  type="text"
                  value={createForm.name}
                  onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                  placeholder="Ingresa nombre de campaña..."
                  disabled={isProcessing}
                  className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm placeholder:text-white/30 disabled:opacity-50"
                  maxLength={100}
                />
              </div>

              {/* Campaign Description */}
              <div>
                <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                  <span className="material-icons text-sm">description</span> Descripción (Opcional)
                </label>
                <textarea
                  value={createForm.description}
                  onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
                  placeholder="Describe tu campaña..."
                  disabled={isProcessing}
                  rows={4}
                  className="w-full bg-surface-dark border border-primary/30 text-white px-4 py-3 focus:ring-primary focus:border-primary text-sm placeholder:text-white/30 resize-none disabled:opacity-50"
                  maxLength={500}
                />
                <p className="text-[9px] text-primary/40 mt-1 text-right">
                  {createForm.description.length}/500
                </p>
              </div>

              {/* Create Button */}
              <div className="pt-4 border-t border-primary/30">
                <Button
                  onClick={handleCreate}
                  disabled={isProcessing || !createForm.name.trim() || !createForm.gameSystemId}
                  variant="primary"
                  size="lg"
                  isLoading={isCreating}
                  icon="rocket_launch"
                  className="w-full"
                >
                  CREAR_CAMPAÑA
                </Button>
              </div>
            </div>
          )}

          {/* Join Campaign Form */}
          {activeTab === 'join' && (
            <div className="space-y-6 animate-fadeIn">
              <div className="border border-primary/20 bg-surface-dark/30 p-4">
                <div className="flex items-center gap-2 mb-4">
                  <span className="material-icons text-primary">group_add</span>
                  <span className="text-[10px] text-primary/60 uppercase tracking-widest">Unirse a Campaña</span>
                </div>
                
                <p className="text-[11px] text-white/60 leading-relaxed">
                  Ingresa el codigo de acceso proporcionado por el <span className="text-primary font-bold">MASTER</span> 
                  de la campaña para unirte como <span className="text-yellow-400 font-bold">JUGADOR</span>.
                </p>
              </div>

              {/* Join Code Input */}
              <div>
                <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                  <span className="material-icons text-sm">key</span> Código de Acceso *
                </label>
                <input
                  type="text"
                  value={joinCode}
                  onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                  placeholder="XXXXXXXX"
                  disabled={isProcessing}
                  className="w-full bg-surface-dark border border-primary/30 text-white h-14 px-4 focus:ring-primary focus:border-primary text-xl text-center tracking-[0.5em] font-bold uppercase placeholder:text-white/30 placeholder:tracking-normal placeholder:text-sm disabled:opacity-50"
                  maxLength={8}
                />
                <p className="text-[9px] text-primary/40 mt-2 text-center">
                  El codigo tiene 8 caracteres alfanumericos
                </p>
              </div>

              {/* Join Button */}
              <div className="pt-4 border-t border-primary/30">
                <Button
                  onClick={handleJoin}
                  disabled={isProcessing || joinCode.length < 8}
                  variant="secondary"
                  size="lg"
                  isLoading={isJoining}
                  icon="login"
                  className="w-full"
                >
                  UNIRSE_CAMPAÑA
                </Button>
              </div>
            </div>
          )}
        </div>

        {/* Preview Panel */}
        <div className="flex-1 flex flex-col gap-4 overflow-y-auto">
          {/* Campaign Preview Card */}
          <div className="border border-primary/30 bg-black p-6 flex-1 flex flex-col">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <span className="material-icons text-primary text-2xl">public</span>
                <span className="text-[10px] text-primary/60 uppercase tracking-widest">Vista Previa</span>
              </div>
              <div className="px-2 py-1 border border-primary/30 text-[8px] text-primary/60 uppercase">
                {activeTab === 'create' ? 'NUEVA' : 'EXISTENTE'}
              </div>
            </div>

            {activeTab === 'create' && createForm.name ? (
              <div className="flex-1 flex flex-col">
                <h2 className="text-2xl text-primary font-display uppercase text-glow mb-2">
                  {createForm.name}
                </h2>
                <div className="h-0.5 bg-gradient-to-r from-primary via-primary/20 to-transparent mb-4"></div>
                
                {/* Game System Badge */}
                {getSelectedGameSystem() && (
                  <div className="mb-4 flex items-center gap-2">
                    <span className="material-icons text-sm text-yellow-400">sports_esports</span>
                    <span className="text-[10px] text-yellow-400 font-bold uppercase">
                      {getSelectedGameSystem()?.name}
                    </span>
                    {getSelectedGameSystem()?.version && (
                      <span className="text-[9px] text-yellow-400/60">
                        v{getSelectedGameSystem()?.version}
                      </span>
                    )}
                  </div>
                )}
                
                {createForm.description && (
                  <p className="text-[11px] text-white/70 leading-relaxed flex-1">
                    {createForm.description}
                  </p>
                )}

                <div className="mt-auto pt-4 border-t border-primary/20">
                  <div className="grid grid-cols-2 gap-3">
                    <div className="bg-surface-dark border border-primary/10 p-3">
                      <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Tu Rol</p>
                      <p className="text-sm text-primary font-bold">MASTER</p>
                    </div>
                    <div className="bg-surface-dark border border-primary/10 p-3">
                      <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Estado</p>
                      <p className="text-sm text-green-400 font-bold">ACTIVA</p>
                    </div>
                  </div>
                </div>
              </div>
            ) : activeTab === 'join' && joinCode.length === 8 ? (
              <div className="flex-1 flex flex-col items-center justify-center">
                <div className="text-center">
                  <span className="material-icons text-6xl text-primary/30 mb-4">vpn_key</span>
                  <p className="text-lg text-primary font-mono tracking-[0.3em] mb-2">{joinCode}</p>
                  <p className="text-[10px] text-primary/40 uppercase">Codigo Validado</p>
                </div>

                <div className="mt-auto pt-4 w-full">
                  <div className="bg-surface-dark border border-primary/10 p-3 text-center">
                    <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Tu Rol</p>
                    <p className="text-sm text-yellow-400 font-bold">JUGADOR</p>
                  </div>
                </div>
              </div>
            ) : (
              <div className="flex-1 flex items-center justify-center">
                <div className="text-center">
                  <span className="material-icons text-6xl text-primary/10 mb-4">
                    {activeTab === 'create' ? 'edit_note' : 'password'}
                  </span>
                  <p className="text-primary/20 text-[10px] tracking-[0.5em] uppercase font-bold">
                    {activeTab === 'create' ? 'Ingresa Datos' : 'Ingresa Código'}
                  </p>
                </div>
              </div>
            )}
          </div>

          {/* Log Panel */}
          <div className="h-24 bg-black/80 border border-primary/20 p-3 text-[10px] text-primary/80 overflow-y-auto font-mono scrollbar-hide">
            {logs.map((log, i) => (
              <p key={i} className={i === logs.length - 1 ? "text-primary font-bold" : "opacity-60"}>
                {log}
              </p>
            ))}
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};

export default CampaignGeneratorPage;
