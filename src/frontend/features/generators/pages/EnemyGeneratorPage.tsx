/**
 * Enemy Generator Page
 * AI-powered enemy/creature generation with cyberpunk terminal aesthetics
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import {
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
import type { EnemyData } from '@core/types';
import { TERMINAL_LOG_COMPACT } from '@core/config/constants';

const UNKNOWN_ENEMY_IMAGE = "https://images.unsplash.com/photo-1509558273944-9ea880029528?q=80&w=400&auto=format&fit=crop";

const SPECIES_OPTIONS = [
  { value: '', label: 'Seleccionar Especie...' },
  { value: 'alien-beast', label: 'Bestia Alien' },
  { value: 'xenomorph', label: 'Xenomorfo' },
  { value: 'android-rogue', label: 'Androide Rebelde' },
  { value: 'mutant', label: 'Mutante' },
  { value: 'cyborg-hunter', label: 'Cyborg Cazador' },
  { value: 'parasite', label: 'Parasito' },
  { value: 'hive-mind', label: 'Mente Colmena' },
  { value: 'void-entity', label: 'Entidad del Vacio' },
];

const THREAT_LEVEL_OPTIONS = [
  { value: 'minor', label: 'Menor', color: 'text-green-400' },
  { value: 'moderate', label: 'Moderado', color: 'text-yellow-400' },
  { value: 'dangerous', label: 'Peligroso', color: 'text-orange-400' },
  { value: 'lethal', label: 'Letal', color: 'text-red-400' },
  { value: 'apocalyptic', label: 'Apocaliptico', color: 'text-purple-400' },
];

const BEHAVIOR_OPTIONS = [
  { value: '', label: 'Seleccionar Comportamiento...' },
  { value: 'aggressive', label: 'Agresivo - Ataca a la vista' },
  { value: 'territorial', label: 'Territorial - Defiende zona' },
  { value: 'predatory', label: 'Depredador - Acecha y embosca' },
  { value: 'swarm', label: 'Enjambre - Ataca en grupo' },
  { value: 'intelligent', label: 'Inteligente - Tacticas avanzadas' },
  { value: 'berserker', label: 'Berserker - Furia descontrolada' },
];

interface EnemyGeneratorPageProps {
  onBack: () => void;
}

export const EnemyGeneratorPage: React.FC<EnemyGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [form, setForm] = useState({
    species: '',
    threatLevel: 'moderate',
    behavior: '',
    environment: 'space-station'
  });

  const generateEnemy = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { species: string; threatLevel: string; behavior: string; environment: string };
    const result = await aiService.generateEnemy({
      gameSystemId: activeCampaign?.gameSystemId,
      species: formParams.species,
      threatLevel: formParams.threatLevel,
      behavior: formParams.behavior,
      environment: formParams.environment,
      generateImage
    });

    const data = parseJsonResponse<EnemyData>(result.enemyJson);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveEnemy = useCallback(async (params: SaveEntityParams) => {
    if (!activeCampaignId) return;

    const species = params.metadata?.species as string | undefined;
    const weakness = params.metadata?.weakness as string | undefined;
    const abilities = params.description;

    await entityService.create(activeCampaignId, {
      entityType: 'monster',
      name: params.name,
      description: abilities,
      imageUrl: params.imageUrl,
      attributes: params.attributes,
      metadata: {
        generatedAt: new Date().toISOString(),
        generator: 'monster_generator',
        generationParams: {
          species: species || form.species,
          threatLevel: form.threatLevel,
          behavior: form.behavior,
          environment: form.environment,
          weakness: weakness
        }
      },
      generationRequestId: params.generationRequestId
    });
  }, [activeCampaignId, form.species, form.threatLevel, form.behavior, form.environment]);

  const getFieldDefinitions = useCallback(async (gameSystemId: string) => {
    return entityTemplateService.getFieldDefinitions(gameSystemId, 'monster');
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
  } = useEntityGeneration<EnemyData>({
    entityType: 'monster',
    placeholderImage: UNKNOWN_ENEMY_IMAGE,
    initialLogs: [
      '> Threat analysis system online...',
      '> [WARNING] Hostile database accessed.',
      '> Awaiting threat parameters...'
    ],
    maxLogs: TERMINAL_LOG_COMPACT,
    generateFn: generateEnemy,
    saveFn: saveEnemy,
    getFieldDefinitions,
    onSaveSuccess: () => {
      setTimeout(onBack, 1000);
    }
  });

  const { handleNameChange, handleStatsChange } =
    useGeneratorForm<EnemyData>({
      editableData,
      setEditableData,
      loadFieldDefinitions,
      gameSystemId: activeCampaign?.gameSystemId,
    });

  /** Enemy-specific: updates the species field */
  const handleSpeciesChange = useCallback(
    (value: string | number) => {
      if (editableData) setEditableData({ ...editableData, species: String(value) });
    },
    [editableData, setEditableData],
  );

  /** Enemy-specific: updates the abilities field */
  const handleAbilitiesChange = useCallback(
    (value: string | number) => {
      if (editableData) setEditableData({ ...editableData, abilities: String(value) });
    },
    [editableData, setEditableData],
  );

  const getThreatColor = () =>
    THREAT_LEVEL_OPTIONS.find(t => t.value === form.threatLevel)?.color || 'text-primary';

  const handleGenerate = async () => {
    if (!form.species || !form.behavior) {
      addLog('ERROR: PARAMETROS DE AMENAZA INCOMPLETOS');
      return;
    }
    await generate(form);
  };

  const handleSave = async () => {
    if (!editableData) return;
    await save(activeCampaignId || '', {
      name: editableData.name,
      description: editableData.abilities,
      attributes: { ...editableData.stats },
      metadata: {
        species: editableData.species,
        weakness: editableData.weakness
      }
    });
  };

  return (
    <TerminalLayout
      title="SYNTH_AMENAZA"
      subtitle="Analizador de Hostiles"
      icon="pest_control"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col md:flex-row gap-8 md:h-full font-mono">
        {/* Left panel — form controls */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          <div className="space-y-6">
            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">bug_report</span> Especie Hostil
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
                <span className="material-icons text-sm">warning</span> Nivel de Amenaza
              </label>
              <div className="grid grid-cols-5 gap-1">
                {THREAT_LEVEL_OPTIONS.map((threat) => (
                  <button
                    key={threat.value}
                    onClick={() => setForm({ ...form, threatLevel: threat.value })}
                    className={`h-12 border font-mono text-xs uppercase transition-all flex flex-col items-center justify-center ${form.threatLevel === threat.value
                      ? `bg-primary/20 border-primary font-bold ${threat.color}`
                      : 'border-primary/30 text-white/60 bg-surface-dark hover:border-primary'
                      }`}
                  >
                    <span className="material-icons text-sm mb-0.5">
                      {threat.value === 'minor' && 'sentiment_satisfied'}
                      {threat.value === 'moderate' && 'sentiment_neutral'}
                      {threat.value === 'dangerous' && 'sentiment_dissatisfied'}
                      {threat.value === 'lethal' && 'sentiment_very_dissatisfied'}
                      {threat.value === 'apocalyptic' && 'whatshot'}
                    </span>
                    {threat.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">psychology</span> Comportamiento
              </label>
              <select
                value={form.behavior}
                onChange={(e) => setForm({ ...form, behavior: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {BEHAVIOR_OPTIONS.map(opt => (
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

          <GeneratorActionButtons
            onGenerate={handleGenerate}
            onSave={handleSave}
            isGenerating={isGenerating}
            isSaving={isSaving}
            hasData={!!editableData}
            hasCampaign={!!activeCampaignId}
            generateLabel="ANALIZAR"
            generateIcon="pest_control"
          />
        </div>

        {/* Right panel — preview */}
        <div className="flex-1 flex flex-col gap-4">
          <div className="flex-1 overflow-y-auto flex flex-col gap-4">
            {/* Enemy portrait with danger variant and threat badge */}
            <div className="relative w-full h-64 md:h-[500px] border border-danger/30 bg-black p-1 flex flex-col clip-tech-br group">
              <div className="relative flex-1 bg-black overflow-hidden flex items-center justify-center">
                <img
                  className={`w-full h-full object-cover object-[center_25%] transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-80 scale-100'} grayscale brightness-75 contrast-125`}
                  src={image}
                  alt="Enemy Preview"
                />
                {isGenerating && (
                  <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 z-20">
                    <div className="w-1/2 h-1 bg-danger/20 relative overflow-hidden mb-2">
                      <div className="absolute inset-0 bg-danger animate-[scan_2s_linear_infinite]" />
                    </div>
                    <span className="text-danger text-[10px] animate-pulse">ESCANEANDO_AMENAZA...</span>
                  </div>
                )}
                <div className="absolute inset-0 pointer-events-none border border-danger/5 opacity-30" />
                {editableData && (
                  <div className={`absolute top-2 right-2 px-2 py-1 bg-black/80 border border-current text-[8px] font-bold uppercase ${getThreatColor()}`}>
                    {form.threatLevel}
                  </div>
                )}
              </div>
              <div className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t border-danger/40 backdrop-blur-sm transition-transform duration-500 ${editableData ? 'translate-y-0' : 'translate-y-full'}`}>
                <EditableField
                  value={editableData?.name || ''}
                  label="Nombre"
                  variant="danger"
                  onChange={handleNameChange}
                  disabled={!editableData}
                  className="font-bold"
                />
              </div>
              {!editableData && !isGenerating && (
                <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                  <span className="text-danger/20 text-[10px] tracking-[0.5em] uppercase font-bold">Sin Datos</span>
                </div>
              )}
            </div>

            {editableData && (
              <div className="space-y-2">
                {editableData.species && (
                  <div className="bg-surface-dark/50 border border-danger/20 p-2">
                    <EditableField
                      value={editableData.species}
                      label="Especie"
                      variant="danger"
                      onChange={handleSpeciesChange}
                      disabled={!editableData}
                    />
                  </div>
                )}
                {editableData.abilities && (
                  <div className="bg-surface-dark/50 border border-danger/20 p-2">
                    <EditableField
                      value={editableData.abilities}
                      label="Habilidades"
                      type="textarea"
                      rows={2}
                      variant="danger"
                      onChange={handleAbilitiesChange}
                      disabled={!editableData}
                    />
                  </div>
                )}
              </div>
            )}

            <EditableStatsPanel
              stats={editableData?.stats || null}
              onStatsChange={handleStatsChange}
              variant="danger"
              maxColumns={4}
              showProgressBar={true}
              maxProgressValue={100}
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

export default EnemyGeneratorPage;
