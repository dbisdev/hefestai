/**
 * Templates Page (Refactored)
 * Single Responsibility: Orchestrate template management UI
 * Uses custom hooks and components (DRY, SOLID-SRP)
 * Restored: 2-column view, stats panel, comparison functionality
 */

import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { AdminLayout, TerminalLayout } from '@shared/components/layout';
import { Button, TerminalLog, EmptyState, ConfirmDialog, Select } from '@shared/components/ui';
import { useAuth } from '@core/context';
import { useTerminalLog, useConfirmDialog } from '@core/hooks';
import { gameSystemService, documentService } from '@core/services/api';
import { 
  useTemplates,
  useTemplateFields,
} from '@features/generators/hooks';
import { 
  TemplateList,
  TemplateFieldEditor,
  GameSystemSelector,
  ExtractionResultList,
  ComparisonPanel,
} from '@features/generators/components/templates';
import { CANONICAL_ENTITY_TYPE_OPTIONS } from '@features/gallery/constants/categories';
import type { 
  GameSystem,
  FieldDefinition,
} from '@core/types';
import { TemplateStatus, TemplateStatusLabels, FieldTypeLabels } from '@core/types';

export const TemplatesPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const urlGameSystemId = searchParams.get('gameSystemId');
  const { user } = useAuth();
  const { logs, addLog } = useTerminalLog({
    maxLogs: 12,
    initialLogs: [
      '> Template management system online...',
      '> [SUCCESS] Admin protocols established.',
      '> Awaiting commands...'
    ]
  });

  const [gameSystems, setGameSystems] = useState<GameSystem[]>([]);
  const [selectedGameSystem, setSelectedGameSystem] = useState<GameSystem | null>(null);
  const [isLoadingGameSystems, setIsLoadingGameSystems] = useState(true);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [hasDocuments, setHasDocuments] = useState(false);
  const [showDeviceWarning, setShowDeviceWarning] = useState(false);

  useEffect(() => {
    if (window.innerWidth < 1024) {
      setShowDeviceWarning(true);
    }
  }, []);

  const {
    isOpen: isConfirmOpen, 
    config: confirmConfig, 
    confirm, 
    handleConfirm, 
    handleCancel 
  } = useConfirmDialog();

  const isAdmin = user?.role === 'ADMIN';
  const isMaster = user?.role === 'MASTER';
  const isOwner = selectedGameSystem?.ownerId === user?.id;

  const {
    templates,
    selectedTemplate,
    counts,
    isLoading: isLoadingTemplates,
    isConfirming,
    isExtracting,
    isSavingFields,
    extractionResult,
    newlyExtractedIds,
    comparisonExtractedFields,
    comparisonTemplateName,
    selectTemplate,
    confirmTemplate,
    confirmAll,
    extractTemplates,
    refresh: refreshTemplates,
    refreshSelectedTemplate,
    clearSelectedTemplate,
    isNewlyExtracted,
    viewSkippedExtraction,
    addFieldFromComparison,
    addAllNewFieldsFromComparison,
    closeComparison,
    revertToDraft,
    isReverting,
  } = useTemplates({
    gameSystemId: selectedGameSystem?.id ?? null,
    userId: user?.id,
    userRole: user?.role,
    isOwner,
    onLog: addLog,
  });

  const {
    isEditing,
    isSaving,
    startEditing,
    cancelEditing,
    saveFields,
    updateEntityTypeName,
  } = useTemplateFields({
    gameSystemId: selectedGameSystem?.id ?? null,
    onSuccess: () => {
      addLog('[SUCCESS] Campos actualizados');
      refreshTemplates();
      refreshSelectedTemplate();
    },
  });

  useEffect(() => {
    const fetchGameSystems = async () => {
      setIsLoadingGameSystems(true);
      try {
        const systems = await gameSystemService.getAll();
        setGameSystems(systems);
        addLog(`[SUCCESS] ${systems.length} sistemas cargados`);
        
        if (systems.length > 0) {
          const selected = urlGameSystemId 
            ? systems.find(s => s.id === urlGameSystemId) ?? systems[0]
            : systems[0];
          setSelectedGameSystem(selected);
        }
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Error loading systems';
        addLog(`ERROR: ${message}`);
      } finally {
        setIsLoadingGameSystems(false);
      }
    };

    fetchGameSystems();
  }, [addLog, urlGameSystemId]);

  useEffect(() => {
    const checkDocuments = async () => {
      if (!selectedGameSystem) {
        setHasDocuments(false);
        return;
      }
      try {
        const result = await documentService.checkDocumentAvailability(selectedGameSystem.id);
        setHasDocuments(result.hasDocuments);
      } catch {
        setHasDocuments(false);
      }
    };
    checkDocuments();
  }, [selectedGameSystem?.id]);

  const handleSelectGameSystem = (systemId: string) => {
    const system = gameSystems.find(s => s.id === systemId);
    if (system) {
      setSelectedGameSystem(system);
      clearSelectedTemplate();
    }
  };

  const handleSelectTemplate = async (templateId: string) => {
    await selectTemplate(templateId);
    addLog(`Plantilla cargada`);
  };

  const handleConfirmTemplate = async (templateId: string) => {
    const confirmed = await confirm({
      title: 'Confirmar Plantilla',
      message: '¿Confirmar esta plantilla? Estará disponible para crear entidades.',
      confirmLabel: 'CONFIRMAR',
      variant: 'info',
    });

    if (confirmed) {
      setConfirmingId(templateId);
      const success = await confirmTemplate(templateId);
      setConfirmingId(null);
    }
  };

  const handleConfirmAll = async () => {
    const confirmed = await confirm({
      title: 'Confirmar Todas',
      message: `¿Confirmar ${counts.pending} plantillas pendientes?`,
      confirmLabel: 'CONFIRMAR TODAS',
      variant: 'warning',
    });

    if (confirmed) {
      await confirmAll();
    }
  };

  const handleExtract = async () => {
    if (!hasDocuments) {
      addLog('[ERROR] No hay manuales RAG disponibles para este sistema.');
      addLog('Sube manuales primero desde Sistemas de Juego.');
      return;
    }
    addLog('EXTRAYENDO PLANTILLAS DE MANUALES...');
    addLog('Analizando documentos con IA...');
    await extractTemplates();
  };

  const handleSaveFields = async (fields: FieldDefinition[]) => {
    if (!selectedTemplate) return false;
    return saveFields(selectedTemplate.id, fields, {
      displayName: selectedTemplate.displayName,
      description: selectedTemplate.description,
      iconHint: selectedTemplate.iconHint,
      version: selectedTemplate.version,
    });
  };

  const handleEntityTypeChange = async (newEntityType: string) => {
    if (!selectedTemplate) return;
    
    const success = await updateEntityTypeName(
      selectedTemplate.id,
      newEntityType,
      {
        displayName: selectedTemplate.displayName,
        description: selectedTemplate.description,
        iconHint: selectedTemplate.iconHint,
        version: selectedTemplate.version,
      }
    );
    
    if (success) {
      addLog(`[SUCCESS] Tipo cambiado a "${newEntityType}"`);
    }
  };

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

  const canAccess = isAdmin || (isMaster && isOwner);

  const loadingContent = (
    <div className="flex items-center justify-center h-full">
      <div className="text-primary animate-pulse">Cargando...</div>
    </div>
  );

  const accessDeniedContent = (
    <div className="flex flex-col items-center justify-center h-full text-danger/60">
      <span className="material-icons text-6xl mb-4">lock</span>
      <p className="text-sm uppercase tracking-widest">Acceso restringido</p>
      <Button onClick={() => navigate(-1)} className="mt-4">VOLVER</Button>
    </div>
  );

  const systemAccessDeniedContent = (
    <div className="flex flex-col items-center justify-center h-full text-danger/60">
      <span className="material-icons text-6xl mb-4">lock</span>
      <p className="text-sm uppercase tracking-widest">No tienes acceso a este sistema</p>
      <Button onClick={() => navigate('/game-systems')} className="mt-4">VOLVER A SISTEMAS</Button>
    </div>
  );

  if (isLoadingGameSystems) {
    return isAdmin ? (
      <AdminLayout>{loadingContent}</AdminLayout>
    ) : (
      <TerminalLayout title="PLANTILLAS">{loadingContent}</TerminalLayout>
    );
  }

  if (!isAdmin && !isMaster) {
    return isAdmin ? (
      <AdminLayout>{accessDeniedContent}</AdminLayout>
    ) : (
      <TerminalLayout title="PLANTILLAS">{accessDeniedContent}</TerminalLayout>
    );
  }

  if (isMaster && !isAdmin && selectedGameSystem && !isOwner) {
    return isAdmin ? (
      <AdminLayout>{systemAccessDeniedContent}</AdminLayout>
    ) : (
      <TerminalLayout title="PLANTILLAS">{systemAccessDeniedContent}</TerminalLayout>
    );
  }

  const mainContent = (
    <div className="flex flex-col md:grid md:grid-cols-2 md:grid-rows-[250px_110px_1fr] md:h-screen md:overflow-hidden lg:flex lg:flex-row lg:h-full lg:gap-6">
      {/* Left Column - Game Systems, Templates, Stats & Log */}
      <div className="w-full md:contents lg:w-[30%] lg:flex lg:flex-col lg:gap-4 lg:overflow-hidden">
        {/* Game System & Actions - Combined Panel */}
        <div className="border border-primary/30 bg-black/60 p-4 md:col-start-1 md:row-start-1">
          <h2 className="text-xs text-primary/60 uppercase tracking-widest mb-3 flex items-center gap-2">
            <span className="material-icons text-sm">sports_esports</span>
            Sistema de Juego
          </h2>
          
          {isMaster && !isAdmin ? (
            <div className="bg-cyan-900/20 border border-cyan-500/30 p-3 rounded">
              <div className="flex items-center gap-2">
                <span className="material-icons text-cyan-400 text-sm">check_circle</span>
                <span className="text-cyan-400 text-sm font-medium">
                  {selectedGameSystem?.name}
                </span>
              </div>
              {selectedGameSystem?.code && (
                <div className="text-primary/40 text-xs mt-1 ml-6">
                  {selectedGameSystem.code}
                </div>
              )}
            </div>
          ) : (
            <GameSystemSelector
              systems={gameSystems}
              selectedId={selectedGameSystem?.id ?? null}
              onSelect={handleSelectGameSystem}
            />
          )}
          
          {/* Actions - inside same panel */}
          {selectedGameSystem && (
            <div className="mt-4">
              <div className="space-y-2">
                <Button
                  onClick={handleExtract}
                  disabled={isExtracting}
                  fullWidth
                  variant="secondary"
                  icon="auto_awesome"
                  className={!hasDocuments ? 'opacity-50' : ''}
                >
                  {isExtracting ? 'EXTRAYENDO...' : 'EXTRAER DE MANUALES'}
                </Button>
                
                {counts.pending > 0 && (
                  <Button
                    onClick={handleConfirmAll}
                    disabled={isConfirming}
                    fullWidth
                    variant="primary"
                    icon="check_circle"
                  >
                    {isConfirming ? 'CONFIRMANDO...' : `CONFIRMAR TODAS (${counts.pending})`}
                  </Button>
                )}
              </div>
            </div>
          )}
        </div>

        {/* System Log */}
        {selectedGameSystem && (
          <div className="flex flex-col border border-primary/30 bg-black/80 md:col-start-1 md:row-start-2">
              <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
                <span className="material-icons text-sm">terminal</span>
                System Log
              </div>
              <div className="h-24 p-4 font-mono text-xs text-primary/70 space-y-1 overflow-y-auto">
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
        )}

        {/* Templates Panel - Two Columns */}
        <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden md:col-start-2 md:row-span-2 lg:h-auto lg:flex-1">
          <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
            <span className="flex items-center gap-2">
              <span className="material-icons text-sm">description</span>
              Plantillas
            </span>
            <button 
              onClick={() => refreshTemplates()}
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
                {!selectedGameSystem ? (
                  <div className="flex flex-col items-center justify-center h-full text-primary/40">
                    <span className="material-icons text-3xl mb-2">sports_esports</span>
                    <p className="text-[10px] uppercase">Selecciona un sistema</p>
                  </div>
                ) : (
                  <ExtractionResultList
                    extractionResult={extractionResult || []}
                    templates={templates}
                    selectedId={selectedTemplate?.id ?? null}
                    onSelect={handleSelectTemplate}
                    onViewComparison={viewSkippedExtraction}
                    onConfirm={handleConfirmTemplate}
                    confirmingId={confirmingId}
                    isNewlyExtracted={isNewlyExtracted}
                    isLoading={isLoadingTemplates}
                  />
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
                {!selectedGameSystem ? (
                  <div className="flex flex-col items-center justify-center h-full text-primary/40">
                    <span className="material-icons text-3xl mb-2">sports_esports</span>
                    <p className="text-[10px] uppercase">Selecciona un sistema</p>
                  </div>
                ) : (
                  <TemplateList
                    templates={templates}
                    selectedId={selectedTemplate?.id ?? null}
                    onSelect={handleSelectTemplate}
                    onConfirm={handleConfirmTemplate}
                    confirmingId={confirmingId}
                    canConfirm={canAccess}
                    isLoading={isLoadingTemplates}
                    isNewlyExtracted={isNewlyExtracted}
                  />
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Middle Column - Template Details */}
      <div className="flex-1 flex flex-col gap-4 overflow-hidden md:col-start-1 md:row-start-3 md:col-span-2 md:overflow-hidden lg:flex-1">
        {selectedTemplate ? (
          <>
            {/* Template Header */}
            <div className="border border-primary/30 bg-black/60 p-4 w-full">
              <div className="w-full">
                <div className="w-full">
                  <div className="flex items-center gap-3 flex-wrap">
                    <h2 className="text-lg text-primary font-bold">{selectedTemplate.displayName}</h2>
                    <span className={`text-xs px-2 py-0.5 border ${getStatusColor(selectedTemplate.status)}`}>
                      {TemplateStatusLabels[selectedTemplate.status]}
                    </span>
                    {isNewlyExtracted(selectedTemplate.id) && (
                      <span className="text-xs px-2 py-0.5 border border-orange-500/60 text-orange-400 bg-orange-500/20 animate-pulse">
                        RECIÉN EXTRAÍDA
                      </span>
                    )}
                  </div>
                  <div className="mt-2 w-full">
                    <Select
                      label="Tipo de Entidad"
                      options={CANONICAL_ENTITY_TYPE_OPTIONS}
                      value={selectedTemplate.entityTypeName}
                      onChange={(e) => handleEntityTypeChange(e.target.value)}
                      disabled={isSaving || (selectedTemplate.status === TemplateStatus.Confirmed && !isAdmin)}
                      className="text-xs"
                    />
                  </div>
                  {selectedTemplate.description && (
                    <p className="text-sm text-primary/60 mt-2">{selectedTemplate.description}</p>
                  )}
                </div>
                
                {/* Template Metadata */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mt-4 text-xs w-full">
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

                {/* Action Buttons */}
                <div className="mt-4 flex justify-end gap-2">
                  {(selectedTemplate.status === TemplateStatus.Draft || selectedTemplate.status === TemplateStatus.PendingReview) && (
                    <Button
                      onClick={() => handleConfirmTemplate(selectedTemplate.id)}
                      disabled={isConfirming}
                      size="sm"
                      icon="check_circle"
                    >
                      {isConfirming ? 'CONFIRMANDO...' : 'CONFIRMAR'}
                    </Button>
                  )}
                  
                  {selectedTemplate.status === TemplateStatus.Confirmed && (
                    <Button
                      onClick={async () => {
                        const confirmed = await confirm({
                          title: 'Revertir a Borrador',
                          message: '¿Estás seguro de que quieres revertir esta plantilla a borrador? Esto permitirá modificar sus campos.',
                          confirmLabel: 'REVERTIR',
                          variant: 'warning',
                        });
                        if (confirmed) {
                          await revertToDraft();
                        }
                      }}
                      disabled={isReverting}
                      size="sm"
                      icon="undo"
                      variant="secondary"
                    >
                      {isReverting ? 'REVIRTIENDO...' : 'REVERTIR A BORRADOR'}
                    </Button>
                  )}
                </div>
              </div>
            </div>

            {/* Two Column Layout - New Fields (Left) & Existing Fields (Right) */}
            <div className="flex-1 flex gap-4 overflow-hidden min-h-0 md:h-full md:min-h-0">
              {/* Left Column - New Extracted Fields */}
              <div className="w-1/2 flex flex-col overflow-hidden md:h-full">
                {comparisonExtractedFields && comparisonExtractedFields.length > 0 && selectedTemplate ? (
                  <ComparisonPanel
                    comparisonExtractedFields={comparisonExtractedFields}
                    comparisonTemplateName={comparisonTemplateName}
                    selectedTemplate={selectedTemplate}
                    isSavingFields={isSavingFields}
                    onAddField={(field) => {
                      if (selectedTemplate.status === TemplateStatus.Confirmed) {
                        confirm({
                          title: 'Plantilla Confirmada',
                          message: 'No se puede modificar una plantilla CONFIRMADA, conviertela en BORRADOR antes',
                          confirmLabel: 'Entendido',
                          variant: 'warning',
                        });
                      } else {
                        addFieldFromComparison(field);
                      }
                    }}
                    onAddAllNewFields={() => {
                      if (selectedTemplate.status === TemplateStatus.Confirmed) {
                        confirm({
                          title: 'Plantilla Confirmada',
                          message: 'No se puede modificar una plantilla CONFIRMADA, conviertela en BORRADOR antes',
                          confirmLabel: 'Entendido',
                          variant: 'warning',
                        });
                      } else {
                        addAllNewFieldsFromComparison();
                      }
                    }}
                    onClose={closeComparison}
                  />
                ) : (
                  <div className="border border-purple-500/30 bg-black/40 flex-1 flex flex-col overflow-hidden">
                    <div className="bg-purple-500/10 p-2 text-[10px] text-purple-400 uppercase tracking-widest flex items-center justify-between border-b border-purple-500/20">
                      <span className="flex items-center gap-1">
                        <span className="material-icons text-xs">auto_awesome</span>
                        Campos Nuevos Extraídos
                      </span>
                    </div>
                    <div className="flex-1 flex flex-col items-center justify-center text-primary/40 p-4">
                      <span className="material-icons text-4xl mb-2">search</span>
                      <p className="text-xs uppercase">Sin campos nuevos</p>
                      <p className="text-[10px] mt-1 text-center">Los campos extraídos aparecerán aquí</p>
                    </div>
                  </div>
                )}
              </div>

              {/* Right Column - Existing Fields */}
              <div className="w-1/2 flex flex-col border border-primary/30 bg-black/40 overflow-hidden md:h-full">
                <div className="bg-primary/10 p-2 text-[10px] text-primary uppercase tracking-widest flex items-center justify-between border-b border-primary/20">
                  <span className="flex items-center gap-1">
                    <span className="material-icons text-xs">list</span>
                    Campos Existentes ({selectedTemplate.fields.length})
                  </span>
                  
                  {!isEditing && (
                    <button
                      onClick={() => {
                        if (selectedTemplate.status === TemplateStatus.Confirmed) {
                          confirm({
                            title: 'Plantilla Confirmada',
                            message: 'No se puede modificar una plantilla CONFIRMADA, conviertela en BORRADOR antes',
                            confirmLabel: 'Entendido',
                            variant: 'warning',
                          });
                        } else {
                          startEditing([]);
                        }
                      }}
                      className="text-[10px] px-2 py-0.5 border border-cyan-500/40 text-cyan-400 hover:bg-cyan-500/20 transition-colors"
                    >
                      EDITAR
                    </button>
                  )}
                </div>
                
                <div className="flex-1 overflow-y-auto p-3">
                  {isEditing ? (
                    <TemplateFieldEditor
                      fields={selectedTemplate.fields}
                      onSave={handleSaveFields}
                      onCancel={cancelEditing}
                      isLoading={isSaving}
                    />
                  ) : selectedTemplate.fields.length === 0 ? (
                    <div className="text-center text-primary/40 py-8">
                      <span className="material-icons text-4xl mb-2">warning</span>
                      <p className="text-xs uppercase">Sin campos definidos</p>
                      <p className="text-[10px] mt-1">La plantilla necesita campos para funcionar</p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {selectedTemplate.fields
                        .sort((a, b) => a.order - b.order)
                        .map((field, index) => (
                          <div 
                            key={field.name}
                            className="border border-primary/20 bg-black/40 p-2"
                          >
                            <div className="flex items-start justify-between">
                              <div className="flex-1">
                                <div className="flex items-center gap-2">
                                  <span className="text-xs text-primary/40">#{index + 1}</span>
                                  <span className="font-mono text-cyan-400 text-xs">{field.name}</span>
                                  {field.isRequired && (
                                    <span className="text-danger text-xs">*</span>
                                  )}
                                </div>
                                <p className="text-xs text-primary mt-0.5">{field.displayName}</p>
                                {field.description && (
                                  <p className="text-[10px] text-primary/50 mt-0.5">{field.description}</p>
                                )}
                              </div>
                              
                              <span className="text-[9px] px-1.5 py-0.5 bg-primary/10 border border-primary/20 text-primary/60">
                                {FieldTypeLabels[field.fieldType]}
                              </span>
                            </div>
                            
                            <div className="mt-1.5 flex flex-wrap gap-2 text-[9px]">
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
    </div>
  );

  return (
    <>
      {isAdmin ? (
        <AdminLayout>{mainContent}</AdminLayout>
      ) : (
        <TerminalLayout title="PLANTILLAS">{mainContent}</TerminalLayout>
      )}
      <ConfirmDialog
        isOpen={isConfirmOpen}
        title={confirmConfig?.title ?? ''}
        message={confirmConfig?.message ?? ''}
        confirmLabel={confirmConfig?.confirmLabel}
        cancelLabel={confirmConfig?.cancelLabel}
        variant={confirmConfig?.variant}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
        isLoading={isConfirming}
      />
      <ConfirmDialog
        isOpen={showDeviceWarning}
        title="Aviso"
        message="Este panel de control está optimizado para escritorio por la gran cantidad de datos que contiene, en tablet o móvil podría haber errores de visualización."
        confirmLabel="Entendido"
        variant="warning"
        onConfirm={() => setShowDeviceWarning(false)}
        onCancel={() => setShowDeviceWarning(false)}
      />
    </>
  );
};

export default TemplatesPage;
