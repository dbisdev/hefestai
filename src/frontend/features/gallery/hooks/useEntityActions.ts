/**
 * useEntityActions Hook
 * Single Responsibility: CRUD operations for entities
 * Uses useMutation (from useApi) for each operation to avoid repeating
 * the loading/error/try-catch pattern (DRY principle).
 */

import { useCallback } from 'react';
import { entityService } from '@core/services/api';
import { useMutation } from '@core/hooks/useApi';
import type { LoreEntity, VisibilityLevel } from '@core/types';

interface UseEntityActionsConfig {
  campaignId: string;
  onSuccess?: () => void;
}

interface UseEntityActionsReturn {
  deleteEntity: (id: string) => Promise<boolean>;
  updateVisibility: (id: string, visibility: VisibilityLevel) => Promise<boolean>;
  duplicateEntity: (entity: LoreEntity) => Promise<LoreEntity | null>;
  isDeleting: boolean;
  isUpdating: boolean;
  isDuplicating: boolean;
  error: string | null;
}

export function useEntityActions({
  campaignId,
  onSuccess,
}: UseEntityActionsConfig): UseEntityActionsReturn {

  const deleteMutation = useMutation(
    async (id: string) => {
      await entityService.delete(campaignId, id);
      onSuccess?.();
    },
  );

  const visibilityMutation = useMutation(
    async (id: string, visibility: VisibilityLevel) => {
      await entityService.changeVisibility(campaignId, id, { visibility });
      onSuccess?.();
    },
  );

  const duplicateMutation = useMutation(
    async (entity: LoreEntity): Promise<LoreEntity> => {
      const created = await entityService.create(campaignId, {
        name: `${entity.name} (copia)`,
        description: entity.description,
        entityType: entity.entityType,
        attributes: entity.attributes || {},
        imageUrl: entity.imageUrl,
        visibility: 0,
      });
      onSuccess?.();
      return created;
    },
  );

  /**
   * Deletes an entity by ID. Returns true on success, false on error.
   */
  const deleteEntity = useCallback(async (id: string): Promise<boolean> => {
    const result = await deleteMutation.mutate(id);
    return result !== null;
  }, [deleteMutation]);

  /**
   * Changes the visibility level of an entity. Returns true on success.
   */
  const updateVisibility = useCallback(
    async (id: string, visibility: VisibilityLevel): Promise<boolean> => {
      const result = await visibilityMutation.mutate(id, visibility);
      return result !== null;
    },
    [visibilityMutation],
  );

  /**
   * Creates a copy of the entity with "(copia)" appended to its name.
   * Returns the created entity on success, null on error.
   */
  const duplicateEntity = useCallback(
    async (entity: LoreEntity): Promise<LoreEntity | null> => {
      return duplicateMutation.mutate(entity);
    },
    [duplicateMutation],
  );

  return {
    deleteEntity,
    updateVisibility,
    duplicateEntity,
    isDeleting: deleteMutation.isLoading,
    isUpdating: visibilityMutation.isLoading,
    isDuplicating: duplicateMutation.isLoading,
    // Surface first active error across all three mutations
    error: deleteMutation.error ?? visibilityMutation.error ?? duplicateMutation.error,
  };
}
