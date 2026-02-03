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
  DynamicStats,
  CharacterData,
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
