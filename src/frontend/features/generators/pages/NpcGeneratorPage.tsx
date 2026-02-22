/**
 * NPC Generator Page
 * AI-powered NPC generation with cyberpunk terminal aesthetics
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback, useEffect } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, TerminalLog, EditableField, EditableStatsPanel } from '@shared/components/ui';
import { aiService, entityService, entityTemplateService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useEntityGeneration } from '@core/hooks';
import type { NpcData } from '@core/types';

const UNKNOWN_NPC_IMAGE = "https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop";

const OCCUPATION_OPTIONS = [
  { value: '', label: 'Seleccionar Ocupacion...' },
  { value: 'merchant', label: 'Comerciante' },
  { value: 'informant', label: 'Informante' },
  { value: 'scientist', label: 'Cientifico' },
  { value: 'pilot', label: 'Piloto' },
  { value: 'mechanic', label: 'Mecanico' },
  { value: 'bartender', label: 'Cantinero' },
  { value: 'diplomat', label: 'Diplomatico' },
  { value: 'smuggler', label: 'Contrabandista' },
];

const PERSONALITY_OPTIONS = [
  { value: '', label: 'Seleccionar Personalidad...' },
  { value: 'friendly', label: 'Amigable' },
  { value: 'suspicious', label: 'Sospechoso' },
  { value: 'mysterious', label: 'Misterioso' },
  { value: 'greedy', label: 'Codicioso' },
  { value: 'helpful', label: 'Servicial' },
  { value: 'aggressive', label: 'Agresivo' },
  { value: 'cunning', label: 'Astuto' },
  { value: 'naive', label: 'Ingenuo' },
];

const SPECIES_OPTIONS = [
  { value: 'human', label: 'Humano' },
  { value: 'android', label: 'Androide' },
  { value: 'alien-humanoid', label: 'Alien Humanoide' },
  { value: 'cyber-enhanced', label: 'Cyber-Aumentado' },
  { value: 'clone', label: 'Clon' },
];

interface NpcGeneratorPageProps {
  onBack: () => void;
}

export const NpcGeneratorPage: React.FC<NpcGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [form, setForm] = useState({
    species: 'human',
    occupation: '',
    personality: '',
    setting: 'space-station'
  });

  const generateNpc = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { species: string; occupation: string; personality: string; setting: string };
    const result = await aiService.generateNpc({
      gameSystemId: activeCampaign?.gameSystemId,
      species: formParams.species,
      occupation: formParams.occupation,
      personality: formParams.personality,
      setting: formParams.setting,
      generateImage
    });

    const data = parseJsonResponse<NpcData>(result.npcJson);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveNpc = useCallback(async (params: {
    name: string;
    description?: string;
    imageUrl?: string;
    attributes: Record<string, unknown>;
    metadata?: Record<string, unknown>;
    generationRequestId?: string;
  }) => {
    if (!activeCampaignId) return;
    
    const occupation = params.metadata?.occupation as string | undefined;

    await entityService.create(activeCampaignId, {
      entityType: 'actor',
      name: params.name,
      description: params.description,
      imageUrl: params.imageUrl,
      attributes: params.attributes,
      metadata: {
        generatedAt: new Date().toISOString(),
        generator: 'actor_generator',
        generationParams: {
          species: form.species,
          occupation: occupation || form.occupation,
          personality: form.personality,
          setting: form.setting
        }
      },
      generationRequestId: params.generationRequestId
    });
  }, [activeCampaignId, form.species, form.occupation, form.personality, form.setting]);

  const getFieldDefinitions = useCallback(async (gameSystemId: string) => {
    return entityTemplateService.getFieldDefinitions(gameSystemId, 'actor');
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
    addLog,
    generate,
    save,
    loadFieldDefinitions,
    setEditableData,
    setImageMode,
    setUploadedImageData
  } = useEntityGeneration<NpcData>({
    entityType: 'actor',
    placeholderImage: UNKNOWN_NPC_IMAGE,
    initialLogs: [
      '> Actor database initialized...',
      '> [SUCCESS] Social profiling module loaded.',
      '> Awaiting actor parameters...'
    ],
    maxLogs: 6,
    generateFn: generateNpc,
    saveFn: saveNpc,
    getFieldDefinitions,
    onSaveSuccess: () => {
      setTimeout(onBack, 1000);
    }
  });

  useEffect(() => {
    loadFieldDefinitions(activeCampaign?.gameSystemId);
  }, [activeCampaign?.gameSystemId, loadFieldDefinitions]);

  const handleGenerate = async () => {
    if (!form.occupation || !form.personality) {
      addLog('ERROR: PARAMETROS INCOMPLETOS');
      return;
    }
    await generate(form);
  };

  const handleSave = async () => {
    if (!editableData) return;
    await save(activeCampaignId || '', {
      name: editableData.name,
      description: editableData.description,
      attributes: { ...editableData.stats },
      metadata: {
        occupation: editableData.occupation
      }
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

  const handleOccupationChange = (value: string | number) => {
    if (editableData) {
      setEditableData({ ...editableData, occupation: String(value) });
    }
  };

  const handleDescriptionChange = (value: string | number) => {
    if (editableData) {
      setEditableData({ ...editableData, description: String(value) });
    }
  };

  return (
    <TerminalLayout
      title="SYNTH_ACTOR"
      subtitle="Generador de NPCs"
      icon="groups"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col md:flex-row gap-8 md:h-full font-mono">
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          <div className="space-y-6">
            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">fingerprint</span> Especie
              </label>
              <div className="grid grid-cols-3 gap-2">
                {SPECIES_OPTIONS.map((spec) => (
                  <button
                    key={spec.value}
                    onClick={() => setForm({ ...form, species: spec.value })}
                    className={`h-10 border font-mono text-xs uppercase transition-all ${form.species === spec.value
                      ? 'bg-primary text-black border-primary font-bold'
                      : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                      }`}
                  >
                    {spec.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">work</span> Ocupacion
              </label>
              <select
                value={form.occupation}
                onChange={(e) => setForm({ ...form, occupation: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {OCCUPATION_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">psychology</span> Personalidad
              </label>
              <select
                value={form.personality}
                onChange={(e) => setForm({ ...form, personality: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {PERSONALITY_OPTIONS.map(opt => (
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

          <div className="mt-auto pt-6 border-t border-primary/30 grid grid-cols-2 gap-4">
            <Button
              onClick={handleGenerate}
              disabled={isGenerating}
              variant="secondary"
              size="lg"
              isLoading={isGenerating}
              icon="person_add"
            >
              GENERAR
            </Button>
            <Button
              onClick={handleSave}
              disabled={!editableData || isSaving || isGenerating || !activeCampaignId}
              variant="primary"
              size="lg"
              isLoading={isSaving}
              icon="save"
            >
              GUARDAR
            </Button>
          </div>
        </div>

        <div className="flex-1 flex flex-col gap-4">
          <div className="flex-1 overflow-y-auto flex flex-col gap-4">
            <div className="relative w-full h-64 md:h-[500px] border border-primary/30 bg-black p-1 flex flex-col clip-tech-br group">
              <div className="relative flex-1 bg-black overflow-hidden flex items-center justify-center">
                <img
                  className={`w-full h-full object-cover object-[center_25%] transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-80 scale-100'} grayscale brightness-90`}
                  src={image}
                  alt="NPC Preview"
                />
                {isGenerating && (
                  <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 z-20">
                    <div className="w-1/2 h-1 bg-primary/20 relative overflow-hidden mb-2">
                      <div className="absolute inset-0 bg-primary animate-[scan_2s_linear_infinite]"></div>
                    </div>
                    <span className="text-primary text-[10px] animate-pulse">PROCESANDO_IDENTIDAD...</span>
                  </div>
                )}
                <div className="absolute inset-0 pointer-events-none border border-primary/5 opacity-30"></div>
              </div>
              <div className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t border-primary/40 backdrop-blur-sm transition-transform duration-500 ${editableData ? 'translate-y-0' : 'translate-y-full'}`}>
                <EditableField
                  value={editableData?.name || ''}
                  label="Nombre"
                  variant="primary"
                  onChange={handleNameChange}
                  disabled={!editableData}
                  className="font-bold"
                />
              </div>
              {!editableData && !isGenerating && (
                <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                  <span className="text-primary/20 text-[10px] tracking-[0.5em] uppercase font-bold">Sin Datos</span>
                </div>
              )}
            </div>

            {editableData && (
              <div className="space-y-2">
                {editableData.occupation && (
                  <div className="bg-surface-dark/50 border border-primary/20 p-2">
                    <EditableField
                      value={editableData.occupation}
                      label="Ocupacion"
                      variant="primary"
                      onChange={handleOccupationChange}
                      disabled={!editableData}
                    />
                  </div>
                )}
                {editableData.description && (
                  <div className="bg-surface-dark/50 border border-primary/20 p-2">
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
              </div>
            )}

            <EditableStatsPanel
              stats={editableData?.stats || null}
              onStatsChange={handleStatsChange}
              variant="primary"
              maxColumns={3}
              showProgressBar={true}
              maxProgressValue={10}
              fieldDefinitions={fieldDefinitions}
              disabled={!editableData}
            />
          </div>

          <TerminalLog logs={logs} maxLogs={6} className="h-24 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default NpcGeneratorPage;
