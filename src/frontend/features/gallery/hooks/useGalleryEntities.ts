/**
 * useGalleryEntities Hook
 * Single Responsibility: Fetch and manage gallery entities
 * DIP: Receives campaignId as parameter, doesn't depend on context directly
 */

import { useState, useEffect, useCallback } from 'react';
import { entityService } from '@core/services/api';
import { useApi } from '@core/hooks';
import type { LoreEntity, EntityCategory } from '@core/types';

interface UseGalleryEntitiesConfig {
  campaignId: string | undefined;
  enabled?: boolean;
}

interface UseGalleryEntitiesReturn {
  entities: LoreEntity[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  getByCategory: (category: EntityCategory) => LoreEntity[];
  setEntities: React.Dispatch<React.SetStateAction<LoreEntity[]>>;
}

export function useGalleryEntities({
  campaignId,
  enabled = true,
}: UseGalleryEntitiesConfig): UseGalleryEntitiesReturn {
  const [entities, setEntities] = useState<LoreEntity[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    if (!campaignId || !enabled) {
      setEntities([]);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const data = await entityService.getByCampaign(campaignId);
      setEntities(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error loading entities';
      setError(message);
      setEntities([]);
    } finally {
      setIsLoading(false);
    }
  }, [campaignId, enabled]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const getByCategory = useCallback(
    (category: EntityCategory): LoreEntity[] => {
      return entities.filter((e) => e.entityType === category);
    },
    [entities]
  );

  return {
    entities,
    isLoading,
    error,
    refresh,
    getByCategory,
    setEntities,
  };
}
