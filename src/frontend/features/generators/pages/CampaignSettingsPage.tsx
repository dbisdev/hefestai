/**
 * Campaign Settings Page
 * Allows Masters to edit campaign details, manage status, and regenerate join codes
 * Cyberpunk terminal aesthetics with campaign management features
 */

import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useCampaign } from '@core/context';

interface CampaignSettingsPageProps {
  onBack: () => void;
}

/**
 * Campaign Settings Page Component
 * Provides UI for editing campaign details, changing status, and managing join codes
 * Only accessible to campaign Masters
 */
export const CampaignSettingsPage: React.FC<CampaignSettingsPageProps> = ({ onBack }) => {
  const navigate = useNavigate();
  const { 
    activeCampaign, 
    updateCampaign, 
    updateCampaignStatus, 
    regenerateJoinCode,
    deleteCampaign,
    isLoading,
    isActiveCampaignMaster 
  } = useCampaign();
  
  const [logs, setLogs] = useState([
    '> Campaign settings system online...',
    '> [SUCCESS] Configuration protocols established.',
    '> Awaiting commands...'
  ]);
  
  const [isSaving, setIsSaving] = useState(false);
  const [isRegenerating, setIsRegenerating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isTogglingStatus, setIsTogglingStatus] = useState(false);

  // Form state for editing campaign
  const [editForm, setEditForm] = useState({
    name: '',
    description: '',
  });

  // Sync form with active campaign
  useEffect(() => {
    if (activeCampaign) {
      setEditForm({
        name: activeCampaign.name,
        description: activeCampaign.description || '',
      });
      addLog(`Campaña cargada: ${activeCampaign.name.toUpperCase()}`);
    }
  }, [activeCampaign?.id]);

  /**
   * Adds a log entry to the terminal display
   */
  const addLog = useCallback((msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-8));
  }, []);

  /**
   * Handles saving campaign changes
   */
  const handleSave = async () => {
    if (!activeCampaign) return;
    
    if (!editForm.name.trim()) {
      addLog('ERROR: NOMBRE DE CAMPAÑA REQUERIDO');
      return;
    }

    setIsSaving(true);
    addLog('GUARDANDO CAMBIOS...');

    try {
      await updateCampaign(activeCampaign.id, {
        name: editForm.name.trim(),
        description: editForm.description.trim() || undefined,
      });

      addLog(`[SUCCESS] Campaña actualizada: ${editForm.name.toUpperCase()}`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL GUARDAR';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsSaving(false);
    }
  };

  /**
   * Handles toggling campaign active status
   */
  const handleToggleStatus = async () => {
    if (!activeCampaign) return;
    
    const newStatus = !activeCampaign.isActive;
    const statusText = newStatus ? 'ACTIVANDO' : 'DESACTIVANDO';
    
    if (!newStatus && !confirm('¿Desactivar esta campaña? Los jugadores no podrán acceder hasta que se reactive.')) {
      return;
    }

    setIsTogglingStatus(true);
    addLog(`${statusText} CAMPAÑA...`);

    try {
      await updateCampaignStatus(activeCampaign.id, newStatus);
      addLog(`[SUCCESS] Campaña ${newStatus ? 'ACTIVADA' : 'DESACTIVADA'}`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL CAMBIAR ESTADO';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsTogglingStatus(false);
    }
  };

  /**
   * Handles regenerating join code
   */
  const handleRegenerateCode = async () => {
    if (!activeCampaign) return;
    
    if (!confirm('¿Regenerar código de acceso? El código anterior dejará de funcionar.')) {
      return;
    }

    setIsRegenerating(true);
    addLog('REGENERANDO CODIGO DE ACCESO...');

    try {
      const newCode = await regenerateJoinCode(activeCampaign.id);
      addLog(`[SUCCESS] Nuevo código: ${newCode}`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL REGENERAR';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsRegenerating(false);
    }
  };

  /**
   * Handles deleting campaign
   */
  const handleDelete = async () => {
    if (!activeCampaign) return;
    
    if (!confirm(`¿ELIMINAR PERMANENTEMENTE la campaña "${activeCampaign.name}"? Esta acción NO se puede deshacer.`)) {
      return;
    }
    
    // Second confirmation for safety
    if (!confirm('¿Estás ABSOLUTAMENTE seguro? Todos los datos de la campaña se perderán.')) {
      return;
    }

    setIsDeleting(true);
    addLog('ELIMINANDO CAMPAÑA...');

    try {
      await deleteCampaign(activeCampaign.id);
      addLog('[SUCCESS] Campaña eliminada. Redirigiendo...');
      setTimeout(onBack, 1500);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL ELIMINAR';
      addLog(`ERROR_CRITICO: ${message}`);
      setIsDeleting(false);
    }
  };

  /**
   * Copy join code to clipboard
   */
  const handleCopyCode = () => {
    if (activeCampaign?.joinCode) {
      navigator.clipboard.writeText(activeCampaign.joinCode);
      addLog('[SUCCESS] Código copiado al portapapeles');
    }
  };

  // Check if user has access
  if (!activeCampaign) {
    return (
      <TerminalLayout 
        title="CONFIG_CAMPAÑA" 
        subtitle="Configuracion de Campaña"
        icon="settings"
        hideCampaignSelector={true}
      >
        <div className="flex flex-col items-center justify-center h-full text-primary/60">
          <span className="material-icons text-6xl mb-4">warning</span>
          <p className="text-sm uppercase tracking-widest">No hay campaña seleccionada</p>
          <Button onClick={onBack} className="mt-4">VOLVER</Button>
        </div>
      </TerminalLayout>
    );
  }

  if (!isActiveCampaignMaster) {
    return (
      <TerminalLayout 
        title="CONFIG_CAMPAÑA" 
        subtitle="Configuracion de Campaña"
        icon="settings"
        hideCampaignSelector={true}
      >
        <div className="flex flex-col items-center justify-center h-full text-danger/60">
          <span className="material-icons text-6xl mb-4">lock</span>
          <p className="text-sm uppercase tracking-widest">Acceso restringido a Masters</p>
          <Button onClick={onBack} className="mt-4">VOLVER</Button>
        </div>
      </TerminalLayout>
    );
  }

  const isAnyOperationInProgress = isSaving || isRegenerating || isDeleting || isTogglingStatus || isLoading;

    return (
      <TerminalLayout 
        title="CONFIG_CAMPAÑA" 
        subtitle="Configuracion de Campaña"
        icon="settings"
        hideCampaignSelector={true}
      >
      <div className="flex flex-col lg:flex-row h-full p-4 lg:p-8 gap-6">
        {/* Main Form Section */}
        <div className="flex-1 flex flex-col gap-6  overflow-y-auto">
          {/* Header */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                  Configuración de Campaña
                </h1>
                <p className="text-primary/40 text-xs mt-1">
                  ID: {activeCampaign.id.substring(0, 8)}...
                </p>
              </div>
              <button 
                onClick={onBack}
                className="material-icons text-primary/60 hover:text-primary transition-colors"
                aria-label="Volver"
              >
                arrow_back
              </button>
            </div>
          </div>

          {/* Edit Form */}
          <div className="border border-primary/30 bg-black/60 p-6 flex-1">
            <h2 className="text-sm text-primary/60 uppercase tracking-widest mb-4 flex items-center gap-2">
              <span className="material-icons text-xs">edit</span>
              Datos de la Campaña
            </h2>
            
            <div className="space-y-4">
              {/* Campaign Name */}
              <div>
                <label className="block text-xs text-primary/40 uppercase mb-1">
                  Nombre
                </label>
                <input
                  type="text"
                  value={editForm.name}
                  onChange={(e) => setEditForm(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Nombre de la campaña"
                  className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20"
                  disabled={isAnyOperationInProgress}
                />
              </div>

              {/* Campaign Description */}
              <div>
                <label className="block text-xs text-primary/40 uppercase mb-1">
                  Descripción
                </label>
                <textarea
                  value={editForm.description}
                  onChange={(e) => setEditForm(prev => ({ ...prev, description: e.target.value }))}
                  placeholder="Descripción de la campaña (opcional)"
                  rows={4}
                  className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20 resize-none"
                  disabled={isAnyOperationInProgress}
                />
              </div>

              {/* Save Button */}
              <Button 
                onClick={handleSave} 
                disabled={isAnyOperationInProgress}
                className="w-full"
              >
                {isSaving ? 'GUARDANDO...' : 'GUARDAR CAMBIOS'}
              </Button>
            </div>
          </div>

          {/* Status and Actions */}
          <div className="border border-primary/30 bg-black/60 p-6">
            <h2 className="text-sm text-primary/60 uppercase tracking-widest mb-4 flex items-center gap-2">
              <span className="material-icons text-xs">settings</span>
              Estado y Acciones
            </h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Campaign Status */}
              <div className="bg-black/40 border border-primary/20 p-4">
                <p className="text-xs text-primary/40 uppercase mb-2">Estado</p>
                <div className="flex items-center justify-between">
                  <span className={`text-sm font-bold uppercase ${activeCampaign.isActive ? 'text-green-400' : 'text-yellow-400'}`}>
                    {activeCampaign.isActive ? 'ACTIVA' : 'INACTIVA'}
                  </span>
                  <button
                    onClick={handleToggleStatus}
                    disabled={isAnyOperationInProgress}
                    className={`px-3 py-1 text-xs uppercase border transition-colors ${
                      activeCampaign.isActive 
                        ? 'border-yellow-500/60 text-yellow-500 hover:bg-yellow-500/20' 
                        : 'border-green-500/60 text-green-500 hover:bg-green-500/20'
                    } disabled:opacity-50`}
                  >
                    {isTogglingStatus ? '...' : activeCampaign.isActive ? 'Desactivar' : 'Activar'}
                  </button>
                </div>
              </div>

              {/* Join Code */}
              <div className="bg-black/40 border border-primary/20 p-4">
                <p className="text-xs text-primary/40 uppercase mb-2">Código de Acceso</p>
                <div className="flex items-center gap-2">
                  <span className="text-primary font-mono font-bold flex-1 text-lg tracking-wider">
                    {activeCampaign.joinCode || '------'}
                  </span>
                  <button
                    onClick={handleCopyCode}
                    disabled={!activeCampaign.joinCode}
                    className="material-icons text-primary/60 hover:text-primary transition-colors text-sm disabled:opacity-30"
                    title="Copiar código"
                  >
                    content_copy
                  </button>
                  <button
                    onClick={handleRegenerateCode}
                    disabled={isAnyOperationInProgress}
                    className="material-icons text-cyan-500/60 hover:text-cyan-500 transition-colors text-sm disabled:opacity-30"
                    title="Regenerar código"
                  >
                    refresh
                  </button>
                </div>
              </div>

              {/* Member Count */}
              <div className="bg-black/40 border border-primary/20 p-4">
                <p className="text-xs text-primary/40 uppercase mb-2">Miembros</p>
                <span className="text-primary font-bold text-lg">
                  {activeCampaign.memberCount}
                </span>
              </div>

              {/* Created Date */}
              <div className="bg-black/40 border border-primary/20 p-4">
                <p className="text-xs text-primary/40 uppercase mb-2">Creada</p>
                <span className="text-primary/80 text-sm">
                  {new Date(activeCampaign.createdAt).toLocaleDateString('es-ES', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric'
                  })}
                </span>
              </div>
            </div>

            {/* Danger Zone */}
            <div className="mt-6 border-t border-danger/30 pt-4">
              <h3 className="text-xs text-danger/60 uppercase tracking-widest mb-3 flex items-center gap-2">
                <span className="material-icons text-xs">warning</span>
                Zona de Peligro
              </h3>
              <button
                onClick={handleDelete}
                disabled={isAnyOperationInProgress}
                className="w-full py-3 border border-danger/60 text-danger text-xs uppercase tracking-widest hover:bg-danger hover:text-white transition-all disabled:opacity-50"
              >
                {isDeleting ? 'ELIMINANDO...' : 'ELIMINAR CAMPAÑA PERMANENTEMENTE'}
              </button>
            </div>
          </div>
        </div>

        {/* Terminal Log Section */}
        <div className="w-full lg:w-80 flex flex-col border border-primary/30 bg-black/80">
          <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">terminal</span>
            System Log
          </div>
          <div className="md:flex-1 flex-none h-24 md:h-32 p-4 font-mono text-xs text-primary/70 space-y-1 overflow-y-auto">
            {logs.map((log, i) => (
              <p key={i} className={`${log.includes('ERROR') ? 'text-danger' : log.includes('SUCCESS') ? 'text-green-400' : ''}`}>
                {log}
              </p>
            ))}
            <p className="animate-pulse">_</p>
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};
