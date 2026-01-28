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
export type {
  EntityCategory,
  Entity,
  EntityDto,
  CreateEntityInput,
  UpdateEntityInput,
  CharacterData,
  SystemData,
  VehicleData,
} from './entity.types';

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
  RequestConfig,
  HttpMethod,
} from './api.types';

// App types
export { Screen } from './app.types';
export type { TransitionStage, AppState } from './app.types';
