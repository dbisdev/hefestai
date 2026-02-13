/**
 * Solar System Generator Page
 * AI-powered solar system generation with orbital visualization
 * Uses campaign context for entity creation
 */

import React, { useState } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { SystemData } from '@core/types';
import { Screen } from '@core/types';

interface SolarSystemGeneratorPageProps {
  onBack: () => void;
  onNavigate?: (screen: Screen) => void;
  onLogout?: () => void;
}

const SYSTEM_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1464802686167-b939a6910659?q=80&w=400&auto=format&fit=crop";

const SPECTRAL_CLASSES = [
  { value: 'M', label: 'Red Dwarf' },
  { value: 'G', label: 'Yellow Sun' },
  { value: 'O', label: 'Blue Giant' },
];

export const SolarSystemGeneratorPage: React.FC<SolarSystemGeneratorPageProps> = ({ onBack, onNavigate, onLogout }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();
  
  const [spectralClass, setSpectralClass] = useState('G');
  const [planetCount, setPlanetCount] = useState(8);
  const [isInitializing, setIsInitializing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [systemData, setSystemData] = useState<SystemData | null>(null);
  const [systemImage, setSystemImage] = useState<string>(SYSTEM_PLACEHOLDER_IMAGE);

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
      setSystemData(data);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image
        setSystemImage(`data:image/png;base64,${uploadedImageData}`);
      } else if (imageMode === 'generate') {
        if (result.imageBase64) {
          setSystemImage(`data:image/png;base64,${result.imageBase64}`);
        } else if (result.imageUrl) {
          setSystemImage(result.imageUrl);
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
          spectralClass,
          planetCount,
          planets: systemData.planets
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'solar_generator_v1'
        }
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
      title="GENERADOR SOLAR" 
      subtitle="Sintetizador de Mapas Estelares"
      icon="public"
      onLogout={onLogout}
      onNavigate={onNavigate}
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
                GUARDAR_ENTIDAD
              </Button>
            </div>
          </div>
        </aside>

        {/* Visualization Panel */}
        <section className="flex-1 flex flex-col relative bg-surface-dark/30 border border-primary/20 overflow-hidden overflow-y-auto">
          <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'linear-gradient(rgba(37, 244, 106, 0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(37, 244, 106, 0.1) 1px, transparent 1px)', backgroundSize: '40px 40px' }} />
          
          {/* System Data Overlay */}
          <div className={`absolute top-4 left-4 z-20 font-mono text-[9px] text-primary transition-all duration-500 ${systemData ? 'opacity-100 translate-x-0' : 'opacity-20 -translate-x-2'}`}>
            <p className="font-bold text-xs mb-1">DATA_LOG: {systemData?.name || 'CALCULATING...'}</p>
            <p className="max-w-xs text-white/70 italic leading-tight">{systemData?.description}</p>
          </div>

          {/* Background Image */}
          <div className="absolute inset-0 z-0">
            <img src={systemImage} className={`w-full h-full object-cover grayscale opacity-40 transition-opacity duration-1000 ${isInitializing ? 'opacity-10 scale-105' : 'opacity-40 scale-100'}`} />
            <div className="absolute inset-0 bg-gradient-to-b from-transparent via-black/20 to-black/80"></div>
          </div>

          {/* Orbital Visualization */}
          <div className="flex-1 flex items-center justify-center p-8 z-10">
            <div className="relative w-72 h-72 md:w-[400px] md:h-[400px] flex items-center justify-center">
              {/* Star */}
              <div className={`absolute w-12 h-12 md:w-16 md:h-16 rounded-full animate-pulse transition-all duration-1000 z-10 ${getStarStyle()}`} />

              {/* Orbital Rings */}
              {[...Array(planetCount)].map((_, i) => (
                <div 
                  key={i}
                  className="absolute border border-primary/20 rounded-full"
                  style={{ 
                    width: `${80 + i * 40}px`, 
                    height: `${80 + i * 40}px`,
                    animation: `spin ${10 + i * 5}s linear infinite`
                  }}
                >
                  <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 group">
                    <div className="w-1.5 h-1.5 rounded-full bg-primary shadow-[0_0_5px_#25f46a]" />
                    <span className="absolute left-4 top-0 text-[7px] text-primary opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap bg-black/80 px-1 font-bold">
                      {systemData?.planets?.[i] || `PLN-${(i+1).toString().padStart(2, '0')}`}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Status Bar */}
          <div className="absolute bottom-4 left-0 w-full text-center z-20">
            <span className="px-4 py-1 bg-black/80 text-primary text-[8px] border border-primary/50 font-mono tracking-widest uppercase">
              {isInitializing ? 'Calculando Coordenadas...' : 'Simulación de Orbita Estelar Activa'}
            </span>
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

export default SolarSystemGeneratorPage;
