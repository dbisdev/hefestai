/**
 * Application-wide types
 * Single Responsibility: Only app navigation and state types
 */

export enum Screen {
  LOGIN = 'LOGIN',
  SIGNUP = 'SIGNUP',
  MASTER_HUB = 'MASTER_HUB',
  GALLERY = 'GALLERY',
  CHAR_GEN = 'CHAR_GEN',
  SOLAR_GEN = 'SOLAR_GEN',
  VEHI_GEN = 'VEHI_GEN',
  NPC_GEN = 'NPC_GEN',
  ENEMY_GEN = 'ENEMY_GEN',
  MISSION_GEN = 'MISSION_GEN',
  ENCOUNTER_GEN = 'ENCOUNTER_GEN',
  CAMPAIGN_GEN = 'CAMPAIGN_GEN',
  CAMPAIGN_SETTINGS = 'CAMPAIGN_SETTINGS',
  GAME_SYSTEMS = 'GAME_SYSTEMS',
  TEMPLATES = 'TEMPLATES',
  INVITATIONS = 'INVITATIONS',
  ADMIN_USERS = 'ADMIN_USERS',
  ADMIN_CAMPAIGNS = 'ADMIN_CAMPAIGNS',
  ADMIN_SYSTEM = 'ADMIN_SYSTEM',
  ERROR = 'ERROR',
  ACCESS_DENIED = 'ACCESS_DENIED'
}

export type TransitionStage = 'idle' | 'out' | 'in';

export interface AppState {
  currentScreen: Screen;
  transitionStage: TransitionStage;
  error: string | null;
}
