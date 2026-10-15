/**
 * useTemplates Hook — facade
 *
 * Composes three focused sub-hooks into a single unified interface so that
 * existing consumers (TemplatesPage) require no changes:
 *
 *  • useTemplateList       — fetching, confirming, extracting (list state)
 *  • useTemplateSelection  — selecting, refreshing, reverting a single template
 *  • useTemplateComparison — comparison panel for skipped extractions
 *
 * This file is intentionally thin: it wires callbacks across sub-hooks and
 * merges their return values into the public UseTemplatesReturn interface.
 */

import { useCallback, useEffect } from 'react';
import { useTemplateList } from './useTemplateList';
import { useTemplateSelection } from './useTemplateSelection';
import { useTemplateComparison } from './useTemplateComparison';
import type { UseTemplatesConfig, UseTemplatesReturn } from './useTemplates.types';
import type { ExtractedTemplateInfo, FieldDefinition } from '@core/types';

export function useTemplates({
  gameSystemId,
  userId,
  userRole,
  isOwner,
  onLog,
}: UseTemplatesConfig): UseTemplatesReturn {

  // ── Sub-hooks ─────────────────────────────────────────────────────────────

  const list = useTemplateList({ gameSystemId, userRole, isOwner, onLog });
  const selection = useTemplateSelection({ gameSystemId, onLog });
  const comparison = useTemplateComparison({ gameSystemId, onLog });

  // ── Reset derived state when the game system changes ─────────────────────

  useEffect(() => {
    list.clearAllNewlyExtracted();
    comparison.closeComparison();
    selection.clearSelectedTemplate();
    void list.refresh(); // load templates on mount and when game system changes
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gameSystemId]);

  // ── Cross-sub-hook wiring ─────────────────────────────────────────────────

  /**
   * Selects a template and clears any active comparison panel.
   */
  const selectTemplate = useCallback(async (templateId: string) => {
    comparison.closeComparison();
    await selection.selectTemplate(templateId);
  }, [comparison, selection]);

  /**
   * Confirms a single template and refreshes the selected template if needed.
   */
  const confirmTemplate = useCallback(async (templateId: string): Promise<boolean> => {
    return list.confirmTemplate(templateId, async (id) => {
      if (selection.selectedTemplate?.id === id) {
        await selection.refreshSelectedTemplate();
      }
    });
  }, [list, selection]);

  /**
   * Opens the comparison panel for a skipped extraction and updates the
   * selected template simultaneously.
   */
  const viewSkippedExtraction = useCallback(async (info: ExtractedTemplateInfo) => {
    const template = await comparison.viewSkippedExtraction(info);
    if (template) {
      // Sync selection layer so selectedTemplate reflects what the comparison panel is showing
      await selection.selectTemplate(template.id);
    }
  }, [comparison, selection]);

  /**
   * Adds a single field from the comparison panel, keeping selection in sync.
   */
  const addFieldFromComparison = useCallback(async (field: FieldDefinition): Promise<boolean> => {
    if (!selection.selectedTemplate) return false;
    return comparison.addFieldFromComparison(
      field,
      selection.selectedTemplate,
      (updated) => { /* selection already holds selectedTemplate; update via refreshSelectedTemplate */ void selection.refreshSelectedTemplate(); void updated; },
      list.refresh,
    );
  }, [comparison, selection, list.refresh]);

  /**
   * Adds all new fields from the comparison panel, keeping selection in sync.
   */
  const addAllNewFieldsFromComparison = useCallback(async (): Promise<boolean> => {
    if (!selection.selectedTemplate) return false;
    return comparison.addAllNewFieldsFromComparison(
      selection.selectedTemplate,
      () => { void selection.refreshSelectedTemplate(); },
      list.refresh,
    );
  }, [comparison, selection, list.refresh]);

  /**
   * Reverts the selected template to draft and refreshes the list.
   */
  const revertToDraft = useCallback(async (): Promise<boolean> => {
    return selection.revertToDraft(list.refresh);
  }, [selection, list.refresh]);

  /**
   * Clears the selected template and any comparison state.
   */
  const clearSelectedTemplate = useCallback(() => {
    selection.clearSelectedTemplate();
    comparison.closeComparison();
  }, [selection, comparison]);

  // ── Merge and expose ──────────────────────────────────────────────────────

  return {
    // List state
    templates: list.templates,
    counts: list.counts,
    isLoading: list.isLoading,
    isConfirming: list.isConfirming,
    isExtracting: list.isExtracting,
    extractionResult: list.extractionResult,
    newlyExtractedIds: list.newlyExtractedIds,
    error: list.error ?? selection.error ?? comparison.error,

    // Selection state
    selectedTemplate: selection.selectedTemplate,
    isReverting: selection.isReverting,

    // Comparison state
    comparisonExtractedFields: comparison.comparisonExtractedFields,
    comparisonTemplateName: comparison.comparisonTemplateName,
    isSavingFields: comparison.isSavingFields,

    // List actions
    refresh: list.refresh,
    confirmAll: list.confirmAll,
    extractTemplates: list.extractTemplates,
    clearExtractionResult: list.clearExtractionResult,
    isNewlyExtracted: list.isNewlyExtracted,

    // Wired actions (cross-sub-hook)
    selectTemplate,
    confirmTemplate,
    viewSkippedExtraction,
    addFieldFromComparison,
    addAllNewFieldsFromComparison,
    revertToDraft,
    clearSelectedTemplate,

    // Selection actions (no wiring needed)
    refreshSelectedTemplate: selection.refreshSelectedTemplate,

    // Comparison actions (no wiring needed)
    closeComparison: comparison.closeComparison,
  };
}
