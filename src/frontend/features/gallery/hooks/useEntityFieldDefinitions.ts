/**
 * useEntityFieldDefinitions Hook
 * Single Responsibility: Load field definitions for the selected entity's type.
 *
 * Field definitions drive the edit modal and PDF export label mapping.
 * They are scoped to a (gameSystemId, entityType) pair and reset whenever
 * either value changes or becomes unavailable.
 *
 * Memory-safety: uses an isMounted flag so no state mutation happens after
 * unmount.
 */

import { useState, useEffect } from 'react';
import { entityTemplateService } from '@core/services/api';
import type { FieldDefinition } from '@core/types';
import { logger } from '@core/utils';

export interface UseEntityFieldDefinitionsReturn {
  /** Field definitions for the currently selected entity type */
  fieldDefinitions: FieldDefinition[];
}

/**
 * Loads field definitions for the given gameSystemId + entityType pair.
 * Pass `undefined` for either argument when no entity is selected.
 */
export function useEntityFieldDefinitions(
  gameSystemId: string | undefined,
  entityType: string | undefined
): UseEntityFieldDefinitionsReturn {
  const [fieldDefinitions, setFieldDefinitions] = useState<FieldDefinition[]>([]);

  useEffect(() => {
    if (!gameSystemId || !entityType) {
      setFieldDefinitions([]);
      return;
    }

    let isMounted = true;

    const loadFieldDefinitions = async () => {
      try {
        const fields = await entityTemplateService.getFieldDefinitions(gameSystemId, entityType);
        if (isMounted) {
          setFieldDefinitions(fields);
        }
      } catch (error) {
        logger.error('[useEntityFieldDefinitions] Failed to load field definitions:', error);
        if (isMounted) {
          setFieldDefinitions([]);
        }
      }
    };

    loadFieldDefinitions();

    return () => {
      isMounted = false;
    };
  }, [gameSystemId, entityType]);

  return { fieldDefinitions };
}
