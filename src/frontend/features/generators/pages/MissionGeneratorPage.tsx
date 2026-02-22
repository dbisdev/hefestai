/**
 * Mission Generator Page
 * AI-powered mission/quest generation with cyberpunk terminal aesthetics
 * Uses useEntityGeneration hook for generation orchestration
 */

import React, { useState, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, TerminalLog, EditableField } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useEntityGeneration } from '@core/hooks';
import type { MissionData } from '@core/types';

const MISSION_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?q=80&w=400&auto=format&fit=crop";

const MISSION_TYPE_OPTIONS = [
  { value: '', label: 'Seleccionar Tipo de Mision...' },
  { value: 'extraction', label: 'Extraccion - Rescate de objetivo' },
  { value: 'infiltration', label: 'Infiltracion - Entrada encubierta' },
  { value: 'assassination', label: 'Eliminacion - Objetivo hostil' },
  { value: 'retrieval', label: 'Recuperacion - Obtener objeto' },
  { value: 'escort', label: 'Escolta - Proteger VIP' },
  { value: 'sabotage', label: 'Sabotaje - Destruir objetivo' },
  { value: 'reconnaissance', label: 'Reconocimiento - Obtener intel' },
  { value: 'defense', label: 'Defensa - Proteger posicion' },
];

type DifficultyLevel = 'EASY' | 'MEDIUM' | 'HARD' | 'EXTREME';

const DIFFICULTY_OPTIONS: { value: DifficultyLevel; label: string; color: string; icon: string }[] = [
  { value: 'EASY', label: 'Facil', color: 'text-green-400', icon: 'shield' },
  { value: 'MEDIUM', label: 'Medio', color: 'text-yellow-400', icon: 'security' },
  { value: 'HARD', label: 'Dificil', color: 'text-orange-400', icon: 'gpp_maybe' },
  { value: 'EXTREME', label: 'Extremo', color: 'text-red-400', icon: 'dangerous' },
];

const ENVIRONMENT_OPTIONS = [
  { value: 'space-station', label: 'Estacion Espacial' },
  { value: 'planet-surface', label: 'Superficie Planetaria' },
  { value: 'asteroid-field', label: 'Campo de Asteroides' },
  { value: 'derelict-ship', label: 'Nave Abandonada' },
  { value: 'megacity', label: 'Megaciudad' },
  { value: 'underground', label: 'Instalacion Subterranea' },
];

interface MissionGeneratorPageProps {
  onBack: () => void;
}

export const MissionGeneratorPage: React.FC<MissionGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [form, setForm] = useState({
    missionType: '',
    difficulty: 'MEDIUM' as DifficultyLevel,
    environment: 'space-station',
    factionInvolved: 'corporate'
  });

  const generateMission = useCallback(async (params: unknown, generateImage: boolean) => {
    const formParams = params as { missionType: string; difficulty: string; environment: string; factionInvolved: string };
    const result = await aiService.generateMission({
      gameSystemId: activeCampaign?.gameSystemId,
      missionType: formParams.missionType,
      difficulty: formParams.difficulty,
      environment: formParams.environment,
      factionInvolved: formParams.factionInvolved,
      generateImage
    });

    const data = parseJsonResponse<MissionData>(result.missionJson);
    return {
      data,
      imageBase64: result.imageBase64,
      imageUrl: result.imageUrl,
      generationRequestId: result.generationRequestId
    };
  }, [activeCampaign?.gameSystemId]);

  const saveMission = useCallback(async (params: {
    name: string;
    description?: string;
    imageUrl?: string;
    attributes: Record<string, unknown>;
    metadata?: Record<string, unknown>;
    generationRequestId?: string;
  }) => {
    if (!activeCampaignId) return;

    await entityService.create(activeCampaignId, {
      entityType: 'mission',
      name: params.name,
      description: params.description,
      imageUrl: params.imageUrl,
      attributes: params.attributes,
      metadata: {
        generatedAt: new Date().toISOString(),
        generator: 'mission_briefing_v1'
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
  } = useEntityGeneration<MissionData>({
    entityType: 'mission',
    placeholderImage: MISSION_PLACEHOLDER_IMAGE,
    initialLogs: [
      '> Mission briefing system online...',
      '> [SUCCESS] Tactical database connected.',
      '> Awaiting mission parameters...'
    ],
    maxLogs: 6,
    generateFn: generateMission,
    saveFn: saveMission,
    onSaveSuccess: () => {
      setTimeout(onBack, 1000);
    }
  });

  const handleGenerate = async () => {
    if (!form.missionType) {
      addLog('ERROR: TIPO DE MISION NO ESPECIFICADO');
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
        missionType: form.missionType.toUpperCase(),
        difficulty: editableData.stats?.difficulty ?? form.difficulty,
        environment: form.environment,
        objective: editableData.stats?.objective,
        rewards: editableData.stats?.rewards,
        estimatedDuration: editableData.stats?.estimatedDuration
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
      title="SYNTH_MISION"
      subtitle="Generador de Misiones Tacticas"
      icon="assignment"
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col lg:flex-row gap-8 h-full font-mono">
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          {!activeCampaignId && (
            <div className="border border-yellow-500/50 bg-yellow-500/10 p-3 text-xs text-yellow-500 uppercase">
              <span className="material-icons text-sm mr-1 align-middle">warning</span>
              Selecciona una campana para guardar entidades
            </div>
          )}

          <div className="space-y-6">
            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">assignment</span> Tipo de Mision
              </label>
              <select
                value={form.missionType}
                onChange={(e) => setForm({ ...form, missionType: e.target.value })}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {MISSION_TYPE_OPTIONS.map(opt => (
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
                    className={`h-14 border font-mono text-xs uppercase transition-all flex flex-col items-center justify-center ${form.difficulty === diff.value
                      ? `bg-primary/20 border-primary font-bold ${diff.color}`
                      : 'border-primary/30 text-white/60 bg-surface-dark hover:border-primary'
                      }`}
                  >
                    <span className="material-icons text-lg mb-0.5">{diff.icon}</span>
                    {diff.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-primary text-xs uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">location_on</span> Entorno
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
              icon="assignment_add"
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
            <div className={`border border-primary/30 bg-black p-4 transition-all ${editableData ? 'border-primary' : ''}`}>
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <span className="material-icons text-primary text-xl">assignment</span>
                  <span className="text-[10px] text-primary/60 uppercase tracking-widest">Briefing Tactico</span>
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
                    label="Nombre de la Mision"
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
                    {isGenerating ? 'GENERANDO...' : 'Sin Mision'}
                  </span>
                </div>
              )}
            </div>

            {editableData && (
              <>
                {editableData.description && (
                  <div className="bg-surface-dark/50 border border-primary/20 p-4">
                    <p className="text-[9px] text-primary/40 uppercase tracking-widest mb-2 flex items-center gap-1">
                      <span className="material-icons text-xs">description</span> Briefing
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

                {editableData.stats?.objective && (
                  <div className="bg-black/60 border border-yellow-500/30 p-4">
                    <p className="text-[9px] text-yellow-500/60 uppercase tracking-widest mb-2 flex items-center gap-1">
                      <span className="material-icons text-xs">flag</span> Objetivo Principal
                    </p>
                    <EditableField
                      value={editableData.stats.objective}
                      type="textarea"
                      rows={2}
                      variant="warning"
                      onChange={(val) => handleStatsChange('objective', val)}
                      disabled={!editableData}
                    />
                  </div>
                )}

                <div className="grid grid-cols-2 gap-3">
                  {editableData.stats?.rewards && (
                    <div className="bg-surface-dark border border-primary/20 p-3">
                      <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Recompensa</p>
                      <EditableField
                        value={editableData.stats.rewards}
                        variant="primary"
                        onChange={(val) => handleStatsChange('rewards', val)}
                        disabled={!editableData}
                      />
                    </div>
                  )}
                  {editableData.stats?.estimatedDuration && (
                    <div className="bg-surface-dark border border-primary/20 p-3">
                      <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Duracion Est.</p>
                      <EditableField
                        value={editableData.stats.estimatedDuration}
                        variant="primary"
                        onChange={(val) => handleStatsChange('estimatedDuration', val)}
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

          <TerminalLog logs={logs} maxLogs={6} className="h-24 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default MissionGeneratorPage;
