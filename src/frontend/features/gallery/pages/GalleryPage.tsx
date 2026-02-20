/**
 * Gallery Page
 * Displays all entities within the active campaign with category filtering
 * Includes campaign selection and management
 * 
 * Accessibility Features:
 * - Full keyboard navigation (Tab, Enter, Space, Arrow keys)
 * - ARIA labels and roles for screen readers
 * - Focus management for modal dialogs
 * - Live region announcements for state changes
 */

import React, { useState, useEffect, useCallback, useRef, KeyboardEvent, forwardRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { EntityEditModal, EntityViewModal } from '@shared/components/modals';
import { useCampaign, useAuth } from '@core/context';
import { entityService, entityTemplateService } from '@core/services/api';
import { useCharacterSheetPdf } from '@core/hooks';
import type { LoreEntity, EntityCategory, CreateLoreEntityInput, EntityTemplateSummary, FieldDefinition } from '@core/types';
import { CampaignRole, VisibilityLevel } from '@core/types';

/**
 * Category info for sidebar navigation
 */
type CategoryInfo = {
  id: EntityCategory;
  label: string;
  icon: string;
};

/**
 * Entity categories available in the gallery
 * These map to backend entityType values (lowercase)
 */
const ENTITY_CATEGORIES: CategoryInfo[] = [  
  { id: 'character', label: 'PERSONAJES', icon: 'face' },
  { id: 'npc', label: 'ACTORES', icon: 'groups' },
  // { id: 'enemy', label: 'ENEMIGOS', icon: 'pest_control' },
  { id: 'vehicle', label: 'VEHÍCULOS', icon: 'rocket_launch' },
  { id: 'mission', label: 'MISIONES', icon: 'assignment' },
  { id: 'encounter', label: 'ENCUENTROS', icon: 'pest_control' },
  // { id: 'solar_system', label: 'SISTEMAS SOLARES', icon: 'public' },
];


const ENTITY_CATEGORIES_LABS: CategoryInfo[] = [  
  { id: 'solar_system', label: 'SISTEMAS SOLARES', icon: 'public' },
];

/**
 * Map entity category to the corresponding generator route
 */
const CATEGORY_TO_ROUTE: Record<EntityCategory, string> = {
  'solar_system': '/gallery/solar-gen',
  'character': '/gallery/char-gen',
  'npc': '/gallery/npc-gen',
  'enemy': '/gallery/enemy-gen',
  'vehicle': '/gallery/vehi-gen',
  'mission': '/gallery/mission-gen',
  'encounter': '/gallery/encounter-gen',
};

/**
 * Map template entityTypeName to the corresponding generator route.
 * Handles common variations and naming conventions.
 * @param entityTypeName - The entity type name from the template
 * @returns The corresponding route or null if not found
 */
const getRouteForTemplate = (entityTypeName: string): string | null => {
  const routeMap: Record<string, string> = {
    'character': '/gallery/char-gen',
    'npc': '/gallery/npc-gen',
    'enemy': '/gallery/enemy-gen',
    'vehicle': '/gallery/vehi-gen',
    'mission': '/gallery/mission-gen',
    'encounter': '/gallery/encounter-gen',
    'solar_system': '/gallery/solar-gen',
    'solarsystem': '/gallery/solar-gen',
    'solar-system': '/gallery/solar-gen',
  };
  return routeMap[entityTypeName.toLowerCase()] || null;
};

/**
 * Get a Material Icons icon name for a template based on its entityTypeName.
 * Uses iconHint from template if available, otherwise falls back to defaults.
 * @param template - The template summary
 * @returns Material icon name
 */
const getIconForTemplate = (template: EntityTemplateSummary): string => {
  // Use iconHint if available
  if (template.iconHint) {
    return template.iconHint;
  }
  
  // Fallback based on entityTypeName
  const icons: Record<string, string> = {
    'player_character': 'face',
    'character': 'face',
    'npc': 'groups',
    'non_player_character': 'groups',
    'actor': 'groups',
    'enemy': 'pest_control',
    'adversary': 'dangerous',
    'vehicle': 'rocket_launch',
    'starship': 'rocket',
    'spacecraft': 'flight',
    'solar_system': 'public',
    'star_system': 'public',
    'planet': 'language',
    'mission': 'assignment',
    'quest': 'explore',
    'encounter': 'pest_control',
    'combat': 'sports_martial_arts',
  };
  return icons[template.entityTypeName.toLowerCase()] || 'category';
};

export const GalleryPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { 
    campaigns, 
    activeCampaign, 
    activeCampaignId,
    selectCampaign, 
    isActiveCampaignMaster,
    isLoading: isCampaignLoading 
  } = useCampaign();

  const [entities, setEntities] = useState<LoreEntity[]>([]);
  const [selectedEntity, setSelectedEntity] = useState<LoreEntity | null>(null);
  const [activeCategory, setActiveCategory] = useState<EntityCategory>('character');
  const [displayCategory, setDisplayCategory] = useState<EntityCategory>('character');
  const [isLoadingEntities, setIsLoadingEntities] = useState(false);
  const [showInvite, setShowInvite] = useState(false);
  const [showCampaignSelector, setShowCampaignSelector] = useState(false);
  const [showImportDialog, setShowImportDialog] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showViewModal, setShowViewModal] = useState(false);
  const [transitionStatus, setTransitionStatus] = useState<'idle' | 'out' | 'in'>('idle');
  const [searchTerm, setSearchTerm] = useState<string>('');
  
  // Confirmed templates for the current campaign's game system (used for dynamic generator links)
  const [confirmedTemplates, setConfirmedTemplates] = useState<EntityTemplateSummary[]>([]);
  const [isLoadingTemplates, setIsLoadingTemplates] = useState(false);
  
  // Field definitions for the selected entity's type (used for display name mapping)
  const [selectedEntityFieldDefs, setSelectedEntityFieldDefs] = useState<FieldDefinition[]>([]);
  
  // Accessibility: Screen reader announcement state
  const [announcement, setAnnouncement] = useState<string>('');
  
  // Refs for focus management
  const categoryNavRef = useRef<HTMLElement>(null);
  const entityGridRef = useRef<HTMLDivElement>(null);
  const detailPanelRef = useRef<HTMLElement>(null);
  const campaignSelectorRef = useRef<HTMLDivElement>(null);
  const inviteDialogRef = useRef<HTMLDivElement>(null);
  const importDialogRef = useRef<HTMLDivElement>(null);
  const importFileInputRef = useRef<HTMLInputElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  // Determine if user is master (either by user role or campaign role)
  const isMaster = user?.role === 'MASTER' || isActiveCampaignMaster;
  
  // PDF import hook
  const { state: pdfImportState, importFromPdf, clearError: clearPdfError } = useCharacterSheetPdf();
  
  /**
   * Accessibility: Announce message to screen readers
   */
  const announce = useCallback((message: string) => {
    setAnnouncement(message);
    // Clear after announcement to allow repeated messages
    setTimeout(() => setAnnouncement(''), 1000);
  }, []);

  /**
   * Load entities for the active campaign
   */
  const loadEntities = useCallback(async () => {
    if (!user || !activeCampaignId) {
      setEntities([]);
      setIsLoadingEntities(false);
      return;
    }
    
    setIsLoadingEntities(true);
    try {
      const data = await entityService.getByCampaign(activeCampaignId);
      setEntities(data);
    } catch (error) {
      console.error('Failed to load entities:', error);
      setEntities([]);
    } finally {
      setIsLoadingEntities(false);
    }
  }, [user, activeCampaignId]);

  // Load entities when campaign changes
  useEffect(() => {
    loadEntities();
  }, [loadEntities]);

  /**
   * Load confirmed templates for the active campaign's game system.
   * These templates are displayed in the GENERADORES sidebar section for Masters.
   * Uses confirmedOnly=true to get all confirmed templates regardless of owner.
   */
  useEffect(() => {
    const loadTemplates = async () => {
      if (!activeCampaign?.gameSystemId) {
        setConfirmedTemplates([]);
        return;
      }
      
      setIsLoadingTemplates(true);
      try {
        // Use confirmedOnly=true to get ALL confirmed templates for this game system
        // (not filtered by owner, so any Master can see templates created by others)
        const result = await entityTemplateService.getByGameSystem(
          activeCampaign.gameSystemId,
          undefined,  // no status filter
          true        // confirmedOnly
        );
        setConfirmedTemplates(result.templates);
      } catch (error) {
        console.error('Failed to load templates:', error);
        setConfirmedTemplates([]);
      } finally {
        setIsLoadingTemplates(false);
      }
    };
    
    loadTemplates();
  }, [activeCampaign?.gameSystemId]);

  /**
   * Load field definitions when selected entity changes.
   * Used for display name mapping in edit modal and PDF export.
   */
  useEffect(() => {
    const loadFieldDefinitions = async () => {
      if (!selectedEntity || !activeCampaign?.gameSystemId) {
        setSelectedEntityFieldDefs([]);
        return;
      }
      
      try {
        const fields = await entityTemplateService.getFieldDefinitions(
          activeCampaign.gameSystemId,
          selectedEntity.entityType
        );
        setSelectedEntityFieldDefs(fields);
      } catch (error) {
        console.error('Failed to load field definitions:', error);
        setSelectedEntityFieldDefs([]);
      }
    };
    
    loadFieldDefinitions();
  }, [selectedEntity?.entityType, activeCampaign?.gameSystemId]);

  /**
   * Handle category tab change with transition effect
   */
  const handleCategoryChange = (newCat: EntityCategory) => {
    if (newCat === activeCategory || transitionStatus !== 'idle') return;
    
    setTransitionStatus('out');
    setActiveCategory(newCat);
    
    // Announce category change to screen readers
    const categoryLabel = ENTITY_CATEGORIES.find(c => c.id === newCat)?.label || ENTITY_CATEGORIES_LABS.find(c => c.id === newCat)?.label || newCat;
    announce(`Cambiando a categoría ${categoryLabel}`);
    
    setTimeout(() => {
      setDisplayCategory(newCat);
      setSelectedEntity(null);
      setTransitionStatus('in');
      
      setTimeout(() => {
        setTransitionStatus('idle');
        // Announce completion with entity count
        const count = entities.filter(e => e.entityType === newCat).length;
        announce(`Categoría ${categoryLabel} seleccionada. ${count} ${count === 1 ? 'entidad encontrada' : 'entidades encontradas'}.`);
      }, 500);
    }, 400);
  };

  /**
   * Accessibility: Handle keyboard navigation in category sidebar
   */
  const handleCategoryKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    const categories = ENTITY_CATEGORIES;
    let newIndex = index;
    
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        newIndex = (index + 1) % categories.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        newIndex = index === 0 ? categories.length - 1 : index - 1;
        break;
      case 'Home':
        event.preventDefault();
        newIndex = 0;
        break;
      case 'End':
        event.preventDefault();
        newIndex = categories.length - 1;
        break;
      default:
        return;
    }
    
    // Focus the new category button
    const navElement = categoryNavRef.current;
    if (navElement) {
      const buttons = navElement.querySelectorAll<HTMLButtonElement>('button[role="tab"]');
      buttons[newIndex]?.focus();
    }
  };

  /**
   * Filter entities by the current display category and search term
   */
  const filteredEntities = entities.filter(e => {
    // Filter by category
    if (e.entityType !== displayCategory) return false;
    // Filter by search term (case-insensitive)
    if (searchTerm.trim()) {
      const normalizedSearch = searchTerm.toLowerCase().trim();
      const matchesName = e.name.toLowerCase().includes(normalizedSearch);
      const matchesDescription = e.description?.toLowerCase().includes(normalizedSearch) ?? false;
      return matchesName || matchesDescription;
    }
    return true;
  });

  /**
   * Handle entity deletion
   */
  const handleDelete = async (id: string) => {
    if (!activeCampaignId) return;
    
    if (confirm('¿Confirmar purga de datos? Esta acción es irreversible.')) {
      try {
        await entityService.delete(activeCampaignId, id);
        setSelectedEntity(null);
        announce('Entidad eliminada correctamente');
        loadEntities();
      } catch (e: unknown) {
        const message = e instanceof Error ? e.message : 'Error al eliminar';
        announce(`Error: ${message}`);
        alert(message);
      }
    }
  };

  /**
   * Handle entity visibility change
   * Allows Masters to change an entity's visibility level
   */
  const handleVisibilityChange = async (entityId: string, newVisibility: VisibilityLevel) => {
    if (!activeCampaignId) return;
    
    try {
      const updatedEntity = await entityService.changeVisibility(
        activeCampaignId, 
        entityId, 
        { visibility: newVisibility }
      );
      
      // Update the selected entity with new visibility
      setSelectedEntity(updatedEntity);
      
      // Refresh entities list to reflect change
      loadEntities();
      
      const visibilityLabels: Record<VisibilityLevel, string> = {
        [VisibilityLevel.Draft]: 'BORRADOR',
        [VisibilityLevel.Private]: 'PRIVADO',
        [VisibilityLevel.Campaign]: 'CAMPAÑA',
        [VisibilityLevel.Public]: 'PÚBLICO'
      };
      announce(`Visibilidad cambiada a ${visibilityLabels[newVisibility]}`);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Error al cambiar visibilidad';
      announce(`Error: ${message}`);
      alert(message);
    }
  };

  /**
   * Handle opening the edit modal
   * Allowed for Masters or entity owners
   */
  const handleEditEntity = () => {
    if (selectedEntity && (isMaster || user?.id === selectedEntity.ownerId)) {
      setShowEditModal(true);
      announce(`Editando entidad: ${selectedEntity.name}`);
    }
  };

  /**
   * Handle saving edited entity
   * Clears selection if ownership was transferred to another user
   */
  const handleSaveEdit = async (updatedEntity: LoreEntity) => {
    // Check if ownership was transferred to another user
    const ownershipChanged = updatedEntity.ownerId !== selectedEntity?.ownerId;
    
    // Close modal first
    setShowEditModal(false);
    
    // Refresh entities list
    await loadEntities();
    
    // If ownership changed, clear selection (entity may no longer be accessible)
    // Otherwise, update the selected entity with fresh data
    if (ownershipChanged) {
      setSelectedEntity(null);
      announce(`Propiedad de "${updatedEntity.name}" transferida`);
    } else {
      setSelectedEntity(updatedEntity);
      announce(`Entidad actualizada: ${updatedEntity.name}`);
    }
  };

  /**
   * Handle entity selection with focus management
   */
  const handleEntitySelect = (entity: LoreEntity) => {
    setSelectedEntity(entity);
    announce(`Entidad seleccionada: ${entity.name}`);
    // Move focus to detail panel when entity is selected
    setTimeout(() => {
      detailPanelRef.current?.focus();
    }, 100);
  };

  /**
   * Handle closing detail panel with focus restoration
   */
  const handleDetailClose = () => {
    setSelectedEntity(null);
    announce('Panel de detalles cerrado');
    // Return focus to the entity grid
    entityGridRef.current?.focus();
  };

  /**
   * Accessibility: Handle keyboard navigation for entity grid
   */
  const handleEntityGridKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (!entityGridRef.current) return;
    
    const cards = entityGridRef.current.querySelectorAll<HTMLDivElement>('[role="gridcell"]');
    const currentIndex = Array.from(cards).findIndex(card => card === document.activeElement || card.contains(document.activeElement));
    
    if (currentIndex === -1) return;
    
    let newIndex = currentIndex;
    const columnsPerRow = 3; // Matches grid-cols-3 at xl breakpoint
    
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
  
  /**
   * Handle campaign selector toggle with focus management
   */
  const handleCampaignSelectorToggle = () => {
    const newState = !showCampaignSelector;
    setShowCampaignSelector(newState);
    
    if (newState) {
      announce('Selector de campaña abierto');
      setTimeout(() => {
        campaignSelectorRef.current?.focus();
      }, 100);
    } else {
      announce('Selector de campaña cerrado');
    }
  };
  
  /**
   * Handle invite dialog toggle with focus management
   */
  const handleInviteToggle = () => {
    const newState = !showInvite;
    setShowInvite(newState);
    
    if (newState) {
      announce(`Código de invitación: ${activeCampaign?.joinCode}`);
      setTimeout(() => {
        inviteDialogRef.current?.focus();
      }, 100);
    } else {
      announce('Diálogo de invitación cerrado');
    }
  };
  
  /**
   * Handle import dialog toggle with focus management
   */
  const handleImportDialogToggle = () => {
    const newState = !showImportDialog;
    setShowImportDialog(newState);
    clearPdfError();
    
    if (newState) {
      announce('Diálogo de importación abierto');
      setTimeout(() => {
        importDialogRef.current?.focus();
      }, 100);
    } else {
      announce('Diálogo de importación cerrado');
    }
  };
  
  /**
   * Handle import button click - opens file picker
   */
  const handleImportClick = () => {
    importFileInputRef.current?.click();
  };
  
  /**
   * Handle PDF file selection and import
   */
  const handleImportFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !activeCampaignId) return;
    
    try {
      const result = await importFromPdf(file);
      
      if (result) {
        // Create the entity from imported data
        const entityInput: CreateLoreEntityInput = {
          entityType: result.entityType as EntityCategory,
          name: result.name,
          description: result.description,
          attributes: result.attributes,
          metadata: result.metadata,
        };
        
        const createdEntity = await entityService.create(activeCampaignId, entityInput);
        
        // Refresh entity list and close dialog
        await loadEntities();
        setShowImportDialog(false);
        announce(`Entidad "${createdEntity.name}" importada correctamente`);
        
        // Navigate to the category of the imported entity
        if (createdEntity.entityType !== displayCategory) {
          handleCategoryChange(createdEntity.entityType);
        }
      }
    } catch (error) {
      console.error('Import failed:', error);
      announce('Error al importar PDF');
    }
    
    // Reset file input
    event.target.value = '';
  };
  
  /**
   * Handle Escape key to close dialogs
   */
  useEffect(() => {
    const handleEscape = (event: globalThis.KeyboardEvent) => {
      if (event.key === 'Escape') {
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
      }
    };
    
    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [showCampaignSelector, showInvite, showImportDialog, selectedEntity]);

  /**
   * Navigate to the appropriate generator for the current category
   */
  const handleAddNew = () => {
    const route = CATEGORY_TO_ROUTE[displayCategory];
    if (route) {
      navigate(route);
    }
  };

  /**
   * Navigate to campaign creation/joining page
   */
  const handleManageCampaigns = () => {
    navigate('/campaigns/new');
  };

  const isLoading = isCampaignLoading || isLoadingEntities;

  return (
    <TerminalLayout 
      title="GALERÍA" 
      subtitle="Archivo de Entidades"
      icon="grid_view"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
      // actions={
      //   <div className="flex items-center gap-2">
      //     {/* Import PDF Button (Master only, requires campaign) */}
      //     {/* {isMaster && activeCampaignId && (
      //       <button 
      //         onClick={handleImportDialogToggle}
      //         aria-expanded={showImportDialog}
      //         aria-haspopup="dialog"
      //         aria-label="Importar ficha desde PDF"
      //         className="border border-cyan-500/60 px-3 py-1 text-[10px] uppercase hover:bg-cyan-500/10 hover:border-cyan-500 transition-colors text-cyan-500 font-bold flex items-center gap-1 focus:outline-none focus:ring-2 focus:ring-cyan-500"
      //       >
      //         <span className="material-icons text-sm">upload_file</span> 
      //         <span className="hidden sm:inline">IMPORTAR</span>
      //       </button>
      //     )} */}

      //     {/* Invite Button (Master only) */}
      //     {/* {isMaster && activeCampaign?.joinCode && (
      //       <button 
      //         onClick={handleInviteToggle}
      //         aria-expanded={showInvite}
      //         aria-haspopup="dialog"
      //         aria-label="Mostrar código de invitación"
      //         className="border border-primary px-3 py-1 text-[10px] uppercase hover:bg-primary hover:text-black transition-colors text-primary font-bold flex items-center gap-1 focus:outline-none focus:ring-2 focus:ring-primary"
      //       >
      //         <span className="material-icons text-sm">share</span> 
      //         <span className="hidden sm:inline">INVITAR</span>
      //       </button>
      //     )} */}
      //   </div>
      // }
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

        {/* Campaign Selector Popup */}
        {showCampaignSelector && (
          <div 
            ref={campaignSelectorRef}
            role="dialog"
            aria-label="Selector de campaña"
            aria-modal="true"
            tabIndex={-1}
            className="absolute top-0 left-16 md:left-64 z-50 bg-surface-dark border border-primary p-4 shadow-2xl animate-glitch-in font-mono min-w-[250px]"
          >
            <h4 className="text-primary text-[10px] uppercase font-bold mb-3 flex items-center gap-2">
              <span className="material-icons text-sm">public</span>
              Seleccionar Campaña
            </h4>
            
            {campaigns.length === 0 ? (
              <div className="text-center py-4">
                <p className="text-[10px] text-primary/60 mb-3">No perteneces a ninguna campaña</p>
                <button 
                  onClick={handleManageCampaigns}
                  className="w-full bg-primary/20 border border-primary py-2 text-[10px] text-primary uppercase font-bold hover:bg-primary hover:text-black transition-colors"
                >
                  CREAR/UNIRSE
                </button>
              </div>
            ) : (
              <>
                <div className="space-y-2 max-h-48 overflow-y-auto mb-3">
                  {campaigns.map(campaign => (
                    <button
                      key={campaign.id}
                      onClick={() => selectCampaign(campaign.id)}
                      className={`w-full text-left p-2 border transition-all ${
                        activeCampaign?.id === campaign.id
                          ? 'border-primary bg-primary/20 text-primary'
                          : 'border-primary/30 hover:border-primary text-white/80 hover:text-primary'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <span className="text-[11px] font-bold uppercase truncate">{campaign.name}</span>
                        <span className={`text-[8px] px-1 border ${
                          campaign.userRole === CampaignRole.Master 
                            ? 'border-primary text-primary' 
                            : 'border-yellow-500 text-yellow-500'
                        }`}>
                          {campaign.userRole === CampaignRole.Master ? 'MASTER' : 'PLAYER'}
                        </span>
                      </div>
                    </button>
                  ))}
                </div>
                
                {/* Campaign Settings Button (Master only) */}
                {isMaster && activeCampaign && (
                  <button 
                    onClick={() => {
                      setShowCampaignSelector(false);
                      navigate(`/campaigns/${activeCampaign.id}`);
                    }}
                    className="w-full bg-cyan-500/10 border border-cyan-500/50 py-2 text-[10px] text-cyan-500 uppercase font-bold hover:bg-cyan-500/30 transition-colors mb-2 flex items-center justify-center gap-2"
                  >
                    <span className="material-icons text-xs">settings</span>
                    CONFIGURAR CAMPAÑA
                  </button>
                )}
                
                {/* Game Systems Button (Master only) */}
                {isMaster && (
                  <button 
                    onClick={() => {
                      setShowCampaignSelector(false);
                      navigate('/game-systems');
                    }}
                    className="w-full bg-purple-500/10 border border-purple-500/50 py-2 text-[10px] text-purple-400 uppercase font-bold hover:bg-purple-500/30 transition-colors mb-2 flex items-center justify-center gap-2"
                  >
                    <span className="material-icons text-xs">sports_esports</span>
                    SISTEMAS DE JUEGO
                  </button>
                )}
                
                <button 
                  onClick={handleManageCampaigns}
                  className="w-full bg-primary/10 border border-primary/50 py-2 text-[10px] text-primary/80 uppercase font-bold hover:bg-primary hover:text-black transition-colors"
                >
                  + NUEVA CAMPAÑA
                </button>
              </>
            )}
            
            <button 
              onClick={() => setShowCampaignSelector(false)} 
              className="absolute top-2 right-2 text-primary/60 hover:text-primary"
            >
              <span className="material-icons text-sm">close</span>
            </button>
          </div>
        )}

        {/* Invitation Popup for Masters */}
        {showInvite && isMaster && activeCampaign?.joinCode && (
          <div 
            ref={inviteDialogRef}
            role="dialog"
            aria-label="Código de invitación"
            aria-modal="true"
            tabIndex={-1}
            className="absolute top-0 right-0 z-50 bg-surface-dark border border-primary p-4 shadow-2xl animate-glitch-in font-mono"
          >
            <h4 className="text-primary text-[10px] uppercase font-bold mb-2">Enlace de Reclutamiento</h4>
            <div className="bg-black/60 p-2 border border-primary/20 text-xs text-white mb-4">
              <p className="text-[8px] text-primary/60 mb-1 tracking-widest">CÓDIGO_ACTIVO</p>
              <span className="font-bold text-primary text-lg tracking-[0.3em]">{activeCampaign.joinCode}</span>
            </div>
            <p className="text-[9px] text-primary/40 leading-tight mb-4 max-w-[150px]">
              Comparte este código con nuevos operativos para asociarlos a tu campaña.
            </p>
            <button onClick={() => setShowInvite(false)} className="w-full bg-primary/20 border border-primary py-1 text-[10px] text-primary uppercase font-bold">CERRAR</button>
          </div>
        )}

        {/* PDF Import Dialog */}
        {showImportDialog && isMaster && activeCampaignId && (
          <div 
            ref={importDialogRef}
            role="dialog"
            aria-label="Importar ficha desde PDF"
            aria-modal="true"
            tabIndex={-1}
            className="absolute top-0 right-0 z-50 bg-surface-dark border border-cyan-500 p-4 shadow-2xl animate-glitch-in font-mono min-w-[280px]"
          >
            <h4 className="text-cyan-500 text-[10px] uppercase font-bold mb-2 flex items-center gap-2">
              <span className="material-icons text-sm">upload_file</span>
              Importar Ficha PDF
            </h4>
            
            <p className="text-[9px] text-cyan-500/60 leading-tight mb-4">
              Selecciona un archivo PDF exportado desde Hefestai para importar los datos de la entidad a esta campaña.
            </p>
            
            {/* Error display */}
            {pdfImportState.error && (
              <div className="bg-danger/20 border border-danger/50 p-2 mb-4 text-[10px] text-danger flex items-center gap-2">
                <span className="material-icons text-sm">error</span>
                {pdfImportState.error}
              </div>
            )}
            
            {/* Import button */}
            <button 
              onClick={handleImportClick}
              disabled={pdfImportState.isImporting}
              className="w-full bg-cyan-500/10 border border-cyan-500/50 py-3 text-[10px] text-cyan-500 uppercase font-bold hover:bg-cyan-500/20 transition-colors mb-3 flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {pdfImportState.isImporting ? (
                <>
                  <span className="material-icons text-sm animate-spin">sync</span>
                  PROCESANDO...
                </>
              ) : (
                <>
                  <span className="material-icons text-sm">folder_open</span>
                  SELECCIONAR ARCHIVO
                </>
              )}
            </button>
            
            {/* Hidden file input */}
            <input
              ref={importFileInputRef}
              type="file"
              accept=".pdf"
              onChange={handleImportFileChange}
              className="hidden"
              aria-hidden="true"
            />
            
            <div className="border-t border-cyan-500/20 pt-3">
              <button 
                onClick={() => setShowImportDialog(false)} 
                className="w-full bg-transparent border border-cyan-500/30 py-1 text-[10px] text-cyan-500/60 uppercase font-bold hover:border-cyan-500/50 hover:text-cyan-500 transition-colors"
              >
                CANCELAR
              </button>
            </div>
            
            <button 
              onClick={() => setShowImportDialog(false)} 
              className="absolute top-2 right-2 text-cyan-500/60 hover:text-cyan-500"
              aria-label="Cerrar diálogo de importación"
            >
              <span className="material-icons text-sm">close</span>
            </button>
          </div>
        )}

        {/* Sidebar Navigation */}
        <aside className="w-16 md:w-64 flex flex-col gap-4 shrink-0">
          {/* Entity Categories - Hidden for Admin users */}
          {user?.role !== 'ADMIN' && (
            <nav 
              ref={categoryNavRef}
              role="tablist"
              aria-label="Categorías de entidades"
              className="flex flex-col gap-2"
            >
              <div className="p-1 border border-primary/50 text-[10px] text-primary text-center uppercase mb-0 bg-primary/5 font-bold tracking-[0.2em]">
                <span className="hidden md:inline">:: GENERADORES ::</span>
                <span className="md:hidden xs:inline">:: GEN.AI ::</span>
              </div>
              {ENTITY_CATEGORIES.map((cat, index) => (
                <button
                  key={cat.id}
                  role="tab"
                  aria-selected={activeCategory === cat.id}
                  aria-controls="entity-grid"
                  tabIndex={activeCategory === cat.id ? 0 : -1}
                  onClick={() => handleCategoryChange(cat.id)}
                  onKeyDown={(e) => handleCategoryKeyDown(e, index)}
                  disabled={transitionStatus !== 'idle' || !activeCampaignId}
                  className={`group flex items-center gap-3 p-3 border transition-all clip-tech-tl relative overflow-hidden ${
                    activeCategory === cat.id 
                      ? 'border-l-4 border-l-primary border-y-primary/30 border-r-primary/30 bg-primary/20 shadow-[inset_0_0_15px_rgba(37,244,106,0.1)]' 
                      : 'border-primary/30 hover:border-primary hover:bg-primary/5 bg-surface-dark disabled:opacity-50'
                  }`}
                >
                  {activeCategory === cat.id && (
                    <div className="absolute inset-0 bg-primary/5 animate-pulse pointer-events-none"></div>
                  )}
                  <span className={`material-icons text-xl ${activeCategory === cat.id ? 'text-primary' : 'text-primary/60'}`}>{cat.icon}</span>
                  <span className={`hidden md:inline text-xs font-bold tracking-widest ${activeCategory === cat.id ? 'text-primary text-glow' : 'text-primary/70'}`}>
                    {cat.label}
                  </span>
                  {activeCategory === cat.id && (
                    <div className="absolute right-2 top-1/2 -translate-y-1/2 w-1 h-1 bg-primary rounded-full animate-ping"></div>
                  )}
                </button>
              ))}
              
              <div className="p-1 border border-cyan-500/50 text-[10px] text-cyan-500 text-center uppercase mt-2 mb-0 bg-cyan-500/5 font-bold tracking-[0.2em]">
                <span className="hidden md:inline">:: Experimental ::</span>
                <span className="md:hidden xs:inline">:: LABS.AI ::</span>
              </div>
              {ENTITY_CATEGORIES_LABS.map((cat, index) => (
                <button
                  key={cat.id}
                  role="tab"
                  aria-selected={activeCategory === cat.id}
                  aria-controls="entity-grid"
                  tabIndex={activeCategory === cat.id ? 0 : -1}
                  onClick={() => handleCategoryChange(cat.id)}
                  onKeyDown={(e) => handleCategoryKeyDown(e, index)}
                  disabled={transitionStatus !== 'idle' || !activeCampaignId}
                  className={`group flex items-center gap-3 p-3 border transition-all clip-tech-tl relative overflow-hidden ${
                    activeCategory === cat.id 
                      ? 'border-l-4 border-l-cyan-500 border-y-cyan-500/30 border-r-cyan-500/30 bg-cyan-500/20 shadow-[inset_0_0_15px_rgba(37,244,106,0.1)]' 
                      : 'border-cyan-500/30 hover:border-cyan-500 hover:bg-cyan-500/5 bg-surface-dark disabled:opacity-50'
                  }`}
                >
                  {activeCategory === cat.id && (
                    <div className="absolute inset-0 bg-cyan-500/5 animate-pulse pointer-events-none"></div>
                  )}
                  <span className={`material-icons text-xl ${activeCategory === cat.id ? 'text-cyan-500' : 'text-cyan-500/60'}`}>{cat.icon}</span>
                  <span className={`hidden md:inline text-xs font-bold tracking-widest ${activeCategory === cat.id ? 'text-cyan-500 text-glow' : 'text-cyan-500/70'}`}>
                    {cat.label}
                  </span>
                  {activeCategory === cat.id && (
                    <div className="absolute right-2 top-1/2 -translate-y-1/2 w-1 h-1 bg-cyan-500 rounded-full animate-ping"></div>
                  )}
                </button>
              ))}
            </nav>
          )}

          {/* Generators Section - Master users only, when templates are available */}
          {/* {isMaster && user?.role !== 'ADMIN' && confirmedTemplates.length > 0 && (
            <nav 
              aria-label="Generadores de entidades"
              className="flex flex-col gap-2"
            >
              <div className="p-1 border border-cyan-500/50 text-[10px] text-cyan-400 text-center uppercase mb-2 bg-cyan-500/5 font-bold tracking-[0.2em]">
                <span className="hidden md:inline">:: GENERADORES ::</span>
                <span className="md:hidden xs:inline">:: GEN.AI ::</span>
              </div>
              {isLoadingTemplates ? (
                <div className="flex items-center justify-center p-4 text-cyan-500/60">
                  <span className="material-icons text-sm animate-spin mr-2">sync</span>
                  <span className="text-[10px] uppercase tracking-widest">Cargando...</span>
                </div>
              ) : (
                confirmedTemplates.map((template) => {
                  const route = getRouteForTemplate(template.entityTypeName);
                  const icon = getIconForTemplate(template);
                  
                  // Skip templates that don't have a corresponding generator route
                  if (!route) return null;
                  
                  return (
                    <button
                      key={template.id}
                      onClick={() => navigate(route)}
                      disabled={!activeCampaignId}
                      className="group flex items-center gap-3 p-3 border border-cyan-500/30 hover:border-cyan-500 hover:bg-cyan-500/10 bg-surface-dark transition-all disabled:opacity-50"
                      title={`Generar ${template.displayName}`}
                    >
                      <span className="material-icons text-xl text-cyan-500/60 group-hover:text-cyan-400">
                        {icon}
                      </span>
                      <span className="hidden md:inline text-xs font-bold tracking-widest text-cyan-500/70 group-hover:text-cyan-400 truncate">
                        {template.displayName.toUpperCase()}
                      </span>
                    </button>
                  );
                })
              )}
            </nav>
          )} */}

          {/* Admin Section (Admin only) */}
          {user?.role === 'ADMIN' && (
            <div>
              <div className="p-1 border border-red-500/50 text-[10px] text-red-400 text-center uppercase mb-2 bg-red-500/5 font-bold tracking-[0.2em]">
                :: ADMIN_PANEL ::
              </div>
              <div className="flex flex-col gap-2">
                <button
                  onClick={() => navigate('/admin/users')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">admin_panel_settings</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    USUARIOS
                  </span>
                </button>
                <button
                  onClick={() => navigate('/admin/campaigns')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">shield</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    CAMPAÑAS
                  </span>
                </button>
                <button
                  onClick={() => navigate('/templates')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">description</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    PLANTILLAS
                  </span>
                </button>
                <button
                  onClick={() => navigate('/game-systems')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">admin_panel_settings</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    USUARIOS
                  </span>
                </button>
                <button
                  onClick={() => navigate('/admin/campaigns')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">shield</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    CAMPAÑAS
                  </span>
                </button>
                <button
                  onClick={() => navigate('/templates')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">description</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    PLANTILLAS
                  </span>
                </button>
                <button
                  onClick={() => navigate('/game-systems')}
                  className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
                >
                  <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">sports_esports</span>
                  <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
                    SISTEMAS
                  </span>
                </button>
              </div>
            </div>
          )}
          
          <div className="mt-auto hidden md:block p-3 border border-primary/10 bg-black/20 text-[8px] text-primary/40 leading-tight uppercase tracking-widest">
            <p className="mb-1">ESTADO: {transitionStatus === 'idle' ? 'ESTABLE' : 'TRANSFIRIENDO...'}</p>
            <p>CAMPAÑA: {activeCampaign?.id.slice(0, 8) || 'N/A'}</p>
          </div>
        </aside>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto pr-2 relative custom-scrollbar">
          {/* No Campaign Selected Message */}
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

          {/* Loading State */}
          {isLoading && (
            <div className="flex flex-col items-center justify-center h-64 text-primary animate-pulse font-mono uppercase tracking-[0.3em]">
              <span className="material-icons text-4xl mb-4">settings_input_antenna</span>
              Recuperando registros...
            </div>
          )}

          {/* Entity Grid */}
          {!isLoading && activeCampaignId && (
            <>
              {/* Search Input */}
              <div className="mb-4 flex gap-2">
                <div className="relative flex-1">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 material-icons text-primary/40 text-sm">search</span>
                  <input
                    ref={searchInputRef}
                    type="text"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
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
                  {filteredEntities.length} {filteredEntities.length === 1 ? 'resultado' : 'resultados'}
                </div>
              </div>

              <div 
                ref={entityGridRef}
              id="entity-grid"
              role="grid"
              aria-label={`Entidades de tipo ${ENTITY_CATEGORIES.find(c => c.id === displayCategory)?.label || ENTITY_CATEGORIES_LABS.find(c => c.id === displayCategory)?.label || displayCategory}`}
              tabIndex={0}
              onKeyDown={handleEntityGridKeyDown}
              className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 xl:grid-cols-3 gap-6 pb-8 transition-all duration-300 ${
                transitionStatus === 'out' ? 'section-transition-out' : 
                transitionStatus === 'in' ? 'section-transition-in' : ''
              }`}
            >
              {filteredEntities.map((entity, idx) => (
                <EntityCard
                  key={entity.id}
                  entity={entity}
                  selected={selectedEntity?.id === entity.id}
                  currentUserId={user?.id}
                  onClick={() => handleEntitySelect(entity)}
                  animationDelay={idx * 50}
                />
              ))}

              {isMaster && transitionStatus === 'idle' && (
                <AddNewCard onClick={handleAddNew} />
              )}
            </div>
            </>
          )}
          
          {/* Scanline effect during transition */}
          {transitionStatus !== 'idle' && (
            <div className="absolute inset-0 z-50 pointer-events-none flex items-center justify-center">
              <div className="w-full h-1 bg-primary/20 animate-[scan_0.5s_linear_infinite] shadow-[0_0_20px_#25f46a]"></div>
            </div>
          )}
        </main>

{/* Detail Panel */}
        {selectedEntity && (
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
        )}
      </div>
      
{/* Entity Edit Modal */}
      {showEditModal && selectedEntity && (
        <EntityEditModal
          entity={selectedEntity}
          canEditVisibility={
            // Owner or campaign master can edit visibility
            (user?.id === selectedEntity.ownerId) || isActiveCampaignMaster
          }
          canEditOwnership={
            // Owner or campaign master can transfer ownership
            (user?.id === selectedEntity.ownerId) || isActiveCampaignMaster
          }
          currentUserId={user?.id}
          fieldDefinitions={selectedEntityFieldDefs}
          onClose={() => setShowEditModal(false)}
          onSave={handleSaveEdit}
        />
      )}
      
      {/* Entity View Modal (Read-only details) */}
      {showViewModal && selectedEntity && (
        <EntityViewModal
          entity={selectedEntity}
          fieldDefinitions={selectedEntityFieldDefs}
          onClose={() => setShowViewModal(false)}
        />
      )}
    </TerminalLayout>
  );
};

// ============================================
// Sub-components
// ============================================

interface EntityCardProps {
  entity: LoreEntity;
  selected: boolean;
  currentUserId?: string;
  onClick: () => void;
  animationDelay: number;
}

/**
 * Entity Card Component
 * Displays a single entity in the gallery grid
 * Supports keyboard navigation as part of the grid
 */
const EntityCard: React.FC<EntityCardProps> = ({ entity, selected, currentUserId, onClick, animationDelay }) => {
  // Fallback image if none provided
  const imageUrl = entity.imageUrl || 'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop';
  const isOwner = currentUserId === entity.ownerId;
  
  return (
    <div
      role="gridcell"
      tabIndex={0}
      aria-label={`${entity.name}, ${entity.entityType.replace('_', ' ')}`}
      aria-selected={selected}
      onClick={onClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick();
        }
      }}
      className={`group relative bg-surface-dark border transition-all hover:shadow-[0_0_20px_rgba(37,244,106,0.2)] hover:scale-[1.02] cursor-pointer clip-tech-br overflow-hidden focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-surface-dark ${
        selected ? 'border-primary' : 'border-primary/40'
      }`}
      style={{ animationDelay: `${animationDelay}ms` }}
    >
      <div className="h-44 overflow-hidden border-b border-primary/50 relative">
        <img 
          src={imageUrl} 
          alt={entity.name} 
          className="w-full h-full object-cover object-[center_25%] grayscale brightness-50 group-hover:grayscale-0 group-hover:brightness-100 transition-all duration-500" 
        />
        <div className="absolute inset-0 bg-primary/5 mix-blend-overlay group-hover:bg-transparent transition-colors"></div>
        
        {/* Owner badge */}
        {isOwner && (
          <div className="absolute top-2 left-2">
            <span className="bg-primary/90 text-black text-[8px] font-bold px-2 py-0.5 uppercase tracking-wider">
              OWNER
            </span>
          </div>
        )}
        
        <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity">
          <div className="flex gap-1">
            <div className="w-1 h-3 bg-primary/40 animate-pulse"></div>
            <div className="w-1 h-3 bg-primary/40 animate-pulse" style={{ animationDelay: '0.2s' }}></div>
          </div>
        </div>
      </div>
      
      <div className="p-4 bg-gradient-to-b from-surface-dark to-black/80">
        <div className="flex justify-between items-start mb-2">
          <h3 className="text-xl text-primary font-display uppercase tracking-widest group-hover:text-glow truncate max-w-[80%]">
            {entity.name}
          </h3>
          <span className="text-[8px] text-primary/40 border border-primary/20 px-1">ID_{entity.id.slice(0,4)}</span>
        </div>
        <div className="flex justify-between items-center text-[10px] text-primary/60 font-mono tracking-tighter uppercase">
          <span className="flex items-center gap-1">
            <span className="w-1 h-1 bg-primary rounded-full"></span> 
            {entity.entityType.replace('_', ' ')}
          </span>
          <span className="opacity-50 italic">
            {entity.visibility === 0 ? 'BORRADOR' : 
             entity.visibility === 1 ? 'PRIVADO' : 
             entity.visibility === 2 ? 'CAMPAÑA' : 'PUBLICO'}
          </span>
        </div>
      </div>
      
      <div className="absolute inset-0 pointer-events-none opacity-0 group-hover:opacity-10 bg-[linear-gradient(rgba(18,16,16,0)_50%,rgba(0,0,0,0.25)_50%),linear-gradient(90deg,rgba(255,0,0,0.06),rgba(0,255,0,0.02),rgba(0,0,255,0.06))] bg-[length:100%_4px,3px_100%]"></div>
    </div>
  );
};

interface AddNewCardProps {
  onClick: () => void;
}

/**
 * Add New Card Component
 * Button card for creating new entities
 * Keyboard accessible with focus indication
 */
const AddNewCard: React.FC<AddNewCardProps> = ({ onClick }) => (
  <div 
    role="button"
    tabIndex={0}
    aria-label="Crear nueva entidad"
    onClick={onClick}
    onKeyDown={(e) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        onClick();
      }
    }}
    className="group relative bg-surface-dark border border-primary/40 border-dashed hover:border-solid hover:border-primary transition-all cursor-pointer clip-tech-br flex flex-col items-center justify-center min-h-[220px] hover:bg-primary/5 shadow-inner focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-surface-dark"
  >
    <div className="relative">
      <span className="material-icons text-6xl text-primary opacity-30 group-hover:opacity-100 transition-all group-hover:scale-110 group-hover:rotate-90">add</span>
      <div className="absolute inset-0 border border-primary opacity-0 group-hover:opacity-20 animate-ping rounded-full scale-150"></div>
    </div>
    <div className="p-3 text-center">
      <h3 className="text-sm text-primary font-bold uppercase tracking-[0.4em] group-hover:text-glow">Nueva_Entrada</h3>
      <p className="text-[8px] text-primary/40 mt-1 uppercase">Slot de Memoria Libre</p>
    </div>
  </div>
);

interface EntityDetailPanelProps {
  entity: LoreEntity;
  isMaster: boolean;
  currentUserId?: string;
  fieldDefinitions?: FieldDefinition[];
  onClose: () => void;
  onDelete: (id: string) => void;
  onVisibilityChange: (entityId: string, visibility: VisibilityLevel) => void;
  onEdit: () => void;
  onView: () => void;
}

/**
 * Entity Detail Panel Component
 * Shows detailed information about a selected entity
 * Supports focus management for accessibility
 * Includes PDF export functionality for character sheets
 */
const EntityDetailPanel = forwardRef<HTMLElement, EntityDetailPanelProps>(
  ({ entity, isMaster, currentUserId, fieldDefinitions, onClose, onDelete, onVisibilityChange, onEdit, onView }, ref) => {
    // Fallback image if none provided
    const imageUrl = entity.imageUrl || 'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop';
    
    // PDF export hook
    const { state: pdfState, exportToPdf } = useCharacterSheetPdf();
    
    /**
     * Handle PDF export
     * Passes field definitions for display name mapping
     */
    const handleExportPdf = async () => {
      try {
        await exportToPdf(entity, { fieldDefinitions });
      } catch (error) {
        console.error('PDF export failed:', error);
      }
    };

    return (
      <aside 
        ref={ref}
        role="complementary"
        aria-label={`Detalles de ${entity.name}`}
        tabIndex={-1}
        className="hidden lg:flex w-80 flex-col border border-primary bg-surface-dark/95 backdrop-blur-md relative animate-glitch-in focus:outline-none"
      >
        <div className="bg-primary text-black font-bold p-2 text-xs flex justify-between items-center shadow-[0_4px_10px_rgba(0,0,0,0.5)]">
          <span className="tracking-widest flex items-center gap-2">
            <span className="material-icons text-sm">analytics</span> &gt; INSPECTOR_ENTIDAD
          </span>
          <button
            aria-label="Cerrar panel de detalles"
            onClick={onClose}
            className="material-icons text-sm cursor-pointer hover:rotate-90 transition-transform focus:outline-none focus:ring-2 focus:ring-black"
          >
            close
          </button>
        </div>
        
        <div className="p-6 flex flex-col gap-6 flex-1 overflow-y-auto custom-scrollbar font-mono">
          <div className="relative w-full aspect-square border-2 border-primary/30 p-1 bg-black shadow-[0_0_15px_rgba(37,244,106,0.1)]">
            <img src={imageUrl} alt={`Imagen de ${entity.name}`} className="w-full h-full object-cover filter transition-all duration-700" />
            <div className="absolute top-2 left-2 px-1 bg-primary/80 text-black text-[8px] font-bold">ANALYSIS_LIVE</div>
            <div className="absolute bottom-2 right-2 flex gap-1">
              {[...Array(3)].map((_, i) => <div key={i} className="w-1.5 h-1.5 bg-primary/40 animate-pulse" style={{ animationDelay: `${i*0.1}s` }} />)}
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <h2 className="text-3xl font-display text-primary text-glow leading-none uppercase">{entity.name}</h2>
              <div className="h-0.5 w-full bg-gradient-to-r from-primary via-primary/20 to-transparent mt-1"></div>
            </div>
            
            <div className="grid grid-cols-2 gap-2">
              <div className="bg-black/40 border border-primary/10 p-2 text-[9px]">
                <span className="text-primary/40 block">TIPO</span>
                <span className="text-primary font-bold uppercase">{entity.entityType.replace('_', ' ')}</span>
              </div>
              
              {/* Owner Name display */}
              <div className="bg-black/40 border border-primary/10 p-2 text-[9px]">
                <span className="text-primary/40 block">PROPIETARIO</span>
                <span className="text-primary font-bold truncate block" title={entity.ownerName || 'Desconocido'}>
                  {entity.ownerName || 'Desconocido'}
                </span>
              </div>
            </div>
            
            {/* Visibility - Read-only display */}
            <div className="bg-black/40 border border-primary/10 p-2 text-[9px]">
              <span className="text-primary/40 block">VISIBILIDAD</span>
              <span className="text-primary font-bold uppercase">
                {entity.visibility === 0 ? 'BORRADOR' : 
                 entity.visibility === 1 ? 'PRIVADO' : 
                 entity.visibility === 2 ? 'CAMPAÑA' : 'PUBLICO'}
              </span>
            </div>

            <div className="bg-black/60 p-4 border border-primary/20 text-[11px] text-primary/80 leading-relaxed shadow-inner">
              <p className="text-[9px] text-primary/40 mb-2 uppercase tracking-[0.2em] font-bold">// BITÁCORA_NÚCLEO</p>
              <p>{entity.description || "Sin descripción adicional en el núcleo de datos. Acceso a metadatos restringido."}</p>
            </div>
          </div>

{/* Action buttons */}
          <div className="mt-auto flex flex-col gap-2 pt-4 border-t border-primary/10">
            {/* View Details Button (Available to all users) */}
            <button 
              onClick={onView}
              aria-label={`Ver detalles de ${entity.name}`}
              className="w-full py-3 border border-primary/60 text-primary text-[10px] hover:bg-primary/20 transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <span className="material-icons text-sm">visibility</span> VER_DETALLES
            </button>
            
            {/* Edit Button (Master or Owner can edit) */}
            {(isMaster || currentUserId === entity.ownerId) && (
              <button 
                onClick={onEdit}
                aria-label={`Editar ${entity.name}`}
                className="w-full py-3 border border-primary/60 text-primary text-[10px] hover:bg-primary/20 transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <span className="material-icons text-sm">edit</span> EDITAR_ENTIDAD
              </button>
            )}
            
            {/* PDF Export Button */}
            <button 
              onClick={handleExportPdf}
              disabled={pdfState.isExporting}
              aria-label={`Exportar ${entity.name} a PDF`}
              className="w-full py-3 border border-cyan-500/60 text-cyan-500 text-[10px] hover:bg-cyan-500/20 transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-cyan-500 disabled:opacity-50"
            >
              {pdfState.isExporting ? (
                <>
                  <span className="material-icons text-sm animate-spin">sync</span> EXPORTANDO...
                </>
              ) : (
                <>
                  <span className="material-icons text-sm">picture_as_pdf</span> EXPORTAR_FICHA
                </>
              )}
            </button>
            
            {/* Delete Button (Master or Owner can delete) */}
            {(isMaster || currentUserId === entity.ownerId) && (
              <button 
                onClick={() => onDelete(entity.id)}
                aria-label={`Eliminar ${entity.name}`}
                className="w-full py-3 border border-danger/60 text-danger text-[10px] hover:bg-danger hover:text-white transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2 focus:outline-none focus:ring-2 focus:ring-danger"
              >
                <span className="material-icons text-sm">delete_forever</span> PURGAR_REGISTRO
              </button>
            )}
          </div>
        </div>
        
        {/* Aesthetic Side Details */}
        <div className="absolute -left-1 top-1/4 w-1 h-20 bg-primary/20"></div>
        <div className="absolute -right-1 bottom-1/4 w-1 h-20 bg-primary/20"></div>
      </aside>
    );
  }
);

EntityDetailPanel.displayName = 'EntityDetailPanel';

export default GalleryPage;
