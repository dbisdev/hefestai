/**
 * Encounter Generator Page
 * AI-powered encounter/combat scenario generation with cyberpunk terminal aesthetics
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, EditableField } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useEntityGeneration } from '@core/hooks';
import type { EncounterData } from '@core/types';

const ENCOUNTER_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1534796636912-3b95b3ab5986?q=80&w=400&auto=format&fit=crop";

const ENCOUNTER_TYPE_OPTIONS = [
  { value: '', label: 'Seleccionar Tipo...' },
  { value: 'combat', label: 'Combate - Enfrentamiento directo' },
  { value: 'ambush', label: 'Emboscada - Ataque sorpresa' },
  { value: 'negotiation', label: 'Negociacion - Conflicto social' },
  { value: 'chase', label: 'Persecucion - Escape o captura' },
  { value: 'stealth', label: 'Sigilo - Infiltracion furtiva' },
  { value: 'survival', label: 'Supervivencia - Amenaza ambiental' },
  { value: 'puzzle', label: 'Puzzle - Desafio mental' },
  { value: 'boss', label: 'Jefe - Enemigo poderoso' },
];

type DifficultyLevel = 'EASY' | 'MEDIUM' | 'HARD' | 'EXTREME';

const DIFFICULTY_OPTIONS: { value: DifficultyLevel; label: string; color: string }[] = [
  { value: 'EASY', label: 'Facil', color: 'text-green-400' },
  { value: 'MEDIUM', label: 'Medio', color: 'text-yellow-400' },
  { value: 'HARD', label: 'Dificil', color: 'text-orange-400' },
  { value: 'EXTREME', label: 'Extremo', color: 'text-red-400' },
];

const ENVIRONMENT_OPTIONS = [
  { value: 'corridor', label: 'Corredor Estrecho' },
  { value: 'open-area', label: 'Area Abierta' },
  { value: 'multi-level', label: 'Multinivel' },
  { value: 'hazardous', label: 'Zona Peligrosa' },
  { value: 'confined', label: 'Espacio Confinado' },
  { value: 'vehicle', label: 'Vehiculo/Nave' },
];

const ENEMY_COUNT_OPTIONS = [
  { value: 'solo', label: '1 Enemigo' },
  { value: 'pair', label: '2 Enemigos' },
  { value: 'squad', label: '3-5 Enemigos' },
  { value: 'horde', label: '6+ Enemigos' },
];

interface EncounterGeneratorPageProps {
  onBack: () => void;
}

export const EncounterGeneratorPage: React.FC<EncounterGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [form, setForm] = useState({
    encounterType: '',
    difficulty: 'MEDIUM' as DifficultyLevel,
    environment: 'open-area',
    enemyCount: 'squad'
  });

  const generateEncounter = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { encounterType: string; difficulty: string; environment: string; enemyCount: string };
    const result = await aiService.generateEncounter({
      gameSystemId: activeCampaign?.gameSystemId,
      encounterType: formParams.encounterType,
      difficulty: formParams.difficulty,
      environment: formParams.environment,
      enemyCount: formParams.enemyCount,
      generateImage
    });

    const data = parseJsonResponse<EncounterData>(result.encounterJson);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveEncounter = useCallback(async (params: {
    name: string;
    description?: string;
    imageUrl?: string;
    attributes: Record<string, unknown>;
    metadata?: Record<string, unknown>;
    generationRequestId?: string;
  }) => {
    if (!activeCampaignId) return;

    await entityService.create(activeCampaignId, {
      entityType: 'encounter',
      name: params.name,
      description: params.description,
      imageUrl: params.imageUrl,
      attributes: params.attributes,
      metadata: {
        generatedAt: new Date().toISOString(),
        generator: 'encounter_generator'
      },
      generationRequestId: params.generationRequestId
    });
  }, [activeCampaignId]);

  const {
    isGenerating,
    isSaving,
    editableData,
    logs,
    addLog,
    imageMode,
    uploadedImageData,
    generate,
    save,
    setEditableData,
    setImageMode,
    setUploadedImageData
  } = useEntityGeneration<EncounterData>({
    entityType: 'encounter',
    placeholderImage: ENCOUNTER_PLACEHOLDER_IMAGE,
    initialLogs: [
      '> Tactical simulation online...',
      '> [SUCCESS] Combat analyzer loaded.',
      '> Awaiting encounter parameters...'
    ],
    maxLogs: 6,
    generateFn: generateEncounter,
    saveFn: saveEncounter,
    onSaveSuccess: () => {
      setTimeout(onBack, 1000);
    }
  });

  const handleGenerate = async () => {
    if (!form.encounterType) {
      addLog('ERROR: TIPO DE ENCUENTRO NO ESPECIFICADO');
      return;
    }
    await generate(form);
  };

  const handleSave = async () => {
    if (!editableData) return;
    await save(activeCampaignId || '', {
      name: editableData.name,
      description: editableData.description,
      attributes: {
        encounterType: form.encounterType,
        difficulty: editableData.stats?.difficulty ?? form.difficulty,
        environment: editableData.stats?.environment ?? form.environment,
        enemyCount: form.enemyCount,
        participants: editableData.stats?.participants,
        loot: editableData.stats?.loot
      },
    });
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

  const handleStatsChange = (key: string, value: string | number) => {
    if (editableData) {
      setEditableData({
        ...editableData,
        stats: { ...editableData.stats, [key]: value }
      });
    }
  };

  const getDifficultyInfo = () => {
    return DIFFICULTY_OPTIONS.find(d => d.value === form.difficulty) || DIFFICULTY_OPTIONS[1];
  };

  return (
    <TerminalLayout
      title="SYNTH_ENCUENTRO"
      subtitle="Generador de Encuentros Tacticos"
      icon="pest_control"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col lg:flex-row gap-8 h-full font-mono">
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          <div className="space-y-6">
            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">pest_control</span> Tipo de Encuentro
              </label>
              <select
                value={form.encounterType}
                onChange={(e) => setForm({ ...form, encounterType: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {ENCOUNTER_TYPE_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">trending_up</span> Dificultad
              </label>
              <div className="grid grid-cols-4 gap-2">
                {DIFFICULTY_OPTIONS.map((diff) => (
                  <button
                    key={diff.value}
                    onClick={() => setForm({ ...form, difficulty: diff.value })}
                    className={`h-10 border font-mono text-sm uppercase transition-all ${form.difficulty === diff.value
                      ? `bg-primary/20 border-primary font-bold ${diff.color}`
                      : 'border-primary/30 text-white/60 bg-surface-dark hover:border-primary'
                      }`}
                  >
                    {diff.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">terrain</span> Terreno
              </label>
              <div className="grid grid-cols-3 gap-2">
                {ENVIRONMENT_OPTIONS.map((env) => (
                  <button
                    key={env.value}
                    onClick={() => setForm({ ...form, environment: env.value })}
                    className={`h-10 border font-mono text-xs uppercase transition-all ${form.environment === env.value
                      ? 'bg-primary text-black border-primary font-bold'
                      : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                      }`}
                  >
                    {env.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">groups</span> Cantidad de Enemigos
              </label>
              <div className="grid grid-cols-4 gap-2">
                {ENEMY_COUNT_OPTIONS.map((count) => (
                  <button
                    key={count.value}
                    onClick={() => setForm({ ...form, enemyCount: count.value })}
                    className={`h-10 border font-mono text-xs uppercase transition-all ${form.enemyCount === count.value
                      ? 'bg-primary text-black border-primary font-bold'
                      : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                      }`}
                  >
                    {count.label}
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

          <div className="mt-auto pt-6 border-t border-primary/30 grid grid-cols-2 gap-4">
            <Button
              onClick={handleGenerate}
              disabled={isGenerating}
              variant="secondary"
              size="lg"
              isLoading={isGenerating}
              icon="play_circle"
            >
              SIMULAR
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
            <div className={`border border-primary/30 bg-black p-4 transition-all ${editableData ? 'border-primary' : ''}`}>
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <span className="material-icons text-primary text-xl">swords</span>
                  <span className="text-[10px] text-primary/60 uppercase tracking-widest">Simulacion Tactica</span>
                </div>
                {editableData && (
                  <div className={`px-2 py-1 border text-[8px] font-bold uppercase ${getDifficultyInfo().color} border-current`}>
                    {getDifficultyInfo().label}
                  </div>
                )}
              </div>

              {editableData ? (
                <div className="space-y-3">
                  <EditableField
                    value={editableData.name}
                    label="Nombre del Encuentro"
                    variant="primary"
                    onChange={handleNameChange}
                    className="text-xl font-display uppercase text-glow"
                    disabled={!editableData}
                  />
                  <div className="h-0.5 bg-gradient-to-r from-primary via-primary/20 to-transparent"></div>
                </div>
              ) : (
                <div className="h-16 flex items-center justify-center">
                  <span className="text-primary/20 text-[10px] tracking-[0.5em] uppercase font-bold">
                    {isGenerating ? 'SIMULANDO...' : 'Sin Encuentro'}
                  </span>
                </div>
              )}
            </div>

            {editableData && (
              <>
                {editableData.description && (
                  <div className="bg-surface-dark/50 border border-primary/20 p-4">
                    <p className="text-[9px] text-primary/40 uppercase tracking-widest mb-2 flex items-center gap-1">
                      <span className="material-icons text-xs">description</span> Descripcion
                    </p>
                    <EditableField
                      value={editableData.description}
                      type="textarea"
                      rows={3}
                      variant="primary"
                      onChange={handleDescriptionChange}
                      disabled={!editableData}
                    />
                  </div>
                )}

                {editableData.stats?.environment && (
                  <div className="bg-black/60 border border-blue-500/30 p-4">
                    <p className="text-[9px] text-blue-500/60 uppercase tracking-widest mb-2 flex items-center gap-1">
                      <span className="material-icons text-xs">terrain</span> Entorno
                    </p>
                    <EditableField
                      value={editableData.stats.environment}
                      variant="primary"
                      onChange={(val) => handleStatsChange('environment', val)}
                      disabled={!editableData}
                    />
                  </div>
                )}

                {editableData.stats?.participants && editableData.stats.participants.length > 0 && (
                  <div className="bg-surface-dark border border-danger/20 p-4">
                    <p className="text-[9px] text-danger/60 uppercase tracking-widest mb-2 flex items-center gap-1">
                      <span className="material-icons text-xs">groups</span> Participantes
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {editableData.stats.participants.map((participant, idx) => {
                        const displayText = typeof participant === 'string'
                          ? participant
                          : `${participant.type ?? 'Unknown'}${participant.count ? ` x${participant.count}` : ''}`;
                        return (
                          <span key={idx} className="px-2 py-1 bg-danger/10 border border-danger/30 text-[9px] text-danger/80">
                            {displayText}
                          </span>
                        );
                      })}
                    </div>
                  </div>
                )}

                <div className="grid grid-cols-2 gap-3">
                  {editableData.stats?.loot && (
                    <div className="bg-black/60 border border-yellow-500/30 p-3">
                      <p className="text-[8px] text-yellow-500/60 uppercase tracking-widest mb-1">Botin Potencial</p>
                      <EditableField
                        value={editableData.stats.loot}
                        variant="warning"
                        onChange={(val) => handleStatsChange('loot', val)}
                        disabled={!editableData}
                      />
                    </div>
                  )}
                  {editableData.stats?.difficulty && (
                    <div className="bg-surface-dark border border-primary/20 p-3">
                      <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Dificultad IA</p>
                      <EditableField
                        value={editableData.stats.difficulty}
                        variant="primary"
                        onChange={(val) => handleStatsChange('difficulty', val)}
                        disabled={!editableData}
                      />
                    </div>
                  )}
                </div>
              </>
            )}
          </div>

          <div className="h-24 bg-black/80 border border-primary/20 p-3 text-[10px] text-primary/80 overflow-y-auto font-mono scrollbar-hide shrink-0">
            {logs.map((log, i) => <p key={i} className={i === logs.length - 1 ? "text-primary font-bold" : "opacity-60"}>{log}</p>)}
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};

export default EncounterGeneratorPage;
