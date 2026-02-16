/**
 * Encounter Generator Page
 * AI-powered encounter/combat scenario generation with cyberpunk terminal aesthetics
 * Creates combat encounters, random events, and tactical scenarios
 */

import React, { useState } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { EncounterData } from '@core/types';
import { Screen } from '@core/types';

interface EncounterGeneratorPageProps {
  onBack: () => void;
  onNavigate?: (screen: Screen) => void;
  onLogout?: () => void;
}

/** Placeholder image for encounters without generated images */
const ENCOUNTER_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1534796636912-3b95b3ab5986?q=80&w=400&auto=format&fit=crop";

/**
 * Encounter type options
 */
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

/**
 * Difficulty level type for form state
 */
type DifficultyLevel = 'EASY' | 'MEDIUM' | 'HARD' | 'EXTREME';

/**
 * Difficulty level options
 */
const DIFFICULTY_OPTIONS: { value: DifficultyLevel; label: string; color: string }[] = [
  { value: 'EASY', label: 'Facil', color: 'text-green-400' },
  { value: 'MEDIUM', label: 'Medio', color: 'text-yellow-400' },
  { value: 'HARD', label: 'Dificil', color: 'text-orange-400' },
  { value: 'EXTREME', label: 'Extremo', color: 'text-red-400' },
];

/**
 * Environment options for encounters
 */
const ENVIRONMENT_OPTIONS = [
  { value: 'corridor', label: 'Corredor Estrecho' },
  { value: 'open-area', label: 'Area Abierta' },
  { value: 'multi-level', label: 'Multinivel' },
  { value: 'hazardous', label: 'Zona Peligrosa' },
  { value: 'confined', label: 'Espacio Confinado' },
  { value: 'vehicle', label: 'Vehiculo/Nave' },
];

/**
 * Number of enemies options
 */
const ENEMY_COUNT_OPTIONS = [
  { value: 'solo', label: '1 Enemigo' },
  { value: 'pair', label: '2 Enemigos' },
  { value: 'squad', label: '3-5 Enemigos' },
  { value: 'horde', label: '6+ Enemigos' },
];

export const EncounterGeneratorPage: React.FC<EncounterGeneratorPageProps> = ({ onBack, onNavigate, onLogout }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();
  const [logs, setLogs] = useState([
    '> Tactical simulation online...',
    '> [SUCCESS] Combat analyzer loaded.',
    '> Awaiting encounter parameters...'
  ]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedEncounter, setGeneratedEncounter] = useState<EncounterData | null>(null);
  const [encounterImage, setEncounterImage] = useState<string>(ENCOUNTER_PLACEHOLDER_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();

  const [form, setForm] = useState({
    encounterType: '',
    difficulty: 'MEDIUM' as DifficultyLevel,
    environment: 'open-area',
    enemyCount: 'squad'
  });

  /** Image source mode state */
  const [imageMode, setImageMode] = useState<ImageSourceMode>('generate');
  /** Uploaded image data (base64) */
  const [uploadedImageData, setUploadedImageData] = useState<string | null>(null);

  /**
   * Adds a log entry to the terminal display
   */
  const addLog = (msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-6));
  };

  /**
   * Handles encounter generation via AI service
   */
  const handleGenerate = async () => {
    if (!form.encounterType) {
      addLog('ERROR: TIPO DE ENCUENTRO NO ESPECIFICADO');
      return;
    }

    setIsGenerating(true);
    addLog('SIMULANDO ESCENARIO TACTICO...');

    try {
      addLog('CALCULANDO PARAMETROS DE COMBATE...');
      
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateEncounter({
        gameSystemId: activeCampaign?.gameSystemId,
        encounterType: form.encounterType,
        difficulty: form.difficulty,
        environment: form.environment,
        enemyCount: form.enemyCount,
        generateImage: shouldGenerateImage
      });

      const encounterData = parseJsonResponse<EncounterData>(result.encounterJson);
      setGeneratedEncounter(encounterData);
      setGenerationRequestId(result.generationRequestId);
      addLog(`ENCUENTRO GENERADO: ${encounterData.name.toUpperCase()}`);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image
        setEncounterImage(`data:image/png;base64,${uploadedImageData}`);
        addLog('USANDO IMAGEN CARGADA.');
      } else if (imageMode === 'generate') {
        addLog('GENERANDO REPRESENTACION VISUAL...');
        if (result.imageBase64) {
          setEncounterImage(`data:image/png;base64,${result.imageBase64}`);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else if (result.imageUrl) {
          setEncounterImage(result.imageUrl);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else {
          setEncounterImage(ENCOUNTER_PLACEHOLDER_IMAGE);
          addLog('ADVERTENCIA: RENDER VISUAL FALLIDO. USANDO PLACEHOLDER.');
        }
      } else {
        // Mode is 'none' - use placeholder
        setEncounterImage(ENCOUNTER_PLACEHOLDER_IMAGE);
        addLog('GENERACION DE IMAGEN OMITIDA.');
      }

      addLog('SIMULACION TACTICA COMPLETADA.');
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'SIMULACION FALLIDA';
      addLog(`ERROR_CRITICO: ${message}`);
      console.error(error);
    } finally {
      setIsGenerating(false);
    }
  };

  /**
   * Saves the generated encounter to the entity service using campaign-scoped endpoint
   * Maps AI response fields to entity attributes
   */
  const handleSave = async () => {
    if (!generatedEncounter || !activeCampaignId) return;
    setIsSaving(true);
    addLog('ARCHIVANDO ENCUENTRO...');
    
    try {
      await entityService.create(activeCampaignId, {
        entityType: 'encounter',
        name: generatedEncounter.name,
        description: generatedEncounter.description,
        imageUrl: encounterImage !== ENCOUNTER_PLACEHOLDER_IMAGE ? encounterImage : undefined,
        attributes: {
          encounterType: form.encounterType,
          difficulty: generatedEncounter.stats?.difficulty ?? form.difficulty,
          environment: generatedEncounter.stats?.environment ?? form.environment,
          enemyCount: form.enemyCount,
          participants: generatedEncounter.stats?.participants,
          loot: generatedEncounter.stats?.loot
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'encounter_generator'
        },
        generationRequestId
      });
      addLog('EXITO: ENCUENTRO ARCHIVADO EN NUCLEO');
      setTimeout(onBack, 1000);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'ALMACENAMIENTO RECHAZADO';
      addLog(`DB_WRITE_ERROR: ${message}`);
    } finally {
      setIsSaving(false);
    }
  };

  /**
   * Gets the color class for difficulty display
   */
  const getDifficultyInfo = () => {
    return DIFFICULTY_OPTIONS.find(d => d.value === form.difficulty) || DIFFICULTY_OPTIONS[1];
  };

  return (
    <TerminalLayout 
      title="SIMULADOR DE COMBATE" 
      subtitle="Generador de Encuentros Tacticos"
      icon="swords"
      onLogout={onLogout}
      onNavigate={onNavigate}
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col lg:flex-row gap-8 h-full font-mono">
        {/* Form Panel */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          <div className="space-y-6">
            {/* Encounter Type Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">swords</span> Tipo de Encuentro
              </label>
              <select 
                value={form.encounterType}
                onChange={(e) => setForm({...form, encounterType: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {ENCOUNTER_TYPE_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            {/* Difficulty Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">trending_up</span> Dificultad
              </label>
              <div className="grid grid-cols-4 gap-2">
                {DIFFICULTY_OPTIONS.map((diff) => (
                  <button
                    key={diff.value}
                    onClick={() => setForm({...form, difficulty: diff.value})}
                    className={`h-10 border font-mono text-[9px] uppercase transition-all ${
                      form.difficulty === diff.value 
                        ? `bg-primary/20 border-primary font-bold ${diff.color}` 
                        : 'border-primary/30 text-white/60 bg-surface-dark hover:border-primary'
                    }`}
                  >
                    {diff.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Environment Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">terrain</span> Terreno
              </label>
              <div className="grid grid-cols-3 gap-2">
                {ENVIRONMENT_OPTIONS.map((env) => (
                  <button
                    key={env.value}
                    onClick={() => setForm({...form, environment: env.value})}
                    className={`h-10 border font-mono text-[8px] uppercase transition-all ${
                      form.environment === env.value 
                        ? 'bg-primary text-black border-primary font-bold' 
                        : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                    }`}
                  >
                    {env.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Enemy Count Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">groups</span> Cantidad de Enemigos
              </label>
              <div className="grid grid-cols-4 gap-2">
                {ENEMY_COUNT_OPTIONS.map((count) => (
                  <button
                    key={count.value}
                    onClick={() => setForm({...form, enemyCount: count.value})}
                    className={`h-10 border font-mono text-[8px] uppercase transition-all ${
                      form.enemyCount === count.value 
                        ? 'bg-primary text-black border-primary font-bold' 
                        : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                    }`}
                  >
                    {count.label}
                  </button>
                ))}
              </div>
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
              disabled={!generatedEncounter || isSaving || isGenerating || !activeCampaignId}
              variant="primary"
              size="lg"
              isLoading={isSaving}
              icon="save"
            >
              GUARDAR
            </Button>
          </div>
        </div>

        {/* Preview Panel - Tactical Display Style */}
        <div className="flex-1 flex flex-col gap-4 overflow-y-auto">
          {/* Encounter Header */}
          <div className={`border border-primary/30 bg-black p-4 transition-all ${generatedEncounter ? 'border-primary' : ''}`}>
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <span className="material-icons text-primary text-xl">swords</span>
                <span className="text-[10px] text-primary/60 uppercase tracking-widest">Simulacion Tactica</span>
              </div>
              {generatedEncounter && (
                <div className={`px-2 py-1 border text-[8px] font-bold uppercase ${getDifficultyInfo().color} border-current`}>
                  {getDifficultyInfo().label}
                </div>
              )}
            </div>
            
            {generatedEncounter ? (
              <div className="space-y-3">
                <h2 className="text-xl text-primary font-display uppercase text-glow">{generatedEncounter.name}</h2>
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

          {/* Encounter Details */}
          {generatedEncounter && (
            <>
              {/* Description Section */}
              {generatedEncounter.description && (
                <div className="bg-surface-dark/50 border border-primary/20 p-4">
                  <p className="text-[9px] text-primary/40 uppercase tracking-widest mb-2 flex items-center gap-1">
                    <span className="material-icons text-xs">description</span> Descripcion
                  </p>
                  <p className="text-[11px] text-white/80 leading-relaxed">{generatedEncounter.description}</p>
                </div>
              )}

              {/* Environment Section */}
              {generatedEncounter.stats?.environment && (
                <div className="bg-black/60 border border-blue-500/30 p-4">
                  <p className="text-[9px] text-blue-500/60 uppercase tracking-widest mb-2 flex items-center gap-1">
                    <span className="material-icons text-xs">terrain</span> Entorno
                  </p>
                  <p className="text-[11px] text-blue-500/90">{generatedEncounter.stats.environment}</p>
                </div>
              )}

              {/* Participants Section */}
              {generatedEncounter.stats?.participants && generatedEncounter.stats.participants.length > 0 && (
                <div className="bg-surface-dark border border-danger/20 p-4">
                  <p className="text-[9px] text-danger/60 uppercase tracking-widest mb-2 flex items-center gap-1">
                    <span className="material-icons text-xs">groups</span> Participantes
                  </p>
                  <div className="flex flex-wrap gap-2">
                    {generatedEncounter.stats.participants.map((participant, idx) => {
                      // Handle both string and object formats
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

              {/* Info Grid */}
              <div className="grid grid-cols-2 gap-3">
                {generatedEncounter.stats?.loot && (
                  <div className="bg-black/60 border border-yellow-500/30 p-3">
                    <p className="text-[8px] text-yellow-500/60 uppercase tracking-widest mb-1">Botin Potencial</p>
                    <p className="text-[10px] text-yellow-500/90">{generatedEncounter.stats.loot}</p>
                  </div>
                )}
                {generatedEncounter.stats?.difficulty && (
                  <div className="bg-surface-dark border border-primary/20 p-3">
                    <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Dificultad IA</p>
                    <p className="text-[10px] text-primary">{generatedEncounter.stats.difficulty}</p>
                  </div>
                )}
              </div>
            </>
          )}

          {/* Log Panel */}
          <div className="mt-auto h-24 bg-black/80 border border-primary/20 p-3 text-[10px] text-primary/80 overflow-y-auto font-mono scrollbar-hide">
            {logs.map((log, i) => <p key={i} className={i === logs.length - 1 ? "text-primary font-bold" : "opacity-60"}>{log}</p>)}
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};

export default EncounterGeneratorPage;
