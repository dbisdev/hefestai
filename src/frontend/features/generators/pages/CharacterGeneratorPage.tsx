/**
 * Character Generator Page
 * AI-powered character generation with cyberpunk terminal aesthetics
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import {
  Button,
  ImageSourceSelector,
  TerminalLog,
  EditableField,
  EditableStatsPanel,
  GeneratorPortrait,
  GeneratorActionButtons,
} from '@shared/components/ui';
import { aiService, entityService, entityTemplateService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useEntityGeneration, useGeneratorForm, type SaveEntityParams } from '@core/hooks';
import type { CharacterData } from '@core/types';
import { TERMINAL_LOG_COMPACT } from '@core/config/constants';

const UNKNOWN_CHAR_IMAGE = "https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop";

const SPECIES_OPTIONS = [
  { value: '', label: 'Seleccionar Genotipo...' },
  { value: 'human', label: 'Humano' },
  { value: 'android', label: 'Sintético' },
  { value: 'cyber-enhanced', label: 'Cyber-Aumentado' },
];

const ROLE_OPTIONS = [
  { value: '', label: 'Seleccionar Función...' },
  { value: 'colonial marine', label: 'Colonial Marine' },
  { value: 'colonial marshal', label: 'Colonial Marshal' },
  { value: 'company agent', label: 'Company Agent' },
  { value: 'kid', label: 'Kid' },
  { value: 'medic', label: 'Medic' },
  { value: 'officer', label: 'Officer' },
  { value: 'pilot', label: 'Pilot' },
  { value: 'roughneck', label: 'Roughneck' },
  { value: 'scientist', label: 'Scientist' },
];

const MORPHOLOGY_OPTIONS = ['MASCULINE', 'FEMININE', 'NEUTRAL'] as const;

interface CharacterGeneratorPageProps {
  onBack: () => void;
}

export const CharacterGeneratorPage: React.FC<CharacterGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  type MorphologyType = typeof MORPHOLOGY_OPTIONS[number];

  const [form, setForm] = useState({
    species: '',
    role: '',
    morphology: 'NEUTRAL' as MorphologyType,
    attire: 'Techwear'
  });

  const generateCharacter = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { species: string; role: string; morphology: string; attire: string };
    const result = await aiService.generateCharacter({
      gameSystemId: activeCampaign?.gameSystemId,
      species: formParams.species,
      role: formParams.role,
      morphology: formParams.morphology,
      attire: formParams.attire,
      generateImage
    });

    const data = parseJsonResponse<CharacterData>(result.characterJson);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveCharacter = useCallback(async (params: SaveEntityParams) => {
    if (!activeCampaignId) return;

    await entityService.create(activeCampaignId, {
      entityType: 'character',
      name: params.name,
      description: params.description,
      imageUrl: params.imageUrl,
      attributes: params.attributes,
      metadata: {
        ...params.metadata,
        generatedAt: new Date().toISOString(),
        generator: 'character_synth_v2',
        generationParams: {
          species: form.species.toUpperCase(),
          role: form.role.toUpperCase(),
          morphology: form.morphology
        }
      },
      generationRequestId: params.generationRequestId
    });
  }, [activeCampaignId, form.species, form.role, form.morphology]);

  const getFieldDefinitions = useCallback(async (gameSystemId: string) => {
    return entityTemplateService.getFieldDefinitions(gameSystemId, 'character');
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
  } = useEntityGeneration<CharacterData>({
    entityType: 'character',
    placeholderImage: UNKNOWN_CHAR_IMAGE,
    initialLogs: [
      '> System initialization sequence started...',
      '> [SUCCESS] Neural link established.',
      '> Awaiting user input parameters...'
    ],
    maxLogs: TERMINAL_LOG_COMPACT,
    generateFn: generateCharacter,
    saveFn: saveCharacter,
    getFieldDefinitions,
    onSaveSuccess: () => {
      setTimeout(onBack, 1000);
    }
  });

  const { handleNameChange, handleDescriptionChange, handleStatsChange } =
    useGeneratorForm<CharacterData>({
      editableData,
      setEditableData,
      loadFieldDefinitions,
      gameSystemId: activeCampaign?.gameSystemId,
    });

  const handleGenerate = async () => {
    if (!form.species || !form.role) {
      addLog('ERROR: PARAMETERS MISSING');
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
    });
  };

  return (
    <TerminalLayout
      title="SYNTH_PERSONAJE"
      subtitle="Sintetizador Biométrico"
      icon="face"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col md:flex-row gap-8 md:h-full font-mono">
        {/* Left panel — form controls */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          {!activeCampaignId && (
            <div className="border border-yellow-500/50 bg-yellow-500/10 p-3 text-[10px] text-yellow-500 uppercase">
              <span className="material-icons text-sm mr-1 align-middle">warning</span>
              Selecciona una campaña para guardar entidades
            </div>
          )}

          <div className="space-y-6">
            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">fingerprint</span> Genotype
              </label>
              <select
                value={form.species}
                onChange={(e) => setForm({ ...form, species: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {SPECIES_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">badge</span> Role
              </label>
              <select
                value={form.role}
                onChange={(e) => setForm({ ...form, role: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {ROLE_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">wc</span> Morphology
              </label>
              <div className="grid grid-cols-3 gap-3">
                {MORPHOLOGY_OPTIONS.map((morph) => (
                  <button
                    key={morph}
                    onClick={() => setForm({ ...form, morphology: morph })}
                    className={`h-10 border font-mono text-sm uppercase transition-all ${form.morphology === morph
                      ? 'bg-primary text-black border-primary font-bold'
                      : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                      }`}
                  >
                    {morph}
                  </button>
                ))}
              </div>
            </div>

            <ImageSourceSelector
              mode={imageMode}
              onModeChange={setImageMode}
              onImageUpload={setUploadedImageData}
              uploadedImage={uploadedImageData}
              disabled={isGenerating}
            />
          </div>

          <GeneratorActionButtons
            onGenerate={handleGenerate}
            onSave={handleSave}
            isGenerating={isGenerating}
            isSaving={isSaving}
            hasData={!!editableData}
            hasCampaign={!!activeCampaignId}
            generateIcon="blur_on"
          />
        </div>

        {/* Right panel — preview */}
        <div className="flex-1 flex flex-col gap-4">
          <div className="flex-1 overflow-y-auto flex flex-col gap-4">
            <GeneratorPortrait
              src={image}
              alt="Character Preview"
              name={editableData?.name || ''}
              isGenerating={isGenerating}
              hasData={!!editableData}
              onNameChange={handleNameChange}
              generatingLabel="RECONSTRUYENDO_PIXELES..."
            />

            {editableData?.description && (
              <div className="bg-surface-dark/50 border border-primary/20 p-3">
                <EditableField
                  value={editableData.description}
                  label="Description"
                  type="textarea"
                  rows={3}
                  variant="primary"
                  onChange={handleDescriptionChange}
                  disabled={!editableData}
                  className="text-sm"
                />
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

          <TerminalLog logs={logs} maxLogs={TERMINAL_LOG_COMPACT} className="h-24 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default CharacterGeneratorPage;
