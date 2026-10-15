/**
 * useGalleryEntities Hook
 * Single Responsibility: Fetch and manage gallery entities
 * DIP: Receives campaignId as parameter, doesn't depend on context directly
 * DRY: Delegates generic list logic to useList
 */

import { useCallback } from 'react';
import { entityService } from '@core/services/api';
import { useList } from '@core/hooks';
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
}

/**
 * Fetches and manages lore entities for a given campaign.
 * Wraps useList to avoid duplicating fetch/loading/error/mount-guard logic.
 *
 * @param campaignId - The campaign whose entities to load.
 * @param enabled    - When false, no fetch is triggered and entities is empty.
 */
export function useGalleryEntities({
  campaignId,
  enabled = true,
}: UseGalleryEntitiesConfig): UseGalleryEntitiesReturn {
  const {
    items: entities,
    isLoading,
    error,
    refresh,
  } = useList<LoreEntity>({
    fetchFn: useCallback(
      () => (campaignId ? entityService.getByCampaign(campaignId) : Promise.resolve([])),
      [campaignId]
    ),
    enabled: enabled && !!campaignId,
  });

  /** Returns entities filtered by EntityCategory */
  const getByCategory = useCallback(
    (category: EntityCategory): LoreEntity[] =>
      entities.filter((e) => e.entityType === category),
    [entities]
  );

  return { entities, isLoading, error, refresh, getByCategory };
}
