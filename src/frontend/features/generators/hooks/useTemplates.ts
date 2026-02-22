/**
 * useTemplates Hook
 * Single Responsibility: Template fetching and management
 * Includes extraction comparison functionality
 */

import { useState, useCallback, useEffect } from 'react';
import { entityTemplateService } from '@core/services/api';
import type { 
  EntityTemplateSummary, 
  EntityTemplate,
  ExtractedTemplateInfo,
  FieldDefinition 
} from '@core/types';

interface UseTemplatesConfig {
  gameSystemId: string | null;
  userId?: string;
  userRole?: string;
  isOwner: boolean;
  onLog?: (message: string) => void;
}

interface TemplateCounts {
  total: number;
  confirmed: number;
  pending: number;
}

interface UseTemplatesReturn {
  templates: EntityTemplateSummary[];
  selectedTemplate: EntityTemplate | null;
  counts: TemplateCounts;
  isLoading: boolean;
  isConfirming: boolean;
  isExtracting: boolean;
  isSavingFields: boolean;
  isReverting: boolean;
  extractionResult: ExtractedTemplateInfo[] | null;
  newlyExtractedIds: Set<string>;
  comparisonExtractedFields: FieldDefinition[] | null;
  comparisonTemplateName: string;
  error: string | null;
  selectTemplate: (templateId: string) => Promise<void>;
  confirmTemplate: (templateId: string) => Promise<boolean>;
  confirmAll: () => Promise<boolean>;
  extractTemplates: () => Promise<ExtractedTemplateInfo[] | null>;
  clearExtractionResult: () => void;
  refresh: () => Promise<void>;
  refreshSelectedTemplate: () => Promise<void>;
  clearSelectedTemplate: () => void;
  isNewlyExtracted: (templateId: string) => boolean;
  viewSkippedExtraction: (info: ExtractedTemplateInfo) => Promise<void>;
  addFieldFromComparison: (field: FieldDefinition) => Promise<boolean>;
  addAllNewFieldsFromComparison: () => Promise<boolean>;
  revertToDraft: () => Promise<boolean>;
  closeComparison: () => void;
}

export function useTemplates({
  gameSystemId,
  userId,
  userRole,
  isOwner,
  onLog,
}: UseTemplatesConfig): UseTemplatesReturn {
  const [templates, setTemplates] = useState<EntityTemplateSummary[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<EntityTemplate | null>(null);
  const [counts, setCounts] = useState<TemplateCounts>({ total: 0, confirmed: 0, pending: 0 });
  const [isLoading, setIsLoading] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const [isExtracting, setIsExtracting] = useState(false);
  const [isSavingFields, setIsSavingFields] = useState(false);
  const [isReverting, setIsReverting] = useState(false);
  const [extractionResult, setExtractionResult] = useState<ExtractedTemplateInfo[] | null>(null);
  const [newlyExtractedIds, setNewlyExtractedIds] = useState<Set<string>>(new Set());
  const [comparisonExtractedFields, setComparisonExtractedFields] = useState<FieldDefinition[] | null>(null);
  const [comparisonTemplateName, setComparisonTemplateName] = useState<string>('');
  const [error, setError] = useState<string | null>(null);

  const log = useCallback((message: string) => {
    onLog?.(message);
  }, [onLog]);

  const refresh = useCallback(async () => {
    if (!gameSystemId) {
      setTemplates([]);
      setCounts({ total: 0, confirmed: 0, pending: 0 });
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const isAdmin = userRole === 'ADMIN';
      const isMasterUser = userRole === 'MASTER';
      
      const result = await entityTemplateService.getByGameSystem(
        gameSystemId,
        undefined,
        isMasterUser && !isOwner && !isAdmin,
        isAdmin || (isMasterUser && isOwner)
      );
      
      setTemplates(result.templates);
      setCounts({
        total: result.totalCount,
        confirmed: result.confirmedCount,
        pending: result.pendingCount,
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error loading templates';
      setError(message);
      setTemplates([]);
    } finally {
      setIsLoading(false);
    }
  }, [gameSystemId, userRole, isOwner]);

  useEffect(() => {
    refresh();
    setNewlyExtractedIds(new Set());
    setComparisonExtractedFields(null);
    setComparisonTemplateName('');
  }, [gameSystemId]);

  const selectTemplate = useCallback(async (templateId: string) => {
    if (!gameSystemId) return;

    try {
      const template = await entityTemplateService.getById(gameSystemId, templateId);
      setSelectedTemplate(template);
      setComparisonExtractedFields(null);
      setComparisonTemplateName('');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error loading template';
      setError(message);
    }
  }, [gameSystemId]);

  const refreshSelectedTemplate = useCallback(async () => {
    if (!gameSystemId || !selectedTemplate) return;

    try {
      const template = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      setSelectedTemplate(template);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error refreshing template';
      setError(message);
    }
  }, [gameSystemId, selectedTemplate]);

  const confirmTemplate = useCallback(async (templateId: string): Promise<boolean> => {
    if (!gameSystemId) return false;

    setIsConfirming(true);
    setError(null);

    try {
      await entityTemplateService.confirm(gameSystemId, templateId, { notes: 'Confirmed from UI' });
      log(`[SUCCESS] Plantilla confirmada`);
      await refresh();
      
      if (selectedTemplate?.id === templateId) {
        const updated = await entityTemplateService.getById(gameSystemId, templateId);
        setSelectedTemplate(updated);
      }
      
      setNewlyExtractedIds(prev => {
        const next = new Set(prev);
        next.delete(templateId);
        return next;
      });
      
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error confirming template';
      setError(message);
      return false;
    } finally {
      setIsConfirming(false);
    }
  }, [gameSystemId, refresh, selectedTemplate?.id, log]);

  const confirmAll = useCallback(async (): Promise<boolean> => {
    if (!gameSystemId) return false;

    setIsConfirming(true);
    setError(null);

    try {
      await entityTemplateService.confirmAll(gameSystemId);
      log(`[SUCCESS] Todas las plantillas confirmadas`);
      await refresh();
      setNewlyExtractedIds(new Set());
      setExtractionResult(null);
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error confirming all templates';
      setError(message);
      return false;
    } finally {
      setIsConfirming(false);
    }
  }, [gameSystemId, refresh, log]);

  const extractTemplates = useCallback(async (): Promise<ExtractedTemplateInfo[] | null> => {
    if (!gameSystemId) return null;

    setIsExtracting(true);
    setError(null);

    try {
      const result = await entityTemplateService.extractFromManual(gameSystemId);
      
      if (result.errorMessage) {
        log(`ERROR: ${result.errorMessage}`);
        return null;
      }
      
      setExtractionResult(result.templates);
      
      const newIds = new Set(
        result.templates
          .filter(t => t.templateId && t.templateId !== '00000000-0000-0000-0000-000000000000')
          .map(t => t.templateId as string)
      );
      setNewlyExtractedIds(newIds);
      
      log(`[SUCCESS] Extracción completada:`);
      log(`  - Creadas: ${result.templatesCreated}`);
      log(`  - Actualizadas: ${result.templatesUpdated}`);
      log(`  - Omitidas: ${result.templatesSkipped}`);
      
      await refresh();
      return result.templates;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error extracting templates';
      setError(message);
      log(`ERROR_CRITICO: ${message}`);
      return null;
    } finally {
      setIsExtracting(false);
    }
  }, [gameSystemId, refresh, log]);

  const isNewlyExtracted = useCallback((templateId: string): boolean => {
    return newlyExtractedIds.has(templateId);
  }, [newlyExtractedIds]);

  const viewSkippedExtraction = useCallback(async (info: ExtractedTemplateInfo) => {
    if (!gameSystemId || !info.extractedFields || !info.templateId) return;

    try {
      const template = await entityTemplateService.getById(gameSystemId, info.templateId);
      setSelectedTemplate(template);
      setComparisonExtractedFields(info.extractedFields);
      setComparisonTemplateName(info.displayName);
      log(`Comparando extracción: ${info.displayName}`);
      log(`  - Existente: ${template.fields.length} campos`);
      log(`  - Extraído: ${info.extractedFields.length} campos`);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error loading template for comparison';
      setError(message);
    }
  }, [gameSystemId, log]);

  const addFieldFromComparison = useCallback(async (field: FieldDefinition): Promise<boolean> => {
    if (!gameSystemId || !selectedTemplate) return false;

    if (selectedTemplate.fields.some(f => f.name === field.name)) {
      log(`ERROR: El campo "${field.name}" ya existe en la plantilla`);
      return false;
    }

    setIsSavingFields(true);
    log(`Añadiendo campo: ${field.name}...`);

    try {
      const maxOrder = selectedTemplate.fields.length > 0
        ? Math.max(...selectedTemplate.fields.map(f => f.order))
        : 0;

      const newField = { ...field, order: maxOrder + 1 };
      const updatedFields = [...selectedTemplate.fields, newField];

      await entityTemplateService.update(gameSystemId, selectedTemplate.id, {
        displayName: selectedTemplate.displayName,
        description: selectedTemplate.description,
        iconHint: selectedTemplate.iconHint,
        version: selectedTemplate.version,
        fields: updatedFields,
      });

      log(`[SUCCESS] Campo añadido: ${field.displayName}`);

      const updated = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      setSelectedTemplate(updated);

      setComparisonExtractedFields(prev =>
        prev ? prev.filter(f => f.name !== field.name) : null
      );

      await refresh();
      return true;
    } catch (err) {
     
      const message = err instanceof Error ? err.message : 'Error añadiendo campo';
      log(`ERROR: ${message}`);
      setError(message);
      return false;
    } finally {
      setIsSavingFields(false);
    }
  }, [gameSystemId, selectedTemplate, refresh, log]);

  const addAllNewFieldsFromComparison = useCallback(async (): Promise<boolean> => {
    if (!gameSystemId || !selectedTemplate || !comparisonExtractedFields) return false;

    const newFields = comparisonExtractedFields.filter(
      cf => !selectedTemplate.fields.some(tf => tf.name === cf.name)
    );

    if (newFields.length === 0) {
      log('No hay campos nuevos para añadir');
      return false;
    }

    setIsSavingFields(true);
    log(`Añadiendo ${newFields.length} campos nuevos...`);

    try {
      const maxOrder = selectedTemplate.fields.length > 0
        ? Math.max(...selectedTemplate.fields.map(f => f.order))
        : 0;

      const fieldsWithOrder = newFields.map((field, index) => ({
        ...field,
        order: maxOrder + 1 + index
      }));

      const updatedFields = [...selectedTemplate.fields, ...fieldsWithOrder];

      await entityTemplateService.update(gameSystemId, selectedTemplate.id, {
        displayName: selectedTemplate.displayName,
        description: selectedTemplate.description,
        iconHint: selectedTemplate.iconHint,
        version: selectedTemplate.version,
        fields: updatedFields,
      });

      log(`[SUCCESS] ${newFields.length} campos añadidos`);

      const updated = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      setSelectedTemplate(updated);

      setComparisonExtractedFields(null);

      await refresh();
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error adding fields';
      setError(message);
      return false;
    } finally {
      setIsSavingFields(false);
    }
  }, [gameSystemId, selectedTemplate, comparisonExtractedFields, refresh, log]);

  const closeComparison = useCallback(() => {
    setComparisonExtractedFields(null);
    setComparisonTemplateName('');
  }, []);

  const clearExtractionResult = useCallback(() => {
    setExtractionResult(null);
  }, []);

  const clearSelectedTemplate = useCallback(() => {
    setSelectedTemplate(null);
    setComparisonExtractedFields(null);
    setComparisonTemplateName('');
  }, []);

  const revertToDraft = useCallback(async (): Promise<boolean> => {
    if (!gameSystemId || !selectedTemplate) return false;

    setIsReverting(true);
    log('Revirtiendo plantilla a borrador...');

    try {
      await entityTemplateService.revertToDraft(gameSystemId, selectedTemplate.id);
      log('[SUCCESS] Plantilla revertida a borrador');

      const updated = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      setSelectedTemplate(updated);

      await refresh();
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al revertir plantilla';
      log(`ERROR: ${message}`);
      setError(message);
      return false;
    } finally {
      setIsReverting(false);
    }
  }, [gameSystemId, selectedTemplate, refresh, log]);

  return {
    templates,
    selectedTemplate,
    counts,
    isLoading,
    isConfirming,
    isExtracting,
    isSavingFields,
    isReverting,
    extractionResult,
    newlyExtractedIds,
    comparisonExtractedFields,
    comparisonTemplateName,
    error,
    selectTemplate,
    confirmTemplate,
    confirmAll,
    extractTemplates,
    clearExtractionResult,
    refresh,
    refreshSelectedTemplate,
    clearSelectedTemplate,
    isNewlyExtracted,
    viewSkippedExtraction,
    addFieldFromComparison,
    addAllNewFieldsFromComparison,
    revertToDraft,
    closeComparison,
  };
}
