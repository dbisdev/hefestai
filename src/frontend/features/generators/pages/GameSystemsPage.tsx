/**
 * Game Systems Management Page (Refactored)
 * Single Responsibility: Orchestrate game systems management UI
 * Uses custom hooks and components (DRY, SOLID-SRP)
 */

import React, { useEffect, useCallback, useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout, AdminLayout } from '@shared/components/layout';
import { Button, TerminalLog, EmptyState, Input } from '@shared/components/ui';
import { ManualUploadModal } from '@shared/components/modals';
import { useAuth } from '@core/context';
import { useTerminalLog, useList, useConfirmDialog } from '@core/hooks';
import { gameSystemService, entityTemplateService, campaignService } from '@core/services/api';
import { 
  useGameSystems 
} from '@features/generators/hooks';
import { 
  GameSystemList, 
  GameSystemForm, 
  GameSystemDetails 
} from '@features/generators/components/game-systems';
import type { 
  GameSystem, 
  CreateGameSystemRequest, 
  UpdateGameSystemRequest, 
  EntityTemplateSummary, 
  Campaign 
} from '@core/types';

export const GameSystemsPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { logs, addLog } = useTerminalLog({
    maxLogs: 10,
    initialLogs: [
      '> Game systems management online...',
      '> [SUCCESS] Repository connection established.',
      '> Awaiting commands...'
    ]
  });

  const [showManualUpload, setShowManualUpload] = useState(false);
  const [confirmedTemplates, setConfirmedTemplates] = useState<EntityTemplateSummary[]>([]);
  const [campaignsUsingSystem, setCampaignsUsingSystem] = useState<Campaign[]>([]);
  const [isLoadingSystemData, setIsLoadingSystemData] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  const {
    selectedSystem,
    isCreating,
    isUpdating,
    formMode,
    selectSystem,
    openCreateForm,
    openEditForm,
    closeForm,
    create,
    update,
    toggleStatus,
  } = useGameSystems();

  const { 
    isOpen: isConfirmOpen, 
    config: confirmConfig, 
    confirm, 
    handleConfirm, 
    handleCancel 
  } = useConfirmDialog();

  const fetchGameSystems = useCallback(
    () => gameSystemService.getAll(),
    []
  );

  const searchFields = useMemo(
    () => ['name', 'code', 'publisher'] as ('name' | 'code' | 'publisher')[],
    []
  );

  const { 
    filteredItems: systems, 
    isLoading,
    refresh: refreshSystems,
    setSearchTerm: setListSearchTerm 
  } = useList<GameSystem>({
    fetchFn: fetchGameSystems,
    searchFields,
  });

  useEffect(() => {
    setListSearchTerm(searchTerm);
  }, [searchTerm, setListSearchTerm]);

  const isSystemOwned = useCallback(
    (system: GameSystem): boolean => {
      if (!user) return false;
      if (user.role === 'ADMIN') return true;
      return system.ownerId === user.id;
    },
    [user]
  );

  useEffect(() => {
    if (!selectedSystem) {
      setConfirmedTemplates([]);
      setCampaignsUsingSystem([]);
      return;
    }

    setIsLoadingSystemData(true);

    const loadSystemData = async () => {
      try {
        const templatesResult = await entityTemplateService.getByGameSystem(
          selectedSystem.id,
          undefined,
          true
        );
        setConfirmedTemplates(templatesResult.templates);

        const allCampaigns = await campaignService.getAll();
        const filtered = allCampaigns.filter((c) => c.gameSystemId === selectedSystem.id);
        setCampaignsUsingSystem(filtered);
      } catch (error) {
        if (error instanceof Error) {
          console.warn('[GameSystems] Could not load system data:', error.message);
          addLog(`WARN: No se pudo cargar datos del sistema`);
        }
        setConfirmedTemplates([]);
        setCampaignsUsingSystem([]);
      } finally {
        setIsLoadingSystemData(false);
      }
    };

    loadSystemData();
  }, [selectedSystem, addLog]);

  const handleCreate = async (data: CreateGameSystemRequest) => {
    const result = await create(data);
    if (result) {
      addLog(`[SUCCESS] Sistema creado: ${result.name.toUpperCase()}`);
      refreshSystems();
    } else {
      addLog('ERROR: No se pudo crear el sistema');
    }
    return result;
  };

  const handleUpdate = async (id: string, data: UpdateGameSystemRequest) => {
    const result = await update(id, data);
    if (result) {
      addLog(`[SUCCESS] Sistema actualizado: ${result.name.toUpperCase()}`);
      refreshSystems();
    } else {
      addLog('ERROR: No se pudo actualizar el sistema');
    }
    return result;
  };

  const handleToggleStatus = async (system: GameSystem) => {
    const confirmed = await confirm({
      title: system.isActive ? 'Desactivar Sistema' : 'Activar Sistema',
      message: `¿Confirmar ${system.isActive ? 'desactivación' : 'activación'} de ${system.name}?`,
      confirmLabel: system.isActive ? 'DESACTIVAR' : 'ACTIVAR',
      variant: 'warning',
    });

    if (confirmed) {
      const success = await toggleStatus(system.id, !system.isActive);
      if (success) {
        addLog(`[SUCCESS] ${system.code.toUpperCase()} ${system.isActive ? 'DESACTIVADO' : 'ACTIVADO'}`);
        refreshSystems();
      }
    }
  };

  const handleFormSubmit = async (data: CreateGameSystemRequest | UpdateGameSystemRequest) => {
    if (formMode === 'create') {
      return handleCreate(data as CreateGameSystemRequest);
    } else if (selectedSystem) {
      return handleUpdate(selectedSystem.id, data as UpdateGameSystemRequest);
    }
    return null;
  };

  const isMasterOrAdmin = user?.role === 'MASTER' || user?.role === 'ADMIN';
  const isAdmin = user?.role === 'ADMIN';
  const useAdminLayoutFlag = isAdmin;

  const accessDeniedContent = (
    <div className="flex flex-col items-center justify-center h-full text-danger/60">
      <span className="material-icons text-6xl mb-4">lock</span>
      <p className="text-sm uppercase tracking-widest">Acceso restringido a Masters</p>
      <Button onClick={() => navigate(-1)} className="mt-4">VOLVER</Button>
    </div>
  );

  if (!isMasterOrAdmin) {
    return useAdminLayoutFlag ? (
      <AdminLayout activePath="/game-systems">{accessDeniedContent}</AdminLayout>
    ) : (
      <TerminalLayout title="SISTEMAS" subtitle="Gestión de sistemas de juego" icon="sports_esports" hideCampaignSelector>
        {accessDeniedContent}
      </TerminalLayout>
    );
  }

  const mainContent = (
    <div className="flex flex-col lg:flex-row h-full gap-6">
      <div className="flex-1 flex flex-col gap-6 overflow-hidden">
        <div className="border border-primary/30 bg-black/60 p-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                Sistemas de Juego
              </h1>
              <p className="text-primary/40 text-xs mt-1 hidden md:block">
                Gestiona los sistemas de reglas disponibles
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Input
                icon="search"
                placeholder="Buscar..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-40"
              />
              <Button onClick={openCreateForm} variant="primary" size="sm" icon="add">
                Nuevo
              </Button>
            </div>
          </div>
        </div>

        <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
          <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
            <span className="flex items-center gap-2">
              <span className="material-icons text-sm">sports_esports</span>
              Sistemas Disponibles
            </span>
            <span className="text-primary/40">{systems.length} sistemas</span>
          </div>
          
          <div className="flex-1 overflow-y-auto p-4">
            <GameSystemList
              systems={systems}
              selectedId={selectedSystem?.id ?? null}
              currentUserId={user?.id}
              userRole={user?.role}
              onSelect={selectSystem}
              onEdit={openEditForm}
              isLoading={isLoading}
            />
          </div>
        </div>
      </div>

      <div className="w-full lg:w-80 flex flex-col border border-primary/30 bg-black/80">
        {selectedSystem && (
          <>
            <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
              <span className="material-icons text-sm">terminal</span>
              Sistema Seleccionado
            </div>
            <GameSystemDetails
              system={selectedSystem}
              isOwned={isSystemOwned(selectedSystem)}
              isLoadingData={isLoadingSystemData}
              confirmedTemplates={confirmedTemplates}
              campaignsUsingSystem={campaignsUsingSystem}
              onEdit={() => openEditForm(selectedSystem)}
              onUploadManual={() => setShowManualUpload(true)}
              onExtractEntities={() => navigate(`/templates?gameSystemId=${selectedSystem.id}`)}
            />
          </>
        )}

        <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
          <span className="material-icons text-sm">terminal</span>
          System Log
        </div>
        <TerminalLog logs={logs} maxLogs={10} className="h-24 md:h-32" />
      </div>
    </div>
  );

  return (
    <>
      {useAdminLayoutFlag ? (
        <AdminLayout activePath="/game-systems">{mainContent}</AdminLayout>
      ) : (
        <TerminalLayout title="SISTEMAS" subtitle="Gestión de sistemas de juego" icon="sports_esports" hideCampaignSelector>
          {mainContent}
        </TerminalLayout>
      )}

      {formMode && (
        <GameSystemForm
          mode={formMode}
          initialData={selectedSystem}
          onSubmit={handleFormSubmit}
          onCancel={closeForm}
          isLoading={isCreating || isUpdating}
        />
      )}

      {showManualUpload && selectedSystem && (
        <ManualUploadModal
          onClose={() => setShowManualUpload(false)}
          gameSystemId={selectedSystem.id}
          gameSystemName={selectedSystem.name}
          onSuccess={() => {
            addLog(`[SUCCESS] Manual cargado para ${selectedSystem.code.toUpperCase()}`);
            setShowManualUpload(false);
          }}
        />
      )}
    </>
  );
};

export default GameSystemsPage;
