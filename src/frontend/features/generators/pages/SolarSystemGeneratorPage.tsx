/**
 * Solar System Generator Page
 * AI-powered solar system generation with orbital visualization
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useEntityGeneration } from '@core/hooks';
import type { SystemData, PlanetData } from '@core/types';

const SYSTEM_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1464802686167-b939a6910659?q=80&w=400&auto=format&fit=crop";

const SPECTRAL_CLASSES = [
  { value: 'M', label: 'Red Dwarf' },
  { value: 'G', label: 'Yellow Sun' },
  { value: 'O', label: 'Blue Giant' },
];

interface SolarSystemGeneratorPageProps {
  onBack: () => void;
}

export const SolarSystemGeneratorPage: React.FC<SolarSystemGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [form, setForm] = useState({
    spectralClass: 'G',
    planetCount: 8
  });
  const [hoveredPlanetIndex, setHoveredPlanetIndex] = useState<number | null>(null);

  const generateSolarSystem = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { spectralClass: string; planetCount: number };
    const result = await aiService.generateSolarSystem({
      gameSystemId: activeCampaign?.gameSystemId,
      spectralClass: formParams.spectralClass,
      planetCount: formParams.planetCount,
      generateImage
    });

    const data = parseJsonResponse<SystemData>(result.systemJson);
    console.log('[SolarSystemGenerator] Parsed system data:', data);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveSolarSystem = useCallback(async (params: {
    name: string;
    description?: string;
    imageUrl?: string;
    attributes: Record<string, unknown>;
    metadata?: Record<string, unknown>;
    generationRequestId?: string;
  }) => {
    if (!activeCampaignId) return;
    
    const spectralClass = params.attributes?.spectralClass as string | undefined;
    const planetCount = params.attributes?.planetCount as number | undefined;
    const planets = params.attributes?.planets as PlanetData[] | undefined;

    await entityService.create(activeCampaignId, {
      entityType: 'solar_system',
      name: params.name,
      description: params.description,
      imageUrl: params.imageUrl,
      attributes: {
        spectralClass: spectralClass || form.spectralClass,
        planetCount: planetCount || form.planetCount,
        planets: planets || []
      },
      metadata: {
        generatedAt: new Date().toISOString(),
        generator: 'solar_generator_v1'
      },
      generationRequestId: params.generationRequestId
    });
  }, [activeCampaignId, form.spectralClass, form.planetCount]);

  const {
    isGenerating,
    isSaving,
    editableData,
    image,
    imageMode,
    uploadedImageData,
    generate,
    save,
    setImageMode,
    setUploadedImageData
  } = useEntityGeneration<SystemData>({
    entityType: 'solar_system',
    placeholderImage: SYSTEM_PLACEHOLDER_IMAGE,
    initialLogs: [],
    maxLogs: 6,
    generateFn: generateSolarSystem,
    saveFn: saveSolarSystem,
    onSaveSuccess: () => {
      setTimeout(onBack, 1000);
    }
  });

  const handleGenerate = () => generate(form);

  const handleSave = async () => {
    if (!editableData) return;
    await save(activeCampaignId || '', {
      name: editableData.name,
      description: editableData.description,
      attributes: {
        spectralClass: editableData.stats?.star_type || form.spectralClass,
        planetCount: editableData.stats?.planets?.length || form.planetCount,
        planets: editableData.stats?.planets || []
      },
    });
  };

  const getStarStyle = () => {
    switch (form.spectralClass) {
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
        <aside className="lg:w-80 xl:w-96 flex flex-col gap-4 relative z-10 overflow-y-auto pr-2">
          <div className="bg-surface-dark/50 border border-primary/30 p-4 h-full flex flex-col gap-6 backdrop-blur-sm">
            <h2 className="text-xl uppercase border-b border-primary/30 pb-1 mb-2 flex justify-between font-display text-primary">
              <span>// Parametros</span>
              <span className="material-icons text-sm">tune</span>
            </h2>

            <div className="space-y-4">
              <label className="text-xs uppercase tracking-wider text-primary/70">Clase Espectral Estelar</label>
              <div className="grid grid-cols-3 gap-2">
                {SPECTRAL_CLASSES.map(cls => (
                  <button
                    key={cls.value}
                    onClick={() => setForm({ ...form, spectralClass: cls.value })}
                    className={`border border-primary p-2 text-center transition-all ${form.spectralClass === cls.value ? 'bg-primary text-black font-bold' : 'text-primary hover:bg-primary/10'
                      }`}
                  >
                    <span className="block text-lg">{cls.value}</span>
                    <span className="text-xs opacity-70">{cls.label}</span>
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-4">
              <div className="flex justify-between text-xs uppercase text-primary/70">
                <label>Cantidad de Planetas</label>
                <span className="bg-primary/10 px-2 text-primary">{form.planetCount.toString().padStart(2, '0')}</span>
              </div>
              <input
                type="range"
                min="1"
                max="12"
                value={form.planetCount}
                onChange={(e) => setForm({ ...form, planetCount: parseInt(e.target.value) })}
                className="w-full h-1 bg-primary/20 appearance-none rounded-none outline-none accent-primary cursor-pointer"
              />
            </div>

            <ImageSourceSelector
              mode={imageMode}
              onModeChange={setImageMode}
              onImageUpload={setUploadedImageData}
              uploadedImage={uploadedImageData}
              disabled={isGenerating}
            />

            <div className="mt-auto pt-4 flex flex-col gap-3">
              <Button
                onClick={handleGenerate}
                disabled={isGenerating}
                variant="secondary"
                fullWidth
                size="lg"
                isLoading={isGenerating}
                icon="settings"
              >
                {isGenerating ? 'SINTETIZANDO...' : 'INICIALIZAR'}
              </Button>
              <Button
                onClick={handleSave}
                disabled={!editableData || isGenerating || isSaving || !activeCampaignId}
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

        <section className="flex-1 flex flex-col relative bg-surface-dark/30 border border-primary/20 overflow-hidden">
          <div className="absolute inset-0 opacity-10 pointer-events-none" style={{ backgroundImage: 'linear-gradient(rgba(37, 244, 106, 0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(37, 244, 106, 0.1) 1px, transparent 1px)', backgroundSize: '40px 40px' }} />

          <div className="absolute inset-0 z-0 pointer-events-none">
            <img src={image} className={`w-full h-full object-cover grayscale opacity-40 transition-opacity duration-1000 ${isGenerating ? 'opacity-10 scale-105' : 'opacity-40 scale-100'}`} />
            <div className="absolute inset-0 bg-gradient-to-b from-black/60 via-black/40 to-black/90"></div>
          </div>

          <div className="flex-1 overflow-y-auto z-10 p-4 md:p-6">
            <div className={`mb-6 transition-all duration-500 ${editableData ? 'opacity-100 translate-y-0' : 'opacity-30 translate-y-2'}`}>
              <div className="flex items-center gap-3 mb-2">
                <div className={`w-8 h-8 rounded-full ${getStarStyle()} shrink-0`} />
                <div>
                  <h2 className="text-lg md:text-xl font-display text-primary uppercase tracking-wider">
                    {editableData?.name || 'SISTEMA_PENDIENTE'}
                  </h2>
                  <p className="text-[10px] text-primary/60 uppercase tracking-widest">
                    Tipo Estelar: {editableData?.stats?.star_type || form.spectralClass} // Planetas: {editableData?.stats?.planets?.length || form.planetCount}
                  </p>
                </div>
              </div>
              <p className="text-sm text-white/70 leading-relaxed max-w-3xl">
                {editableData?.description || 'Esperando inicializacion del sistema...'}
              </p>
            </div>

            <div className="flex justify-center mb-6">
              <div className="relative w-48 h-48 md:w-64 md:h-64 flex items-center justify-center">
                <div className={`absolute w-8 h-8 md:w-10 md:h-10 rounded-full animate-pulse transition-all duration-1000 z-10 ${getStarStyle()}`} />

                {[...Array(editableData?.stats?.planets?.length || form.planetCount)].map((_, i) => {
                  const planet = editableData?.stats?.planets?.[i];
                  const isHovered = hoveredPlanetIndex === i;
                  const baseColor = planet?.type?.includes('Gas') ? 'bg-orange-400' :
                    planet?.type?.includes('Ice') ? 'bg-cyan-400' : 'bg-primary';

                  return (
                    <div
                      key={i}
                      className={`absolute rounded-full transition-all duration-300 ${isHovered
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
                        <div className={`rounded-full transition-all duration-300 ${baseColor} ${isHovered
                          ? 'w-4 h-4 shadow-[0_0_20px_currentColor,0_0_40px_currentColor] ring-2 ring-white/50'
                          : 'w-2 h-2 shadow-[0_0_5px_#25f46a]'
                          }`} />
                        <span className={`absolute left-5 top-0 text-[8px] text-primary whitespace-nowrap bg-black/90 px-2 py-0.5 border border-primary/30 font-bold z-20 transition-opacity ${isHovered ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'
                          }`}>
                          {planet?.name || `PLN-${(i + 1).toString().padStart(2, '0')}`}
                        </span>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {editableData?.stats?.planets && editableData.stats.planets.length > 0 && (
              <div className="space-y-4">
                <h3 className="text-xs text-primary/70 uppercase tracking-widest border-b border-primary/20 pb-2 flex items-center gap-2">
                  <span className="material-icons text-sm">public</span>
                  Registro Planetario ({editableData.stats.planets.length})
                </h3>

                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                  {editableData.stats.planets.map((planet: PlanetData, idx: number) => (
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

            {!editableData && !isGenerating && (
              <div className="flex flex-col items-center justify-center py-12 text-primary/30">
                <span className="material-icons text-5xl mb-4">satellite_alt</span>
                <p className="text-sm uppercase tracking-widest">Sistema no inicializado</p>
                <p className="text-[10px] mt-1">Configure los parametros e inicialice la generacion</p>
              </div>
            )}

            {isGenerating && (
              <div className="flex flex-col items-center justify-center py-12">
                <div className="w-16 h-16 border-2 border-primary/30 border-t-primary rounded-full animate-spin mb-4" />
                <p className="text-primary text-sm uppercase tracking-widest animate-pulse">Sintetizando sistema estelar...</p>
              </div>
            )}
          </div>

          <div className="border-t border-primary/20 bg-black/60 px-4 py-2 flex justify-between items-center text-[8px] text-primary/50 uppercase tracking-widest z-10">
            <span>Motor: Stellar_Synthesis_v2</span>
            <span>{isGenerating ? 'Calculando...' : editableData ? 'Datos Cargados' : 'En Espera'}</span>
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

interface PlanetCardProps {
  planet: PlanetData;
  index: number;
  onHover?: (index: number | null) => void;
  isHighlighted?: boolean;
}

const PlanetCard: React.FC<PlanetCardProps> = ({ planet, index, onHover, isHighlighted }) => {
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
    if (lower.includes('cold') || lower.includes('frozen') || lower.includes('frio')) return 'text-cyan-400';
    return 'text-yellow-400';
  };

  return (
    <div
      className={`border p-3 backdrop-blur-sm transition-all cursor-pointer ${getPlanetTypeColor(planetType)} ${isHighlighted
        ? 'scale-[1.02] ring-2 ring-primary shadow-[0_0_20px_rgba(37,244,106,0.3)]'
        : 'hover:scale-[1.02]'
        }`}
      onMouseEnter={() => onHover?.(index)}
      onMouseLeave={() => onHover?.(null)}
    >
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center gap-2">
          <span className={`material-icons text-lg ${planetType.includes('Gas') ? 'text-orange-400' :
            planetType.includes('Ice') ? 'text-cyan-400' :
            'text-primary'
            }`}>
            {getPlanetTypeIcon(planetType)}
          </span>
          <div>
            <h4 className="text-sm font-bold text-white">
              {planet.name || `Planeta ${index + 1}`}
            </h4>
            <p className="text-xs text-primary/60 uppercase">{planetType}</p>
          </div>
        </div>
        <span className="text-xs bg-black/50 px-2 py-0.5 border border-primary/20 text-primary/70">
          ORB-{(planet.orbital_position ?? index + 1).toString().padStart(2, '0')}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-2 text-xs mb-2">
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Tamano</span>
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
          <span className="text-primary/50 uppercase block">Atmosfera</span>
          <span className="text-white/80 text-[8px] leading-tight block">
            {planet.atmosphere || '—'}
          </span>
        </div>
      </div>

      {planet.features && (
        <div className="text-xs mb-2">
          <span className="text-primary/50 uppercase">Caracteristicas: </span>
          <span className="text-white/70">{planet.features}</span>
        </div>
      )}

      {planet.resources && (
        <div className="text-xs bg-black/40 p-1.5 border border-primary/10">
          <span className="text-yellow-500/70 uppercase flex items-center gap-1">
            <span className="material-icons text-xs">inventory_2</span>
            Recursos:
          </span>
          <span className="text-white/80">{planet.resources}</span>
        </div>
      )}
    </div>
  );
};

export default SolarSystemGeneratorPage;
