/**
 * useGameSystems Hook
 * Single Responsibility: Game system form state and CRUD operations
 * Uses useAsyncOperation for each mutation to avoid repeating
 * the loading/error/try-catch/finally scaffold (DRY principle).
 * Note: List state is managed by useList hook (SRP).
 */

import { useState, useCallback } from 'react';
import { gameSystemService } from '@core/services/api';
import { useAsyncOperation } from '@core/hooks/useAsyncOperation';
import type {
  GameSystem,
  CreateGameSystemInput,
  UpdateGameSystemInput,
} from '@core/types';

type FormMode = 'create' | 'edit' | null;

interface UseGameSystemsReturn {
  selectedSystem: GameSystem | null;
  isCreating: boolean;
  isUpdating: boolean;
  formMode: FormMode;
  error: string | null;
  selectSystem: (system: GameSystem | null) => void;
  openCreateForm: () => void;
  openEditForm: (system: GameSystem) => void;
  closeForm: () => void;
  create: (data: CreateGameSystemInput) => Promise<GameSystem | null>;
  update: (id: string, data: UpdateGameSystemInput) => Promise<GameSystem | null>;
  toggleStatus: (id: string, isActive: boolean) => Promise<boolean>;
}

export function useGameSystems(): UseGameSystemsReturn {
  const [selectedSystem, setSelectedSystem] = useState<GameSystem | null>(null);
  const [formMode, setFormMode] = useState<FormMode>(null);

  const createOp = useAsyncOperation();
  const updateOp = useAsyncOperation();

  // ── Form helpers ─────────────────────────────────────────────────────────

  const selectSystem = useCallback((system: GameSystem | null) => {
    setSelectedSystem(system);
    setFormMode(null);
  }, []);

  const openCreateForm = useCallback(() => {
    setSelectedSystem(null);
    setFormMode('create');
  }, []);

  const openEditForm = useCallback((system: GameSystem) => {
    setSelectedSystem(system);
    setFormMode('edit');
  }, []);

  const closeForm = useCallback(() => setFormMode(null), []);

  // ── Mutations ─────────────────────────────────────────────────────────────

  /**
   * Creates a new game system and closes the form on success.
   */
  const create = useCallback(
    async (data: CreateGameSystemInput): Promise<GameSystem | null> => {
      const newSystem = await createOp.run(
        () => gameSystemService.create(data),
        'Error creating game system',
      );
      if (newSystem) setFormMode(null);
      return newSystem;
    },
    [createOp],
  );

  /**
   * Updates an existing game system, refreshes selected state and closes form on success.
   */
  const update = useCallback(
    async (id: string, data: UpdateGameSystemInput): Promise<GameSystem | null> => {
      const updatedSystem = await updateOp.run(
        () => gameSystemService.update(id, data),
        'Error updating game system',
      );
      if (updatedSystem) {
        setSelectedSystem(updatedSystem);
        setFormMode(null);
      }
      return updatedSystem;
    },
    [updateOp],
  );

  /**
   * Toggles the active/inactive status of a game system.
   * Also refreshes the selected system state if it is the one being toggled.
   */
  const toggleStatus = useCallback(
    async (id: string, isActive: boolean): Promise<boolean> => {
      const updatedSystem = await updateOp.run(
        () => gameSystemService.updateStatus(id, { isActive }),
        'Error updating status',
      );
      if (updatedSystem && selectedSystem?.id === id) {
        setSelectedSystem(updatedSystem);
      }
      return updatedSystem !== null;
    },
    [updateOp, selectedSystem],
  );

  return {
    selectedSystem,
    isCreating: createOp.isLoading,
    isUpdating: updateOp.isLoading,
    formMode,
    error: createOp.error ?? updateOp.error,
    selectSystem,
    openCreateForm,
    openEditForm,
    closeForm,
    create,
    update,
    toggleStatus,
  };
}
