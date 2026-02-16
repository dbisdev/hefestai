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
  // Legacy aliases (deprecated)
  Entity,
  EntityDto,
  CreateEntityInput,
  UpdateEntityInput,
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
  CreateGameSystemRequest,
  UpdateGameSystemRequest,
  UpdateGameSystemStatusRequest,
} from './gameSystem.types';

// API types
export type {
  ApiError,
  ApiResponse,
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
  ConfirmTemplateRequest,
  CreateTemplateRequest,
  CreateTemplateResult,
  UpdateTemplateRequest,
  UpdateTemplateResult,
  DeleteTemplateResult,
} from './template.types';

// Admin types
export { AdminUserRole, AdminUserRoleLabels, AdminUserRoleColors } from './admin.types';
export type {
  AdminUser,
  AdminCampaign,
  CreateUserRequest,
  UpdateUserRequest,
  AdminUpdateCampaignRequest,
} from './admin.types';
