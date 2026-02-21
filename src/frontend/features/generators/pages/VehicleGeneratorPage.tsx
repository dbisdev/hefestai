/**
 * Vehicle Generator Page
 * AI-powered vehicle generation for starships, rovers, and mechs
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback, useEffect } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, TerminalLog, EditableField, EditableStatsPanel } from '@shared/components/ui';
import { aiService, entityService, entityTemplateService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useEntityGeneration } from '@core/hooks';
import type { VehicleData } from '@core/types';

const VEHICLE_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1702499903230-867455db1752?q=80&w=400&auto=format&fit=crop";

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

interface VehicleGeneratorPageProps {
  onBack: () => void;
}

export const VehicleGeneratorPage: React.FC<VehicleGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [form, setForm] = useState({
    type: 'starship',
    class: 'interceptor',
    engine: 'fusion'
  });

  const generateVehicle = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { type: string; class: string; engine: string };
    const result = await aiService.generateVehicle({
      gameSystemId: activeCampaign?.gameSystemId,
      type: formParams.type,
      class: formParams.class,
      engine: formParams.engine,
      generateImage
    });

    const data = parseJsonResponse<VehicleData>(result.vehicleJson);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveVehicle = useCallback(async (params: {
    name: string;
    description?: string;
    imageUrl?: string;
    attributes: Record<string, unknown>;
    metadata?: Record<string, unknown>;
    generationRequestId?: string;
  }) => {
    if (!activeCampaignId) return;
    
    await entityService.create(activeCampaignId, {
      entityType: 'vehicle',
      name: params.name,
      description: params.description,
      imageUrl: params.imageUrl,
      attributes: params.attributes,
      metadata: {
        ...params.metadata,
        generatedAt: new Date().toISOString(),
        generator: 'vehicle_generator',
        generationParams: {
          vehicleType: form.type,
          chassisClass: form.class,
          engine: form.engine
        }
      },
      generationRequestId: params.generationRequestId
    });
  }, [activeCampaignId, form.type, form.class, form.engine]);

  const getFieldDefinitions = useCallback(async (gameSystemId: string) => {
    return entityTemplateService.getFieldDefinitions(gameSystemId, 'vehicle');
  }, []);

  const {
    isGenerating,
    isSaving,
    editableData,
    image,
    fieldDefinitions,
    imageMode,
    uploadedImageData,
    logs,
    generate,
    save,
    loadFieldDefinitions,
    setEditableData,
    setImageMode,
    setUploadedImageData
  } = useEntityGeneration<VehicleData>({
    entityType: 'vehicle',
    placeholderImage: VEHICLE_PLACEHOLDER_IMAGE,
    initialLogs: ['> Awaiting construction parameters...'],
    maxLogs: 6,
    generateFn: generateVehicle,
    saveFn: saveVehicle,
    getFieldDefinitions,
    onSaveSuccess: () => {
      setTimeout(onBack, 1500);
    }
  });

  useEffect(() => {
    loadFieldDefinitions(activeCampaign?.gameSystemId);
  }, [activeCampaign?.gameSystemId, loadFieldDefinitions]);

  const handleGenerate = () => generate(form);

  const handleSave = async () => {
    if (!editableData) return;
    await save(activeCampaignId || '', {
      name: editableData.name,
      description: editableData.description,
      attributes: { ...editableData.stats },
    });
  };

  const handleStatsChange = (newStats: Record<string, unknown>) => {
    if (editableData) {
      setEditableData({ ...editableData, stats: newStats });
    }
  };

  const handleNameChange = (value: string | number) => {
    if (editableData) {
      setEditableData({ ...editableData, name: String(value) });
    }
  };

  const handleDescriptionChange = (value: string | number) => {
    if (editableData) {
      setEditableData({ ...editableData, description: String(value) });
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
      <div className="flex flex-col md:flex-row gap-8 md:h-full font-mono">
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto">
          <div className="space-y-4">
            <div>
              <label className="text-primary text-[10px] uppercase block mb-1">Tipo de Vehiculo</label>
              <select
                className="w-full bg-surface-dark border border-primary/30 p-2 text-sm text-white"
                value={form.type}
                onChange={e => setForm({ ...form, type: e.target.value })}
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
                onChange={e => setForm({ ...form, class: e.target.value })}
              >
                {CHASSIS_CLASS_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <ImageSourceSelector
              mode={imageMode}
              onModeChange={setImageMode}
              onImageUpload={setUploadedImageData}
              uploadedImage={uploadedImageData}
              disabled={isGenerating}
            />
          </div>

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
              disabled={!editableData || isSaving || !activeCampaignId}
              variant="primary"
              fullWidth
              size="lg"
              isLoading={isSaving}
            >
              GUARDAR
            </Button>
          </div>
        </div>

        <div className="flex-1 flex flex-col gap-4">
          <div className="flex-1 overflow-y-auto flex flex-col gap-4">
            <div className="relative h-64 md:h-[500px] border border-primary/30 bg-black clip-tech-br">
              <img
                src={image}
                className={`w-full h-full object-cover object-[center_25%] transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-60 scale-100'} grayscale`}
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
                <EditableField
                  value={editableData?.name || ''}
                  label="Nombre"
                  variant="primary"
                  onChange={handleNameChange}
                  className="font-bold text-lg"
                  disabled={!editableData}
                />
              </div>
            </div>

            {editableData?.description && (
              <div className="bg-surface-dark/50 border border-primary/20 p-3">
                <EditableField
                  value={editableData.description}
                  label="Description"
                  type="textarea"
                  rows={2}
                  variant="primary"
                  onChange={handleDescriptionChange}
                  disabled={!editableData}
                />
              </div>
            )}

            <EditableStatsPanel
              stats={editableData?.stats || null}
              onStatsChange={handleStatsChange}
              variant="primary"
              maxColumns={3}
              showProgressBar={true}
              maxProgressValue={100}
              fieldDefinitions={fieldDefinitions}
              disabled={!editableData}
            />
          </div>

          <TerminalLog logs={logs} maxLogs={6} className="h-20 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default VehicleGeneratorPage;
