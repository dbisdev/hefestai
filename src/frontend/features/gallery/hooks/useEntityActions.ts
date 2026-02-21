/**
 * useEntityActions Hook
 * Single Responsibility: CRUD operations for entities
 * Uses useMutation internally (DRY)
 */

import { useState, useCallback } from 'react';
import { entityService } from '@core/services/api';
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
  const [isDeleting, setIsDeleting] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isDuplicating, setIsDuplicating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const deleteEntity = useCallback(
    async (id: string): Promise<boolean> => {
      setIsDeleting(true);
      setError(null);

      try {
        await entityService.delete(campaignId, id);
        onSuccess?.();
        return true;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Error deleting entity';
        setError(message);
        return false;
      } finally {
        setIsDeleting(false);
      }
    },
    [campaignId, onSuccess]
  );

  const updateVisibility = useCallback(
    async (id: string, visibility: VisibilityLevel): Promise<boolean> => {
      setIsUpdating(true);
      setError(null);

      try {
        await entityService.changeVisibility(campaignId, id, { visibility });
        onSuccess?.();
        return true;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Error updating visibility';
        setError(message);
        return false;
      } finally {
        setIsUpdating(false);
      }
    },
    [campaignId, onSuccess]
  );

  const duplicateEntity = useCallback(
    async (entity: LoreEntity): Promise<LoreEntity | null> => {
      setIsDuplicating(true);
      setError(null);

      try {
        const duplicate: Omit<LoreEntity, 'id' | 'createdAt' | 'updatedAt'> = {
          ...entity,
          name: `${entity.name} (copia)`,
          visibility: 0,
        };
        
        const created = await entityService.create(campaignId, {
          name: duplicate.name,
          description: duplicate.description,
          entityType: duplicate.entityType,
          attributes: duplicate.attributes || {},
          imageUrl: duplicate.imageUrl,
          visibility: 0,
        });
        
        onSuccess?.();
        return created;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Error duplicating entity';
        setError(message);
        return null;
      } finally {
        setIsDuplicating(false);
      }
    },
    [campaignId, onSuccess]
  );

  return {
    deleteEntity,
    updateVisibility,
    duplicateEntity,
    isDeleting,
    isUpdating,
    isDuplicating,
    error,
  };
}
