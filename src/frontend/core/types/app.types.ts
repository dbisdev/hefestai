/**
 * Application-wide types
 * Single Responsibility: Only app navigation and state types
 */

export enum Screen {
  LOGIN = 'LOGIN',
  SIGNUP = 'SIGNUP',
  GALLERY = 'GALLERY',
  CHAR_GEN = 'CHAR_GEN',
  SOLAR_GEN = 'SOLAR_GEN',
  VEHI_GEN = 'VEHI_GEN',
  INVITATIONS = 'INVITATIONS',
  ERROR = 'ERROR',
  ACCESS_DENIED = 'ACCESS_DENIED'
}

export type TransitionStage = 'idle' | 'out' | 'in';

export interface AppState {
  currentScreen: Screen;
  transitionStage: TransitionStage;
  error: string | null;
}
