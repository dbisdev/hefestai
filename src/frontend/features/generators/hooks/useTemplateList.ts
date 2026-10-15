/**
 * useTemplateList
 * Single Responsibility: Fetching, confirming, and extracting the template list
 * for a given game system.
 *
 * Concerns handled here:
 *  - Fetching template summaries from the server (refresh)
 *  - Confirming individual templates or all pending templates
 *  - Extracting templates from a game-system manual
 *  - Tracking which templates were newly extracted (newlyExtractedIds)
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import { entityTemplateService } from '@core/services/api';
import { extractErrorMessage } from '@core/utils/error';
import type { EntityTemplateSummary, ExtractedTemplateInfo } from '@core/types';
import type { TemplateCounts, UseTemplatesConfig } from './useTemplates.types';

export interface UseTemplateListReturn {
  templates: EntityTemplateSummary[];
  counts: TemplateCounts;
  isLoading: boolean;
  isConfirming: boolean;
  isExtracting: boolean;
  extractionResult: ExtractedTemplateInfo[] | null;
  newlyExtractedIds: Set<string>;
  error: string | null;
  refresh: () => Promise<void>;
  confirmTemplate: (templateId: string, onConfirmed?: (id: string) => void) => Promise<boolean>;
  confirmAll: () => Promise<boolean>;
  extractTemplates: () => Promise<ExtractedTemplateInfo[] | null>;
  clearExtractionResult: () => void;
  isNewlyExtracted: (templateId: string) => boolean;
  clearNewlyExtractedId: (templateId: string) => void;
  clearAllNewlyExtracted: () => void;
}

/**
 * Manages the template list state for a given game system:
 * fetching, confirming, and AI-extraction operations.
 */
export function useTemplateList({
  gameSystemId,
  userRole,
  isOwner,
  onLog,
}: Pick<UseTemplatesConfig, 'gameSystemId' | 'userRole' | 'isOwner' | 'onLog'>): UseTemplateListReturn {
  const [templates, setTemplates] = useState<EntityTemplateSummary[]>([]);
  const [counts, setCounts] = useState<TemplateCounts>({ total: 0, confirmed: 0, pending: 0 });
  const [isLoading, setIsLoading] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const [isExtracting, setIsExtracting] = useState(false);
  const [extractionResult, setExtractionResult] = useState<ExtractedTemplateInfo[] | null>(null);
  const [newlyExtractedIds, setNewlyExtractedIds] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | null>(null);

  /** Guards async state updates against updates on unmounted components */
  const isMountedRef = useRef(true);
  useEffect(() => {
    isMountedRef.current = true;
    return () => { isMountedRef.current = false; };
  }, []);

  const log = useCallback((message: string) => { onLog?.(message); }, [onLog]);

  // ── Fetch ────────────────────────────────────────────────────────────────

  /**
   * Fetches the template list for the current game system.
   * Adapts the query based on the caller's role and ownership status.
   */
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
        isAdmin || (isMasterUser && isOwner),
      );

      if (isMountedRef.current) {
        setTemplates(result.templates);
        setCounts({
          total: result.totalCount,
          confirmed: result.confirmedCount,
          pending: result.pendingCount,
        });
      }
    } catch (err) {
      if (isMountedRef.current) {
        setError(extractErrorMessage(err, 'Error loading templates'));
        setTemplates([]);
      }
    } finally {
      if (isMountedRef.current) setIsLoading(false);
    }
  }, [gameSystemId, userRole, isOwner]);

  // ── Confirm ──────────────────────────────────────────────────────────────

  /**
   * Confirms a single pending template.
   * @param templateId - The template to confirm.
   * @param onConfirmed - Optional callback invoked after the template is
   *   confirmed so that the selection layer can refresh the selected template.
   */
  const confirmTemplate = useCallback(async (
    templateId: string,
    onConfirmed?: (id: string) => void,
  ): Promise<boolean> => {
    if (!gameSystemId) return false;

    setIsConfirming(true);
    setError(null);

    try {
      await entityTemplateService.confirm(gameSystemId, templateId, { notes: 'Confirmed from UI' });
      log('[SUCCESS] Plantilla confirmada');
      await refresh();
      onConfirmed?.(templateId);
      setNewlyExtractedIds(prev => {
        const next = new Set(prev);
        next.delete(templateId);
        return next;
      });
      return true;
    } catch (err) {
      setError(extractErrorMessage(err, 'Error confirming template'));
      return false;
    } finally {
      setIsConfirming(false);
    }
  }, [gameSystemId, refresh, log]);

  /**
   * Confirms all pending templates in the current game system at once.
   */
  const confirmAll = useCallback(async (): Promise<boolean> => {
    if (!gameSystemId) return false;

    setIsConfirming(true);
    setError(null);

    try {
      await entityTemplateService.confirmAll(gameSystemId);
      log('[SUCCESS] Todas las plantillas confirmadas');
      await refresh();
      setNewlyExtractedIds(new Set());
      setExtractionResult(null);
      return true;
    } catch (err) {
      setError(extractErrorMessage(err, 'Error confirming all templates'));
      return false;
    } finally {
      setIsConfirming(false);
    }
  }, [gameSystemId, refresh, log]);

  // ── Extract ──────────────────────────────────────────────────────────────

  /**
   * Triggers AI extraction of templates from the game system's manual.
   * Populates `extractionResult` and `newlyExtractedIds` on success.
   */
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
          .filter(t =>
            t.templateId &&
            t.templateId !== '00000000-0000-0000-0000-000000000000' &&
            // Exclude skipped/confirmed templates (they have extractedFields for comparison, not a new extraction)
            !(t.extractedFields && t.extractedFields.length > 0),
          )
          .map(t => t.templateId as string),
      );
      setNewlyExtractedIds(newIds);

      log('[SUCCESS] Extracción completada:');
      log(`  - Creadas: ${result.templatesCreated}`);
      log(`  - Actualizadas: ${result.templatesUpdated}`);
      log(`  - Omitidas: ${result.templatesSkipped}`);

      await refresh();
      return result.templates;
    } catch (err) {
      const message = extractErrorMessage(err, 'Error extracting templates');
      setError(message);
      log(`ERROR_CRITICO: ${message}`);
      return null;
    } finally {
      setIsExtracting(false);
    }
  }, [gameSystemId, refresh, log]);

  // ── Helpers ──────────────────────────────────────────────────────────────

  /** Returns true if the given template was created/updated in the last extraction run. */
  const isNewlyExtracted = useCallback(
    (templateId: string) => newlyExtractedIds.has(templateId),
    [newlyExtractedIds],
  );

  /** Removes a single ID from the newly-extracted tracking set (e.g. after confirming). */
  const clearNewlyExtractedId = useCallback((templateId: string) => {
    setNewlyExtractedIds(prev => {
      const next = new Set(prev);
      next.delete(templateId);
      return next;
    });
  }, []);

  /** Clears the full newly-extracted tracking set (e.g. after confirmAll). */
  const clearAllNewlyExtracted = useCallback(() => setNewlyExtractedIds(new Set()), []);

  /** Dismisses the extraction result panel without undoing the extraction. */
  const clearExtractionResult = useCallback(() => setExtractionResult(null), []);

  return {
    templates,
    counts,
    isLoading,
    isConfirming,
    isExtracting,
    extractionResult,
    newlyExtractedIds,
    error,
    refresh,
    confirmTemplate,
    confirmAll,
    extractTemplates,
    clearExtractionResult,
    isNewlyExtracted,
    clearNewlyExtractedId,
    clearAllNewlyExtracted,
  };
}
