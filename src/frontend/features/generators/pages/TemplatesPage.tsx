/**
 * Entity Templates Management Page
 * Allows Admins to extract, view, edit, and confirm entity templates
 * Templates define the schema for entity types (character, npc, vehicle, etc.)
 * Cyberpunk terminal aesthetics with template management features
 */

import React, { useState, useEffect, useCallback } from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useAuth } from '@core/context';
import { entityTemplateService, gameSystemService } from '@core/services/api';
import type { 
  GameSystem,
  EntityTemplateSummary, 
  EntityTemplate,
  ExtractedTemplateInfo 
} from '@core/types';
import { TemplateStatus, TemplateStatusLabels, FieldTypeLabels } from '@core/types';

interface TemplatesPageProps {
  onBack: () => void;
}

/**
 * Templates Page Component
 * Provides UI for managing entity templates (Admin only)
 * - Extract templates from uploaded manuals using RAG
 * - View and edit template field definitions
 * - Confirm templates to make them available for entity creation
 */
export const TemplatesPage: React.FC<TemplatesPageProps> = ({ onBack }) => {
  const { user } = useAuth();
  
  // Data state
  const [gameSystems, setGameSystems] = useState<GameSystem[]>([]);
  const [selectedGameSystem, setSelectedGameSystem] = useState<GameSystem | null>(null);
  const [templates, setTemplates] = useState<EntityTemplateSummary[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<EntityTemplate | null>(null);
  const [templateCounts, setTemplateCounts] = useState({ total: 0, confirmed: 0, pending: 0 });
  
  // UI state
  const [isLoadingGameSystems, setIsLoadingGameSystems] = useState(true);
  const [isLoadingTemplates, setIsLoadingTemplates] = useState(false);
  const [isExtracting, setIsExtracting] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const [isConfirmingAll, setIsConfirmingAll] = useState(false);
  const [extractionResult, setExtractionResult] = useState<ExtractedTemplateInfo[] | null>(null);
  const [newlyExtractedIds, setNewlyExtractedIds] = useState<Set<string>>(new Set());
  
  const [logs, setLogs] = useState([
    '> Template management system online...',
    '> [SUCCESS] Admin protocols established.',
    '> Awaiting commands...'
  ]);

  /**
   * Adds a log entry to the terminal display
   */
  const addLog = useCallback((msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-12));
  }, []);

  /**
   * Fetches all game systems
   */
  const fetchGameSystems = useCallback(async () => {
    setIsLoadingGameSystems(true);
    try {
      const systems = await gameSystemService.getAll();
      setGameSystems(systems);
      addLog(`[SUCCESS] ${systems.length} sistemas cargados`);
      
      // Auto-select first system if available
      if (systems.length > 0 && !selectedGameSystem) {
        setSelectedGameSystem(systems[0]);
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar sistemas';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsLoadingGameSystems(false);
    }
  }, [addLog, selectedGameSystem]);

  /**
   * Fetches templates for the selected game system
   */
  const fetchTemplates = useCallback(async () => {
    if (!selectedGameSystem) return;
    
    setIsLoadingTemplates(true);
    try {
      const result = await entityTemplateService.getByGameSystem(selectedGameSystem.id);
      setTemplates(result.templates);
      setTemplateCounts({
        total: result.totalCount,
        confirmed: result.confirmedCount,
        pending: result.pendingCount,
      });
      addLog(`[SUCCESS] ${result.totalCount} plantillas cargadas`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar plantillas';
      addLog(`ERROR: ${message}`);
      setTemplates([]);
    } finally {
      setIsLoadingTemplates(false);
    }
  }, [selectedGameSystem, addLog]);

  /**
   * Fetches full template details
   */
  const fetchTemplateDetails = useCallback(async (templateId: string) => {
    if (!selectedGameSystem) return;
    
    try {
      const template = await entityTemplateService.getById(selectedGameSystem.id, templateId);
      setSelectedTemplate(template);
      addLog(`Plantilla cargada: ${template.displayName}`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar detalles';
      addLog(`ERROR: ${message}`);
    }
  }, [selectedGameSystem, addLog]);

  // Load game systems on mount
  useEffect(() => {
    fetchGameSystems();
  }, []);

  // Load templates when game system changes
  useEffect(() => {
    if (selectedGameSystem) {
      fetchTemplates();
      setSelectedTemplate(null);
      setExtractionResult(null);
      setNewlyExtractedIds(new Set());
    }
  }, [selectedGameSystem?.id]);

  /**
   * Handles extracting templates from manuals
   */
  const handleExtract = async () => {
    if (!selectedGameSystem) return;
    
    setIsExtracting(true);
    setExtractionResult(null);
    addLog('EXTRAYENDO PLANTILLAS DE MANUALES...');
    addLog('Analizando documentos con IA...');

    try {
      const result = await entityTemplateService.extractFromManual(selectedGameSystem.id);
      
      if (result.errorMessage) {
        addLog(`ERROR: ${result.errorMessage}`);
      } else {
        setExtractionResult(result.templates);
        
        // Track newly extracted template IDs
        const newIds = new Set(
          result.templates
            .filter(t => t.templateId && t.templateId !== '00000000-0000-0000-0000-000000000000')
            .map(t => t.templateId)
        );
        setNewlyExtractedIds(newIds);
        
        addLog(`[SUCCESS] Extracción completada:`);
        addLog(`  - Creadas: ${result.templatesCreated}`);
        addLog(`  - Actualizadas: ${result.templatesUpdated}`);
        addLog(`  - Omitidas: ${result.templatesSkipped}`);
        
        // Refresh templates list
        await fetchTemplates();
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error en extracción';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsExtracting(false);
    }
  };

  /**
   * Handles confirming a single template
   */
  const handleConfirm = async (templateId: string) => {
    if (!selectedGameSystem) return;
    
    setIsConfirming(true);
    addLog('CONFIRMANDO PLANTILLA...');

    try {
      const result = await entityTemplateService.confirm(
        selectedGameSystem.id, 
        templateId,
        { notes: 'Confirmed from Admin UI' }
      );
      addLog(`[SUCCESS] ${result.entityTypeName} confirmado`);
      
      // Refresh templates
      await fetchTemplates();
      
      // Update extraction results if present
      if (extractionResult) {
        setExtractionResult(prev => 
          prev?.map(t => t.templateId === templateId ? { ...t, isNew: false } : t) || null
        );
      }
      
      // Update selected template if it was the one confirmed
      if (selectedTemplate?.id === templateId) {
        await fetchTemplateDetails(templateId);
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al confirmar';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsConfirming(false);
    }
  };

  /**
   * Handles confirming all templates at once
   */
  const handleConfirmAll = async () => {
    if (!selectedGameSystem) return;
    
    const pendingCount = templates.filter(
      t => t.status === TemplateStatus.Draft || t.status === TemplateStatus.PendingReview
    ).length;
    
    if (pendingCount === 0) {
      addLog('No hay plantillas pendientes de confirmar');
      return;
    }
    
    if (!confirm(`¿Confirmar ${pendingCount} plantillas? Esto las habilitará para crear entidades.`)) {
      return;
    }
    
    setIsConfirmingAll(true);
    addLog(`CONFIRMANDO ${pendingCount} PLANTILLAS...`);

    try {
      const result = await entityTemplateService.confirmAll(selectedGameSystem.id);
      addLog(`[SUCCESS] Confirmadas: ${result.confirmed}, Fallidas: ${result.failed}`);
      
      // Clear extraction results and newly extracted IDs
      setExtractionResult(null);
      setNewlyExtractedIds(new Set());
      
      // Refresh templates
      await fetchTemplates();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al confirmar';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsConfirmingAll(false);
    }
  };

  /**
   * Gets the appropriate status color class
   */
  const getStatusColor = (status: TemplateStatus): string => {
    switch (status) {
      case TemplateStatus.Confirmed:
        return 'border-green-500/40 text-green-400 bg-green-500/10';
      case TemplateStatus.PendingReview:
        return 'border-yellow-500/40 text-yellow-400 bg-yellow-500/10';
      case TemplateStatus.Rejected:
        return 'border-red-500/40 text-red-400 bg-red-500/10';
      default:
        return 'border-primary/40 text-primary/60 bg-primary/10';
    }
  };

  /**
   * Check if a template was newly extracted
   */
  const isNewlyExtracted = (templateId: string): boolean => {
    return newlyExtractedIds.has(templateId);
  };

  // Check if user is Admin
  const isAdmin = user?.role === 'ADMIN';
  
  if (!isAdmin) {
    return (
      <TerminalLayout title="TEMPLATES" subtitle="Gestión de plantillas" onLogout={() => {}}>
        <div className="flex flex-col items-center justify-center h-full text-danger/60">
          <span className="material-icons text-6xl mb-4">lock</span>
          <p className="text-sm uppercase tracking-widest">Acceso restringido a Administradores</p>
          <Button onClick={onBack} className="mt-4">VOLVER</Button>
        </div>
      </TerminalLayout>
    );
  }

  return (
    <TerminalLayout title="TEMPLATES" subtitle="Gestión de plantillas de entidad" onLogout={() => {}}>
      <div className="flex flex-col lg:flex-row h-full p-4 lg:p-8 gap-6">
        {/* Left Column - Game Systems & Templates List */}
        <div className="w-full lg:w-1/3 flex flex-col gap-4 overflow-hidden">
          {/* Game System Selector */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-xs text-primary/60 uppercase tracking-widest flex items-center gap-2">
                <span className="material-icons text-sm">sports_esports</span>
                Sistema de Juego
              </h2>
              <button 
                onClick={onBack}
                className="material-icons text-primary/60 hover:text-primary transition-colors text-sm"
                aria-label="Volver"
              >
                arrow_back
              </button>
            </div>
            
            {isLoadingGameSystems ? (
              <div className="text-primary/40 text-sm animate-pulse">Cargando sistemas...</div>
            ) : (
              <select
                value={selectedGameSystem?.id || ''}
                onChange={(e) => {
                  const system = gameSystems.find(s => s.id === e.target.value);
                  setSelectedGameSystem(system || null);
                }}
                className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
              >
                <option value="" disabled>Seleccionar sistema...</option>
                {gameSystems.map((system) => (
                  <option key={system.id} value={system.id}>
                    {system.name} ({system.code})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Actions Panel */}
          {selectedGameSystem && (
            <div className="border border-cyan-500/30 bg-black/60 p-4">
              <h2 className="text-xs text-cyan-500/60 uppercase tracking-widest mb-3 flex items-center gap-2">
                <span className="material-icons text-sm">build</span>
                Acciones
              </h2>
              
              <div className="space-y-2">
                <Button
                  onClick={handleExtract}
                  disabled={isExtracting}
                  className="w-full text-xs"
                  variant="secondary"
                >
                  {isExtracting ? (
                    <>
                      <span className="material-icons text-sm animate-spin mr-2">sync</span>
                      EXTRAYENDO...
                    </>
                  ) : (
                    <>
                      <span className="material-icons text-sm mr-2">auto_awesome</span>
                      EXTRAER DE MANUALES
                    </>
                  )}
                </Button>
                
                <Button
                  onClick={handleConfirmAll}
                  disabled={isConfirmingAll || templateCounts.pending === 0}
                  className="w-full text-xs"
                >
                  {isConfirmingAll ? (
                    <>
                      <span className="material-icons text-sm animate-spin mr-2">sync</span>
                      CONFIRMANDO...
                    </>
                  ) : (
                    <>
                      <span className="material-icons text-sm mr-2">check_circle</span>
                      CONFIRMAR TODAS ({templateCounts.pending})
                    </>
                  )}
                </Button>
              </div>
            </div>
          )}

          {/* Templates Panel - Two Columns */}
          <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
            {/* Panel Header */}
            <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="material-icons text-sm">description</span>
                Plantillas
              </span>
              <button 
                onClick={fetchTemplates}
                disabled={isLoadingTemplates || !selectedGameSystem}
                className="material-icons text-sm text-primary/60 hover:text-primary transition-colors disabled:opacity-50"
                title="Recargar"
              >
                refresh
              </button>
            </div>
            
            {/* Two Column Content */}
            <div className="flex-1 flex overflow-hidden">
              {/* Left Column - Extracted Templates */}
              <div className="w-1/2 flex flex-col border-r border-primary/20">
                <div className="bg-orange-500/10 p-2 text-[10px] text-orange-400 uppercase tracking-widest flex items-center justify-between border-b border-orange-500/20">
                  <span className="flex items-center gap-1">
                    <span className="material-icons text-xs">auto_awesome</span>
                    Extraídas ({extractionResult?.length || 0})
                  </span>
                </div>
                
                <div className="flex-1 overflow-y-auto p-2">
                  {isLoadingTemplates ? (
                    <div className="flex items-center justify-center h-full text-primary/40">
                      <span className="animate-pulse text-xs">CARGANDO...</span>
                    </div>
                  ) : !selectedGameSystem ? (
                    <div className="flex flex-col items-center justify-center h-full text-primary/40">
                      <span className="material-icons text-3xl mb-2">sports_esports</span>
                      <p className="text-[10px] uppercase">Selecciona un sistema</p>
                    </div>
                  ) : !extractionResult || extractionResult.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-full text-orange-400/40">
                      <span className="material-icons text-3xl mb-2">auto_awesome</span>
                      <p className="text-[10px] uppercase text-center">Sin extracción activa</p>
                      <p className="text-[9px] mt-1 text-center">Ejecuta "Extraer de Manuales"</p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {extractionResult.map((info) => {
                        const hasValidId = info.templateId && info.templateId !== '00000000-0000-0000-0000-000000000000';
                        const templateInDb = hasValidId ? templates.find(t => t.id === info.templateId) : null;
                        const canConfirm = templateInDb && (templateInDb.status === TemplateStatus.Draft || templateInDb.status === TemplateStatus.PendingReview);
                        const isConfirmedStatus = templateInDb?.status === TemplateStatus.Confirmed;
                        
                        return (
                          <div
                            key={info.templateId || info.entityTypeName}
                            onClick={() => hasValidId && fetchTemplateDetails(info.templateId)}
                            className={`border p-2 cursor-pointer transition-all ${
                              selectedTemplate?.id === info.templateId
                                ? 'border-cyan-500 bg-cyan-500/10'
                                : info.extractionNotes
                                ? 'border-red-500/40 bg-red-500/5 hover:border-red-500/60'
                                : info.isNew
                                ? 'border-green-500/40 bg-green-500/5 hover:border-green-500/60'
                                : 'border-yellow-500/40 bg-yellow-500/5 hover:border-yellow-500/60'
                            }`}
                          >
                            <div className="flex items-start justify-between">
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-1 flex-wrap">
                                  <span className="font-mono text-cyan-400 text-[10px] truncate">
                                    {info.entityTypeName}
                                  </span>
                                  {info.extractionNotes ? (
                                    <span className="text-[8px] px-1 py-0.5 border border-red-500/60 text-red-400 bg-red-500/20">
                                      ERROR
                                    </span>
                                  ) : info.isNew ? (
                                    <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                                      NUEVA
                                    </span>
                                  ) : (
                                    <span className="text-[8px] px-1 py-0.5 border border-yellow-500/60 text-yellow-400 bg-yellow-500/20">
                                      ACTUALIZADA
                                    </span>
                                  )}
                                  {isConfirmedStatus && (
                                    <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                                      CONFIRMADA
                                    </span>
                                  )}
                                </div>
                                <p className="text-xs text-primary mt-1 truncate">{info.displayName}</p>
                                <p className="text-[9px] text-primary/40">
                                  {info.fieldCount} campos
                                </p>
                                {info.extractionNotes && (
                                  <p className="text-[9px] text-red-400/80 mt-1 truncate" title={info.extractionNotes}>
                                    {info.extractionNotes}
                                  </p>
                                )}
                              </div>
                              
                              {canConfirm && (
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    handleConfirm(info.templateId);
                                  }}
                                  disabled={isConfirming}
                                  className="material-icons text-sm text-orange-400/60 hover:text-green-500 transition-colors ml-1"
                                  title="Confirmar plantilla"
                                >
                                  check_circle_outline
                                </button>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>

              {/* Right Column - Saved Templates */}
              <div className="w-1/2 flex flex-col">
                <div className="bg-primary/10 p-2 text-[10px] text-primary uppercase tracking-widest flex items-center justify-between border-b border-primary/20">
                  <span className="flex items-center gap-1">
                    <span className="material-icons text-xs">storage</span>
                    Guardadas ({templates.length})
                  </span>
                </div>
                
                <div className="flex-1 overflow-y-auto p-2">
                  {isLoadingTemplates ? (
                    <div className="flex items-center justify-center h-full text-primary/40">
                      <span className="animate-pulse text-xs">CARGANDO...</span>
                    </div>
                  ) : !selectedGameSystem ? (
                    <div className="flex flex-col items-center justify-center h-full text-primary/40">
                      <span className="material-icons text-3xl mb-2">sports_esports</span>
                      <p className="text-[10px] uppercase">Selecciona un sistema</p>
                    </div>
                  ) : templates.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-full text-primary/40">
                      <span className="material-icons text-3xl mb-2">inventory_2</span>
                      <p className="text-[10px] uppercase text-center">No hay plantillas</p>
                      <p className="text-[9px] mt-1 text-center">Extrae de los manuales</p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {templates.map((template) => (
                        <div
                          key={template.id}
                          onClick={() => fetchTemplateDetails(template.id)}
                          className={`border p-2 cursor-pointer transition-all ${
                            selectedTemplate?.id === template.id
                              ? 'border-cyan-500 bg-cyan-500/10'
                              : template.status === TemplateStatus.Confirmed
                              ? 'border-green-500/30 bg-green-500/5 hover:border-green-500/50'
                              : 'border-yellow-500/30 bg-yellow-500/5 hover:border-yellow-500/50'
                          }`}
                        >
                          <div className="flex items-start justify-between">
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-1 flex-wrap">
                                <span className="font-mono text-cyan-400 text-[10px] truncate">
                                  {template.entityTypeName}
                                </span>
                                <span className={`text-[8px] px-1 py-0.5 border ${getStatusColor(template.status)}`}>
                                  {TemplateStatusLabels[template.status]}
                                </span>
                                {isNewlyExtracted(template.id) && (
                                  <span className="text-[8px] px-1 py-0.5 border border-orange-500/60 text-orange-400 bg-orange-500/20 animate-pulse">
                                    NUEVA
                                  </span>
                                )}
                              </div>
                              <p className="text-xs text-primary mt-1 truncate">{template.displayName}</p>
                              <p className="text-[9px] text-primary/40">
                                {template.fieldCount} campos
                              </p>
                            </div>
                            
                            {(template.status === TemplateStatus.Draft || template.status === TemplateStatus.PendingReview) && (
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleConfirm(template.id);
                                }}
                                disabled={isConfirming}
                                className="material-icons text-sm text-yellow-500/60 hover:text-green-500 transition-colors ml-1"
                                title="Confirmar plantilla"
                              >
                                check_circle_outline
                              </button>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Middle Column - Template Details */}
        <div className="flex-1 flex flex-col gap-4 overflow-hidden">
          {selectedTemplate ? (
            <>
              {/* Template Header */}
              <div className="border border-primary/30 bg-black/60 p-4">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <h2 className="text-lg text-primary font-bold">{selectedTemplate.displayName}</h2>
                      <span className={`text-xs px-2 py-0.5 border ${getStatusColor(selectedTemplate.status)}`}>
                        {TemplateStatusLabels[selectedTemplate.status]}
                      </span>
                      {isNewlyExtracted(selectedTemplate.id) && (
                        <span className="text-xs px-2 py-0.5 border border-orange-500/60 text-orange-400 bg-orange-500/20">
                          RECIÉN EXTRAÍDA
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-cyan-400 font-mono mt-1">{selectedTemplate.entityTypeName}</p>
                    {selectedTemplate.description && (
                      <p className="text-sm text-primary/60 mt-2">{selectedTemplate.description}</p>
                    )}
                  </div>
                  
                  {(selectedTemplate.status === TemplateStatus.Draft || selectedTemplate.status === TemplateStatus.PendingReview) && (
                    <Button
                      onClick={() => handleConfirm(selectedTemplate.id)}
                      disabled={isConfirming}
                      size="sm"
                    >
                      {isConfirming ? 'CONFIRMANDO...' : 'CONFIRMAR'}
                    </Button>
                  )}
                </div>
                
                {/* Template Metadata */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mt-4 text-xs">
                  <div>
                    <span className="text-primary/40">Sistema:</span>
                    <p className="text-primary">{selectedTemplate.gameSystemName}</p>
                  </div>
                  {selectedTemplate.version && (
                    <div>
                      <span className="text-primary/40">Versión:</span>
                      <p className="text-primary">{selectedTemplate.version}</p>
                    </div>
                  )}
                  <div>
                    <span className="text-primary/40">Campos:</span>
                    <p className="text-primary">{selectedTemplate.fields.length}</p>
                  </div>
                  {selectedTemplate.confirmedAt && (
                    <div>
                      <span className="text-primary/40">Confirmado:</span>
                      <p className="text-green-400">
                        {new Date(selectedTemplate.confirmedAt).toLocaleDateString()}
                      </p>
                    </div>
                  )}
                </div>
              </div>

              {/* Fields List */}
              <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
                <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest">
                  <span className="flex items-center gap-2">
                    <span className="material-icons text-sm">list</span>
                    Definición de Campos ({selectedTemplate.fields.length})
                  </span>
                </div>
                
                <div className="flex-1 overflow-y-auto p-4">
                  {selectedTemplate.fields.length === 0 ? (
                    <div className="text-center text-primary/40 py-8">
                      <span className="material-icons text-4xl mb-2">warning</span>
                      <p className="text-xs uppercase">Sin campos definidos</p>
                      <p className="text-[10px] mt-1">La plantilla necesita campos para funcionar</p>
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {selectedTemplate.fields
                        .sort((a, b) => a.order - b.order)
                        .map((field, index) => (
                          <div 
                            key={field.name}
                            className="border border-primary/20 bg-black/40 p-3"
                          >
                            <div className="flex items-start justify-between">
                              <div className="flex-1">
                                <div className="flex items-center gap-2">
                                  <span className="text-xs text-primary/40">#{index + 1}</span>
                                  <span className="font-mono text-cyan-400 text-sm">{field.name}</span>
                                  {field.isRequired && (
                                    <span className="text-danger text-xs">*</span>
                                  )}
                                </div>
                                <p className="text-sm text-primary mt-1">{field.displayName}</p>
                                {field.description && (
                                  <p className="text-xs text-primary/50 mt-1">{field.description}</p>
                                )}
                              </div>
                              <span className="text-[10px] px-2 py-1 bg-primary/10 border border-primary/20 text-primary/60">
                                {FieldTypeLabels[field.fieldType]}
                              </span>
                            </div>
                            
                            {/* Field constraints */}
                            <div className="mt-2 flex flex-wrap gap-2 text-[10px]">
                              {field.defaultValue && (
                                <span className="text-primary/40">
                                  Default: <span className="text-primary/60">{field.defaultValue}</span>
                                </span>
                              )}
                              {field.minValue !== undefined && field.minValue !== null && (
                                <span className="text-primary/40">
                                  Min: <span className="text-primary/60">{field.minValue}</span>
                                </span>
                              )}
                              {field.maxValue !== undefined && field.maxValue !== null && (
                                <span className="text-primary/40">
                                  Max: <span className="text-primary/60">{field.maxValue}</span>
                                </span>
                              )}
                              {field.options && field.options.length > 0 && (
                                <span className="text-primary/40">
                                  Opciones: <span className="text-primary/60">{field.options.join(', ')}</span>
                                </span>
                              )}
                            </div>
                          </div>
                        ))}
                    </div>
                  )}
                </div>
              </div>
            </>
          ) : (
            <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col items-center justify-center text-primary/40">
              <span className="material-icons text-6xl mb-4">description</span>
              <p className="text-sm uppercase">Selecciona una plantilla</p>
              <p className="text-xs mt-1">para ver sus detalles y campos</p>
            </div>
          )}
        </div>

        {/* Right Column - Stats & Terminal Log */}
        <div className="w-full lg:w-64 flex flex-col gap-4">
          {/* Stats Panel */}
          {selectedGameSystem && (
            <div className="border border-primary/30 bg-black/60 p-3">
              <h3 className="text-xs text-primary/60 uppercase tracking-widest mb-3 flex items-center gap-2">
                <span className="material-icons text-sm">analytics</span>
                Estadísticas
              </h3>
              <div className="grid grid-cols-3 gap-2 text-center">
                <div className="bg-black/40 border border-primary/20 p-2">
                  <p className="text-lg font-bold text-primary">{templateCounts.total}</p>
                  <p className="text-[10px] text-primary/40 uppercase">Total</p>
                </div>
                <div className="bg-black/40 border border-green-500/20 p-2">
                  <p className="text-lg font-bold text-green-400">{templateCounts.confirmed}</p>
                  <p className="text-[10px] text-green-400/60 uppercase">Activas</p>
                </div>
                <div className="bg-black/40 border border-yellow-500/20 p-2">
                  <p className="text-lg font-bold text-yellow-400">{templateCounts.pending}</p>
                  <p className="text-[10px] text-yellow-400/60 uppercase">Pendientes</p>
                </div>
              </div>
            </div>
          )}

          {/* System Log */}
          <div className="flex-1 flex flex-col border border-primary/30 bg-black/80">
            <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
              <span className="material-icons text-sm">terminal</span>
              System Log
            </div>
            <div className="flex-1 p-4 font-mono text-xs text-primary/70 space-y-1 overflow-y-auto">
              {logs.map((log, i) => (
                <p 
                  key={i} 
                  className={`${
                    log.includes('ERROR') ? 'text-danger' : 
                    log.includes('SUCCESS') ? 'text-green-400' : ''
                  }`}
                >
                  {log}
                </p>
              ))}
              <p className="animate-pulse">_</p>
            </div>
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};
