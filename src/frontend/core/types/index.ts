/**
 * Central type exports
 * Provides a single entry point for all type imports
 */

// Auth types
export type {
  UserRole,
  User,
  AuthState,
  LoginCredentials,
  RegisterCredentials,
  AuthResponse,
  CurrentUserResponse,
} from './auth.types';

// Entity types
export {
  VisibilityLevel,
  OwnershipType,
} from './entity.types';

export type {
  TemplateEntityType,
  GeneratedEntityType,
  EntityCategory,
  LoreEntity,
  CreateLoreEntityInput,
  UpdateLoreEntityInput,
  ChangeVisibilityInput,
  TransferOwnershipInput,
  DynamicStats,
  CharacterData,
  PlanetData,
  SystemStats,
  SystemData,
  VehicleData,
  NpcData,
  EnemyData,
  MissionData,
  EncounterData,
} from './entity.types';

// Campaign types
export { CampaignRole } from './campaign.types';

export type {
  Campaign,
  CampaignDetail,
  CampaignMember,
  CreateCampaignInput,
  UpdateCampaignInput,
  JoinCampaignInput,
  UpdateMemberRoleInput,
  UpdateCampaignStatusInput,
  JoinCodeResponse,
} from './campaign.types';

// Game System types
export type {
  GameSystem,
  GameSystemOption,
  CreateGameSystemInput,
  UpdateGameSystemInput,
  UpdateGameSystemStatusInput,
} from './gameSystem.types';

// API types
export type {
  ApiError,
  ApiResponse,
  GenerationParamsBase,
  GenerationResponseBase,
  CharacterGenerationParams,
  CharacterGenerationResponse,
  SolarSystemGenerationParams,
  SolarSystemGenerationResponse,
  VehicleGenerationParams,
  VehicleGenerationResponse,
  NpcGenerationParams,
  NpcGenerationResponse,
  EnemyGenerationParams,
  EnemyGenerationResponse,
  MissionGenerationParams,
  MissionGenerationResponse,
  EncounterGenerationParams,
  EncounterGenerationResponse,
  RequestConfig,
  HttpMethod,
} from './api.types';

// App types
export { Screen } from './app.types';
export type { TransitionStage, AppState } from './app.types';

// Template types
export { TemplateStatus, FieldType, TemplateStatusLabels, FieldTypeLabels } from './template.types';
export type {
  FieldDefinition,
  EntityTemplate,
  EntityTemplateSummary,
  GetTemplatesResult,
  ExtractedTemplateInfo,
  ExtractTemplatesResult,
  ConfirmTemplateResult,
  ConfirmTemplateInput,
  CreateTemplateInput,
  CreateTemplateResult,
  UpdateTemplateInput,
  UpdateTemplateResult,
  DeleteTemplateResult,
} from './template.types';

// Admin types
export { AdminUserRole, AdminUserRoleLabels, AdminUserRoleColors } from './admin.types';
export type {
  AdminUser,
  AdminCampaign,
  CreateUserInput,
  UpdateUserInput,
  AdminUpdateCampaignInput,
} from './admin.types';

// Document / RAG types
export type {
  DocumentDto,
  DocumentSearchResult,
  SemanticSearchParams,
  SemanticSearchResult,
  RagSourceType,
  ManualDto,
  ManualSummaryDto,
  UploadManualResult,
  UploadManualParams,
  DocumentAvailabilityResult,
  GenerateMissingEmbeddingsParams,
  GenerateMissingEmbeddingsResult,
} from './document.types';
