/**
 * useGeneratorForm Hook
 * DRY: Centralizes the repeated field-update handlers and field-definition loading
 * that appear identically in every entity generator page.
 *
 * Covers:
 *  - handleNameChange       — updates editableData.name
 *  - handleDescriptionChange — updates editableData.description
 *  - handleStatsChange      — replaces editableData.stats wholesale
 *  - loadFieldDefinitions on gameSystemId change
 */

import { useEffect, useCallback } from 'react';

/** Minimum shape required by every generator page's editableData */
interface WithNameDescStats {
  name: string;
  description?: string;
  // stats is intentionally typed loosely so concrete entity interfaces
  // (e.g. MissionData whose stats: MissionStats) can satisfy this constraint
  // without requiring an index signature on every entity type.
  stats?: object;
}

interface UseGeneratorFormConfig<T extends WithNameDescStats> {
  /** Current editable entity data (null when nothing has been generated yet) */
  editableData: T | null;
  /** Setter from useEntityGeneration */
  setEditableData: React.Dispatch<React.SetStateAction<T | null>>;
  /** loadFieldDefinitions from useEntityGeneration */
  loadFieldDefinitions: (gameSystemId: string | undefined) => Promise<void>;
  /** Active campaign's gameSystemId – drives the useEffect */
  gameSystemId: string | undefined;
}

interface UseGeneratorFormReturn {
  /** Updates editableData.name */
  handleNameChange: (value: string | number) => void;
  /** Updates editableData.description */
  handleDescriptionChange: (value: string | number) => void;
  /** Replaces editableData.stats with the provided object */
  handleStatsChange: (newStats: Record<string, unknown>) => void;
}

/**
 * Extracts the three identical field-update callbacks and the
 * loadFieldDefinitions side-effect shared across all generator pages.
 */
export function useGeneratorForm<T extends WithNameDescStats>({
  editableData,
  setEditableData,
  loadFieldDefinitions,
  gameSystemId,
}: UseGeneratorFormConfig<T>): UseGeneratorFormReturn {

  /** Load field definitions whenever the active game system changes */
  useEffect(() => {
    loadFieldDefinitions(gameSystemId);
  }, [gameSystemId, loadFieldDefinitions]);

  const handleNameChange = useCallback(
    (value: string | number) => {
      if (editableData) {
        setEditableData({ ...editableData, name: String(value) });
      }
    },
    [editableData, setEditableData],
  );

  const handleDescriptionChange = useCallback(
    (value: string | number) => {
      if (editableData) {
        setEditableData({ ...editableData, description: String(value) });
      }
    },
    [editableData, setEditableData],
  );

  const handleStatsChange = useCallback(
    (newStats: Record<string, unknown>) => {
      if (editableData) {
        setEditableData({ ...editableData, stats: newStats });
      }
    },
    [editableData, setEditableData],
  );

  return { handleNameChange, handleDescriptionChange, handleStatsChange };
}
