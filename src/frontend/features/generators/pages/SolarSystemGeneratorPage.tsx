/**
 * Solar System Generator Page
 * AI-powered solar system generation with orbital visualization
 * Uses campaign context for entity creation
 */

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { SystemData, PlanetData } from '@core/types';

interface SolarSystemGeneratorPageProps {
  onBack: () => void;
}

const SYSTEM_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1464802686167-b939a6910659?q=80&w=400&auto=format&fit=crop";

const SPECTRAL_CLASSES = [
  { value: 'M', label: 'Red Dwarf' },
  { value: 'G', label: 'Yellow Sun' },
  { value: 'O', label: 'Blue Giant' },
];

export const SolarSystemGeneratorPage: React.FC<SolarSystemGeneratorPageProps> = ({ onBack }) => {
  const navigate = useNavigate();
  const { activeCampaignId, activeCampaign } = useCampaign();
  
  const [spectralClass, setSpectralClass] = useState('G');
  const [planetCount, setPlanetCount] = useState(8);
  const [isInitializing, setIsInitializing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [systemData, setSystemData] = useState<SystemData | null>(null);
  const [systemImage, setSystemImage] = useState<string>(SYSTEM_PLACEHOLDER_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();
  /** Index of the planet currently being hovered in the cards grid */
  const [hoveredPlanetIndex, setHoveredPlanetIndex] = useState<number | null>(null);

  /** Image source mode state */
  const [imageMode, setImageMode] = useState<ImageSourceMode>('generate');
  /** Uploaded image data (base64) */
  const [uploadedImageData, setUploadedImageData] = useState<string | null>(null);

  const handleInitialize = async () => {
    setIsInitializing(true);
    setSystemData(null);
    try {
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateSolarSystem({
        gameSystemId: activeCampaign?.gameSystemId,
        spectralClass,
        planetCount,
        generateImage: shouldGenerateImage
      });

      const data = parseJsonResponse<SystemData>(result.systemJson);
      console.log('[SolarSystemGenerator] Parsed system data:', data);
      setSystemData(data);
      setGenerationRequestId(result.generationRequestId);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image (already compressed to WebP)
        setSystemImage(`data:image/webp;base64,${uploadedImageData}`);
      } else if (imageMode === 'generate') {
        if (result.imageUrl) {
          setSystemImage(result.imageUrl);
        } else if (result.imageBase64) {
          setSystemImage(`data:image/webp;base64,${result.imageBase64}`);
        } else {
          setSystemImage(SYSTEM_PLACEHOLDER_IMAGE);
        }
      } else {
        // Mode is 'none' - use placeholder
        setSystemImage(SYSTEM_PLACEHOLDER_IMAGE);
      }

    } catch (error) {
      console.error("Initialization failed", error);
    } finally {
      setIsInitializing(false);
    }
  };

  const handleSave = async () => {
    if (!systemData || !activeCampaignId) return;
    setIsSaving(true);
    try {
      await entityService.create(activeCampaignId, {
        entityType: 'solar_system',
        name: systemData.name,
        description: systemData.description,
        imageUrl: systemImage !== SYSTEM_PLACEHOLDER_IMAGE ? systemImage : undefined,
        attributes: {
          spectralClass: systemData.stats?.star_type || spectralClass,
          planetCount: systemData.stats?.planets?.length || planetCount,
          planets: systemData.stats?.planets || []
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'solar_generator_v1'
        },
        generationRequestId
      });
      setTimeout(onBack, 1000);
    } catch (e) {
      console.error(e);
    } finally {
      setIsSaving(false);
    }
  };

  const getStarStyle = () => {
    switch (spectralClass) {
      case 'M': return 'bg-red-500 shadow-[0_0_30px_red]';
      case 'O': return 'bg-blue-400 shadow-[0_0_40px_cyan]';
      default: return 'bg-yellow-200 shadow-[0_0_40px_yellow]';
    }
  };

  return (
    <TerminalLayout 
      title="SYNTH_SOLAR" 
      subtitle="Sintetizador de Mapas Estelares"
      icon="public"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col lg:flex-row gap-6 h-full overflow-hidden font-mono">
        {/* Controls Panel */}
        <aside className="lg:w-80 xl:w-96 flex flex-col gap-4 relative z-10 overflow-y-auto pr-2">
          <div className="bg-surface-dark/50 border border-primary/30 p-4 h-full flex flex-col gap-6 backdrop-blur-sm">
            <h2 className="text-xl uppercase border-b border-primary/30 pb-1 mb-2 flex justify-between font-display text-primary">
              <span>// Parámetros</span>
              <span className="material-icons text-sm">tune</span>
            </h2>

            {/* Spectral Class Selection */}
            <div className="space-y-4">
              <label className="text-[10px] uppercase tracking-wider text-primary/70">Clase Espectral Estelar</label>
              <div className="grid grid-cols-3 gap-2">
                {SPECTRAL_CLASSES.map(cls => (
                  <button
                    key={cls.value}
                    onClick={() => setSpectralClass(cls.value)}
                    className={`border border-primary p-2 text-center transition-all ${
                      spectralClass === cls.value ? 'bg-primary text-black font-bold' : 'text-primary hover:bg-primary/10'
                    }`}
                  >
                    <span className="block text-lg">{cls.value}</span>
                    <span className="text-[8px] opacity-70">{cls.label}</span>
                  </button>
                ))}
              </div>
            </div>

            {/* Planet Count Slider */}
            <div className="space-y-4">
              <div className="flex justify-between text-[10px] uppercase text-primary/70">
                <label>Cantidad de Planetas</label>
                <span className="bg-primary/10 px-2 text-primary">{planetCount.toString().padStart(2, '0')}</span>
              </div>
              <input 
                type="range" 
                min="1" 
                max="12" 
                value={planetCount} 
                onChange={(e) => setPlanetCount(parseInt(e.target.value))}
                className="w-full h-1 bg-primary/20 appearance-none rounded-none outline-none accent-primary cursor-pointer" 
              />
            </div>

            {/* Image Source Selector */}
            <ImageSourceSelector
              mode={imageMode}
              onModeChange={setImageMode}
              onImageUpload={setUploadedImageData}
              uploadedImage={uploadedImageData}
              disabled={isInitializing}
            />

            {/* Action Buttons */}
            <div className="mt-auto pt-4 flex flex-col gap-3">
              <Button
                onClick={handleInitialize}
                disabled={isInitializing}
                variant="secondary"
                fullWidth
                size="lg"
                isLoading={isInitializing}
                icon="settings"
              >
                {isInitializing ? 'SINTETIZANDO...' : 'INICIALIZAR'}
              </Button>
              <Button
                onClick={handleSave}
                disabled={!systemData || isInitializing || isSaving || !activeCampaignId}
                variant="primary"
                fullWidth
                size="lg"
                isLoading={isSaving}
                icon="save"
              >
                GUARDAR
              </Button>
            </div>
          </div>
        </aside>

        {/* Visualization Panel */}
        <section className="flex-1 flex flex-col relative bg-surface-dark/30 border border-primary/20 overflow-hidden">
          <div className="absolute inset-0 opacity-10 pointer-events-none" style={{ backgroundImage: 'linear-gradient(rgba(37, 244, 106, 0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(37, 244, 106, 0.1) 1px, transparent 1px)', backgroundSize: '40px 40px' }} />
          
          {/* Background Image */}
          <div className="absolute inset-0 z-0 pointer-events-none">
            <img src={systemImage} className={`w-full h-full object-cover grayscale opacity-40 transition-opacity duration-1000 ${isInitializing ? 'opacity-10 scale-105' : 'opacity-40 scale-100'}`} />
            <div className="absolute inset-0 bg-gradient-to-b from-black/60 via-black/40 to-black/90"></div>
          </div>

          {/* Scrollable Content */}
          <div className="flex-1 overflow-y-auto z-10 p-4 md:p-6">
            {/* System Header */}
            <div className={`mb-6 transition-all duration-500 ${systemData ? 'opacity-100 translate-y-0' : 'opacity-30 translate-y-2'}`}>
              <div className="flex items-center gap-3 mb-2">
                <div className={`w-8 h-8 rounded-full ${getStarStyle()} shrink-0`} />
                <div>
                  <h2 className="text-lg md:text-xl font-display text-primary uppercase tracking-wider">
                    {systemData?.name || 'SISTEMA_PENDIENTE'}
                  </h2>
                  <p className="text-[10px] text-primary/60 uppercase tracking-widest">
                    Tipo Estelar: {systemData?.stats?.star_type || spectralClass} // Planetas: {systemData?.stats?.planets?.length || planetCount}
                  </p>
                </div>
              </div>
              <p className="text-sm text-white/70 leading-relaxed max-w-3xl">
                {systemData?.description || 'Esperando inicialización del sistema...'}
              </p>
            </div>

            {/* Orbital Visualization (Compact) */}
            <div className="flex justify-center mb-6">
              <div className="relative w-48 h-48 md:w-64 md:h-64 flex items-center justify-center">
                {/* Star */}
                <div className={`absolute w-8 h-8 md:w-10 md:h-10 rounded-full animate-pulse transition-all duration-1000 z-10 ${getStarStyle()}`} />

                {/* Orbital Rings */}
                {[...Array(systemData?.stats?.planets?.length || planetCount)].map((_, i) => {
                  const planet = systemData?.stats?.planets?.[i];
                  const isHovered = hoveredPlanetIndex === i;
                  const baseColor = planet?.type?.includes('Gas') ? 'bg-orange-400' :
                    planet?.type?.includes('Ice') ? 'bg-cyan-400' : 'bg-primary';
                  
                  return (
                    <div 
                      key={i}
                      className={`absolute rounded-full transition-all duration-300 ${
                        isHovered 
                          ? 'border-2 border-primary shadow-[0_0_15px_rgba(37,244,106,0.5)]' 
                          : 'border border-primary/30'
                      }`}
                      style={{ 
                        width: `${50 + i * 25}px`, 
                        height: `${50 + i * 25}px`,
                        animation: isHovered ? 'none' : `spin ${10 + i * 5}s linear infinite`
                      }}
                    >
                      <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 group cursor-pointer">
                        <div className={`rounded-full transition-all duration-300 ${baseColor} ${
                          isHovered 
                            ? 'w-4 h-4 shadow-[0_0_20px_currentColor,0_0_40px_currentColor] ring-2 ring-white/50' 
                            : 'w-2 h-2 shadow-[0_0_5px_#25f46a]'
                        }`} />
                        <span className={`absolute left-5 top-0 text-[8px] text-primary whitespace-nowrap bg-black/90 px-2 py-0.5 border border-primary/30 font-bold z-20 transition-opacity ${
                          isHovered ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'
                        }`}>
                          {planet?.name || `PLN-${(i+1).toString().padStart(2, '0')}`}
                        </span>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Planets Grid */}
            {systemData?.stats?.planets && systemData.stats.planets.length > 0 && (
              <div className="space-y-4">
                <h3 className="text-xs text-primary/70 uppercase tracking-widest border-b border-primary/20 pb-2 flex items-center gap-2">
                  <span className="material-icons text-sm">public</span>
                  Registro Planetario ({systemData.stats.planets.length})
                </h3>
                
                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                  {systemData.stats.planets.map((planet: PlanetData, idx: number) => (
                    <PlanetCard 
                      key={idx} 
                      planet={planet} 
                      index={idx}
                      onHover={setHoveredPlanetIndex}
                      isHighlighted={hoveredPlanetIndex === idx}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* Empty State */}
            {!systemData && !isInitializing && (
              <div className="flex flex-col items-center justify-center py-12 text-primary/30">
                <span className="material-icons text-5xl mb-4">satellite_alt</span>
                <p className="text-sm uppercase tracking-widest">Sistema no inicializado</p>
                <p className="text-[10px] mt-1">Configure los parámetros e inicialice la generación</p>
              </div>
            )}

            {/* Loading State */}
            {isInitializing && (
              <div className="flex flex-col items-center justify-center py-12">
                <div className="w-16 h-16 border-2 border-primary/30 border-t-primary rounded-full animate-spin mb-4" />
                <p className="text-primary text-sm uppercase tracking-widest animate-pulse">Sintetizando sistema estelar...</p>
              </div>
            )}
          </div>

          {/* Status Bar */}
          <div className="border-t border-primary/20 bg-black/60 px-4 py-2 flex justify-between items-center text-[8px] text-primary/50 uppercase tracking-widest z-10">
            <span>Motor: Stellar_Synthesis_v2</span>
            <span>{isInitializing ? 'Calculando...' : systemData ? 'Datos Cargados' : 'En Espera'}</span>
          </div>
        </section>
      </div>
      
      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </TerminalLayout>
  );
};

/**
 * Planet Card Component Props
 */
interface PlanetCardProps {
  planet: PlanetData;
  index: number;
  /** Callback when mouse enters/leaves the card */
  onHover?: (index: number | null) => void;
  /** Whether this card's planet is highlighted in the orbital view */
  isHighlighted?: boolean;
}

/**
 * Planet Card Component
 * Displays detailed information about a single planet
 */
const PlanetCard: React.FC<PlanetCardProps> = ({ planet, index, onHover, isHighlighted }) => {
  // Safely get planet type with fallback
  const planetType = planet.type || 'Unknown';
  
  const getPlanetTypeColor = (type: string) => {
    if (type.includes('Gas')) return 'border-orange-500/50 bg-orange-500/5';
    if (type.includes('Ice')) return 'border-cyan-500/50 bg-cyan-500/5';
    if (type.includes('Terrestrial')) return 'border-primary/50 bg-primary/5';
    return 'border-primary/30 bg-primary/5';
  };

  const getPlanetTypeIcon = (type: string) => {
    if (type.includes('Gas')) return 'blur_on';
    if (type.includes('Ice')) return 'ac_unit';
    return 'public';
  };

  const getTemperatureColor = (temp: string) => {
    const lower = (temp || '').toLowerCase();
    if (lower.includes('hot') || lower.includes('caliente')) return 'text-red-400';
    if (lower.includes('cold') || lower.includes('frozen') || lower.includes('frío')) return 'text-cyan-400';
    return 'text-yellow-400';
  };

  return (
    <div 
      className={`border p-3 backdrop-blur-sm transition-all cursor-pointer ${getPlanetTypeColor(planetType)} ${
        isHighlighted 
          ? 'scale-[1.02] ring-2 ring-primary shadow-[0_0_20px_rgba(37,244,106,0.3)]' 
          : 'hover:scale-[1.02]'
      }`}
      onMouseEnter={() => onHover?.(index)}
      onMouseLeave={() => onHover?.(null)}
    >
      {/* Header */}
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center gap-2">
          <span className={`material-icons text-lg ${
            planetType.includes('Gas') ? 'text-orange-400' :
            planetType.includes('Ice') ? 'text-cyan-400' :
            'text-primary'
          }`}>
            {getPlanetTypeIcon(planetType)}
          </span>
          <div>
            <h4 className="text-sm font-bold text-white">
              {planet.name || `Planeta ${index + 1}`}
            </h4>
            <p className="text-[9px] text-primary/60 uppercase">{planetType}</p>
          </div>
        </div>
        <span className="text-[8px] bg-black/50 px-2 py-0.5 border border-primary/20 text-primary/70">
          ORB-{(planet.orbital_position ?? index + 1).toString().padStart(2, '0')}
        </span>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 gap-2 text-[9px] mb-2">
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Tamaño</span>
          <span className="text-white font-mono">
            {planet.size != null ? planet.size.toLocaleString() : '—'} km
          </span>
        </div>
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Gravedad</span>
          <span className="text-white font-mono">{planet.gravity ?? '—'} g</span>
        </div>
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Temperatura</span>
          <span className={`font-mono ${getTemperatureColor(planet.temperature)}`}>
            {planet.temperature || '—'}
          </span>
        </div>
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Atmósfera</span>
          <span className="text-white/80 text-[8px] leading-tight block">
            {planet.atmosphere || '—'}
          </span>
        </div>
      </div>

      {/* Features */}
      {planet.features && (
        <div className="text-[9px] mb-2">
          <span className="text-primary/50 uppercase">Características: </span>
          <span className="text-white/70">{planet.features}</span>
        </div>
      )}

      {/* Resources */}
      {planet.resources && (
        <div className="text-[9px] bg-black/40 p-1.5 border border-primary/10">
          <span className="text-yellow-500/70 uppercase flex items-center gap-1">
            <span className="material-icons text-[10px]">inventory_2</span>
            Recursos:
          </span>
          <span className="text-white/80">{planet.resources}</span>
        </div>
      )}
    </div>
  );
};

export default SolarSystemGeneratorPage;
