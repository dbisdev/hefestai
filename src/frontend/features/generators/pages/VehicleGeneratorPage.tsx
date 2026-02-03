/**
 * Vehicle Generator Page
 * AI-powered vehicle generation for starships, rovers, and mechs
 * Uses campaign context for entity creation
 */

import React, { useState } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, DynamicStatsPanel } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { VehicleData } from '@core/types';

/** Placeholder image for vehicles without generated images */
const VEHICLE_PLACEHOLDER_IMAGE = "https://lh3.googleusercontent.com/aida-public/AB6AXuDwdfYYr9eKFLnajyN2Ac6wDARXA_-mfibVDogKPYkAVDBc8v4xmz5S0onKageqWHbJkwaMQal6d_37piOBkfBRODrtpzVCAORmDmN9Lhms-1nWa0CAGhzL-5Cn16UzV3rpA-y-YrjlCMY3FBwJuARw1b7kBd9u5-Ix8KNLLf33w-D8gYTS1IH94XfBXDAo-nEqDs-LwRpisgMDqMM3vEgtruTqz-qjLsv8dR7IrSoRWDYyOqfAh36rTTDQBiDtNWaL6sCxsMV7POo";

interface VehicleGeneratorPageProps {
  onBack: () => void;
}

const VEHICLE_TYPE_OPTIONS = [
  { value: 'starship', label: 'Nave Espacial' },
  { value: 'rover', label: 'Rover Terrestre' },
  { value: 'mech', label: 'Mech de Combate' },
];

const CHASSIS_CLASS_OPTIONS = [
  { value: 'interceptor', label: 'Interceptor Ligero' },
  { value: 'freighter', label: 'Carguero Pesado' },
  { value: 'explorer', label: 'Explorador de Larga Distancia' },
];

export const VehicleGeneratorPage: React.FC<VehicleGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();
  const [logs, setLogs] = useState(['> Awaiting construction parameters...']);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedVehi, setGeneratedVehi] = useState<VehicleData | null>(null);
  const [vehicleImage, setVehicleImage] = useState<string>(VEHICLE_PLACEHOLDER_IMAGE);

  const [form, setForm] = useState({
    type: 'starship',
    class: 'interceptor',
    engine: 'fusion'
  });

  /** Image source mode state */
  const [imageMode, setImageMode] = useState<ImageSourceMode>('generate');
  /** Uploaded image data (base64) */
  const [uploadedImageData, setUploadedImageData] = useState<string | null>(null);

  const addLog = (msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-6));
  };

  /**
   * Handles vehicle generation via AI service
   */
  const handleGenerate = async () => {
    setIsGenerating(true);
    addLog(`Initializing assembly for ${form.class}...`);

    try {
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateVehicle({
        gameSystemId: activeCampaign?.gameSystemId,
        type: form.type,
        class: form.class,
        engine: form.engine,
        generateImage: shouldGenerateImage
      });

      const data = parseJsonResponse<VehicleData>(result.vehicleJson);
      setGeneratedVehi(data);
      addLog(`Assembly complete: ${data.name}`);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image
        setVehicleImage(`data:image/png;base64,${uploadedImageData}`);
        addLog('Using uploaded image.');
      } else if (imageMode === 'generate') {
        if (result.imageBase64) {
          setVehicleImage(`data:image/png;base64,${result.imageBase64}`);
          addLog('Visual synthesis complete.');
        } else if (result.imageUrl) {
          setVehicleImage(result.imageUrl);
          addLog('Visual synthesis complete.');
        } else {
          setVehicleImage(VEHICLE_PLACEHOLDER_IMAGE);
          addLog('WARNING: Visual render failed. Using placeholder.');
        }
      } else {
        // Mode is 'none' - use placeholder
        setVehicleImage(VEHICLE_PLACEHOLDER_IMAGE);
        addLog('Image generation skipped.');
      }
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unknown error';
      addLog(`Error during assembly: ${message}`);
    } finally {
      setIsGenerating(false);
    }
  };

  /**
   * Saves the generated vehicle to the entity service using campaign-scoped endpoint
   */
  const handleSave = async () => {
    if (!generatedVehi || !activeCampaignId) return;
    setIsSaving(true);
    addLog('Writing to shipyard database...');

    try {
      await entityService.create(activeCampaignId, {
        entityType: 'vehicle',
        name: generatedVehi.name,
        description: generatedVehi.specs,
        imageUrl: vehicleImage !== VEHICLE_PLACEHOLDER_IMAGE ? vehicleImage : undefined,
        attributes: {
          vehicleType: form.type,
          chassisClass: form.class,
          engine: form.engine,
          stats: generatedVehi.stats
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'vehicle_generator'
        }
      });
      addLog('Data committed to shipyard database.');
      setTimeout(onBack, 1500);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unknown error';
      addLog(`Database write failed: ${message}`);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <TerminalLayout 
      title="Shipyard_Forge // V.1.0" 
      subtitle={`Campaña: ${activeCampaign?.name || 'N/A'} // Ensamblaje de Vehículos Persistentes`}
      actions={
        <button onClick={onBack} className="text-primary/60 hover:text-primary transition-colors flex items-center gap-1 text-xs font-mono uppercase">
          <span className="material-icons text-sm">arrow_back</span> VOLVER
        </button>
      }
    >
      <div className="flex flex-col lg:flex-row gap-8 h-full font-mono">
        {/* Form Panel */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto">
          <div className="space-y-4">
            <div>
              <label className="text-primary text-[10px] uppercase block mb-1">Tipo de Vehículo</label>
              <select 
                className="w-full bg-surface-dark border border-primary/30 p-2 text-sm text-white" 
                value={form.type} 
                onChange={e => setForm({...form, type: e.target.value})}
              >
                {VEHICLE_TYPE_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-primary text-[10px] uppercase block mb-1">Clase de Chasis</label>
              <select 
                className="w-full bg-surface-dark border border-primary/30 p-2 text-sm text-white"
                value={form.class} 
                onChange={e => setForm({...form, class: e.target.value})}
              >
                {CHASSIS_CLASS_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            {/* Image Source Selector */}
            <ImageSourceSelector
              mode={imageMode}
              onModeChange={setImageMode}
              onImageUpload={setUploadedImageData}
              uploadedImage={uploadedImageData}
              disabled={isGenerating}
            />
          </div>

          {/* Action Buttons */}
          <div className="mt-auto flex gap-4">
            <Button
              onClick={handleGenerate}
              disabled={isGenerating}
              variant="secondary"
              fullWidth
              size="lg"
              isLoading={isGenerating}
            >
              {isGenerating ? 'ENSAMBLANDO...' : 'ENSAMBLAR'}
            </Button>
            <Button
              onClick={handleSave}
              disabled={!generatedVehi || isSaving || !activeCampaignId}
              variant="primary"
              fullWidth
              size="lg"
              isLoading={isSaving}
            >
              GUARDAR_ENTIDAD
            </Button>
          </div>
        </div>

        {/* Preview Panel */}
        <div className="flex-1 flex flex-col gap-4">
          <div className="relative flex-1 border border-primary/30 bg-black clip-tech-br overflow-hidden">
            <img 
              src={vehicleImage} 
              className={`w-full h-full object-cover transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-60 scale-100'} grayscale`} 
              alt="Vehicle Preview"
            />
            {isGenerating && (
              <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 z-20">
                <div className="w-1/2 h-1 bg-primary/20 relative overflow-hidden mb-2">
                  <div className="absolute inset-0 bg-primary animate-[scan_2s_linear_infinite]"></div>
                </div>
                <span className="text-primary text-[10px] animate-pulse">ENSAMBLANDO_VEHICULO...</span>
              </div>
            )}
            <div className="absolute inset-0 flex flex-col justify-end p-4 bg-gradient-to-t from-black to-transparent">
              <h3 className="text-primary font-bold">{generatedVehi?.name || '---'}</h3>
              <p className="text-[10px] text-primary/70">{generatedVehi?.specs}</p>
            </div>
          </div>
          
          {/* Log Panel */}
          <div className="h-20 bg-black/80 border border-primary/20 p-2 text-[10px] text-primary/60 overflow-y-auto">
            {logs.map((l, i) => <p key={i}>{l}</p>)}
          </div>
          
          {/* Stats Panel - Dynamic based on game system */}
          <DynamicStatsPanel 
            stats={generatedVehi?.stats} 
            variant="primary"
            maxColumns={3}
            showProgressBar={true}
            maxProgressValue={100}
          />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default VehicleGeneratorPage;
