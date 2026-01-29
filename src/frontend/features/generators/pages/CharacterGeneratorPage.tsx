/**
 * Character Generator Page
 * AI-powered character generation with cyberpunk terminal aesthetics
 * Uses campaign context for entity creation
 */

import React, { useState } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { aiService, entityService } from '@core/services/api';
import { useCampaign } from '@core/context';
import type { CharacterData } from '@core/types';

interface CharacterGeneratorPageProps {
  onBack: () => void;
}

const UNKNOWN_CHAR_IMAGE = "https://images.unsplash.com/photo-1518020382113-a7e8fc38eac9?q=80&w=400&auto=format&fit=crop";

const SPECIES_OPTIONS = [
  { value: '', label: 'Seleccionar Genotipo...' },
  { value: 'human', label: 'Humano' },
  { value: 'android', label: 'Androide' },
  { value: 'xenomorph', label: 'Xenomorfo' },
  { value: 'cyber-enhanced', label: 'Cyber-Aumentado' },
];

const ROLE_OPTIONS = [
  { value: '', label: 'Seleccionar Función...' },
  { value: 'operative', label: 'Operativo' },
  { value: 'hacker', label: 'Netrunner' },
  { value: 'medic', label: 'Médico de Combate' },
  { value: 'bounty-hunter', label: 'Caza-Recompensas' },
];

const MORPHOLOGY_OPTIONS = ['MASCULINE', 'FEMININE', 'NEUTRAL'] as const;

export const CharacterGeneratorPage: React.FC<CharacterGeneratorPageProps> = ({ onBack }) => {
  const { activeCampaignId, activeCampaign } = useCampaign();
  
  const [logs, setLogs] = useState([
    '> System initialization sequence started...',
    '> [SUCCESS] Neural link established.',
    '> Awaiting user input parameters...'
  ]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedChar, setGeneratedChar] = useState<CharacterData | null>(null);
  const [charImage, setCharImage] = useState<string>(UNKNOWN_CHAR_IMAGE);

  const [form, setForm] = useState({
    species: '',
    role: '',
    morphology: 'NEUTRAL',
    attire: 'Techwear'
  });

  /**
   * Adds a log entry to the terminal display
   */
  const addLog = (msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-6));
  };

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
      
      const result = await aiService.generateCharacter({
        species: form.species,
        role: form.role,
        morphology: form.morphology,
        attire: form.attire
      });

      const charData = JSON.parse(result.characterJson) as CharacterData;
      setGeneratedChar(charData);
      addLog(`DATA RECEIVED: ${charData.name.toUpperCase()}`);

      addLog('GENERATING VISUAL REPRESENTATION...');
      if (result.imageBase64) {
        setCharImage(`data:image/png;base64,${result.imageBase64}`);
        addLog('VISUAL SYNTHESIS COMPLETE.');
      } else if (result.imageUrl) {
        setCharImage(result.imageUrl);
        addLog('VISUAL SYNTHESIS COMPLETE.');
      } else {
        addLog('WARNING: VISUAL RENDER FAILED. USING PLACEHOLDER.');
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
    if (!generatedChar) return;
    
    if (!activeCampaignId) {
      addLog('ERROR: NO CAMPAIGN SELECTED');
      return;
    }

    setIsSaving(true);
    addLog('WRITING TO PERSISTENT STORAGE...');
    
    try {
      await entityService.create(activeCampaignId, {
        entityType: 'character',
        name: generatedChar.name,
        description: generatedChar.bio,
        imageUrl: charImage !== UNKNOWN_CHAR_IMAGE ? charImage : undefined,
        attributes: {
          species: form.species.toUpperCase(),
          role: form.role.toUpperCase(),
          morphology: form.morphology,
          ...generatedChar.stats
        },
        metadata: {
          generatedAt: new Date().toISOString(),
          generator: 'character_synth_v2'
        }
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

  return (
    <TerminalLayout 
      title="Character_Synth // V.2.0" 
      subtitle={`Campaña: ${activeCampaign?.name || 'N/A'} // Sintetizador Biométrico`}
      actions={
        <button onClick={onBack} className="text-primary/60 hover:text-primary transition-colors flex items-center gap-1 text-xs font-mono uppercase">
          <span className="material-icons text-sm">arrow_back</span> VOLVER
        </button>
      }
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
              GENERAR_SYNTH
            </Button>
            <Button
              onClick={handleSave}
              disabled={!generatedChar || isSaving || isGenerating || !activeCampaignId}
              variant="primary"
              size="lg"
              isLoading={isSaving}
              icon="save"
            >
              GUARDAR_NUCLEO
            </Button>
          </div>
        </div>

        {/* Preview Panel */}
        <div className="flex-1 flex flex-col gap-4">
          <div className="relative w-full aspect-square border border-primary/30 bg-black p-1 flex flex-col overflow-hidden clip-tech-br group">
            <div className="relative flex-1 bg-black overflow-hidden flex items-center justify-center">
              <img 
                className={`w-full h-full object-cover transition-all duration-1000 ${isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-80 scale-100'} grayscale brightness-90`} 
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
            <div className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t border-primary/40 backdrop-blur-sm transition-transform duration-500 ${generatedChar ? 'translate-y-0' : 'translate-y-full'}`}>
              <p className="font-bold text-primary text-sm uppercase mb-1">{generatedChar?.name}</p>
              <p className="text-[9px] text-white/80 line-clamp-3 leading-tight font-mono">{generatedChar?.bio}</p>
            </div>
            {!generatedChar && !isGenerating && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <span className="text-primary/20 text-[10px] tracking-[0.5em] uppercase font-bold">Sin Datos</span>
              </div>
            )}
          </div>

          {/* Log Panel */}
          <div className="h-24 bg-black/80 border border-primary/20 p-3 text-[10px] text-primary/80 overflow-y-auto font-mono scrollbar-hide">
            {logs.map((log, i) => <p key={i} className={i === logs.length - 1 ? "text-primary font-bold" : "opacity-60"}>{log}</p>)}
          </div>

          {/* Stats Panel */}
          <div className="grid grid-cols-3 gap-2">
            {[
              { label: 'STR', val: generatedChar?.stats?.STR || '--' },
              { label: 'INT', val: generatedChar?.stats?.INT || '--' },
              { label: 'DEX', val: generatedChar?.stats?.DEX || '--' }
            ].map(stat => (
              <div key={stat.label} className="bg-surface-dark border border-primary/20 p-2 text-center relative overflow-hidden">
                <p className="text-[9px] text-primary/40 uppercase mb-1">{stat.label}</p>
                <p className="text-lg font-bold text-primary font-mono">{stat.val}</p>
                <div className="absolute bottom-0 left-0 h-0.5 bg-primary/20" style={{ width: stat.val !== '--' ? `${stat.val}%` : '0%' }}></div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};

export default CharacterGeneratorPage;
