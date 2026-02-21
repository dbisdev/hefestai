/**
 * useEntityGeneration Hook
 * Orchestrates AI entity generation flow following SOLID principles
 * - SRP: Single responsibility for generation orchestration
 * - DIP: Dependencies injected via config (generateFn, saveFn)
 * - OCP: Open for extension via callbacks
 */

import { useState, useEffect, useCallback } from 'react';
import { useTerminalLog } from './useTerminalLog';
import type {
  UseEntityGenerationConfig,
  UseEntityGenerationReturn,
  GenerationResult,
} from './useEntityGeneration.types';
import type { ImageSourceMode } from '@shared/components/ui';

export function useEntityGeneration<T>(
  config: UseEntityGenerationConfig<T>
): UseEntityGenerationReturn<T> {
  const {
    placeholderImage,
    initialLogs = [],
    maxLogs = 6,
    generateFn,
    saveFn,
    getFieldDefinitions,
    onGenerationSuccess,
    onGenerationError,
    onSaveSuccess,
    onSaveError,
  } = config;

  const [isGenerating, setIsGenerating] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [generatedData, setGeneratedData] = useState<T | null>(null);
  const [editableData, setEditableData] = useState<T | null>(null);
  const [image, setImage] = useState<string>(placeholderImage);
  const [generationRequestId, setGenerationRequestId] = useState<string | undefined>();
  const [fieldDefinitions, setFieldDefinitions] = useState<
    UseEntityGenerationReturn<T>['fieldDefinitions']
  >([]);

  const [imageMode, setImageMode] = useState<ImageSourceMode>('generate');
  const [uploadedImageData, setUploadedImageData] = useState<string | null>(null);

  const { logs, addLog, clearLogs, setLogs } = useTerminalLog({
    maxLogs,
    initialLogs,
  });

  const fetchFieldDefinitions = useCallback(
    async (gameSystemId: string | undefined) => {
      if (!gameSystemId || !getFieldDefinitions) {
        setFieldDefinitions([]);
        return;
      }

      try {
        const fields = await getFieldDefinitions(gameSystemId);
        setFieldDefinitions(fields);
      } catch (error) {
        console.error('Failed to fetch template fields:', error);
        setFieldDefinitions([]);
      }
    },
    [getFieldDefinitions]
  );

  const generate = useCallback(
    async (formParams: unknown) => {
      setIsGenerating(true);
      addLog('COMMENCING GENERATION...');

      try {
        const shouldGenerateImage = imageMode === 'generate';

        addLog('FETCHING DATA...');
        const result: GenerationResult<T> = await generateFn(formParams, shouldGenerateImage);

        setGeneratedData(result.data);
        setEditableData(result.data);
        setGenerationRequestId(result.generationRequestId);
        addLog('DATA RECEIVED.');

        if (imageMode === 'upload' && uploadedImageData) {
          setImage(`data:image/webp;base64,${uploadedImageData}`);
          addLog('USING UPLOADED IMAGE.');
        } else if (imageMode === 'generate') {
          addLog('GENERATING VISUAL...');
          if (result.imageUrl) {
            setImage(result.imageUrl);
            addLog('VISUAL SYNTHESIS COMPLETE.');
          } else if (result.imageBase64) {
            setImage(`data:image/webp;base64,${result.imageBase64}`);
            addLog('VISUAL SYNTHESIS COMPLETE.');
          } else {
            setImage(placeholderImage);
            addLog('WARNING: VISUAL RENDER FAILED. USING PLACEHOLDER.');
          }
        } else {
          setImage(placeholderImage);
          addLog('IMAGE GENERATION SKIPPED.');
        }

        addLog('GENERATION SUCCESSFUL.');
        onGenerationSuccess?.(result.data);
      } catch (error) {
        const message = error instanceof Error ? error.message : 'GENERATION FAILED';
        addLog(`CRITICAL_ERROR: ${message}`);
        console.error(error);
        onGenerationError?.(error instanceof Error ? error : new Error(message));
      } finally {
        setIsGenerating(false);
      }
    },
    [
      generateFn,
      imageMode,
      uploadedImageData,
      placeholderImage,
      addLog,
      onGenerationSuccess,
      onGenerationError,
    ]
  );

  const save = useCallback(
    async (campaignId: string, saveParams: UseEntityGenerationReturn<T> extends { save: (a: string, b: infer P) => unknown } ? P : never): Promise<boolean> => {
      if (!campaignId) {
        addLog('ERROR: NO CAMPAIGN SELECTED');
        return false;
      }

      setIsSaving(true);
      addLog('WRITING TO STORAGE...');

      try {
        const params = saveParams as {
          name: string;
          description?: string;
          imageUrl?: string;
          attributes: Record<string, unknown>;
          metadata?: Record<string, unknown>;
          generationRequestId?: string;
        };
        
        await saveFn({
          ...params,
          imageUrl: image !== placeholderImage ? image : params.imageUrl,
          generationRequestId,
        });
        
        addLog('SUCCESS: DATA COMMITTED.');
        onSaveSuccess?.();
        return true;
      } catch (error) {
        const message = error instanceof Error ? error.message : 'STORAGE REFUSED';
        addLog(`DB_WRITE_ERROR: ${message}`);
        onSaveError?.(error instanceof Error ? error : new Error(message));
        return false;
      } finally {
        setIsSaving(false);
      }
    },
    [saveFn, image, placeholderImage, generationRequestId, addLog, onSaveSuccess, onSaveError]
  );

  const reset = useCallback(() => {
    setGeneratedData(null);
    setEditableData(null);
    setImage(placeholderImage);
    setGenerationRequestId(undefined);
    setImageMode('generate');
    setUploadedImageData(null);
    clearLogs();
  }, [placeholderImage, clearLogs]);

  return {
    isGenerating,
    isSaving,
    generatedData,
    editableData,
    image,
    generationRequestId,
    fieldDefinitions,
    imageMode,
    uploadedImageData,
    logs,
    addLog,
    clearLogs,
    generate,
    save: save as UseEntityGenerationReturn<T>['save'],
    loadFieldDefinitions: fetchFieldDefinitions,
    setEditableData,
    setImageMode,
    setUploadedImageData,
    reset,
  };
}

export default useEntityGeneration;
