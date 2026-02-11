/**
 * Entity Templates Management Page
 * Allows Admins to extract, view, edit, and confirm entity templates
 * Templates define the schema for entity types (character, npc, vehicle, etc.)
 * Cyberpunk terminal aesthetics with template management features
 */

import React, { useState, useEffect, useCallback } from 'react';
import { AdminLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useAuth } from '@core/context';
import { entityTemplateService, gameSystemService } from '@core/services/api';
import type { 
  GameSystem,
  EntityTemplateSummary, 
  EntityTemplate,
  ExtractedTemplateInfo,
  FieldDefinition
} from '@core/types';
import { TemplateStatus, TemplateStatusLabels, FieldTypeLabels, FieldType, Screen } from '@core/types';

interface TemplatesPageProps {
  /** Handler for navigating to other screens */
  onNavigate: (screen: Screen) => void;
  /** Handler for returning to gallery */
  onBack: () => void;
  /** Handler for logging out */
  onLogout?: () => void;
}

/**
 * Templates Page Component
 * Provides UI for managing entity templates (Admin only)
 * - Extract templates from uploaded manuals using RAG
 * - View and edit template field definitions
 * - Confirm templates to make them available for entity creation
 */
export const TemplatesPage: React.FC<TemplatesPageProps> = ({ onNavigate, onBack, onLogout }) => {
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
  
  // Field editing state
  const [isEditingFields, setIsEditingFields] = useState(false);
  const [editedFields, setEditedFields] = useState<FieldDefinition[]>([]);
  const [editingField, setEditingField] = useState<FieldDefinition | null>(null);
  const [isAddingField, setIsAddingField] = useState(false);
  const [isSavingFields, setIsSavingFields] = useState(false);
  
  // Comparison state for skipped templates (confirmed ones with newly extracted fields)
  const [comparisonExtractedFields, setComparisonExtractedFields] = useState<FieldDefinition[] | null>(null);
  const [comparisonTemplateName, setComparisonTemplateName] = useState<string>('');
  
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
      
      // Auto-select first system if available and none selected
      if (systems.length > 0) {
        setSelectedGameSystem(prev => prev ?? systems[0]);
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar sistemas';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsLoadingGameSystems(false);
    }
  }, [addLog]);

  /**
   * Fetches templates for the selected game system
   */
  const fetchTemplates = useCallback(async (gameSystemId: string) => {
    setIsLoadingTemplates(true);
    try {
      const result = await entityTemplateService.getByGameSystem(gameSystemId);
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
  }, [addLog]);

  /**
   * Fetches full template details
   */
  const fetchTemplateDetails = useCallback(async (templateId: string) => {
    if (!selectedGameSystem) return;
    
    try {
      const template = await entityTemplateService.getById(selectedGameSystem.id, templateId);
      setSelectedTemplate(template);
      // Clear comparison when loading a new template
      setComparisonExtractedFields(null);
      setComparisonTemplateName('');
      addLog(`Plantilla cargada: ${template.displayName}`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar detalles';
      addLog(`ERROR: ${message}`);
    }
  }, [selectedGameSystem, addLog]);

  /**
   * Handles viewing extracted fields from a skipped template (one that already has a confirmed version).
   * Loads the confirmed template and shows the newly extracted fields for comparison.
   */
  const handleViewSkippedExtraction = useCallback(async (info: ExtractedTemplateInfo) => {
    if (!selectedGameSystem || !info.extractedFields || !info.templateId) return;
    
    // First, load the existing confirmed template
    try {
      const template = await entityTemplateService.getById(selectedGameSystem.id, info.templateId);
      setSelectedTemplate(template);
      // Set the extracted fields for comparison
      setComparisonExtractedFields(info.extractedFields);
      setComparisonTemplateName(info.displayName);
      addLog(`Comparando extracción: ${info.displayName}`);
      addLog(`  - Existente: ${template.fields.length} campos`);
      addLog(`  - Extraído: ${info.extractedFields.length} campos`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar plantilla';
      addLog(`ERROR: ${message}`);
    }
  }, [selectedGameSystem, addLog]);

  /**
   * Closes the comparison view
   */
  const handleCloseComparison = useCallback(() => {
    setComparisonExtractedFields(null);
    setComparisonTemplateName('');
  }, []);

  /**
   * Adds a single field from the comparison to the confirmed template.
   * Creates a copy of the field and appends it to the template's field list.
   */
  const handleAddFieldFromComparison = useCallback(async (field: FieldDefinition) => {
    if (!selectedGameSystem || !selectedTemplate) return;
    
    // Check if field already exists
    if (selectedTemplate.fields.some(f => f.name === field.name)) {
      addLog(`ERROR: El campo "${field.name}" ya existe en la plantilla`);
      return;
    }
    
    setIsSavingFields(true);
    addLog(`Añadiendo campo: ${field.name}...`);
    
    try {
      // Calculate new order (append at end)
      const maxOrder = selectedTemplate.fields.length > 0
        ? Math.max(...selectedTemplate.fields.map(f => f.order))
        : 0;
      
      const newField = { ...field, order: maxOrder + 1 };
      const updatedFields = [...selectedTemplate.fields, newField];
      
      await entityTemplateService.update(
        selectedGameSystem.id,
        selectedTemplate.id,
        {
          displayName: selectedTemplate.displayName,
          description: selectedTemplate.description,
          iconHint: selectedTemplate.iconHint,
          version: selectedTemplate.version,
          fields: updatedFields,
        }
      );
      
      addLog(`[SUCCESS] Campo añadido: ${field.displayName}`);
      
      // Refresh template details
      const updated = await entityTemplateService.getById(selectedGameSystem.id, selectedTemplate.id);
      setSelectedTemplate(updated);
      
      // Remove the field from comparison list
      setComparisonExtractedFields(prev => 
        prev ? prev.filter(f => f.name !== field.name) : null
      );
      
      // Refresh templates list
      await fetchTemplates(selectedGameSystem.id);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al añadir campo';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsSavingFields(false);
    }
  }, [selectedGameSystem, selectedTemplate, addLog, fetchTemplates]);

  /**
   * Adds all new fields from the comparison to the confirmed template.
   * Only adds fields that don't already exist in the template.
   */
  const handleAddAllNewFieldsFromComparison = useCallback(async () => {
    if (!selectedGameSystem || !selectedTemplate || !comparisonExtractedFields) return;
    
    // Find fields that don't exist in the template
    const newFields = comparisonExtractedFields.filter(
      cf => !selectedTemplate.fields.some(tf => tf.name === cf.name)
    );
    
    if (newFields.length === 0) {
      addLog('No hay campos nuevos para añadir');
      return;
    }
    
    setIsSavingFields(true);
    addLog(`Añadiendo ${newFields.length} campos nuevos...`);
    
    try {
      // Calculate starting order
      const maxOrder = selectedTemplate.fields.length > 0
        ? Math.max(...selectedTemplate.fields.map(f => f.order))
        : 0;
      
      // Add order to new fields
      const fieldsWithOrder = newFields.map((field, index) => ({
        ...field,
        order: maxOrder + 1 + index
      }));
      
      const updatedFields = [...selectedTemplate.fields, ...fieldsWithOrder];
      
      await entityTemplateService.update(
        selectedGameSystem.id,
        selectedTemplate.id,
        {
          displayName: selectedTemplate.displayName,
          description: selectedTemplate.description,
          iconHint: selectedTemplate.iconHint,
          version: selectedTemplate.version,
          fields: updatedFields,
        }
      );
      
      addLog(`[SUCCESS] ${newFields.length} campos añadidos`);
      
      // Refresh template details
      const updated = await entityTemplateService.getById(selectedGameSystem.id, selectedTemplate.id);
      setSelectedTemplate(updated);
      
      // Remove added fields from comparison list
      setComparisonExtractedFields(prev => 
        prev ? prev.filter(cf => selectedTemplate.fields.some(tf => tf.name === cf.name)) : null
      );
      
      // Refresh templates list
      await fetchTemplates(selectedGameSystem.id);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al añadir campos';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsSavingFields(false);
    }
  }, [selectedGameSystem, selectedTemplate, comparisonExtractedFields, addLog, fetchTemplates]);

  // Load game systems on mount
  useEffect(() => {
    fetchGameSystems();
  }, [fetchGameSystems]);

  // Load templates when game system changes
  useEffect(() => {
    if (selectedGameSystem) {
      fetchTemplates(selectedGameSystem.id);
      setSelectedTemplate(null);
      setExtractionResult(null);
      setNewlyExtractedIds(new Set());
    }
  }, [selectedGameSystem?.id, fetchTemplates]);

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
        await fetchTemplates(selectedGameSystem.id);
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
      await fetchTemplates(selectedGameSystem.id);
      
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
      await fetchTemplates(selectedGameSystem.id);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al confirmar';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsConfirmingAll(false);
    }
  };

  /**
   * Starts editing mode for the selected template's fields
   */
  const handleStartEditFields = () => {
    if (!selectedTemplate) return;
    setEditedFields([...selectedTemplate.fields]);
    setIsEditingFields(true);
    addLog(`Editando campos de ${selectedTemplate.displayName}`);
  };

  /**
   * Cancels field editing and reverts changes
   */
  const handleCancelEditFields = () => {
    setIsEditingFields(false);
    setEditedFields([]);
    setEditingField(null);
    setIsAddingField(false);
    addLog('Edición cancelada');
  };

  /**
   * Removes a field from the edited list (omit)
   */
  const handleOmitField = (fieldName: string) => {
    setEditedFields(prev => prev.filter(f => f.name !== fieldName));
    addLog(`Campo omitido: ${fieldName}`);
  };

  /**
   * Opens the field edit form
   */
  const handleEditField = (field: FieldDefinition) => {
    setIsAddingField(false);
    setEditingField({ ...field });
  };

  /**
   * Opens the form to add a new field
   */
  const handleAddField = () => {
    const maxOrder = editedFields.length > 0 
      ? Math.max(...editedFields.map(f => f.order)) 
      : 0;
    
    setIsAddingField(true);
    setEditingField({
      name: '',
      displayName: '',
      fieldType: FieldType.Text,
      isRequired: false,
      order: maxOrder + 1,
      description: '',
      defaultValue: undefined,
      options: undefined,
      minValue: undefined,
      maxValue: undefined,
    });
  };

  /**
   * Saves changes to the field being edited or adds new field
   */
  const handleSaveFieldEdit = () => {
    if (!editingField) return;
    
    // Validate field name for new fields
    if (isAddingField) {
      if (!editingField.name.trim()) {
        addLog('ERROR: El nombre del campo es requerido');
        return;
      }
      
      // Sanitize field name: lowercase, underscores, no special chars
      const sanitizedName = editingField.name
        .toLowerCase()
        .replace(/\s+/g, '_')
        .replace(/[^a-z0-9_]/g, '');
      
      if (!sanitizedName) {
        addLog('ERROR: Nombre de campo inválido');
        return;
      }
      
      // Check for duplicates
      if (editedFields.some(f => f.name === sanitizedName)) {
        addLog(`ERROR: Ya existe un campo con el nombre "${sanitizedName}"`);
        return;
      }
      
      const newField = { ...editingField, name: sanitizedName };
      setEditedFields(prev => [...prev, newField]);
      addLog(`Campo añadido: ${sanitizedName}`);
    } else {
      setEditedFields(prev => prev.map(f => 
        f.name === editingField.name ? editingField : f
      ));
      addLog(`Campo actualizado: ${editingField.name}`);
    }
    
    setEditingField(null);
    setIsAddingField(false);
  };

  /**
   * Cancels field edit form
   */
  const handleCancelFieldEdit = () => {
    setEditingField(null);
    setIsAddingField(false);
  };

  /**
   * Saves all field changes to the template via API
   */
  const handleSaveFields = async () => {
    if (!selectedGameSystem || !selectedTemplate) return;
    
    setIsSavingFields(true);
    addLog('GUARDANDO CAMBIOS...');

    try {
      await entityTemplateService.update(
        selectedGameSystem.id,
        selectedTemplate.id,
        {
          displayName: selectedTemplate.displayName,
          description: selectedTemplate.description,
          iconHint: selectedTemplate.iconHint,
          version: selectedTemplate.version,
          fields: editedFields,
        }
      );
      
      addLog(`[SUCCESS] Campos actualizados`);
      
      // Refresh template details
      await fetchTemplateDetails(selectedTemplate.id);
      
      // Exit edit mode
      setIsEditingFields(false);
      setEditedFields([]);
      
      // Refresh templates list
      await fetchTemplates(selectedGameSystem.id);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al guardar';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsSavingFields(false);
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
      <AdminLayout 
        activeScreen={Screen.TEMPLATES} 
        onNavigate={onNavigate} 
        onBack={onBack}
        onLogout={onLogout}
      >
        <div className="flex flex-col items-center justify-center h-full text-danger/60">
          <span className="material-icons text-6xl mb-4">lock</span>
          <p className="text-sm uppercase tracking-widest">Acceso restringido a Administradores</p>
          <Button onClick={onBack} className="mt-4">VOLVER</Button>
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout 
      activeScreen={Screen.TEMPLATES} 
      onNavigate={onNavigate} 
      onBack={onBack}
      onLogout={onLogout}
    >
      <div className="flex flex-col lg:flex-row h-full gap-6">
        {/* Left Column - Game Systems & Templates List */}
        <div className="w-full lg:w-1/3 flex flex-col gap-4 overflow-hidden">
          {/* Game System Selector */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-xs text-primary/60 uppercase tracking-widest flex items-center gap-2">
                <span className="material-icons text-sm">sports_esports</span>
                Sistema de Juego
              </h2>
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
                onClick={() => selectedGameSystem && fetchTemplates(selectedGameSystem.id)}
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
                        
                        // Check if this is a skipped template with extracted fields (can compare)
                        const hasExtractedFields = info.extractedFields && info.extractedFields.length > 0;
                        const isSkippedWithFields = hasExtractedFields && isConfirmedStatus;
                        
                        // Determine click handler
                        const handleClick = () => {
                          if (isSkippedWithFields) {
                            // Show comparison view
                            handleViewSkippedExtraction(info);
                          } else if (hasValidId) {
                            // Normal view
                            fetchTemplateDetails(info.templateId);
                          }
                        };
                        
                        return (
                          <div
                            key={info.templateId || info.entityTypeName}
                            onClick={handleClick}
                            className={`border p-2 cursor-pointer transition-all ${
                              selectedTemplate?.id === info.templateId
                                ? 'border-cyan-500 bg-cyan-500/10'
                                : isSkippedWithFields
                                ? 'border-purple-500/40 bg-purple-500/5 hover:border-purple-500/60'
                                : info.extractionNotes && !hasExtractedFields
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
                                  {isSkippedWithFields ? (
                                    <span className="text-[8px] px-1 py-0.5 border border-purple-500/60 text-purple-400 bg-purple-500/20">
                                      COMPARAR
                                    </span>
                                  ) : info.extractionNotes ? (
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
                                  {isConfirmedStatus && !isSkippedWithFields && (
                                    <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                                      CONFIRMADA
                                    </span>
                                  )}
                                </div>
                                <p className="text-xs text-primary mt-1 truncate">{info.displayName}</p>
                                <p className="text-[9px] text-primary/40">
                                  {isSkippedWithFields 
                                    ? `${info.extractedFields?.length} campos extraídos` 
                                    : `${info.fieldCount} campos`}
                                </p>
                                {info.extractionNotes && (
                                  <p className={`text-[9px] mt-1 truncate ${isSkippedWithFields ? 'text-purple-400/80' : 'text-red-400/80'}`} title={info.extractionNotes}>
                                    {info.extractionNotes}
                                  </p>
                                )}
                              </div>
                              
                              {isSkippedWithFields && (
                                <span 
                                  className="material-icons text-sm text-purple-400/60 ml-1"
                                  title="Ver campos extraídos"
                                >
                                  compare_arrows
                                </span>
                              )}
                              
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
                <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
                  <span className="flex items-center gap-2">
                    <span className="material-icons text-sm">list</span>
                    Definición de Campos ({isEditingFields ? editedFields.length : selectedTemplate.fields.length})
                  </span>
                  
                  {/* Edit mode controls */}
                  {selectedTemplate.status !== TemplateStatus.Confirmed && (
                    <div className="flex items-center gap-2">
                      {isEditingFields ? (
                        <>
                          <button
                            onClick={handleAddField}
                            disabled={isSavingFields}
                            className="text-[10px] px-2 py-1 border border-green-500/40 text-green-400 hover:bg-green-500/20 transition-colors disabled:opacity-50 flex items-center gap-1"
                          >
                            <span className="material-icons text-xs">add</span>
                            AÑADIR
                          </button>
                          <button
                            onClick={handleCancelEditFields}
                            disabled={isSavingFields}
                            className="text-[10px] px-2 py-1 border border-red-500/40 text-red-400 hover:bg-red-500/20 transition-colors disabled:opacity-50"
                          >
                            CANCELAR
                          </button>
                          <button
                            onClick={handleSaveFields}
                            disabled={isSavingFields}
                            className="text-[10px] px-2 py-1 border border-cyan-500/40 text-cyan-400 hover:bg-cyan-500/20 transition-colors disabled:opacity-50"
                          >
                            {isSavingFields ? 'GUARDANDO...' : 'GUARDAR'}
                          </button>
                        </>
                      ) : (
                        <button
                          onClick={handleStartEditFields}
                          className="text-[10px] px-2 py-1 border border-cyan-500/40 text-cyan-400 hover:bg-cyan-500/20 transition-colors"
                        >
                          EDITAR CAMPOS
                        </button>
                      )}
                    </div>
                  )}
                </div>
                
                <div className="flex-1 overflow-y-auto p-4">
                  {(isEditingFields ? editedFields : selectedTemplate.fields).length === 0 ? (
                    <div className="text-center text-primary/40 py-8">
                      <span className="material-icons text-4xl mb-2">warning</span>
                      <p className="text-xs uppercase">Sin campos definidos</p>
                      <p className="text-[10px] mt-1">La plantilla necesita campos para funcionar</p>
                      {isEditingFields && (
                        <button
                          onClick={handleAddField}
                          className="mt-4 text-xs px-3 py-2 border border-green-500/40 text-green-400 hover:bg-green-500/20 transition-colors flex items-center gap-2 mx-auto"
                        >
                          <span className="material-icons text-sm">add</span>
                          AÑADIR PRIMER CAMPO
                        </button>
                      )}
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {(isEditingFields ? editedFields : selectedTemplate.fields)
                        .sort((a, b) => a.order - b.order)
                        .map((field, index) => (
                          <div 
                            key={field.name}
                            className={`border p-3 ${
                              isEditingFields 
                                ? 'border-cyan-500/30 bg-cyan-500/5' 
                                : 'border-primary/20 bg-black/40'
                            }`}
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
                              
                              <div className="flex items-center gap-2">
                                <span className="text-[10px] px-2 py-1 bg-primary/10 border border-primary/20 text-primary/60">
                                  {FieldTypeLabels[field.fieldType]}
                                </span>
                                
                                {/* Edit/Omit buttons in edit mode */}
                                {isEditingFields && (
                                  <div className="flex items-center gap-1 ml-2">
                                    <button
                                      onClick={() => handleEditField(field)}
                                      className="material-icons text-sm text-cyan-400/60 hover:text-cyan-400 transition-colors p-1"
                                      title="Editar campo"
                                    >
                                      edit
                                    </button>
                                    <button
                                      onClick={() => handleOmitField(field.name)}
                                      className="material-icons text-sm text-red-400/60 hover:text-red-400 transition-colors p-1"
                                      title="Omitir campo"
                                    >
                                      delete
                                    </button>
                                  </div>
                                )}
                              </div>
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
              
              {/* Comparison Panel for Extracted Fields */}
              {comparisonExtractedFields && comparisonExtractedFields.length > 0 && (
                <div className="border border-purple-500/50 bg-black/60 flex flex-col max-h-[40vh]">
                  <div className="bg-purple-500/20 p-3 text-xs text-purple-400 uppercase tracking-widest flex items-center justify-between">
                    <span className="flex items-center gap-2">
                      <span className="material-icons text-sm">compare_arrows</span>
                      Campos Extraídos - Nueva Extracción ({comparisonExtractedFields.length})
                    </span>
                    <div className="flex items-center gap-2">
                      {/* Add All New Fields Button */}
                      {comparisonExtractedFields.some(cf => !selectedTemplate.fields.some(tf => tf.name === cf.name)) && (
                        <button
                          onClick={handleAddAllNewFieldsFromComparison}
                          disabled={isSavingFields}
                          className="text-[10px] px-2 py-1 border border-green-500/40 text-green-400 hover:bg-green-500/20 transition-colors disabled:opacity-50 flex items-center gap-1"
                          title="Añadir todos los campos nuevos"
                        >
                          <span className="material-icons text-xs">playlist_add</span>
                          {isSavingFields ? 'AÑADIENDO...' : 'AÑADIR TODOS'}
                        </button>
                      )}
                      <button
                        onClick={handleCloseComparison}
                        className="material-icons text-sm text-purple-400 hover:text-purple-300 transition-colors"
                        title="Cerrar comparación"
                      >
                        close
                      </button>
                    </div>
                  </div>
                  
                  <div className="p-3 text-xs text-purple-300/80 border-b border-purple-500/20 bg-purple-500/5">
                    <p>
                      La plantilla "<span className="text-purple-400">{comparisonTemplateName}</span>" ya está confirmada con {selectedTemplate.fields.length} campos.
                    </p>
                    <p className="mt-1">
                      Nueva extracción detectó <span className="text-purple-400">{comparisonExtractedFields.length}</span> campos. 
                      {comparisonExtractedFields.filter(cf => !selectedTemplate.fields.some(tf => tf.name === cf.name)).length > 0 && (
                        <span className="text-green-400 ml-1">
                          ({comparisonExtractedFields.filter(cf => !selectedTemplate.fields.some(tf => tf.name === cf.name)).length} nuevos)
                        </span>
                      )}
                    </p>
                  </div>
                  
                  <div className="flex-1 overflow-y-auto p-4">
                    <div className="space-y-2">
                      {comparisonExtractedFields
                        .sort((a, b) => a.order - b.order)
                        .map((field, index) => {
                          // Check if this field exists in the confirmed template
                          const existingField = selectedTemplate.fields.find(f => f.name === field.name);
                          const isNew = !existingField;
                          const isDifferent = existingField && (
                            existingField.displayName !== field.displayName ||
                            existingField.fieldType !== field.fieldType ||
                            existingField.description !== field.description
                          );
                          
                          return (
                            <div 
                              key={field.name}
                              className={`border p-2 ${
                                isNew 
                                  ? 'border-green-500/40 bg-green-500/10' 
                                  : isDifferent 
                                  ? 'border-yellow-500/40 bg-yellow-500/10'
                                  : 'border-purple-500/20 bg-purple-500/5'
                              }`}
                            >
                              <div className="flex items-start justify-between">
                                <div className="flex-1">
                                  <div className="flex items-center gap-2">
                                    <span className="text-[10px] text-purple-400/40">#{index + 1}</span>
                                    <span className="font-mono text-purple-400 text-xs">{field.name}</span>
                                    {isNew && (
                                      <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                                        NUEVO
                                      </span>
                                    )}
                                    {isDifferent && (
                                      <span className="text-[8px] px-1 py-0.5 border border-yellow-500/60 text-yellow-400 bg-yellow-500/20">
                                        DIFERENTE
                                      </span>
                                    )}
                                    {field.isRequired && (
                                      <span className="text-danger text-[10px]">*</span>
                                    )}
                                  </div>
                                  <p className="text-xs text-primary/80 mt-0.5">{field.displayName}</p>
                                  {field.description && (
                                    <p className="text-[10px] text-primary/40 mt-0.5 truncate" title={field.description}>
                                      {field.description}
                                    </p>
                                  )}
                                </div>
                                
                                <div className="flex items-center gap-2">
                                  <span className="text-[9px] px-1.5 py-0.5 bg-purple-500/10 border border-purple-500/20 text-purple-400/60">
                                    {FieldTypeLabels[field.fieldType]}
                                  </span>
                                  
                                  {/* Add button for new fields */}
                                  {isNew && (
                                    <button
                                      onClick={() => handleAddFieldFromComparison(field)}
                                      disabled={isSavingFields}
                                      className="material-icons text-sm text-green-400/60 hover:text-green-400 transition-colors disabled:opacity-50"
                                      title="Añadir este campo a la plantilla"
                                    >
                                      add_circle
                                    </button>
                                  )}
                                </div>
                              </div>
                            </div>
                          );
                        })}
                    </div>
                  </div>
                </div>
              )}
              
              {/* Field Edit Modal */}
              {editingField && (
                <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4">
                  <div className="border border-cyan-500/50 bg-surface-dark p-6 max-w-lg w-full max-h-[90vh] overflow-y-auto">
                    <h3 className="text-lg text-cyan-400 font-bold mb-4 flex items-center gap-2">
                      <span className="material-icons">edit</span>
                      Editar Campo: {editingField.name}
                    </h3>
                    
                    <div className="space-y-4">
                      {/* Display Name */}
                      <div>
                        <label className="block text-xs text-primary/60 uppercase mb-1">Nombre Visible</label>
                        <input
                          type="text"
                          value={editingField.displayName}
                          onChange={(e) => setEditingField({ ...editingField, displayName: e.target.value })}
                          className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                        />
                      </div>
                      
                      {/* Description */}
                      <div>
                        <label className="block text-xs text-primary/60 uppercase mb-1">Descripción</label>
                        <textarea
                          value={editingField.description || ''}
                          onChange={(e) => setEditingField({ ...editingField, description: e.target.value })}
                          rows={2}
                          className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none resize-none"
                        />
                      </div>
                      
                      {/* Field Type */}
                      <div>
                        <label className="block text-xs text-primary/60 uppercase mb-1">Tipo</label>
                        <select
                          value={editingField.fieldType}
                          onChange={(e) => setEditingField({ ...editingField, fieldType: Number(e.target.value) as FieldType })}
                          className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                        >
                          {Object.entries(FieldTypeLabels).map(([value, label]) => (
                            <option key={value} value={value}>{label}</option>
                          ))}
                        </select>
                      </div>
                      
                      {/* Is Required */}
                      <label className="flex items-center gap-2 text-sm text-primary/60 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={editingField.isRequired}
                          onChange={(e) => setEditingField({ ...editingField, isRequired: e.target.checked })}
                          className="accent-cyan-500"
                        />
                        Campo requerido
                      </label>
                      
                      {/* Default Value */}
                      <div>
                        <label className="block text-xs text-primary/60 uppercase mb-1">Valor por defecto</label>
                        <input
                          type="text"
                          value={editingField.defaultValue || ''}
                          onChange={(e) => setEditingField({ ...editingField, defaultValue: e.target.value || undefined })}
                          className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                        />
                      </div>
                      
                      {/* Min/Max for Number type */}
                      {editingField.fieldType === FieldType.Number && (
                        <div className="grid grid-cols-2 gap-4">
                          <div>
                            <label className="block text-xs text-primary/60 uppercase mb-1">Mínimo</label>
                            <input
                              type="number"
                              value={editingField.minValue ?? ''}
                              onChange={(e) => setEditingField({ 
                                ...editingField, 
                                minValue: e.target.value ? Number(e.target.value) : undefined 
                              })}
                              className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                            />
                          </div>
                          <div>
                            <label className="block text-xs text-primary/60 uppercase mb-1">Máximo</label>
                            <input
                              type="number"
                              value={editingField.maxValue ?? ''}
                              onChange={(e) => setEditingField({ 
                                ...editingField, 
                                maxValue: e.target.value ? Number(e.target.value) : undefined 
                              })}
                              className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                            />
                          </div>
                        </div>
                      )}
                      
                      {/* Options for Select/MultiSelect */}
                      {(editingField.fieldType === FieldType.Select || editingField.fieldType === FieldType.MultiSelect) && (
                        <div>
                          <label className="block text-xs text-primary/60 uppercase mb-1">
                            Opciones (una por línea)
                          </label>
                          <textarea
                            value={(editingField.options || []).join('\n')}
                            onChange={(e) => setEditingField({ 
                              ...editingField, 
                              options: e.target.value.split('\n').filter(o => o.trim()) 
                            })}
                            rows={4}
                            className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none resize-none font-mono"
                            placeholder="Opción 1&#10;Opción 2&#10;Opción 3"
                          />
                        </div>
                      )}
                    </div>
                    
                    {/* Modal Actions */}
                    <div className="flex justify-end gap-2 mt-6">
                      <Button variant="secondary" onClick={handleCancelFieldEdit}>
                        CANCELAR
                      </Button>
                      <Button onClick={handleSaveFieldEdit}>
                        APLICAR
                      </Button>
                    </div>
                  </div>
                </div>
              )}
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
    </AdminLayout>
  );
};
