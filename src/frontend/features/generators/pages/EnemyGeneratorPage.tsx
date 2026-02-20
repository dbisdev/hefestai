/**
 * Enemy Generator Page
 * AI-powered enemy/creature generation with cyberpunk terminal aesthetics
 * Creates hostile creatures, aliens, or antagonists for combat encounters
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
import type { EnemyData, FieldDefinition } from '@core/types';

const UNKNOWN_ENEMY_IMAGE = "https://images.unsplash.com/photo-1614728263952-84ea256f9679?q=80&w=400&auto=format&fit=crop";

/**
 * Enemy species/type options
 */
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

/**
 * Threat level classification
 */
const THREAT_LEVEL_OPTIONS = [
  { value: 'minor', label: 'Menor', color: 'text-green-400' },
  { value: 'moderate', label: 'Moderado', color: 'text-yellow-400' },
  { value: 'dangerous', label: 'Peligroso', color: 'text-orange-400' },
  { value: 'lethal', label: 'Letal', color: 'text-red-400' },
  { value: 'apocalyptic', label: 'Apocaliptico', color: 'text-purple-400' },
];

/**
 * Enemy behavior patterns
 */
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
  const navigate = useNavigate();
  const { activeCampaignId, activeCampaign } = useCampaign();
  const { logs, addLog } = useTerminalLog({
    maxLogs: 6,
    initialLogs: [
      '> Threat analysis system online...',
      '> [WARNING] Hostile database accessed.',
      '> Awaiting threat parameters...'
    ]
  });
const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedEnemy, setGeneratedEnemy] = useState<EnemyData | null>(null);
  /** Editable enemy data for inline editing before saving */
  const [editableEnemy, setEditableEnemy] = useState<EnemyData | null>(null);
  const [enemyImage, setEnemyImage] = useState<string>(UNKNOWN_ENEMY_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();
  /** Template field definitions for display name mapping */
  const [fieldDefinitions, setFieldDefinitions] = useState<FieldDefinition[]>([]);

  const [form, setForm] = useState({
    species: '',
    threatLevel: 'moderate',
    behavior: '',
    environment: 'space-station'
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
          'enemy'
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
   * Handles enemy generation via AI service
   */
  const handleGenerate = async () => {
    if (!form.species || !form.behavior) {
      addLog('ERROR: PARAMETROS DE AMENAZA INCOMPLETOS');
      return;
    }

    setIsGenerating(true);
    addLog('INICIANDO ANALISIS DE AMENAZA...');

    try {
      addLog('ESCANEANDO PERFIL HOSTIL...');
      
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateEnemy({
        gameSystemId: activeCampaign?.gameSystemId,
        species: form.species,
        threatLevel: form.threatLevel,
        behavior: form.behavior,
        environment: form.environment,
        generateImage: shouldGenerateImage
      });

const enemyData = parseJsonResponse<EnemyData>(result.enemyJson);
      setGeneratedEnemy(enemyData);
      setEditableEnemy(enemyData);
      setGenerationRequestId(result.generationRequestId);
      addLog(`AMENAZA IDENTIFICADA: ${enemyData.name.toUpperCase()}`);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image (already compressed to WebP)
        setEnemyImage(`data:image/webp;base64,${uploadedImageData}`);
        addLog('USANDO IMAGEN CARGADA.');
      } else if (imageMode === 'generate') {
        addLog('GENERANDO REPRESENTACION VISUAL...');
        if (result.imageUrl) {
          setEnemyImage(result.imageUrl);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else if (result.imageBase64) {
          setEnemyImage(`data:image/webp;base64,${result.imageBase64}`);
          addLog('SINTESIS VISUAL COMPLETA.');
        } else {
          addLog('ADVERTENCIA: RENDER VISUAL FALLIDO. USANDO PLACEHOLDER.');
          setEnemyImage(UNKNOWN_ENEMY_IMAGE);
        }
      } else {
        // Mode is 'none' - use placeholder
        addLog('GENERACION DE IMAGEN OMITIDA.');
        setEnemyImage(UNKNOWN_ENEMY_IMAGE);
      }
      
      addLog('PERFIL DE AMENAZA COMPLETADO.');
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ANALISIS FALLIDO';
      addLog(`ERROR_CRITICO: ${message}`);
      console.error(error);
    } finally {
      setIsGenerating(false);
    }
  };

/**
    * Saves the generated enemy to the entity service using campaign-scoped endpoint
    */
  const handleSave = async () => {
    if (!editableEnemy || !activeCampaignId) return;
    setIsSaving(true);
    addLog('ARCHIVANDO AMENAZA...');
    
    try {
      await entityService.create(activeCampaignId, {
        entityType: 'enemy',
        name: editableEnemy.name,
        description: editableEnemy.abilities,
        imageUrl: enemyImage !== UNKNOWN_ENEMY_IMAGE ? enemyImage : undefined,
        attributes: {
          ...editableEnemy.stats
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'enemy_generator',
          generationParams: {
            species: editableEnemy.species || form.species,
            threatLevel: form.threatLevel,
            behavior: form.behavior,
            environment: form.environment,
            weakness: editableEnemy.weakness
          }
        },
        generationRequestId
      });
      addLog('EXITO: AMENAZA ARCHIVADA EN NUCLEO');
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
    if (editableEnemy) {
      setEditableEnemy({
        ...editableEnemy,
        stats: newStats
      });
    }
  };

  /**
   * Handle name change
   */
  const handleNameChange = (value: string | number) => {
    if (editableEnemy) {
      setEditableEnemy({
        ...editableEnemy,
        name: String(value)
      });
    }
  };

  /**
   * Handle species change
   */
  const handleSpeciesChange = (value: string | number) => {
    if (editableEnemy) {
      setEditableEnemy({
        ...editableEnemy,
        species: String(value)
      });
    }
  };

  /**
   * Handle abilities change
   */
  const handleAbilitiesChange = (value: string | number) => {
    if (editableEnemy) {
      setEditableEnemy({
        ...editableEnemy,
        abilities: String(value)
      });
    }
  };

  /**
   * Handle weakness change
   */
  const handleWeaknessChange = (value: string | number) => {
    if (editableEnemy) {
      setEditableEnemy({
        ...editableEnemy,
        weakness: String(value)
      });
    }
  };

  /**
   * Handles enemy generation via AI service
   */
  const getThreatColor = () => {
    const threat = THREAT_LEVEL_OPTIONS.find(t => t.value === form.threatLevel);
    return threat?.color || 'text-primary';
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
        {/* Form Panel */}
        <div className="flex-1 flex flex-col gap-6 overflow-y-auto pr-2">
          <div className="space-y-6">
            {/* Species Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">bug_report</span> Especie Hostil
              </label>
              <select 
                value={form.species}
                onChange={(e) => setForm({...form, species: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {SPECIES_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            {/* Threat Level Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">warning</span> Nivel de Amenaza
              </label>
              <div className="grid grid-cols-5 gap-1">
                {THREAT_LEVEL_OPTIONS.map((threat) => (
                  <button
                    key={threat.value}
                    onClick={() => setForm({...form, threatLevel: threat.value})}
                    className={`h-12 border font-mono text-[8px] uppercase transition-all flex flex-col items-center justify-center ${
                      form.threatLevel === threat.value 
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

            {/* Behavior Selection */}
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">psychology</span> Comportamiento
              </label>
              <select 
                value={form.behavior}
                onChange={(e) => setForm({...form, behavior: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {BEHAVIOR_OPTIONS.map(opt => (
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
              icon="pest_control"
            >
              ANALIZAR
            </Button>
            <Button
              onClick={handleSave}
              disabled={!editableEnemy || isSaving || isGenerating || !activeCampaignId}
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
            <div className="relative w-full h-64 md:h-[500px] border border-danger/30 bg-black p-1 flex flex-col clip-tech-br group">
            <div className="relative flex-1 bg-black overflow-hidden flex items-center justify-center">
              <img 
                className={`w-full h-full object-cover object-[center_25%] transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-80 scale-100'} grayscale brightness-75 contrast-125`} 
                src={enemyImage} 
                alt="Enemy Preview"
              />
              {isGenerating && (
                <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 z-20">
                  <div className="w-1/2 h-1 bg-danger/20 relative overflow-hidden mb-2">
                    <div className="absolute inset-0 bg-danger animate-[scan_2s_linear_infinite]"></div>
                  </div>
                  <span className="text-danger text-[10px] animate-pulse">ESCANEANDO_AMENAZA...</span>
                </div>
              )}
              <div className="absolute inset-0 pointer-events-none border border-danger/5 opacity-30"></div>
              
{/* Threat Level Badge */}
              {editableEnemy && (
                <div className={`absolute top-2 right-2 px-2 py-1 bg-black/80 border border-current text-[8px] font-bold uppercase ${getThreatColor()}`}>
                  {form.threatLevel}
                </div>
              )}
            </div>
            <div className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t border-danger/40 backdrop-blur-sm transition-transform duration-500 ${editableEnemy ? 'translate-y-0' : 'translate-y-full'}`}>
              <EditableField
                value={editableEnemy?.name || ''}
                label="Nombre"
                variant="danger"
                onChange={handleNameChange}
                disabled={!editableEnemy}
                className="font-bold"
              />
            </div>
            {!editableEnemy && !isGenerating && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <span className="text-danger/20 text-[10px] tracking-[0.5em] uppercase font-bold">Sin Datos</span>
              </div>
            )}
          </div>

          {/* Details Section - Below image, above stats */}
          {editableEnemy && (
            <div className="space-y-2">
              {editableEnemy.species && (
                <div className="bg-surface-dark/50 border border-danger/20 p-2">
                  <EditableField
                    value={editableEnemy.species}
                    label="Especie"
                    variant="danger"
                    onChange={handleSpeciesChange}
                    disabled={!editableEnemy}
                  />
                </div>
              )}
              {editableEnemy.abilities && (
                <div className="bg-surface-dark/50 border border-danger/20 p-2">
                  <EditableField
                    value={editableEnemy.abilities}
                    label="Habilidades"
                    type="textarea"
                    rows={2}
                    variant="danger"
                    onChange={handleAbilitiesChange}
                    disabled={!editableEnemy}
                  />
                </div>
              )}
            </div>
          )}

          {/* Log Panel */}
          <TerminalLog logs={logs} maxLogs={6} className="h-24" />

          {/* Stats Panel - Dynamic based on game system */}
          <EditableStatsPanel 
            stats={editableEnemy?.stats || null}
            onStatsChange={handleStatsChange}
            variant="danger"
            maxColumns={4}
            showProgressBar={true}
            maxProgressValue={100}
            fieldDefinitions={fieldDefinitions}
            disabled={!editableEnemy}
          />

          {/* Weakness Display */}
          <div className="bg-black/60 border border-yellow-500/30 p-3">
            <p className="text-[8px] text-yellow-500/60 uppercase tracking-widest mb-1">
              <span className="material-icons text-sm align-middle mr-1">tips_and_updates</span>
              Debilidad Detectada
            </p>
            <EditableField
              value={editableEnemy?.weakness || ''}
              variant="warning"
              onChange={handleWeaknessChange}
              type="textarea"
              rows={2}
              disabled={!editableEnemy}
            />
          </div>
          </div>

          {/* Log Panel - Fixed at bottom */}
          <TerminalLog logs={logs} maxLogs={6} className="h-24 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default EnemyGeneratorPage;
