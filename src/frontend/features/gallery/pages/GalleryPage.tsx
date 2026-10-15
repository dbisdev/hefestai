/**
 * Gallery Page
 * Displays all entities within the active campaign with category filtering.
 * Includes campaign selection and management.
 *
 * Accessibility Features:
 * - Full keyboard navigation (Tab, Enter, Space, Arrow keys)
 * - ARIA labels and roles for screen readers
 * - Focus management for modal dialogs
 * - Live region announcements for state changes
 *
 * Refactored using:
 * - useGalleryEntities: Entity fetching and management
 * - useEntityActions: CRUD operations
 * - useGalleryCategories: Category state management
 * - useConfirmDialog: Confirmation dialogs
 * - useConfirmedTemplates: Template availability check (extracted hook)
 * - useEntityFieldDefinitions: Field defs for modals/PDF (extracted hook)
 *
 * Sub-components extracted:
 * - GallerySidebar: Left navigation panel
 * - GalleryEntityCard: Full-image entity card for the grid
 * - AddNewEntityCard: "Create entity" slot at the end of the grid
 * - EntityDetailPanel: Right-side inspector panel
 * - CampaignSelectorPopup: Campaign switcher floating dialog
 * - InviteCodePopup: Join-code display dialog
 * - PdfImportDialog: PDF import floating dialog
 */

import React, {
  useState,
  useEffect,
  useCallback,
  useRef,
  KeyboardEvent,
  useMemo,
} from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { EntityEditModal, EntityViewModal, EntityDetailModal } from '@shared/components/modals';
import { ConfirmDialog, TerminalLog } from '@shared/components/ui';
import { useCampaign, useAuth } from '@core/context';
import { entityService } from '@core/services/api';
import { useCharacterSheetPdf, useConfirmDialog, useTerminalLog } from '@core/hooks';
import {
  useGalleryEntities,
  useEntityActions,
  useGalleryCategories,
  useConfirmedTemplates,
  useEntityFieldDefinitions,
} from '@features/gallery/hooks';
import {
  GallerySidebar,
  GalleryEntityCard,
  AddNewEntityCard,
  EntityDetailPanel,
  CampaignSelectorPopup,
  InviteCodePopup,
  PdfImportDialog,
} from '@features/gallery/components';
import {
  GENERATED_ENTITY_TYPES,
  CATEGORY_TO_ROUTE,
} from '@features/gallery/constants/categories';
import { TERMINAL_LOG_STANDARD } from '@core/config/constants';
import { logger } from '@core/utils';
import type {
  LoreEntity,
  EntityCategory,
  CreateLoreEntityInput,
  VisibilityLevel,
} from '@core/types';

/** Game system id that unlocks solar-system Labs category */
const SOLAR_SYSTEM_GAME_SYSTEM_ID = '376520e8-ff30-44d0-b048-ba07b2fdd12b';

export const GalleryPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout: _logout } = useAuth();
  const {
    campaigns,
    activeCampaign,
    activeCampaignId,
    selectCampaign,
    isActiveCampaignMaster,
    isLoading: isCampaignLoading,
  } = useCampaign();

  // ── UI visibility state ─────────────────────────────────────────────────────
  const [selectedEntity, setSelectedEntity] = useState<LoreEntity | null>(null);
  const [showInvite, setShowInvite] = useState(false);
  const [showCampaignSelector, setShowCampaignSelector] = useState(false);
  const [showImportDialog, setShowImportDialog] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showViewModal, setShowViewModal] = useState(false);
  const [showMobileModal, setShowMobileModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [announcement, setAnnouncement] = useState<string>('');

  // ── DOM refs ────────────────────────────────────────────────────────────────
  const categoryNavRef = useRef<HTMLElement>(null);
  const entityGridRef = useRef<HTMLDivElement>(null);
  const detailPanelRef = useRef<HTMLElement>(null);
  const campaignSelectorRef = useRef<HTMLDivElement>(null);
  const inviteDialogRef = useRef<HTMLDivElement>(null);
  const importDialogRef = useRef<HTMLDivElement>(null);
  const importFileInputRef = useRef<HTMLInputElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const isMaster = user?.role === 'MASTER' || isActiveCampaignMaster;

  // ── Hooks ───────────────────────────────────────────────────────────────────
  const { state: pdfImportState, importFromPdf, exportToPdf, clearError: clearPdfError } =
    useCharacterSheetPdf();

  const { isOpen: isConfirmOpen, config: confirmConfig, confirm, handleConfirm, handleCancel } =
    useConfirmDialog();

  const { entities: _entities, isLoading: isLoadingEntities, refresh: refreshEntities, getByCategory } =
    useGalleryEntities({ campaignId: activeCampaignId });

  const { activeCategory, displayCategory, transitionStatus, setCategory: setCategoryWithTransition, getCategoryLabel } =
    useGalleryCategories('character');

  const { logs, addLog } = useTerminalLog({
    maxLogs: TERMINAL_LOG_STANDARD,
    initialLogs: [
      '> INSPECTOR_ENTIDAD initialized...',
      '> Awaiting entity selection...',
    ],
  });

  const { deleteEntity, updateVisibility, isDeleting } = useEntityActions({
    campaignId: activeCampaignId ?? '',
    onSuccess: () => {
      refreshEntities();
      setSelectedEntity(null);
    },
  });

  /** Confirmed templates for the active campaign's game system */
  const { confirmedTemplates: _confirmedTemplates, isCategoryAvailable } = useConfirmedTemplates(
    activeCampaign?.gameSystemId
  );

  /** Field definitions for the selected entity */
  const { fieldDefinitions: selectedEntityFieldDefs } = useEntityFieldDefinitions(
    activeCampaign?.gameSystemId,
    selectedEntity?.entityType
  );

  // ── Derived values ──────────────────────────────────────────────────────────

  /** Labs categories filtered by active game system */
  const filteredLabsCategories = useMemo(() => {
    const gameSystemId = activeCampaign?.gameSystemId;
    return GENERATED_ENTITY_TYPES.filter(cat => {
      if (cat.id === 'solar_system') {
        return gameSystemId === SOLAR_SYSTEM_GAME_SYSTEM_ID;
      }
      return true;
    });
  }, [activeCampaign?.gameSystemId]);

  /** Entities visible in the current category filtered by search term */
  const filteredEntities = useMemo(() => {
    return getByCategory(displayCategory).filter(e => {
      if (!searchTerm.trim()) return true;
      const q = searchTerm.toLowerCase().trim();
      return (
        e.name.toLowerCase().includes(q) ||
        (e.description?.toLowerCase().includes(q) ?? false)
      );
    });
  }, [getByCategory, displayCategory, searchTerm]);

  const isLoading = isCampaignLoading || isLoadingEntities;

  // ── Accessibility helpers ───────────────────────────────────────────────────

  /** Post an announcement to the live region for screen readers */
  const announce = useCallback((message: string) => {
    setAnnouncement(message);
    setTimeout(() => setAnnouncement(''), 1000);
  }, []);

  // ── Guards ──────────────────────────────────────────────────────────────────

  /** Redirect out of solar_system if the game system doesn't support it */
  useEffect(() => {
    if (
      activeCategory === 'solar_system' &&
      activeCampaign?.gameSystemId !== SOLAR_SYSTEM_GAME_SYSTEM_ID
    ) {
      setCategoryWithTransition('character');
    }
  }, [activeCampaign?.gameSystemId, activeCategory, setCategoryWithTransition]);

  // ── Keyboard handlers ───────────────────────────────────────────────────────

  /** Arrow-key navigation inside the category sidebar */
  const handleCategoryKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    const total = Object.keys(CATEGORY_TO_ROUTE).length;
    let newIndex = index;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        newIndex = (index + 1) % total;
        break;
      case 'ArrowUp':
        event.preventDefault();
        newIndex = index === 0 ? total - 1 : index - 1;
        break;
      case 'Home':
        event.preventDefault();
        newIndex = 0;
        break;
      case 'End':
        event.preventDefault();
        newIndex = total - 1;
        break;
      default:
        return;
    }

    const navElement = categoryNavRef.current;
    if (navElement) {
      const buttons = navElement.querySelectorAll<HTMLButtonElement>('button[role="tab"]');
      buttons[newIndex]?.focus();
    }
  };

  /** Arrow-key navigation inside the entity grid */
  const handleEntityGridKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (!entityGridRef.current) return;

    const cards = entityGridRef.current.querySelectorAll<HTMLDivElement>('[role="gridcell"]');
    const currentIndex = Array.from(cards).findIndex(
      card => card === document.activeElement || card.contains(document.activeElement)
    );

    if (currentIndex === -1) return;

    const columnsPerRow = 3;
    let newIndex = currentIndex;

    switch (event.key) {
      case 'ArrowRight':
        event.preventDefault();
        newIndex = Math.min(currentIndex + 1, cards.length - 1);
        break;
      case 'ArrowLeft':
        event.preventDefault();
        newIndex = Math.max(currentIndex - 1, 0);
        break;
      case 'ArrowDown':
        event.preventDefault();
        newIndex = Math.min(currentIndex + columnsPerRow, cards.length - 1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        newIndex = Math.max(currentIndex - columnsPerRow, 0);
        break;
      case 'Home':
        event.preventDefault();
        newIndex = 0;
        break;
      case 'End':
        event.preventDefault();
        newIndex = cards.length - 1;
        break;
      case 'Enter':
      case ' ':
        event.preventDefault();
        (cards[currentIndex] as HTMLElement)?.click();
        return;
      default:
        return;
    }

    cards[newIndex]?.focus();
  };

  /** Global Escape handler to close open dialogs */
  useEffect(() => {
    const handleEscape = (event: globalThis.KeyboardEvent) => {
      if (event.key !== 'Escape') return;
      if (showCampaignSelector) {
        setShowCampaignSelector(false);
        announce('Selector de campaña cerrado');
      } else if (showInvite) {
        setShowInvite(false);
        announce('Diálogo de invitación cerrado');
      } else if (showImportDialog) {
        setShowImportDialog(false);
        announce('Diálogo de importación cerrado');
      } else if (selectedEntity) {
        handleDetailClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [showCampaignSelector, showInvite, showImportDialog, selectedEntity]);

  // ── Event handlers ──────────────────────────────────────────────────────────

  const handleCategoryChange = (newCat: EntityCategory) => {
    if (!isCategoryAvailable(newCat)) {
      addLog(
        `WARNING: No hay plantilla confirmada para ${getCategoryLabel(newCat).toUpperCase()}. Contacta al Master.`
      );
      return;
    }
    if (newCat === activeCategory || transitionStatus !== 'idle') return;

    const categoryLabel = getCategoryLabel(newCat);
    announce(`Cambiando a categoría ${categoryLabel}`);
    setCategoryWithTransition(newCat);
    setSelectedEntity(null);

    setTimeout(() => {
      const count = getByCategory(newCat).length;
      announce(
        `Categoría ${categoryLabel} seleccionada. ${count} ${count === 1 ? 'entidad encontrada' : 'entidades encontradas'}.`
      );
    }, 900);
  };

  const handleEntitySelect = (entity: LoreEntity) => {
    setSelectedEntity(entity);
    announce(`Entidad seleccionada: ${entity.name}`);

    if (window.innerWidth < 1024) {
      setShowMobileModal(true);
    } else {
      setTimeout(() => detailPanelRef.current?.focus(), 100);
    }
  };

  const handleDetailClose = () => {
    setSelectedEntity(null);
    announce('Panel de detalles cerrado');
    entityGridRef.current?.focus();
  };

  const handleDelete = async (id: string) => {
    if (!activeCampaignId) return;

    const confirmed = await confirm({
      title: 'Confirmar purga de datos',
      message: 'Esta acción es irreversible. ¿Deseas eliminar esta entidad?',
      confirmLabel: 'PURGAR',
      cancelLabel: 'CANCELAR',
      variant: 'danger',
    });

    if (confirmed) {
      const success = await deleteEntity(id);
      announce(success ? 'Entidad eliminada correctamente' : 'Error al eliminar entidad');
    }
  };

  const handleVisibilityChange = async (entityId: string, newVisibility: VisibilityLevel) => {
    if (!activeCampaignId) return;

    const success = await updateVisibility(entityId, newVisibility);
    if (success) {
      announce(`Visibilidad cambiada`);
      refreshEntities();
    } else {
      announce('Error al cambiar visibilidad');
    }
  };

  const handleEditEntity = () => {
    if (selectedEntity && (isMaster || user?.id === selectedEntity.ownerId)) {
      setShowEditModal(true);
      announce(`Editando entidad: ${selectedEntity.name}`);
    }
  };

  const handleSaveEdit = async (updatedEntity: LoreEntity) => {
    const ownershipChanged = updatedEntity.ownerId !== selectedEntity?.ownerId;
    setShowEditModal(false);
    await refreshEntities();

    if (ownershipChanged) {
      setSelectedEntity(null);
      announce(`Propiedad de "${updatedEntity.name}" transferida`);
    } else {
      setSelectedEntity(updatedEntity);
      announce(`Entidad actualizada: ${updatedEntity.name}`);
    }
  };

  const handleExportPdf = async () => {
    if (!selectedEntity) return;
    try {
      await exportToPdf(selectedEntity, { fieldDefinitions: selectedEntityFieldDefs });
    } catch (error) {
      logger.error('[GalleryPage] PDF export failed:', error);
    }
  };

  const handleAddNew = () => {
    const route = CATEGORY_TO_ROUTE[displayCategory];
    if (route) navigate(route);
  };

  const handleManageCampaigns = () => navigate('/campaigns/new');

  // Campaign selector popup handlers
  const handleCampaignSelectorToggle = () => {
    const next = !showCampaignSelector;
    setShowCampaignSelector(next);
    if (next) {
      announce('Selector de campaña abierto');
      setTimeout(() => campaignSelectorRef.current?.focus(), 100);
    } else {
      announce('Selector de campaña cerrado');
    }
  };

  // Invite popup handlers
  const handleInviteToggle = () => {
    const next = !showInvite;
    setShowInvite(next);
    if (next) {
      announce(`Código de invitación: ${activeCampaign?.joinCode}`);
      setTimeout(() => inviteDialogRef.current?.focus(), 100);
    } else {
      announce('Diálogo de invitación cerrado');
    }
  };

  // PDF import dialog handlers
  const handleImportDialogToggle = () => {
    const next = !showImportDialog;
    setShowImportDialog(next);
    clearPdfError();
    if (next) {
      announce('Diálogo de importación abierto');
      setTimeout(() => importDialogRef.current?.focus(), 100);
    }
  };

  const handleImportClick = () => importFileInputRef.current?.click();

  const handleImportFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !activeCampaignId) return;

    try {
      const result = await importFromPdf(file);

      if (result) {
        const entityInput: CreateLoreEntityInput = {
          entityType: result.entityType as EntityCategory,
          name: result.name,
          description: result.description,
          attributes: result.attributes,
          metadata: result.metadata,
        };

        const createdEntity = await entityService.create(activeCampaignId, entityInput);
        await refreshEntities();
        setShowImportDialog(false);
        announce(`Entidad "${createdEntity.name}" importada correctamente`);

        if (createdEntity.entityType !== displayCategory) {
          handleCategoryChange(createdEntity.entityType as EntityCategory);
        }
      }
    } catch (error) {
      logger.error('[GalleryPage] Import failed:', error);
      announce('Error al importar PDF');
    }

    event.target.value = '';
  };

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <TerminalLayout
      title="GALERÍA"
      subtitle="Archivo de Entidades"
      icon="grid_view"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex h-full gap-3 md:gap-6 overflow-hidden relative font-mono">
        {/* Live region for screen reader announcements */}
        <div
          role="status"
          aria-live="polite"
          aria-atomic="true"
          className="sr-only"
        >
          {announcement}
        </div>

        {/* Campaign selector floating dialog */}
        {showCampaignSelector && (
          <CampaignSelectorPopup
            ref={campaignSelectorRef}
            campaigns={campaigns}
            activeCampaign={activeCampaign}
            isMaster={isMaster}
            onSelectCampaign={selectCampaign}
            onManageCampaigns={handleManageCampaigns}
            onClose={() => setShowCampaignSelector(false)}
          />
        )}

        {/* Invite code floating dialog */}
        {showInvite && isMaster && activeCampaign?.joinCode && (
          <InviteCodePopup
            ref={inviteDialogRef}
            joinCode={activeCampaign.joinCode}
            onClose={() => setShowInvite(false)}
          />
        )}

        {/* PDF import floating dialog */}
        {showImportDialog && isMaster && activeCampaignId && (
          <PdfImportDialog
            ref={importDialogRef}
            fileInputRef={importFileInputRef}
            isImporting={pdfImportState.isImporting}
            error={pdfImportState.error ?? null}
            onImportClick={handleImportClick}
            onFileChange={handleImportFileChange}
            onClose={() => setShowImportDialog(false)}
          />
        )}

        {/* Left sidebar */}
        <GallerySidebar
          categoryNavRef={categoryNavRef}
          activeCategory={activeCategory}
          transitionStatus={transitionStatus}
          activeCampaignId={activeCampaignId}
          filteredLabsCategories={filteredLabsCategories}
          isCategoryAvailable={isCategoryAvailable}
          onCategoryChange={handleCategoryChange}
          onCategoryKeyDown={handleCategoryKeyDown}
          userRole={user?.role}
          activeCampaignShortId={activeCampaign?.id.slice(0, 8)}
        />

        {/* Main content */}
        <main className="flex-1 overflow-y-auto pr-2 relative custom-scrollbar">
          {/* No campaign selected */}
          {!activeCampaignId && !isLoading && (
            <div className="flex flex-col items-center justify-center h-64 text-primary font-mono">
              <span className="material-icons text-6xl mb-4 opacity-30">public_off</span>
              <p className="text-sm uppercase tracking-widest mb-4">Sin campaña seleccionada</p>
              <button
                onClick={handleManageCampaigns}
                className="border border-primary px-4 py-2 text-[10px] uppercase hover:bg-primary hover:text-black transition-colors"
              >
                CREAR O UNIRSE A CAMPAÑA
              </button>
            </div>
          )}

          {/* Loading */}
          {isLoading && (
            <div className="flex flex-col items-center justify-center h-64 text-primary animate-pulse font-mono uppercase tracking-[0.3em]">
              <span className="material-icons text-4xl mb-4">settings_input_antenna</span>
              Recuperando registros...
            </div>
          )}

          {/* Entity grid */}
          {!isLoading && activeCampaignId && (
            <>
              {/* Search bar */}
              <div className="mb-4 flex gap-2">
                <div className="relative flex-1">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 material-icons text-primary/40 text-sm">
                    search
                  </span>
                  <input
                    ref={searchInputRef}
                    type="text"
                    value={searchTerm}
                    onChange={e => setSearchTerm(e.target.value)}
                    placeholder="Buscar entidades..."
                    aria-label="Buscar entidades por nombre o descripción"
                    className="w-full bg-black/50 border border-primary/30 pl-10 pr-10 py-2 text-sm text-primary placeholder-primary/30 focus:border-primary focus:outline-none font-mono"
                  />
                  {searchTerm && (
                    <button
                      onClick={() => {
                        setSearchTerm('');
                        searchInputRef.current?.focus();
                        announce('Búsqueda limpiada');
                      }}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-primary/40 hover:text-primary transition-colors"
                      aria-label="Limpiar búsqueda"
                    >
                      <span className="material-icons text-sm">close</span>
                    </button>
                  )}
                </div>
                <div className="text-xs text-primary/40 self-center whitespace-nowrap hidden sm:block">
                  {filteredEntities.length}{' '}
                  {filteredEntities.length === 1 ? 'resultado' : 'resultados'}
                </div>
              </div>

              {/* Grid */}
              <div
                ref={entityGridRef}
                id="entity-grid"
                role="grid"
                aria-label={`Entidades de tipo ${
                  [...Object.values(CATEGORY_TO_ROUTE)].length > 0
                    ? displayCategory
                    : displayCategory
                }`}
                tabIndex={0}
                onKeyDown={handleEntityGridKeyDown}
                className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 xl:grid-cols-3 gap-6 pb-8 transition-all duration-300 ${
                  transitionStatus === 'out'
                    ? 'section-transition-out'
                    : transitionStatus === 'in'
                    ? 'section-transition-in'
                    : ''
                }`}
              >
                {filteredEntities.map((entity, idx) => (
                  <GalleryEntityCard
                    key={entity.id}
                    entity={entity}
                    selected={selectedEntity?.id === entity.id}
                    currentUserId={user?.id}
                    onClick={() => handleEntitySelect(entity)}
                    animationDelay={idx * 50}
                  />
                ))}

                {isMaster && transitionStatus === 'idle' && (
                  <AddNewEntityCard onClick={handleAddNew} />
                )}
              </div>
            </>
          )}

          {/* Scanline transition overlay */}
          {transitionStatus !== 'idle' && (
            <div className="absolute inset-0 z-50 pointer-events-none flex items-center justify-center">
              <div className="w-full h-1 bg-primary/20 animate-[scan_0.5s_linear_infinite] shadow-[0_0_20px_#25f46a]" />
            </div>
          )}
        </main>

        {/* Right panel: Detail inspector + Terminal log */}
        <div className="hidden lg:flex flex-col w-80 shrink-0 h-full">
          <EntityDetailPanel
            ref={detailPanelRef}
            entity={selectedEntity}
            isMaster={isMaster}
            currentUserId={user?.id}
            fieldDefinitions={selectedEntityFieldDefs}
            onClose={handleDetailClose}
            onDelete={handleDelete}
            onVisibilityChange={handleVisibilityChange}
            onEdit={handleEditEntity}
            onView={() => setShowViewModal(true)}
          />
          <TerminalLog
            logs={logs}
            maxLogs={TERMINAL_LOG_STANDARD}
            className="h-24 border border-primary/20 border-t-0 bg-black/60 shrink-0"
          />
        </div>
      </div>

      {/* Entity Edit Modal */}
      {showEditModal && selectedEntity && (
        <EntityEditModal
          entity={selectedEntity}
          canEditVisibility={
            user?.id === selectedEntity.ownerId || isActiveCampaignMaster
          }
          canEditOwnership={
            user?.id === selectedEntity.ownerId || isActiveCampaignMaster
          }
          currentUserId={user?.id}
          fieldDefinitions={selectedEntityFieldDefs}
          onClose={() => setShowEditModal(false)}
          onSave={handleSaveEdit}
        />
      )}

      {/* Entity View Modal (read-only) */}
      {showViewModal && selectedEntity && (
        <EntityViewModal
          entity={selectedEntity}
          fieldDefinitions={selectedEntityFieldDefs}
          onClose={() => setShowViewModal(false)}
        />
      )}

      {/* Mobile entity detail modal */}
      {showMobileModal && selectedEntity && (
        <EntityDetailModal
          entity={selectedEntity}
          isMaster={isMaster}
          currentUserId={user?.id}
          fieldDefinitions={selectedEntityFieldDefs}
          isExportingPdf={pdfImportState.isExporting}
          onClose={() => setShowMobileModal(false)}
          onDelete={handleDelete}
          onVisibilityChange={handleVisibilityChange}
          onEdit={() => {
            handleEditEntity();
            setShowMobileModal(false);
          }}
          onView={() => {
            setShowViewModal(true);
            setShowMobileModal(false);
          }}
          onExportPdf={handleExportPdf}
        />
      )}

      {/* Confirm delete dialog */}
      <ConfirmDialog
        isOpen={isConfirmOpen}
        title={confirmConfig?.title ?? ''}
        message={confirmConfig?.message ?? ''}
        confirmLabel={confirmConfig?.confirmLabel}
        cancelLabel={confirmConfig?.cancelLabel}
        variant={confirmConfig?.variant}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
        isLoading={isDeleting}
      />
    </TerminalLayout>
  );
};

export default GalleryPage;
