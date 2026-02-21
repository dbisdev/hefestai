/**
 * useTemplateFields Hook
 * Single Responsibility: Template field editing state and save logic
 * Note: Field editing state is managed by TemplateFieldEditor component
 */

import { useState, useCallback } from 'react';
import { entityTemplateService } from '@core/services/api';
import type { FieldDefinition } from '@core/types';

interface UseTemplateFieldsConfig {
  gameSystemId: string | null;
  onSuccess?: () => void;
}

interface TemplateData {
  displayName: string;
  description?: string;
  iconHint?: string;
  version?: string;
}

interface UseTemplateFieldsReturn {
  isEditing: boolean;
  isSaving: boolean;
  error: string | null;
  startEditing: (fields: FieldDefinition[]) => void;
  cancelEditing: () => void;
  saveFields: (templateId: string, fields: FieldDefinition[], templateData: TemplateData) => Promise<boolean>;
}

export function useTemplateFields({
  gameSystemId,
  onSuccess,
}: UseTemplateFieldsConfig): UseTemplateFieldsReturn {
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const startEditing = useCallback((fields: FieldDefinition[]) => {
    setIsEditing(true);
    setError(null);
  }, []);

  const cancelEditing = useCallback(() => {
    setIsEditing(false);
    setError(null);
  }, []);

  const saveFields = useCallback(async (
    templateId: string,
    fields: FieldDefinition[],
    templateData: TemplateData
  ): Promise<boolean> => {
    if (!gameSystemId) return false;

    setIsSaving(true);
    setError(null);

    try {
      await entityTemplateService.update(gameSystemId, templateId, {
        displayName: templateData.displayName,
        description: templateData.description,
        iconHint: templateData.iconHint,
        version: templateData.version,
        fields,
      });
      setIsEditing(false);
      onSuccess?.();
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error saving fields';
      setError(message);
      return false;
    } finally {
      setIsSaving(false);
    }
  }, [gameSystemId, onSuccess]);

  return {
    isEditing,
    isSaving,
    error,
    startEditing,
    cancelEditing,
    saveFields,
  };
}
