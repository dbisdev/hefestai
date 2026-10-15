/**
 * useConfirmedTemplates Hook
 * Single Responsibility: Load and manage confirmed templates for a game system.
 *
 * Fetches all confirmed entity templates (confirmedOnly=true) for the game system
 * associated with the active campaign. Returns the template list plus derived
 * helpers consumed by GalleryPage/GallerySidebar:
 *   - confirmedEntityTypes: Set<string> of lower-case entity type names
 *   - isCategoryAvailable(categoryId): whether a given gallery category has templates
 *
 * Memory-safety: uses an AbortController / isMounted flag pattern so no state
 * mutation happens after unmount.
 */

import { useState, useEffect, useCallback, useMemo } from 'react';
import { entityTemplateService } from '@core/services/api';
import { CATEGORIES_WITHOUT_TEMPLATE } from '@features/gallery/constants/categories';
import type { EntityCategory, EntityTemplateSummary } from '@core/types';
import { logger } from '@core/utils';

export interface UseConfirmedTemplatesReturn {
  /** Confirmed templates for the active game system */
  confirmedTemplates: EntityTemplateSummary[];
  /** Whether templates are being fetched */
  isLoadingTemplates: boolean;
  /** Lower-case set of entity type names that have at least one confirmed template */
  confirmedEntityTypes: Set<string>;
  /**
   * Returns true when the given category does not require a template (fixed schema)
   * OR when there is at least one confirmed template for it.
   */
  isCategoryAvailable: (categoryId: EntityCategory) => boolean;
}

/**
 * Loads confirmed templates for the provided gameSystemId.
 * Pass `undefined` when no campaign is active — returns empty state.
 */
export function useConfirmedTemplates(gameSystemId: string | undefined): UseConfirmedTemplatesReturn {
  const [confirmedTemplates, setConfirmedTemplates] = useState<EntityTemplateSummary[]>([]);
  const [isLoadingTemplates, setIsLoadingTemplates] = useState(false);

  useEffect(() => {
    if (!gameSystemId) {
      setConfirmedTemplates([]);
      return;
    }

    let isMounted = true;

    const loadTemplates = async () => {
      setIsLoadingTemplates(true);
      try {
        // Use confirmedOnly=true so Masters can see templates created by others
        const result = await entityTemplateService.getByGameSystem(
          gameSystemId,
          undefined, // no status filter
          true       // confirmedOnly
        );
        if (isMounted) {
          setConfirmedTemplates(result.templates);
        }
      } catch (error) {
        logger.error('[useConfirmedTemplates] Failed to load templates:', error);
        if (isMounted) {
          setConfirmedTemplates([]);
        }
      } finally {
        if (isMounted) {
          setIsLoadingTemplates(false);
        }
      }
    };

    loadTemplates();

    return () => {
      isMounted = false;
    };
  }, [gameSystemId]);

  /** Derived set — recalculated only when templates list changes */
  const confirmedEntityTypes = useMemo<Set<string>>(
    () => new Set(confirmedTemplates.map(t => t.entityTypeName.toLowerCase())),
    [confirmedTemplates]
  );

  const isCategoryAvailable = useCallback(
    (categoryId: EntityCategory): boolean => {
      if ((CATEGORIES_WITHOUT_TEMPLATE as readonly string[]).includes(categoryId)) {
        return true;
      }
      return confirmedEntityTypes.has(categoryId.toLowerCase());
    },
    [confirmedEntityTypes]
  );

  return {
    confirmedTemplates,
    isLoadingTemplates,
    confirmedEntityTypes,
    isCategoryAvailable,
  };
}
