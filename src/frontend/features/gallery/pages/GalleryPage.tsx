/**
 * Gallery Page
 * Displays all entities within the active campaign with category filtering
 * Includes campaign selection and management
 */

import React, { useState, useEffect, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { useCampaign } from '@core/context';
import { entityService } from '@core/services/api';
import type { LoreEntity, User, EntityCategory, Campaign } from '@core/types';
import { Screen, CampaignRole } from '@core/types';

interface GalleryPageProps {
  user: User | null;
  onNavigate: (screen: Screen) => void;
  onLogout: () => void;
}

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
  { id: 'solar_system', label: 'SISTEMAS', icon: 'public' },
  { id: 'character', label: 'PERSONAJES', icon: 'face' },
  { id: 'npc', label: 'ACTORES', icon: 'groups' },
  { id: 'enemy', label: 'ENEMIGOS', icon: 'pest_control' },
  { id: 'vehicle', label: 'VEHÍCULOS', icon: 'rocket_launch' },
  { id: 'mission', label: 'MISIONES', icon: 'assignment' },
  { id: 'encounter', label: 'ENCUENTROS', icon: 'swords' },
];

/**
 * Map entity category to the corresponding generator screen
 */
const CATEGORY_TO_SCREEN: Record<EntityCategory, Screen> = {
  'solar_system': Screen.SOLAR_GEN,
  'character': Screen.CHAR_GEN,
  'npc': Screen.NPC_GEN,
  'enemy': Screen.ENEMY_GEN,
  'vehicle': Screen.VEHI_GEN,
  'mission': Screen.MISSION_GEN,
  'encounter': Screen.ENCOUNTER_GEN,
};

export const GalleryPage: React.FC<GalleryPageProps> = ({ user, onNavigate, onLogout }) => {
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
  const [transitionStatus, setTransitionStatus] = useState<'idle' | 'out' | 'in'>('idle');

  // Determine if user is master (either by user role or campaign role)
  const isMaster = user?.role === 'MASTER' || isActiveCampaignMaster;

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
   * Handle category tab change with transition effect
   */
  const handleCategoryChange = (newCat: EntityCategory) => {
    if (newCat === activeCategory || transitionStatus !== 'idle') return;
    
    setTransitionStatus('out');
    setActiveCategory(newCat);
    
    setTimeout(() => {
      setDisplayCategory(newCat);
      setSelectedEntity(null);
      setTransitionStatus('in');
      
      setTimeout(() => {
        setTransitionStatus('idle');
      }, 500);
    }, 400);
  };

  /**
   * Filter entities by the current display category
   */
  const filteredEntities = entities.filter(e => e.entityType === displayCategory);

  /**
   * Handle entity deletion
   */
  const handleDelete = async (id: string) => {
    if (!activeCampaignId) return;
    
    if (confirm('¿Confirmar purga de datos? Esta acción es irreversible.')) {
      try {
        await entityService.delete(activeCampaignId, id);
        setSelectedEntity(null);
        loadEntities();
      } catch (e: unknown) {
        const message = e instanceof Error ? e.message : 'Error al eliminar';
        alert(message);
      }
    }
  };

  /**
   * Navigate to the appropriate generator for the current category
   */
  const handleAddNew = () => {
    const screen = CATEGORY_TO_SCREEN[displayCategory];
    if (screen) {
      onNavigate(screen);
    }
  };

  /**
   * Navigate to campaign creation/joining page
   */
  const handleManageCampaigns = () => {
    onNavigate(Screen.CAMPAIGN_GEN);
  };

  /**
   * Handle campaign selection
   */
  const handleSelectCampaign = async (campaign: Campaign) => {
    try {
      await selectCampaign(campaign.id);
      setShowCampaignSelector(false);
    } catch (error) {
      console.error('Failed to select campaign:', error);
    }
  };

  const isLoading = isCampaignLoading || isLoadingEntities;

  return (
    <TerminalLayout 
      title="Galería de Creaciones" 
      subtitle={`Usuario: ${user?.username} // ${activeCampaign ? `Campaña: ${activeCampaign.name}` : 'Sin campaña'}`} 
      onLogout={onLogout}
      actions={
        <div className="flex items-center gap-2">
          {/* Campaign Selector Button */}
          <button 
            onClick={() => setShowCampaignSelector(!showCampaignSelector)}
            className="border border-primary/60 px-3 py-1 text-[10px] uppercase hover:bg-primary/10 hover:border-primary transition-colors text-primary font-bold flex items-center gap-1"
          >
            <span className="material-icons text-sm">public</span> 
            {activeCampaign ? activeCampaign.name.slice(0, 10) : 'CAMPAÑA'}
            {activeCampaign && '...'}
          </button>

          {/* Invite Button (Master only) */}
          {isMaster && activeCampaign?.joinCode && (
            <button 
              onClick={() => setShowInvite(!showInvite)}
              className="border border-primary px-3 py-1 text-[10px] uppercase hover:bg-primary hover:text-black transition-colors text-primary font-bold flex items-center gap-1"
            >
              <span className="material-icons text-sm">share</span> INVITAR
            </button>
          )}
        </div>
      }
    >
      <div className="flex h-full gap-6 overflow-hidden relative font-mono">
        {/* Campaign Selector Popup */}
        {showCampaignSelector && (
          <div className="absolute top-0 left-16 md:left-64 z-50 bg-surface-dark border border-primary p-4 shadow-2xl animate-glitch-in font-mono min-w-[250px]">
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
                      onClick={() => handleSelectCampaign(campaign)}
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
          <div className="absolute top-0 right-0 z-50 bg-surface-dark border border-primary p-4 shadow-2xl animate-glitch-in font-mono">
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

        {/* Sidebar Navigation */}
        <aside className="w-16 md:w-64 flex flex-col gap-4 shrink-0">
          <nav className="flex flex-col gap-2">
            <div className="p-1 border border-primary/50 text-[10px] text-primary text-center uppercase mb-2 bg-primary/5 font-bold tracking-[0.2em]">
              :: SECTORES_DATOS ::
            </div>
            {ENTITY_CATEGORIES.map((cat) => (
              <button
                key={cat.id}
                onClick={() => handleCategoryChange(cat.id)}
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
          </nav>
          
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
            <div 
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
                  onClick={() => setSelectedEntity(entity)}
                  animationDelay={idx * 50}
                />
              ))}

              {isMaster && transitionStatus === 'idle' && (
                <AddNewCard onClick={handleAddNew} />
              )}
            </div>
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
            entity={selectedEntity}
            isMaster={isMaster}
            onClose={() => setSelectedEntity(null)}
            onDelete={handleDelete}
          />
        )}
      </div>
    </TerminalLayout>
  );
};

// ============================================
// Sub-components
// ============================================

interface EntityCardProps {
  entity: LoreEntity;
  selected: boolean;
  onClick: () => void;
  animationDelay: number;
}

/**
 * Entity Card Component
 * Displays a single entity in the gallery grid
 */
const EntityCard: React.FC<EntityCardProps> = ({ entity, selected, onClick, animationDelay }) => {
  // Fallback image if none provided
  const imageUrl = entity.imageUrl || 'https://images.unsplash.com/photo-1518020382113-a7e8fc38eac9?q=80&w=400&auto=format&fit=crop';
  
  return (
    <div
      onClick={onClick}
      className={`group relative bg-surface-dark border transition-all hover:shadow-[0_0_20px_rgba(37,244,106,0.2)] hover:scale-[1.02] cursor-pointer clip-tech-br overflow-hidden ${
        selected ? 'border-primary' : 'border-primary/40'
      }`}
      style={{ animationDelay: `${animationDelay}ms` }}
    >
      <div className="h-44 overflow-hidden border-b border-primary/50 relative">
        <img 
          src={imageUrl} 
          alt={entity.name} 
          className="w-full h-full object-cover grayscale brightness-50 group-hover:grayscale-0 group-hover:brightness-100 transition-all duration-500" 
        />
        <div className="absolute inset-0 bg-primary/5 mix-blend-overlay group-hover:bg-transparent transition-colors"></div>
        
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
 */
const AddNewCard: React.FC<AddNewCardProps> = ({ onClick }) => (
  <div 
    onClick={onClick}
    className="group relative bg-surface-dark border border-primary/40 border-dashed hover:border-solid hover:border-primary transition-all cursor-pointer clip-tech-br flex flex-col items-center justify-center min-h-[220px] hover:bg-primary/5 shadow-inner"
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
  onClose: () => void;
  onDelete: (id: string) => void;
}

/**
 * Entity Detail Panel Component
 * Shows detailed information about a selected entity
 */
const EntityDetailPanel: React.FC<EntityDetailPanelProps> = ({ entity, isMaster, onClose, onDelete }) => {
  // Fallback image if none provided
  const imageUrl = entity.imageUrl || 'https://images.unsplash.com/photo-1518020382113-a7e8fc38eac9?q=80&w=400&auto=format&fit=crop';

  return (
    <aside className="hidden lg:flex w-80 flex-col border border-primary bg-surface-dark/95 backdrop-blur-md relative animate-glitch-in">
      <div className="bg-primary text-black font-bold p-2 text-xs flex justify-between items-center shadow-[0_4px_10px_rgba(0,0,0,0.5)]">
        <span className="tracking-widest flex items-center gap-2">
          <span className="material-icons text-sm">analytics</span> &gt; INSPECTOR_ENTIDAD
        </span>
        <span className="material-icons text-sm cursor-pointer hover:rotate-90 transition-transform" onClick={onClose}>close</span>
      </div>
      
      <div className="p-6 flex flex-col gap-6 flex-1 overflow-y-auto custom-scrollbar font-mono">
        <div className="relative w-full aspect-square border-2 border-primary/30 p-1 bg-black shadow-[0_0_15px_rgba(37,244,106,0.1)]">
          <img src={imageUrl} alt="Detail" className="w-full h-full object-cover filter grayscale hover:grayscale-0 transition-all duration-700" />
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
            <div className="bg-black/40 border border-primary/10 p-2 text-[9px]">
              <span className="text-primary/40 block">VISIBILIDAD</span>
              <span className="text-primary font-bold uppercase">
                {entity.visibility === 0 ? 'BORRADOR' : 
                 entity.visibility === 1 ? 'PRIVADO' : 
                 entity.visibility === 2 ? 'CAMPAÑA' : 'PUBLICO'}
              </span>
            </div>
          </div>

          <div className="bg-black/60 p-4 border border-primary/20 text-[11px] text-primary/80 leading-relaxed shadow-inner">
            <p className="text-[9px] text-primary/40 mb-2 uppercase tracking-[0.2em] font-bold">// BITÁCORA_NÚCLEO</p>
            <p>{entity.description || "Sin descripción adicional en el núcleo de datos. Acceso a metadatos restringido."}</p>
          </div>

          {/* Display attributes if available */}
          {entity.attributes && Object.keys(entity.attributes).length > 0 && (
            <div className="space-y-2">
              <p className="text-[9px] text-primary/40 uppercase tracking-[0.2em] font-bold">// ATRIBUTOS</p>
              <div className="grid grid-cols-3 gap-2">
                {Object.entries(entity.attributes).slice(0, 6).map(([key, value]) => (
                  <div key={key} className="bg-surface-dark border border-primary/20 p-2 text-center">
                    <p className="text-[8px] text-primary/40 uppercase mb-1">{key}</p>
                    <p className="text-sm font-bold text-primary">{String(value)}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {isMaster && (
          <div className="mt-auto flex flex-col gap-2 pt-4 border-t border-primary/10">
            <button 
              onClick={() => onDelete(entity.id)}
              className="w-full py-3 border border-danger/60 text-danger text-[10px] hover:bg-danger hover:text-white transition-all font-bold uppercase tracking-[0.2em] flex items-center justify-center gap-2"
            >
              <span className="material-icons text-sm">delete_forever</span> PURGAR_REGISTRO
            </button>
          </div>
        )}
      </div>
      
      {/* Aesthetic Side Details */}
      <div className="absolute -left-1 top-1/4 w-1 h-20 bg-primary/20"></div>
      <div className="absolute -right-1 bottom-1/4 w-1 h-20 bg-primary/20"></div>
    </aside>
  );
};

export default GalleryPage;
