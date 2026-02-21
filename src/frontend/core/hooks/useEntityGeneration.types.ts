/**
 * Types for useEntityGeneration hook
 * Follows SOLID principles with dependency injection for flexibility
 */

import type { FieldDefinition, EntityCategory } from '@core/types';
import type { ImageSourceMode } from '@shared/components/ui';

/**
 * Result from AI generation API call
 */
export interface GenerationResult<T> {
  data: T;
  imageBase64?: string;
  imageUrl?: string;
  generationRequestId?: string;
}

/**
 * Parameters for saving an entity
 */
export interface SaveEntityParams {
  name: string;
  description?: string;
  imageUrl?: string;
  attributes: Record<string, unknown>;
  metadata?: Record<string, unknown>;
  generationRequestId?: string;
}

/**
 * Configuration for the useEntityGeneration hook
 * Uses dependency injection for flexibility and testability
 */
export interface UseEntityGenerationConfig<T> {
  entityType: EntityCategory;
  placeholderImage: string;
  initialLogs?: string[];
  maxLogs?: number;
  
  generateFn: (params: unknown, generateImage: boolean) => Promise<GenerationResult<T>>;
  
  saveFn: (params: SaveEntityParams) => Promise<void>;
  
  getFieldDefinitions?: (gameSystemId: string) => Promise<FieldDefinition[]>;
  
  onGenerationSuccess?: (data: T) => void;
  onGenerationError?: (error: Error) => void;
  onSaveSuccess?: () => void;
  onSaveError?: (error: Error) => void;
}

/**
 * Return type for useEntityGeneration hook
 */
export interface UseEntityGenerationReturn<T> {
  isGenerating: boolean;
  isSaving: boolean;
  generatedData: T | null;
  editableData: T | null;
  image: string;
  generationRequestId: string | undefined;
  fieldDefinitions: FieldDefinition[];
  
  imageMode: ImageSourceMode;
  uploadedImageData: string | null;
  
  logs: string[];
  addLog: (message: string) => void;
  clearLogs: () => void;
  
  generate: (formParams: unknown) => Promise<void>;
  save: (campaignId: string, saveParams: SaveEntityParams) => Promise<boolean>;
  loadFieldDefinitions: (gameSystemId: string | undefined) => Promise<void>;
  
  setEditableData: React.Dispatch<React.SetStateAction<T | null>>;
  setImageMode: (mode: ImageSourceMode) => void;
  setUploadedImageData: (data: string | null) => void;
  reset: () => void;
}
