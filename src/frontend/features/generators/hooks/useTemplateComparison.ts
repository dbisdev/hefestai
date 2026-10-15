/**
 * useTemplateComparison
 * Single Responsibility: Managing the field-comparison workflow that appears
 * when a skipped extraction is reviewed — showing the diff between the
 * existing template fields and the newly extracted ones, and allowing
 * selective or bulk addition of new fields.
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import { entityTemplateService } from '@core/services/api';
import { extractErrorMessage } from '@core/utils/error';
import type { EntityTemplate, ExtractedTemplateInfo, FieldDefinition } from '@core/types';
import type { UseTemplatesConfig } from './useTemplates.types';

export interface UseTemplateComparisonReturn {
  comparisonExtractedFields: FieldDefinition[] | null;
  comparisonTemplateName: string;
  isSavingFields: boolean;
  error: string | null;
  viewSkippedExtraction: (info: ExtractedTemplateInfo) => Promise<EntityTemplate | null>;
  addFieldFromComparison: (
    field: FieldDefinition,
    selectedTemplate: EntityTemplate,
    onUpdated: (updated: EntityTemplate) => void,
    onRefresh: () => Promise<void>,
  ) => Promise<boolean>;
  addAllNewFieldsFromComparison: (
    selectedTemplate: EntityTemplate,
    onUpdated: (updated: EntityTemplate) => void,
    onRefresh: () => Promise<void>,
  ) => Promise<boolean>;
  closeComparison: () => void;
}

/**
 * Manages the comparison panel state and field-merging operations
 * for skipped template extractions.
 */
export function useTemplateComparison({
  gameSystemId,
  onLog,
}: Pick<UseTemplatesConfig, 'gameSystemId' | 'onLog'>): UseTemplateComparisonReturn {
  const [comparisonExtractedFields, setComparisonExtractedFields] = useState<FieldDefinition[] | null>(null);
  const [comparisonTemplateName, setComparisonTemplateName] = useState('');
  const [isSavingFields, setIsSavingFields] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /** Guards async state updates against updates on unmounted components */
  const isMountedRef = useRef(true);
  useEffect(() => {
    isMountedRef.current = true;
    return () => { isMountedRef.current = false; };
  }, []);

  const log = useCallback((message: string) => { onLog?.(message); }, [onLog]);

  // ── View skipped extraction ───────────────────────────────────────────────

  /**
   * Loads the template for a skipped extraction result and opens the
   * comparison panel populated with the extracted fields.
   * Returns the loaded template so the caller can update selected state.
   */
  const viewSkippedExtraction = useCallback(async (
    info: ExtractedTemplateInfo,
  ): Promise<EntityTemplate | null> => {
    if (!gameSystemId || !info.extractedFields || !info.templateId) return null;

    if (isMountedRef.current) setError(null);
    try {
      const template = await entityTemplateService.getById(gameSystemId, info.templateId);
      if (isMountedRef.current) {
        setComparisonExtractedFields(info.extractedFields);
        setComparisonTemplateName(info.displayName);
        log(`Comparando extracción: ${info.displayName}`);
        log(`  - Existente: ${template.fields.length} campos`);
        log(`  - Extraído: ${info.extractedFields.length} campos`);
      }
      return template;
    } catch (err) {
      if (isMountedRef.current) setError(extractErrorMessage(err, 'Error loading template for comparison'));
      return null;
    }
  }, [gameSystemId, log]);

  // ── Add fields ───────────────────────────────────────────────────────────

  /**
   * Adds a single field from the comparison panel to the selected template.
   * Prevents duplicate fields (by name). Appends with order = maxOrder + 1.
   *
   * @param field       - The field to add.
   * @param selectedTemplate - The current template (read for its existing fields).
   * @param onUpdated   - Called with the re-fetched template after the update.
   * @param onRefresh   - Called to refresh the template list after the update.
   */
  const addFieldFromComparison = useCallback(async (
    field: FieldDefinition,
    selectedTemplate: EntityTemplate,
    onUpdated: (updated: EntityTemplate) => void,
    onRefresh: () => Promise<void>,
  ): Promise<boolean> => {
    if (!gameSystemId) return false;

    if (selectedTemplate.fields.some(f => f.name === field.name)) {
      log(`ERROR: El campo "${field.name}" ya existe en la plantilla`);
      return false;
    }

    setIsSavingFields(true);
    if (isMountedRef.current) setError(null);
    log(`Añadiendo campo: ${field.name}...`);

    try {
      const maxOrder = selectedTemplate.fields.length > 0
        ? Math.max(...selectedTemplate.fields.map(f => f.order))
        : 0;

      const updatedFields = [...selectedTemplate.fields, { ...field, order: maxOrder + 1 }];

      await entityTemplateService.update(gameSystemId, selectedTemplate.id, {
        displayName: selectedTemplate.displayName,
        description: selectedTemplate.description,
        iconHint: selectedTemplate.iconHint,
        version: selectedTemplate.version,
        fields: updatedFields,
      });

      log(`[SUCCESS] Campo añadido: ${field.displayName}`);

      const updated = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      if (isMountedRef.current) {
        onUpdated(updated);
        setComparisonExtractedFields(prev => prev ? prev.filter(f => f.name !== field.name) : null);
      }

      await onRefresh();
      return true;
    } catch (err) {
      const message = extractErrorMessage(err, 'Error añadiendo campo');
      log(`ERROR: ${message}`);
      if (isMountedRef.current) setError(message);
      return false;
    } finally {
      if (isMountedRef.current) setIsSavingFields(false);
    }
  }, [gameSystemId, log]);

  /**
   * Adds all fields from the comparison panel that do not already exist
   * in the selected template (bulk operation).
   *
   * @param selectedTemplate - The current template (read for its existing fields).
   * @param onUpdated   - Called with the re-fetched template after the update.
   * @param onRefresh   - Called to refresh the template list after the update.
   */
  const addAllNewFieldsFromComparison = useCallback(async (
    selectedTemplate: EntityTemplate,
    onUpdated: (updated: EntityTemplate) => void,
    onRefresh: () => Promise<void>,
  ): Promise<boolean> => {
    if (!gameSystemId || !comparisonExtractedFields) return false;

    const newFields = comparisonExtractedFields.filter(
      cf => !selectedTemplate.fields.some(tf => tf.name === cf.name),
    );

    if (newFields.length === 0) {
      log('No hay campos nuevos para añadir');
      return false;
    }

    setIsSavingFields(true);
    if (isMountedRef.current) setError(null);
    log(`Añadiendo ${newFields.length} campos nuevos...`);

    try {
      const maxOrder = selectedTemplate.fields.length > 0
        ? Math.max(...selectedTemplate.fields.map(f => f.order))
        : 0;

      const fieldsWithOrder = newFields.map((field, index) => ({
        ...field,
        order: maxOrder + 1 + index,
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
      if (isMountedRef.current) {
        onUpdated(updated);
        setComparisonExtractedFields(null);
      }

      await onRefresh();
      return true;
    } catch (err) {
      const message = extractErrorMessage(err, 'Error adding fields');
      if (isMountedRef.current) setError(message);
      return false;
    } finally {
      if (isMountedRef.current) setIsSavingFields(false);
    }
  }, [gameSystemId, comparisonExtractedFields, log]);

  // ── Close ────────────────────────────────────────────────────────────────

  /** Closes the comparison panel without making any changes. */
  const closeComparison = useCallback(() => {
    setComparisonExtractedFields(null);
    setComparisonTemplateName('');
  }, []);

  return {
    comparisonExtractedFields,
    comparisonTemplateName,
    isSavingFields,
    error,
    viewSkippedExtraction,
    addFieldFromComparison,
    addAllNewFieldsFromComparison,
    closeComparison,
  };
}
