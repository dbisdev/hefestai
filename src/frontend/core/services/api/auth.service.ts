/**
 * Authentication Service
 * Single Responsibility: Authentication operations
 */

import { httpClient } from './client';
import { tokenService } from '@core/services/storage/token.service';
import type { 
  User, 
  UserRole,
  AuthResponse, 
  CurrentUserResponse,
  LoginCredentials,
  RegisterCredentials 
} from '@core/types';

/**
 * Map API response to User object
 */
function mapUserFromResponse(data: AuthResponse | CurrentUserResponse): User {
  const id = 'userId' in data ? data.userId : data.id;
  return {
    id,
    username: data.displayName || data.email,
    email: data.email,
    role: data.role.toUpperCase() as UserRole,
  };
}

export const authService = {
  /**
   * Login with email and password
   */
  async login(credentials: LoginCredentials): Promise<User> {
    const data = await httpClient.post<AuthResponse>('/auth/login', credentials, { 
      skipAuth: true 
    });
    
    tokenService.setTokens(data.accessToken, data.refreshToken);
    const user = mapUserFromResponse(data);
    tokenService.setUser(user);
    
    return user;
  },

  /**
   * Register a new user
   */
  async register(credentials: RegisterCredentials): Promise<User> {
    const payload = {
      email: credentials.email,
      password: credentials.password,
      displayName: credentials.displayName,
      role: credentials.role.charAt(0) + credentials.role.slice(1).toLowerCase(), // MASTER -> Master
      inviteCode: credentials.inviteCode,
    };

    const data = await httpClient.post<AuthResponse>('/auth/register', payload, {
      skipAuth: true
    });

    tokenService.setTokens(data.accessToken, data.refreshToken);
    const user = mapUserFromResponse(data);
    tokenService.setUser(user);

    return user;
  },

  /**
   * Logout the current user
   */
  async logout(): Promise<void> {
    try {
      await httpClient.post('/auth/logout');
    } catch {
      // Ignore errors on logout - clear tokens anyway
    } finally {
      tokenService.clearTokens();
    }
  },

  /**
   * Get the current authenticated user
   * Validates token exists and is not expired before making API call
   */
  async getCurrentUser(): Promise<User | null> {
    // No token at all - no need to make API call
    if (!tokenService.hasToken()) {
      return null;
    }

    // Token appears expired client-side - clear and return null
    // This prevents unnecessary 401 requests
    if (tokenService.isTokenExpired()) {
      tokenService.clearTokens();
      return null;
    }

    try {
      const data = await httpClient.get<CurrentUserResponse>('/auth/me');
      const user = mapUserFromResponse(data);
      tokenService.setUser(user);
      return user;
    } catch {
      // Token invalid on server - clear local storage
      tokenService.clearTokens();
      return null;
    }
  },

  /**
   * Check if user is authenticated (has valid token)
   */
  isAuthenticated(): boolean {
    return tokenService.hasToken();
  },

  /**
   * Get cached user (from localStorage)
   */
  getCachedUser(): User | null {
    return tokenService.getUser();
  },
};
