/**
 * Vehicle Generator Page
 * AI-powered vehicle generation for starships, rovers, and mechs
 * Uses campaign context for entity creation
 */

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, DynamicStatsPanel } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService, entityTemplateService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { VehicleData, FieldDefinition } from '@core/types';

/** Placeholder image for vehicles without generated images */
const VEHICLE_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1702499903230-867455db1752?q=80&w=400&auto=format&fit=crop";

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
  const navigate = useNavigate();
  const { activeCampaignId, activeCampaign } = useCampaign();
  const [logs, setLogs] = useState(['> Awaiting construction parameters...']);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedVehi, setGeneratedVehi] = useState<VehicleData | null>(null);
  const [vehicleImage, setVehicleImage] = useState<string>(VEHICLE_PLACEHOLDER_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();
  /** Template field definitions for display name mapping */
  const [fieldDefinitions, setFieldDefinitions] = useState<FieldDefinition[]>([]);

  const [form, setForm] = useState({
    type: 'starship',
    class: 'interceptor',
    engine: 'fusion'
  });

  /** Image source mode state */
  const [imageMode, setImageMode] = useState<ImageSourceMode>('generate');
  /** Uploaded image data (base64) */
  const [uploadedImageData, setUploadedImageData] = useState<string | null>(null);

  /**
   * Fetch template field definitions when game system changes.
   * Used to map field identifiers to display names in stats panel.
   */
  useEffect(() => {
    const fetchTemplateFields = async () => {
      if (!activeCampaign?.gameSystemId) {
        setFieldDefinitions([]);
        return;
      }
      
      try {
        const fields = await entityTemplateService.getFieldDefinitions(
          activeCampaign.gameSystemId,
          'vehicle'
        );
        setFieldDefinitions(fields);
      } catch (error) {
        console.error('Failed to fetch template fields:', error);
        setFieldDefinitions([]);
      }
    };
    
    fetchTemplateFields();
  }, [activeCampaign?.gameSystemId]);

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
      setGenerationRequestId(result.generationRequestId);
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
      // Flatten stats into attributes to match template field definitions
      // Form fields go into metadata for reference
      await entityService.create(activeCampaignId, {
        entityType: 'vehicle',
        name: generatedVehi.name,
        description: generatedVehi.specs,
        imageUrl: vehicleImage !== VEHICLE_PLACEHOLDER_IMAGE ? vehicleImage : undefined,
        attributes: {
          // Spread AI-generated stats directly as top-level attributes
          // These should match the template field definitions
          ...generatedVehi.stats
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'vehicle_generator',
          // Store generation parameters for reference
          generationParams: {
            vehicleType: form.type,
            chassisClass: form.class,
            engine: form.engine
          }
        },
        generationRequestId
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
      title="SYNTH_VEHICULO" 
      subtitle="Ensamblaje Naval"
      icon="rocket_launch"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
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
              GUARDAR
            </Button>
          </div>
        </div>

        {/* Preview Panel */}
        <div className="flex-1 flex flex-col gap-4 overflow-y-auto">
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
            fieldDefinitions={fieldDefinitions}
          />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default VehicleGeneratorPage;
