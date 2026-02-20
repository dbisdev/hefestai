/**
 * NPC Generator Page
 * AI-powered NPC generation with cyberpunk terminal aesthetics
 * Creates humanoid NPCs (actors) for campaign storytelling
 */

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, DynamicStatsPanel, TerminalLog, EditableField, EditableStatsPanel } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { useTerminalLog } from '@core/hooks/useTerminalLog';
import { aiService, entityService, entityTemplateService } from '@core/services/api';
import { useCampaign } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import type { NpcData, FieldDefinition } from '@core/types';

interface NpcGeneratorPageProps {
  onBack: () => void;
}

const UNKNOWN_NPC_IMAGE = "https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop";

/**
 * NPC occupation options - defines the role of the NPC in the world
 */
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

/**
 * NPC personality archetypes
 */
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

/**
 * NPC species/origin options
 */
const SPECIES_OPTIONS = [
  { value: 'human', label: 'Humano' },
  { value: 'android', label: 'Androide' },
  { value: 'alien-humanoid', label: 'Alien Humanoide' },
  { value: 'cyber-enhanced', label: 'Cyber-Aumentado' },
  { value: 'clone', label: 'Clon' },
];

export const NpcGeneratorPage: React.FC<NpcGeneratorPageProps> = ({ onBack }) => {
  const navigate = useNavigate();
  const { activeCampaignId, activeCampaign } = useCampaign();
  const { logs, addLog } = useTerminalLog({
    maxLogs: 6,
    initialLogs: [
      '> Actor database initialized...',
      '> [SUCCESS] Social profiling module loaded.',
      '> Awaiting actor parameters...'
    ]
  });
const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedNpc, setGeneratedNpc] = useState<NpcData | null>(null);
  /** Editable NPC data for inline editing before saving */
  const [editableNpc, setEditableNpc] = useState<NpcData | null>(null);
  const [npcImage, setNpcImage] = useState<string>(UNKNOWN_NPC_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();
  /** Template field definitions for display name mapping */
  const [fieldDefinitions, setFieldDefinitions] = useState<FieldDefinition[]>([]);

  const [form, setForm] = useState({
    species: 'human',
    occupation: '',
    personality: '',
    setting: 'space-station'
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
          'npc'
        );
        setFieldDefinitions(fields);
      } catch (error) {
        console.error('Failed to fetch template fields:', error);
        setFieldDefinitions([]);
      }
    };
    
    fetchTemplateFields();
  }, [activeCampaign?.gameSystemId]);

  /**
   * Handles NPC generation via AI service
   */
  const handleGenerate = async () => {
    if (!form.occupation || !form.personality) {
      addLog('ERROR: PARAMETROS INCOMPLETOS');
      return;
    }

    setIsGenerating(true);
    addLog('INICIANDO PERFIL SOCIAL...');

    try {
      addLog('CONSTRUYENDO IDENTIDAD...');
      
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateNpc({
        gameSystemId: activeCampaign?.gameSystemId,
        species: form.species,
        occupation: form.occupation,
        personality: form.personality,
        setting: form.setting,
        generateImage: shouldGenerateImage
      });

const npcData = parseJsonResponse<NpcData>(result.npcJson);
      setGeneratedNpc(npcData);
      setEditableNpc(npcData);
      setGenerationRequestId(result.generationRequestId);
      addLog(`ACTOR REGISTRADO: ${npcData.name.toUpperCase()}`);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image (already compressed to WebP)
        setNpcImage(`data:image/webp;base64,${uploadedImageData}`);
        addLog('USANDO IMAGEN CARGADA.');
      } else if (imageMode === 'generate') {
        addLog('GENERANDO REPRESENTACION VISUAL...');
        if (result.imageUrl) {
          setNpcImage(result.imageUrl);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else if (result.imageBase64) {
          setNpcImage(`data:image/webp;base64,${result.imageBase64}`);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else {
          addLog('ADVERTENCIA: RENDER VISUAL FALLIDO. USANDO PLACEHOLDER.');
          setNpcImage(UNKNOWN_NPC_IMAGE);
        }
      } else {
        // Mode is 'none' - use placeholder
        addLog('GENERACION DE IMAGEN OMITIDA.');
        setNpcImage(UNKNOWN_NPC_IMAGE);
      }
      
      addLog('PERFIL COMPLETADO CON EXITO.');
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'GENERACION FALLIDA';
      addLog(`ERROR_CRITICO: ${message}`);
      console.error(error);
    } finally {
      setIsGenerating(false);
    }
  };

/**
    * Handles NPC generation via AI service
    */
  const handleSave = async () => {
    if (!editableNpc || !activeCampaignId) return;
    setIsSaving(true);
    addLog('ESCRIBIENDO EN ALMACENAMIENTO...');
    
    try {
      await entityService.create(activeCampaignId, {
        entityType: 'npc',
        name: editableNpc.name,
        description: editableNpc.description,
        imageUrl: npcImage !== UNKNOWN_NPC_IMAGE ? npcImage : undefined,
        attributes: {
          ...editableNpc.stats
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'npc_generator',
          // Store generation parameters for reference
generationParams: {
            species: form.species,
            occupation: editableNpc.occupation || form.occupation,
            personality: form.personality,
            setting: form.setting
          }
        },
        generationRequestId
      });
      addLog('EXITO: ACTOR REGISTRADO EN NUCLEO');
      setTimeout(onBack, 1000);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'ALMACENAMIENTO RECHAZADO';
      addLog(`DB_WRITE_ERROR: ${message}`);
    } finally {
      setIsSaving(false);
}
  };

  /**
   * Handle stats changes from EditableStatsPanel
   */
  const handleStatsChange = (newStats: Record<string, unknown>) => {
    if (editableNpc) {
      setEditableNpc({
        ...editableNpc,
        stats: newStats
      });
    }
  };

  /**
   * Handle name change
   */
  const handleNameChange = (value: string | number) => {
    if (editableNpc) {
      setEditableNpc({
        ...editableNpc,
        name: String(value)
      });
    }
  };

  /**
   * Handle occupation change
   */
  const handleOccupationChange = (value: string | number) => {
    if (editableNpc) {
      setEditableNpc({
        ...editableNpc,
        occupation: String(value)
      });
    }
  };

  /**
   * Handle description change
   */
  const handleDescriptionChange = (value: string | number) => {
    if (editableNpc) {
      setEditableNpc({
        ...editableNpc,
        description: String(value)
      });
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
        {/* Form Panel */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          <div className="space-y-6">
            {/* Species Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">fingerprint</span> Especie
              </label>
              <div className="grid grid-cols-3 gap-2">
                {SPECIES_OPTIONS.map((spec) => (
                  <button
                    key={spec.value}
                    onClick={() => setForm({...form, species: spec.value})}
                    className={`h-10 border font-mono text-[9px] uppercase transition-all ${
                      form.species === spec.value 
                        ? 'bg-primary text-black border-primary font-bold' 
                        : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                    }`}
                  >
                    {spec.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Occupation Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">work</span> Ocupacion
              </label>
              <select 
                value={form.occupation}
                onChange={(e) => setForm({...form, occupation: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {OCCUPATION_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            {/* Personality Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">psychology</span> Personalidad
              </label>
              <select 
                value={form.personality}
                onChange={(e) => setForm({...form, personality: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {PERSONALITY_OPTIONS.map(opt => (
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
              disabled={!editableNpc || isSaving || isGenerating || !activeCampaignId}
              variant="primary"
              size="lg"
              isLoading={isSaving}
              icon="save"
            >
              GUARDAR
            </Button>
          </div>
        </div>

        {/* Preview Panel */}
        <div className="flex-1 flex flex-col gap-4">
          {/* Scrollable content */}
          <div className="flex-1 overflow-y-auto flex flex-col gap-4">
            <div className="relative w-full h-64 md:h-[500px] border border-primary/30 bg-black p-1 flex flex-col clip-tech-br group">
            <div className="relative flex-1 bg-black overflow-hidden flex items-center justify-center">
              <img 
                className={`w-full h-full object-cover object-[center_25%] transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-80 scale-100'} grayscale brightness-90`} 
                src={npcImage} 
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
<div className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t border-primary/40 backdrop-blur-sm transition-transform duration-500 ${editableNpc ? 'translate-y-0' : 'translate-y-full'}`}>
              <EditableField
                value={editableNpc?.name || ''}
                label="Nombre"
                variant="primary"
                onChange={handleNameChange}
                disabled={!editableNpc}
                className="font-bold"
              />
            </div>
            {!editableNpc && !isGenerating && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <span className="text-primary/20 text-[10px] tracking-[0.5em] uppercase font-bold">Sin Datos</span>
              </div>
            )}
          </div>

          {/* Details Section - Below image, above stats */}
          {editableNpc && (
            <div className="space-y-2">
              {editableNpc.occupation && (
                <div className="bg-surface-dark/50 border border-primary/20 p-2">
                  <EditableField
                    value={editableNpc.occupation}
                    label="Ocupación"
                    variant="primary"
                    onChange={handleOccupationChange}
                    disabled={!editableNpc}
                  />
                </div>
              )}
              {editableNpc.description && (
                <div className="bg-surface-dark/50 border border-primary/20 p-2">
                  <EditableField
                    value={editableNpc.description}
                    label="Description"
                    type="textarea"
                    rows={2}
                    variant="primary"
                    onChange={handleDescriptionChange}
                    disabled={!editableNpc}
                  />
                </div>
              )}
            </div>
          )}

          {/* Stats Panel - Dynamic based on game system */}
          <EditableStatsPanel 
            stats={editableNpc?.stats || null}
            onStatsChange={handleStatsChange}
            variant="primary"
            maxColumns={3}
            showProgressBar={true}
            maxProgressValue={10}
            fieldDefinitions={fieldDefinitions}
            disabled={!editableNpc}
          />
          </div>

          {/* Log Panel - Fixed at bottom */}
          <TerminalLog logs={logs} maxLogs={6} className="h-24 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default NpcGeneratorPage;
