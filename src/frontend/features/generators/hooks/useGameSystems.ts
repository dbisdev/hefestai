/**
 * useGameSystems Hook
 * Single Responsibility: Game system form state and CRUD operations
 * Note: List state is managed by useList hook (SRP)
 */

import { useState, useCallback } from 'react';
import { gameSystemService } from '@core/services/api';
import type { 
  GameSystem, 
  CreateGameSystemRequest, 
  UpdateGameSystemRequest 
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
  create: (data: CreateGameSystemRequest) => Promise<GameSystem | null>;
  update: (id: string, data: UpdateGameSystemRequest) => Promise<GameSystem | null>;
  toggleStatus: (id: string, isActive: boolean) => Promise<boolean>;
}

export function useGameSystems(): UseGameSystemsReturn {
  const [selectedSystem, setSelectedSystem] = useState<GameSystem | null>(null);
  const [formMode, setFormMode] = useState<FormMode>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState<string | null>(null);

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

  const closeForm = useCallback(() => {
    setFormMode(null);
  }, []);

  const create = useCallback(async (data: CreateGameSystemRequest): Promise<GameSystem | null> => {
    setIsCreating(true);
    setError(null);

    try {
      const newSystem = await gameSystemService.create(data);
      setFormMode(null);
      return newSystem;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error creating game system';
      setError(message);
      return null;
    } finally {
      setIsCreating(false);
    }
  }, []);

  const update = useCallback(async (id: string, data: UpdateGameSystemRequest): Promise<GameSystem | null> => {
    setIsUpdating(true);
    setError(null);

    try {
      const updatedSystem = await gameSystemService.update(id, data);
      setSelectedSystem(updatedSystem);
      setFormMode(null);
      return updatedSystem;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error updating game system';
      setError(message);
      return null;
    } finally {
      setIsUpdating(false);
    }
  }, []);

  const toggleStatus = useCallback(async (id: string, isActive: boolean): Promise<boolean> => {
    setIsUpdating(true);
    setError(null);

    try {
      const updatedSystem = await gameSystemService.updateStatus(id, { isActive });
      if (selectedSystem?.id === id) {
        setSelectedSystem(updatedSystem);
      }
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error updating status';
      setError(message);
      return false;
    } finally {
      setIsUpdating(false);
    }
  }, [selectedSystem]);

  return {
    selectedSystem,
    isCreating,
    isUpdating,
    formMode,
    error,
    selectSystem,
    openCreateForm,
    openEditForm,
    closeForm,
    create,
    update,
    toggleStatus,
  };
}
