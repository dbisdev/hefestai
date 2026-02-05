/**
 * Entity Template Types
 * Aligned with backend EntityTemplate DTOs
 * Templates define the schema (fields) for creating entities of specific types.
 */

/**
 * Template status enum matching backend TemplateStatus
 */
export enum TemplateStatus {
  Draft = 0,
  PendingReview = 1,
  Confirmed = 2,
  Rejected = 3,
}

/**
 * Field type enum matching backend FieldType
 */
export enum FieldType {
  Text = 0,
  TextArea = 1,
  Number = 2,
  Boolean = 3,
  Select = 4,
  MultiSelect = 5,
  Date = 6,
  Url = 7,
  Json = 8,
}

/**
 * Human-readable labels for template statuses
 */
export const TemplateStatusLabels: Record<TemplateStatus, string> = {
  [TemplateStatus.Draft]: 'Borrador',
  [TemplateStatus.PendingReview]: 'Pendiente',
  [TemplateStatus.Confirmed]: 'Confirmado',
  [TemplateStatus.Rejected]: 'Rechazado',
};

/**
 * Human-readable labels for field types
 */
export const FieldTypeLabels: Record<FieldType, string> = {
  [FieldType.Text]: 'Texto',
  [FieldType.TextArea]: 'Texto largo',
  [FieldType.Number]: 'Número',
  [FieldType.Boolean]: 'Booleano',
  [FieldType.Select]: 'Selección',
  [FieldType.MultiSelect]: 'Selección múltiple',
  [FieldType.Date]: 'Fecha',
  [FieldType.Url]: 'URL',
  [FieldType.Json]: 'JSON',
};

/**
 * Field definition within a template
 */
export interface FieldDefinition {
  name: string;
  displayName: string;
  fieldType: FieldType;
  isRequired: boolean;
  defaultValue?: string;
  description?: string;
  order: number;
  options?: string[];
  minValue?: number;
  maxValue?: number;
  validationPattern?: string;
}

/**
 * Full entity template DTO
 */
export interface EntityTemplate {
  id: string;
  entityTypeName: string;
  displayName: string;
  description?: string;
  status: TemplateStatus;
  fields: FieldDefinition[];
  iconHint?: string;
  version?: string;
  reviewNotes?: string;
  confirmedAt?: string;
  confirmedByUserId?: string;
  gameSystemId: string;
  gameSystemName: string;
  sourceDocumentId?: string;
  ownerId: string;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Lightweight template summary for listings
 */
export interface EntityTemplateSummary {
  id: string;
  entityTypeName: string;
  displayName: string;
  status: TemplateStatus;
  fieldCount: number;
  iconHint?: string;
  createdAt: string;
}

/**
 * Result of GetTemplatesByGameSystem query
 */
export interface GetTemplatesResult {
  templates: EntityTemplateSummary[];
  totalCount: number;
  confirmedCount: number;
  pendingCount: number;
}

/**
 * Information about an extracted template
 */
export interface ExtractedTemplateInfo {
  templateId: string;
  entityTypeName: string;
  displayName: string;
  fieldCount: number;
  isNew: boolean;
  extractionNotes?: string;
}

/**
 * Result of template extraction from manuals
 */
export interface ExtractTemplatesResult {
  templatesCreated: number;
  templatesUpdated: number;
  templatesSkipped: number;
  templates: ExtractedTemplateInfo[];
  errorMessage?: string;
}

/**
 * Result of confirming a template
 */
export interface ConfirmTemplateResult {
  templateId: string;
  entityTypeName: string;
  confirmedAt: string;
  message: string;
}

/**
 * Request to confirm a template
 */
export interface ConfirmTemplateRequest {
  notes?: string;
}

/**
 * Request to create a template manually
 */
export interface CreateTemplateRequest {
  entityTypeName: string;
  displayName: string;
  description?: string;
  iconHint?: string;
  version?: string;
  fields?: FieldDefinition[];
}

/**
 * Result of creating a template
 */
export interface CreateTemplateResult {
  templateId: string;
  entityTypeName: string;
  message: string;
}

/**
 * Request to update a template
 */
export interface UpdateTemplateRequest {
  displayName: string;
  description?: string;
  iconHint?: string;
  version?: string;
  fields?: FieldDefinition[];
}

/**
 * Result of updating a template
 */
export interface UpdateTemplateResult {
  templateId: string;
  message: string;
}

/**
 * Result of deleting a template
 */
export interface DeleteTemplateResult {
  templateId: string;
  message: string;
}
