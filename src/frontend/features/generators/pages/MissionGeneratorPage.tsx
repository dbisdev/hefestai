/**
 * Mission Generator Page
 * AI-powered mission/quest generation with cyberpunk terminal aesthetics
 * Creates campaign objectives, quests, and story missions
 * Uses campaign context for entity creation
 */

import React, { useState } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { MissionData } from '@core/types';
import { Screen } from '@core/types';

interface MissionGeneratorPageProps {
  onBack: () => void;
  onNavigate?: (screen: Screen) => void;
  onLogout?: () => void;
}

/** Placeholder image for missions without generated images */
const MISSION_PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?q=80&w=400&auto=format&fit=crop";

/**
 * Mission type options
 */
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

/**
 * Difficulty level type for form state
 */
type DifficultyLevel = 'EASY' | 'MEDIUM' | 'HARD' | 'EXTREME';

/**
 * Difficulty level options
 */
const DIFFICULTY_OPTIONS: { value: DifficultyLevel; label: string; color: string; icon: string }[] = [
  { value: 'EASY', label: 'Facil', color: 'text-green-400', icon: 'shield' },
  { value: 'MEDIUM', label: 'Medio', color: 'text-yellow-400', icon: 'security' },
  { value: 'HARD', label: 'Dificil', color: 'text-orange-400', icon: 'gpp_maybe' },
  { value: 'EXTREME', label: 'Extremo', color: 'text-red-400', icon: 'dangerous' },
];

/**
 * Environment/setting options
 */
const ENVIRONMENT_OPTIONS = [
  { value: 'space-station', label: 'Estacion Espacial' },
  { value: 'planet-surface', label: 'Superficie Planetaria' },
  { value: 'asteroid-field', label: 'Campo de Asteroides' },
  { value: 'derelict-ship', label: 'Nave Abandonada' },
  { value: 'megacity', label: 'Megaciudad' },
  { value: 'underground', label: 'Instalacion Subterranea' },
];

export const MissionGeneratorPage: React.FC<MissionGeneratorPageProps> = ({ onBack, onNavigate, onLogout }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();

  const [logs, setLogs] = useState([
    '> Mission briefing system online...',
    '> [SUCCESS] Tactical database connected.',
    '> Awaiting mission parameters...'
  ]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedMission, setGeneratedMission] = useState<MissionData | null>(null);
  const [missionImage, setMissionImage] = useState<string>(MISSION_PLACEHOLDER_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();

  const [form, setForm] = useState({
    missionType: '',
    difficulty: 'MEDIUM' as DifficultyLevel,
    environment: 'space-station',
    factionInvolved: 'corporate'
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
   * Handles mission generation via AI service
   */
  const handleGenerate = async () => {
    if (!form.missionType) {
      addLog('ERROR: TIPO DE MISION NO ESPECIFICADO');
      return;
    }

    setIsGenerating(true);
    addLog('GENERANDO BRIEFING TACTICO...');

    try {
      addLog('COMPILANDO OBJETIVOS...');
      
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateMission({
        gameSystemId: activeCampaign?.gameSystemId,
        missionType: form.missionType,
        difficulty: form.difficulty,
        environment: form.environment,
        factionInvolved: form.factionInvolved,
        generateImage: shouldGenerateImage
      });

      const missionData = parseJsonResponse<MissionData>(result.missionJson);
      setGeneratedMission(missionData);
      setGenerationRequestId(result.generationRequestId);
      addLog(`MISION GENERADA: ${missionData.name.toUpperCase()}`);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image
        setMissionImage(`data:image/png;base64,${uploadedImageData}`);
        addLog('USANDO IMAGEN CARGADA.');
      } else if (imageMode === 'generate') {
        addLog('GENERANDO REPRESENTACION VISUAL...');
        if (result.imageBase64) {
          setMissionImage(`data:image/png;base64,${result.imageBase64}`);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else if (result.imageUrl) {
          setMissionImage(result.imageUrl);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else {
          setMissionImage(MISSION_PLACEHOLDER_IMAGE);
          addLog('ADVERTENCIA: RENDER VISUAL FALLIDO. USANDO PLACEHOLDER.');
        }
      } else {
        // Mode is 'none' - use placeholder
        setMissionImage(MISSION_PLACEHOLDER_IMAGE);
        addLog('GENERACION DE IMAGEN OMITIDA.');
      }

      addLog('BRIEFING TACTICO COMPLETADO.');
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'GENERACION FALLIDA';
      addLog(`ERROR_CRITICO: ${message}`);
      console.error(error);
    } finally {
      setIsGenerating(false);
    }
  };

  /**
   * Saves the generated mission to the active campaign
   * Maps AI response fields to entity attributes
   */
  const handleSave = async () => {
    if (!generatedMission) return;
    
    if (!activeCampaignId) {
      addLog('ERROR: NO CAMPAIGN SELECTED');
      return;
    }

    setIsSaving(true);
    addLog('ARCHIVANDO MISION...');
    
    try {
      await entityService.create(activeCampaignId, {
        entityType: 'mission',
        name: generatedMission.name,
        description: generatedMission.description,
        imageUrl: missionImage !== MISSION_PLACEHOLDER_IMAGE ? missionImage : undefined,
        attributes: {
          missionType: form.missionType.toUpperCase(),
          difficulty: generatedMission.stats?.difficulty ?? form.difficulty,
          environment: form.environment,
          objective: generatedMission.stats?.objective,
          rewards: generatedMission.stats?.rewards,
          estimatedDuration: generatedMission.stats?.estimatedDuration
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'mission_briefing_v1'
        },
        generationRequestId
      });
      addLog('EXITO: MISION ARCHIVADA EN NUCLEO');
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
      title="GENERADOR DE MISIONES" 
      subtitle="Generador de Misiones Tacticas"
      icon="assignment"
      onLogout={onLogout}
      onNavigate={onNavigate}
      gameSystemId={activeCampaign?.gameSystemId}
      hideCampaignSelector={false}
    >
      <div className="flex flex-col lg:flex-row gap-8 h-full font-mono">
        {/* Form Panel */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          {/* No Campaign Warning */}
          {!activeCampaignId && (
            <div className="border border-yellow-500/50 bg-yellow-500/10 p-3 text-[10px] text-yellow-500 uppercase">
              <span className="material-icons text-sm mr-1 align-middle">warning</span>
              Selecciona una campaña para guardar entidades
            </div>
          )}

          <div className="space-y-6">
            {/* Mission Type Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">assignment</span> Tipo de Mision
              </label>
              <select 
                value={form.missionType}
                onChange={(e) => setForm({...form, missionType: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {MISSION_TYPE_OPTIONS.map(opt => (
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
                    className={`h-14 border font-mono text-[9px] uppercase transition-all flex flex-col items-center justify-center ${
                      form.difficulty === diff.value 
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

            {/* Environment Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">location_on</span> Entorno
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
              icon="assignment_add"
            >
              GENERAR
            </Button>
            <Button
              onClick={handleSave}
              disabled={!generatedMission || isSaving || isGenerating || !activeCampaignId}
              variant="primary"
              size="lg"
              isLoading={isSaving}
              icon="save"
            >
              GUARDAR
            </Button>
          </div>
        </div>

        {/* Preview Panel - Mission Briefing Style */}
        <div className="flex-1 flex flex-col gap-4 overflow-y-auto">
          {/* Mission Header */}
          <div className={`border border-primary/30 bg-black p-4 transition-all ${generatedMission ? 'border-primary' : ''}`}>
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <span className="material-icons text-primary text-xl">assignment</span>
                <span className="text-[10px] text-primary/60 uppercase tracking-widest">Briefing Tactico</span>
              </div>
              {generatedMission && (
                <div className={`px-2 py-1 border text-[8px] font-bold uppercase ${getDifficultyInfo().color} border-current`}>
                  {getDifficultyInfo().label}
                </div>
              )}
            </div>
            
            {generatedMission ? (
              <div className="space-y-3">
                <h2 className="text-xl text-primary font-display uppercase text-glow">{generatedMission.name}</h2>
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

          {/* Mission Details */}
          {generatedMission && (
            <>
              {/* Briefing Section */}
              {generatedMission.description && (
                <div className="bg-surface-dark/50 border border-primary/20 p-4">
                  <p className="text-[9px] text-primary/40 uppercase tracking-widest mb-2 flex items-center gap-1">
                    <span className="material-icons text-xs">description</span> Briefing
                  </p>
                  <p className="text-[11px] text-white/80 leading-relaxed">{generatedMission.description}</p>
                </div>
              )}

              {/* Objective Section */}
              {generatedMission.stats?.objective && (
                <div className="bg-black/60 border border-yellow-500/30 p-4">
                  <p className="text-[9px] text-yellow-500/60 uppercase tracking-widest mb-2 flex items-center gap-1">
                    <span className="material-icons text-xs">flag</span> Objetivo Principal
                  </p>
                  <p className="text-[11px] text-yellow-500/90 font-bold">{generatedMission.stats.objective}</p>
                </div>
              )}

              {/* Info Grid */}
              <div className="grid grid-cols-2 gap-3">
                {generatedMission.stats?.rewards && (
                  <div className="bg-surface-dark border border-primary/20 p-3">
                    <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Recompensa</p>
                    <p className="text-[10px] text-primary">{generatedMission.stats.rewards}</p>
                  </div>
                )}
                {generatedMission.stats?.estimatedDuration && (
                  <div className="bg-surface-dark border border-primary/20 p-3">
                    <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Duracion Est.</p>
                    <p className="text-[10px] text-primary">{generatedMission.stats.estimatedDuration}</p>
                  </div>
                )}
                {generatedMission.stats?.difficulty && (
                  <div className="bg-surface-dark border border-primary/20 p-3">
                    <p className="text-[8px] text-primary/40 uppercase tracking-widest mb-1">Dificultad IA</p>
                    <p className="text-[10px] text-primary">{generatedMission.stats.difficulty}</p>
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

export default MissionGeneratorPage;
