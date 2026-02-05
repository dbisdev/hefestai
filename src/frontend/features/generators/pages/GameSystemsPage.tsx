/**
 * Game Systems Management Page
 * Allows Masters and Admins to create and manage game systems
 * Cyberpunk terminal aesthetics with CRUD functionality
 */

import React, { useState, useEffect, useCallback } from 'react';
import { TerminalLayout, AdminLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useAuth } from '@core/context';
import { gameSystemService } from '@core/services/api';
import type { GameSystem, CreateGameSystemRequest } from '@core/types';
import { Screen } from '@core/types';

interface GameSystemsPageProps {
  /** Handler for navigating to other screens (used by Admin layout) */
  onNavigate?: (screen: Screen) => void;
  /** Handler for returning to gallery */
  onBack: () => void;
}

/**
 * Game Systems Page Component
 * Provides UI for creating and managing game systems (tabletop RPG rule sets)
 * Only accessible to Master or Admin users
 */
export const GameSystemsPage: React.FC<GameSystemsPageProps> = ({ onNavigate, onBack }) => {
  const { user } = useAuth();
  
  const [gameSystems, setGameSystems] = useState<GameSystem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [selectedSystem, setSelectedSystem] = useState<GameSystem | null>(null);
  
  const [logs, setLogs] = useState([
    '> Game systems management online...',
    '> [SUCCESS] Repository connection established.',
    '> Awaiting commands...'
  ]);

  // Form state for creating game system
  const [createForm, setCreateForm] = useState<CreateGameSystemRequest>({
    code: '',
    name: '',
    publisher: '',
    version: '',
    description: '',
    supportedEntityTypes: [],
  });
  
  // Entity types input (comma-separated for UX)
  const [entityTypesInput, setEntityTypesInput] = useState('');

  /**
   * Adds a log entry to the terminal display
   */
  const addLog = useCallback((msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-10));
  }, []);

  /**
   * Fetches all game systems from the API
   */
  const fetchGameSystems = useCallback(async () => {
    setIsLoading(true);
    try {
      const systems = await gameSystemService.getAll();
      setGameSystems(systems);
      addLog(`[SUCCESS] ${systems.length} sistemas cargados`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar sistemas';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsLoading(false);
    }
  }, [addLog]);

  // Load game systems on mount
  useEffect(() => {
    fetchGameSystems();
  }, [fetchGameSystems]);

  /**
   * Validates the create form
   */
  const validateForm = (): boolean => {
    if (!createForm.code.trim()) {
      addLog('ERROR: CODIGO REQUERIDO');
      return false;
    }
    
    // Validate code format (lowercase, alphanumeric with hyphens)
    const codeRegex = /^[a-z0-9-]+$/;
    if (!codeRegex.test(createForm.code.trim())) {
      addLog('ERROR: CODIGO SOLO PUEDE CONTENER LETRAS MINUSCULAS, NUMEROS Y GUIONES');
      return false;
    }
    
    if (!createForm.name.trim()) {
      addLog('ERROR: NOMBRE REQUERIDO');
      return false;
    }
    
    return true;
  };

  /**
   * Handles creating a new game system
   */
  const handleCreate = async () => {
    if (!validateForm()) return;

    setIsCreating(true);
    addLog('CREANDO SISTEMA DE JUEGO...');

    try {
      // Parse entity types from comma-separated input
      const entityTypes = entityTypesInput
        .split(',')
        .map(t => t.trim())
        .filter(t => t.length > 0);

      const request: CreateGameSystemRequest = {
        code: createForm.code.trim().toLowerCase(),
        name: createForm.name.trim(),
        publisher: createForm.publisher?.trim() || undefined,
        version: createForm.version?.trim() || undefined,
        description: createForm.description?.trim() || undefined,
        supportedEntityTypes: entityTypes.length > 0 ? entityTypes : undefined,
      };

      const newSystem = await gameSystemService.create(request);
      
      setGameSystems(prev => [...prev, newSystem]);
      addLog(`[SUCCESS] Sistema creado: ${newSystem.name.toUpperCase()}`);
      
      // Reset form
      setCreateForm({
        code: '',
        name: '',
        publisher: '',
        version: '',
        description: '',
        supportedEntityTypes: [],
      });
      setEntityTypesInput('');
      setShowCreateForm(false);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL CREAR SISTEMA';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsCreating(false);
    }
  };

  /**
   * Handles toggling game system active status
   */
  const handleToggleStatus = async (system: GameSystem) => {
    const newStatus = !system.isActive;
    const statusText = newStatus ? 'ACTIVANDO' : 'DESACTIVANDO';
    
    addLog(`${statusText} ${system.code.toUpperCase()}...`);

    try {
      const updated = await gameSystemService.updateStatus(system.id, { isActive: newStatus });
      setGameSystems(prev => prev.map(s => s.id === system.id ? updated : s));
      addLog(`[SUCCESS] ${system.code.toUpperCase()} ${newStatus ? 'ACTIVADO' : 'DESACTIVADO'}`);
      
      if (selectedSystem?.id === system.id) {
        setSelectedSystem(updated);
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL CAMBIAR ESTADO';
      addLog(`ERROR: ${message}`);
    }
  };

  // Check if user has access (Master or Admin)
  const isMasterOrAdmin = user?.role === 'MASTER' || user?.role === 'ADMIN';
  const isAdmin = user?.role === 'ADMIN';
  
  // Wrapper component based on user role
  const LayoutWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    if (isAdmin && onNavigate) {
      return (
        <AdminLayout 
          activeScreen={Screen.GAME_SYSTEMS} 
          onNavigate={onNavigate} 
          onBack={onBack}
        >
          {children}
        </AdminLayout>
      );
    }
    return (
      <TerminalLayout title="GAME_SYSTEMS" subtitle="Gestión de sistemas de juego" onLogout={() => {}}>
        {children}
      </TerminalLayout>
    );
  };
  
  if (!isMasterOrAdmin) {
    return (
      <LayoutWrapper>
        <div className="flex flex-col items-center justify-center h-full text-danger/60">
          <span className="material-icons text-6xl mb-4">lock</span>
          <p className="text-sm uppercase tracking-widest">Acceso restringido a Masters</p>
          <Button onClick={onBack} className="mt-4">VOLVER</Button>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper>
      <div className="flex flex-col lg:flex-row h-full gap-6">
        {/* Main Content Section */}
        <div className="flex-1 flex flex-col gap-6 overflow-hidden">
          {/* Header */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                  Sistemas de Juego
                </h1>
                <p className="text-primary/40 text-xs mt-1">
                  Gestiona los sistemas de reglas disponibles
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Button 
                  onClick={() => setShowCreateForm(!showCreateForm)}
                  variant={showCreateForm ? 'secondary' : 'primary'}
                  size="sm"
                >
                  {showCreateForm ? 'CANCELAR' : '+ NUEVO SISTEMA'}
                </Button>
                <button 
                  onClick={onBack}
                  className="material-icons text-primary/60 hover:text-primary transition-colors"
                  aria-label="Volver"
                >
                  arrow_back
                </button>
              </div>
            </div>
          </div>

          {/* Create Form */}
          {showCreateForm && (
            <div className="border border-cyan-500/30 bg-black/60 p-6">
              <h2 className="text-sm text-cyan-500/60 uppercase tracking-widest mb-4 flex items-center gap-2">
                <span className="material-icons text-xs">add_circle</span>
                Crear Nuevo Sistema
              </h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Code */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Codigo *
                  </label>
                  <input
                    type="text"
                    value={createForm.code}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, code: e.target.value.toLowerCase() }))}
                    placeholder="ej: dnd5e, pathfinder2e"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-cyan-500 focus:outline-none placeholder:text-primary/20 font-mono"
                    disabled={isCreating}
                  />
                  <p className="text-xs text-primary/30 mt-1">Solo letras minusculas, numeros y guiones</p>
                </div>

                {/* Name */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Nombre *
                  </label>
                  <input
                    type="text"
                    value={createForm.name}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="ej: Dungeons & Dragons 5e"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-cyan-500 focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                </div>

                {/* Publisher */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Editorial
                  </label>
                  <input
                    type="text"
                    value={createForm.publisher || ''}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, publisher: e.target.value }))}
                    placeholder="ej: Wizards of the Coast"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                </div>

                {/* Version */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Version
                  </label>
                  <input
                    type="text"
                    value={createForm.version || ''}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, version: e.target.value }))}
                    placeholder="ej: 5.1, 2024"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                </div>

                {/* Description */}
                <div className="md:col-span-2">
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Descripcion
                  </label>
                  <textarea
                    value={createForm.description || ''}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, description: e.target.value }))}
                    placeholder="Descripcion del sistema de juego..."
                    rows={2}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 resize-none"
                    disabled={isCreating}
                  />
                </div>

                {/* Supported Entity Types */}
                <div className="md:col-span-2">
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Tipos de Entidad Soportados
                  </label>
                  <input
                    type="text"
                    value={entityTypesInput}
                    onChange={(e) => setEntityTypesInput(e.target.value)}
                    placeholder="ej: character, npc, monster, item, spell (separados por coma)"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                  <p className="text-xs text-primary/30 mt-1">Separa los tipos con comas</p>
                </div>
              </div>

              {/* Create Button */}
              <div className="mt-4 flex justify-end">
                <Button 
                  onClick={handleCreate} 
                  disabled={isCreating}
                  className="min-w-[200px]"
                >
                  {isCreating ? 'CREANDO...' : 'CREAR SISTEMA'}
                </Button>
              </div>
            </div>
          )}

          {/* Game Systems List */}
          <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
            <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="material-icons text-sm">sports_esports</span>
                Sistemas Disponibles
              </span>
              <span className="text-primary/40">{gameSystems.length} sistemas</span>
            </div>
            
            <div className="flex-1 overflow-y-auto p-4">
              {isLoading ? (
                <div className="flex items-center justify-center h-full text-primary/40">
                  <span className="animate-pulse">CARGANDO SISTEMAS...</span>
                </div>
              ) : gameSystems.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-primary/40">
                  <span className="material-icons text-4xl mb-2">inventory_2</span>
                  <p className="text-sm uppercase">No hay sistemas registrados</p>
                  <p className="text-xs mt-1">Crea el primer sistema de juego</p>
                </div>
              ) : (
                <div className="grid gap-3">
                  {gameSystems.map((system) => (
                    <div
                      key={system.id}
                      className={`border p-4 cursor-pointer transition-all ${
                        selectedSystem?.id === system.id 
                          ? 'border-cyan-500 bg-cyan-500/10' 
                          : 'border-primary/20 bg-black/40 hover:border-primary/40'
                      }`}
                      onClick={() => setSelectedSystem(system)}
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <span className="font-mono text-cyan-400 text-sm">{system.code}</span>
                            <span className={`text-xs px-2 py-0.5 border ${
                              system.isActive !== false
                                ? 'border-green-500/40 text-green-400' 
                                : 'border-yellow-500/40 text-yellow-400'
                            }`}>
                              {system.isActive !== false ? 'ACTIVO' : 'INACTIVO'}
                            </span>
                          </div>
                          <h3 className="text-primary font-bold mt-1">{system.name}</h3>
                          {system.publisher && (
                            <p className="text-primary/40 text-xs mt-1">
                              {system.publisher} {system.version && `v${system.version}`}
                            </p>
                          )}
                        </div>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleToggleStatus(system);
                          }}
                          className={`material-icons text-sm transition-colors ${
                            system.isActive !== false
                              ? 'text-green-500/60 hover:text-yellow-500'
                              : 'text-yellow-500/60 hover:text-green-500'
                          }`}
                          title={system.isActive !== false ? 'Desactivar' : 'Activar'}
                        >
                          {system.isActive !== false ? 'toggle_on' : 'toggle_off'}
                        </button>
                      </div>
                      
                      {system.description && (
                        <p className="text-primary/50 text-xs mt-2 line-clamp-2">
                          {system.description}
                        </p>
                      )}
                      
                      {system.supportedEntityTypes && system.supportedEntityTypes.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-2">
                          {system.supportedEntityTypes.map((type) => (
                            <span 
                              key={type}
                              className="text-xs px-2 py-0.5 bg-primary/10 text-primary/60 border border-primary/20"
                            >
                              {type}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Terminal Log Section */}
        <div className="w-full lg:w-80 flex flex-col border border-primary/30 bg-black/80">
          <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">terminal</span>
            System Log
          </div>
          <div className="flex-1 p-4 font-mono text-xs text-primary/70 space-y-1 overflow-y-auto">
            {logs.map((log, i) => (
              <p key={i} className={`${log.includes('ERROR') ? 'text-danger' : log.includes('SUCCESS') ? 'text-green-400' : ''}`}>
                {log}
              </p>
            ))}
            <p className="animate-pulse">_</p>
          </div>
          
          {/* Selected System Details */}
          {selectedSystem && (
            <div className="border-t border-primary/30 p-4">
              <h3 className="text-xs text-primary/60 uppercase tracking-widest mb-2">
                Sistema Seleccionado
              </h3>
              <div className="space-y-2 text-xs">
                <div className="flex justify-between">
                  <span className="text-primary/40">ID:</span>
                  <span className="text-primary font-mono">{selectedSystem.id.substring(0, 8)}...</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-primary/40">Codigo:</span>
                  <span className="text-cyan-400 font-mono">{selectedSystem.code}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-primary/40">Nombre:</span>
                  <span className="text-primary">{selectedSystem.name}</span>
                </div>
                {selectedSystem.publisher && (
                  <div className="flex justify-between">
                    <span className="text-primary/40">Editorial:</span>
                    <span className="text-primary/70">{selectedSystem.publisher}</span>
                  </div>
                )}
                {selectedSystem.version && (
                  <div className="flex justify-between">
                    <span className="text-primary/40">Version:</span>
                    <span className="text-primary/70">{selectedSystem.version}</span>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
};
