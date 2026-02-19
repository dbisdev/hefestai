/**
 * Authentication and User Types
 * Single Responsibility: Only auth-related type definitions
 */

export type UserRole = 'MASTER' | 'PLAYER' | 'ADMIN';

export interface User {
  id: string;
  username: string;
  email?: string;
  role: UserRole;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  email: string;
  password: string;
  displayName: string;
  role: UserRole;
  inviteCode?: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  displayName?: string;
  role: string;
  accessToken: string;
  refreshToken: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  displayName?: string;
  role: string;
  createdAt: string;
  lastLoginAt?: string;
}
