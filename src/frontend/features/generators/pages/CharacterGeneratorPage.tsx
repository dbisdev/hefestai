/**
 * Character Generator Page
 * AI-powered character generation with cyberpunk terminal aesthetics
 * Uses campaign context for entity creation
 */

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TerminalLayout } from '@shared/components/layout';
import { Button, ImageSourceSelector, DynamicStatsPanel, TerminalLog, EditableField, EditableStatsPanel } from '@shared/components/ui';
import type { ImageSourceMode } from '@shared/components/ui';
import { aiService, entityService, entityTemplateService } from '@core/services/api';
import { useCampaign, useAuth } from '@core/context';
import { parseJsonResponse } from '@core/utils';
import { useTerminalLog } from '@core/hooks/useTerminalLog';
import type { CharacterData, FieldDefinition } from '@core/types';

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
  const navigate = useNavigate();
  const { user } = useAuth();
  const { activeCampaignId, activeCampaign } = useCampaign();
  const { logs, addLog } = useTerminalLog({
    maxLogs: 6,
    initialLogs: [
      '> System initialization sequence started...',
      '> [SUCCESS] Neural link established.',
      '> Awaiting user input parameters...'
    ]
  });
  
const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedChar, setGeneratedChar] = useState<CharacterData | null>(null);
  /** Editable character data for inline editing before saving */
  const [editableChar, setEditableChar] = useState<CharacterData | null>(null);
  const [charImage, setCharImage] = useState<string>(UNKNOWN_CHAR_IMAGE);
  /** Stores the generation request ID to link entity to generation history when saving */
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();
  /** Template field definitions for display name mapping */
  const [fieldDefinitions, setFieldDefinitions] = useState<FieldDefinition[]>([]);

  const [form, setForm] = useState({
    species: '',
    role: '',
    morphology: 'NEUTRAL',
    attire: 'Techwear'
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
          'character'
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
   * Handles character generation via AI service
   */
  const handleGenerate = async () => {
    if (!form.species || !form.role) {
      addLog('ERROR: PARAMETERS MISSING');
      return;
    }

    setIsGenerating(true);
    addLog('COMMENCING NEURAL SYNTHESIS...');

    try {
      addLog('FETCHING NEURAL BIOMETRICS...');
      
      // Only request AI image generation if mode is 'generate'
      const shouldGenerateImage = imageMode === 'generate';
      
      const result = await aiService.generateCharacter({
        gameSystemId: activeCampaign?.gameSystemId,
        species: form.species,
        role: form.role,
        morphology: form.morphology,
        attire: form.attire,
        generateImage: shouldGenerateImage
      });

const charData = parseJsonResponse<CharacterData>(result.characterJson);
      setGeneratedChar(charData);
      setEditableChar(charData);
      setGenerationRequestId(result.generationRequestId);
      addLog(`DATA RECEIVED: ${charData.name.toUpperCase()}`);

      // Handle image based on selected mode
      if (imageMode === 'upload' && uploadedImageData) {
        // Use uploaded image (already compressed to WebP)
        setCharImage(`data:image/webp;base64,${uploadedImageData}`);
        addLog('USING UPLOADED IMAGE.');
      } else if (imageMode === 'generate') {
        addLog('GENERATING VISUAL REPRESENTATION...');
        if (result.imageUrl) {
          setCharImage(result.imageUrl);
          addLog('VISUAL SYNTHESIS COMPLETE.');
        } else if (result.imageBase64) {
          setCharImage(`data:image/webp;base64,${result.imageBase64}`);
          addLog('VISUAL SYNTHESIS COMPLETE.');
        } else {
          addLog('WARNING: VISUAL RENDER FAILED. USING PLACEHOLDER.');
          setCharImage(UNKNOWN_CHAR_IMAGE);
        }
      } else {
        // Mode is 'none' - use placeholder
        addLog('IMAGE GENERATION SKIPPED.');
        setCharImage(UNKNOWN_CHAR_IMAGE);
      }
      
      addLog(`SYNTHESIS SUCCESSFUL.`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'SYNTHESIS FAILED';
      addLog(`CRITICAL_ERROR: ${message}`);
      console.error(error);
    } finally {
      setIsGenerating(false);
    }
  };

  /**
   * Saves the generated character to the active campaign
   */
const handleSave = async () => {
    if (!editableChar) return;
    
    if (!activeCampaignId) {
      addLog('ERROR: NO CAMPAIGN SELECTED');
      return;
    }

    setIsSaving(true);
    addLog('WRITING TO PERSISTENT STORAGE...');
    
    try {
      // Use editableChar for the saved data (includes any user edits)
      await entityService.create(activeCampaignId, {
        entityType: 'character',
        name: editableChar.name,
        description: editableChar.bio,
        imageUrl: charImage !== UNKNOWN_CHAR_IMAGE ? charImage : undefined,
        attributes: {
          ...editableChar.stats
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'character_synth_v2',
          // Store generation parameters for reference
          generationParams: {
            species: form.species.toUpperCase(),
            role: form.role.toUpperCase(),
            morphology: form.morphology
          }
        },
        generationRequestId
      });
      addLog('SUCCESS: DATA COMMITTED TO NUCLEUS');
      setTimeout(onBack, 1000);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'STORAGE REFUSED';
      addLog(`DB_WRITE_ERROR: ${message}`);
    } finally {
      setIsSaving(false);
}
  };

  /**
   * Handle stats changes from EditableStatsPanel
   */
  const handleStatsChange = (newStats: Record<string, unknown>) => {
    if (editableChar) {
      setEditableChar({
        ...editableChar,
        stats: newStats
      });
    }
  };

  /**
   * Handle name change
   */
  const handleNameChange = (value: string | number) => {
    if (editableChar) {
      setEditableChar({
        ...editableChar,
        name: String(value)
      });
    }
  };

  /**
   * Handle bio change
   */
  const handleBioChange = (value: string | number) => {
    if (editableChar) {
      setEditableChar({
        ...editableChar,
        bio: String(value)
      });
    }
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
            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">fingerprint</span> Genotype
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

            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">badge</span> Role
              </label>
              <select 
                value={form.role}
                onChange={(e) => setForm({...form, role: e.target.value})}
                className="w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 focus:ring-primary focus:border-primary text-sm uppercase"
              >
                {ROLE_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2">
                <span className="material-icons text-sm">wc</span> Morphology
              </label>
              <div className="grid grid-cols-3 gap-3">
                {MORPHOLOGY_OPTIONS.map((morph) => (
                  <button
                    key={morph}
                    onClick={() => setForm({...form, morphology: morph})}
                    className={`h-10 border font-mono text-[10px] uppercase transition-all ${
                      form.morphology === morph 
                        ? 'bg-primary text-black border-primary font-bold' 
                        : 'border-primary/30 text-white bg-surface-dark hover:border-primary'
                    }`}
                  >
                    {morph}
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
              icon="blur_on"
            >
              GENERAR
            </Button>
            <Button
              onClick={handleSave}
              disabled={!editableChar || isSaving || isGenerating || !activeCampaignId}
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
                src={charImage} 
                alt="Character Preview"
              />
              {isGenerating && (
                <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 z-20">
                  <div className="w-1/2 h-1 bg-primary/20 relative overflow-hidden mb-2">
                    <div className="absolute inset-0 bg-primary animate-[scan_2s_linear_infinite]"></div>
                  </div>
                  <span className="text-primary text-[10px] animate-pulse">RECONSTRUYENDO_PIXELES...</span>
                </div>
              )}
              <div className="absolute inset-0 pointer-events-none border border-primary/5 opacity-30"></div>
            </div>
<div className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t border-primary/40 backdrop-blur-sm transition-transform duration-500 ${editableChar ? 'translate-y-0' : 'translate-y-full'}`}>
              <EditableField
                value={editableChar?.name || ''}
                label="Nombre"
                variant="primary"
                onChange={handleNameChange}
                disabled={!editableChar}
                className="font-bold"
              />
            </div>
            {!editableChar && !isGenerating && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <span className="text-primary/20 text-[10px] tracking-[0.5em] uppercase font-bold">Sin Datos</span>
              </div>
            )}
          </div>

          {/* Bio Section - Below image, above stats */}
          {editableChar?.bio && (
            <div className="bg-surface-dark/50 border border-primary/20 p-3">
              <EditableField
                value={editableChar.bio}
                label="Bio"
                type="textarea"
                rows={3}
                variant="primary"
                onChange={handleBioChange}
                disabled={!editableChar}
                className="text-sm"
              />
            </div>
          )}

{/* Stats Panel - Dynamic based on game system */}
          <EditableStatsPanel 
            stats={editableChar?.stats || null}
            onStatsChange={handleStatsChange}
            variant="primary"
            maxColumns={3}
            showProgressBar={true}
            maxProgressValue={10}
            fieldDefinitions={fieldDefinitions}
            disabled={!editableChar}
          />
          </div>

          {/* Log Panel - Fixed at bottom */}
          <TerminalLog logs={logs} maxLogs={6} className="h-24 shrink-0" />
        </div>
      </div>
    </TerminalLayout>
  );
};

export default CharacterGeneratorPage;
