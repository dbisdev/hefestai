/**
 * useTemplateSelection
 * Single Responsibility: Managing the currently-selected template and
 * actions that operate on it (select, refresh, revert to draft).
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import { entityTemplateService } from '@core/services/api';
import { extractErrorMessage } from '@core/utils/error';
import type { EntityTemplate } from '@core/types';
import type { UseTemplatesConfig } from './useTemplates.types';

export interface UseTemplateSelectionReturn {
  selectedTemplate: EntityTemplate | null;
  isReverting: boolean;
  error: string | null;
  selectTemplate: (templateId: string) => Promise<void>;
  refreshSelectedTemplate: () => Promise<void>;
  clearSelectedTemplate: () => void;
  revertToDraft: (onReverted?: () => Promise<void>) => Promise<boolean>;
}

/**
 * Manages which template is currently selected and operations that act
 * on it: loading, refreshing, and reverting to draft status.
 */
export function useTemplateSelection({
  gameSystemId,
  onLog,
}: Pick<UseTemplatesConfig, 'gameSystemId' | 'onLog'>): UseTemplateSelectionReturn {
  const [selectedTemplate, setSelectedTemplate] = useState<EntityTemplate | null>(null);
  const [isReverting, setIsReverting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /** Guards async state updates against updates on unmounted components */
  const isMountedRef = useRef(true);
  useEffect(() => {
    isMountedRef.current = true;
    return () => { isMountedRef.current = false; };
  }, []);

  const log = useCallback((message: string) => { onLog?.(message); }, [onLog]);

  // ── Selection ────────────────────────────────────────────────────────────

  /**
   * Loads and selects the full template detail for the given ID.
   * Clears any active comparison state as a side effect.
   */
  const selectTemplate = useCallback(async (templateId: string) => {
    if (!gameSystemId) return;

    if (isMountedRef.current) setError(null);
    try {
      const template = await entityTemplateService.getById(gameSystemId, templateId);
      if (isMountedRef.current) setSelectedTemplate(template);
    } catch (err) {
      if (isMountedRef.current) setError(extractErrorMessage(err, 'Error loading template'));
    }
  }, [gameSystemId]);

  /**
   * Re-fetches the currently selected template to reflect server-side changes.
   */
  const refreshSelectedTemplate = useCallback(async () => {
    if (!gameSystemId || !selectedTemplate) return;

    if (isMountedRef.current) setError(null);
    try {
      const template = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      if (isMountedRef.current) setSelectedTemplate(template);
    } catch (err) {
      if (isMountedRef.current) setError(extractErrorMessage(err, 'Error refreshing template'));
    }
  }, [gameSystemId, selectedTemplate]);

  /**
   * Clears the selected template and any comparison data.
   */
  const clearSelectedTemplate = useCallback(() => {
    setSelectedTemplate(null);
  }, []);

  // ── Revert ───────────────────────────────────────────────────────────────

  /**
   * Reverts the selected template back to DRAFT status.
   * @param onReverted - Optional async callback invoked after the revert
   *   so the list layer can refresh its data.
   */
  const revertToDraft = useCallback(async (onReverted?: () => Promise<void>): Promise<boolean> => {
    if (!gameSystemId || !selectedTemplate) return false;

    setIsReverting(true);
    if (isMountedRef.current) setError(null);
    log('Revirtiendo plantilla a borrador...');

    try {
      await entityTemplateService.revertToDraft(gameSystemId, selectedTemplate.id);
      log('[SUCCESS] Plantilla revertida a borrador');

      const updated = await entityTemplateService.getById(gameSystemId, selectedTemplate.id);
      if (isMountedRef.current) setSelectedTemplate(updated);

      await onReverted?.();
      return true;
    } catch (err) {
      const message = extractErrorMessage(err, 'Error al revertir plantilla');
      log(`ERROR: ${message}`);
      if (isMountedRef.current) setError(message);
      return false;
    } finally {
      if (isMountedRef.current) setIsReverting(false);
    }
  }, [gameSystemId, selectedTemplate, log]);

  return {
    selectedTemplate,
    isReverting,
    error,
    selectTemplate,
    refreshSelectedTemplate,
    clearSelectedTemplate,
    revertToDraft,
  };
}
