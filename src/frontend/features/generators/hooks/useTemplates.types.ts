/**
 * useTemplates — shared types
 * Extracted from useTemplates.ts so that all sub-hooks can import them
 * without circular dependencies.
 */

import type {
  EntityTemplateSummary,
  EntityTemplate,
  ExtractedTemplateInfo,
  FieldDefinition,
} from '@core/types';

export interface UseTemplatesConfig {
  gameSystemId: string | null;
  userId?: string;
  userRole?: string;
  isOwner: boolean;
  onLog?: (message: string) => void;
}

export interface TemplateCounts {
  total: number;
  confirmed: number;
  pending: number;
}

export interface UseTemplatesReturn {
  // ── State ─────────────────────────────────────────────────────────────────
  templates: EntityTemplateSummary[];
  selectedTemplate: EntityTemplate | null;
  counts: TemplateCounts;
  isLoading: boolean;
  isConfirming: boolean;
  isExtracting: boolean;
  isSavingFields: boolean;
  isReverting: boolean;
  extractionResult: ExtractedTemplateInfo[] | null;
  newlyExtractedIds: Set<string>;
  comparisonExtractedFields: FieldDefinition[] | null;
  comparisonTemplateName: string;
  error: string | null;
  // ── Actions ───────────────────────────────────────────────────────────────
  selectTemplate: (templateId: string) => Promise<void>;
  confirmTemplate: (templateId: string) => Promise<boolean>;
  confirmAll: () => Promise<boolean>;
  extractTemplates: () => Promise<ExtractedTemplateInfo[] | null>;
  clearExtractionResult: () => void;
  refresh: () => Promise<void>;
  refreshSelectedTemplate: () => Promise<void>;
  clearSelectedTemplate: () => void;
  isNewlyExtracted: (templateId: string) => boolean;
  viewSkippedExtraction: (info: ExtractedTemplateInfo) => Promise<void>;
  addFieldFromComparison: (field: FieldDefinition) => Promise<boolean>;
  addAllNewFieldsFromComparison: () => Promise<boolean>;
  revertToDraft: () => Promise<boolean>;
  closeComparison: () => void;
}
