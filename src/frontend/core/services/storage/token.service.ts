/**
 * Token Service
 * Single Responsibility: Secure token storage and retrieval
 * OWASP A02: Cryptographic Failures - Proper token handling
 */

import { TOKEN_KEY, REFRESH_TOKEN_KEY, USER_KEY } from '@core/config/constants';
import type { User } from '@core/types';

/**
 * Token storage service
 * Note: In production, consider using httpOnly cookies for token storage
 * to prevent XSS token theft. This implementation uses localStorage
 * with awareness of the security tradeoffs.
 */
class TokenService {
  /**
   * Get the access token from storage
   */
  getAccessToken(): string | null {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      // Handle cases where localStorage is not available
      console.warn('localStorage not available');
      return null;
    }
  }

  /**
   * Get the refresh token from storage
   */
  getRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      console.warn('localStorage not available');
      return null;
    }
  }

  /**
   * Store both tokens securely
   */
  setTokens(accessToken: string, refreshToken: string): void {
    try {
      localStorage.setItem(TOKEN_KEY, accessToken);
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    } catch (error) {
      console.error('Failed to store tokens:', error);
    }
  }

  /**
   * Clear all authentication tokens
   */
  clearTokens(): void {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    } catch (error) {
      console.error('Failed to clear tokens:', error);
    }
  }

  /**
   * Check if user has a valid token (doesn't verify expiration)
   */
  hasToken(): boolean {
    return !!this.getAccessToken();
  }

  /**
   * Store user data (non-sensitive info for UI purposes)
   */
  setUser(user: User): void {
    try {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } catch (error) {
      console.error('Failed to store user:', error);
    }
  }

  /**
   * Get cached user data
   */
  getUser(): User | null {
    try {
      const userData = localStorage.getItem(USER_KEY);
      if (!userData) return null;
      return JSON.parse(userData) as User;
    } catch {
      return null;
    }
  }

  /**
   * Parse JWT token to get expiration (without verification)
   * Note: This is for client-side convenience only - server always validates
   */
  getTokenExpiration(token: string): Date | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      
      const payload = JSON.parse(atob(parts[1]));
      if (!payload.exp) return null;
      
      return new Date(payload.exp * 1000);
    } catch {
      return null;
    }
  }

  /**
   * Check if the access token appears to be expired
   * Note: Server-side validation is authoritative
   */
  isTokenExpired(): boolean {
    const token = this.getAccessToken();
    if (!token) return true;

    const expiration = this.getTokenExpiration(token);
    if (!expiration) return true;

    // Consider expired if less than 30 seconds remaining
    return expiration.getTime() < Date.now() + 30000;
  }
}

// Export singleton instance
export const tokenService = new TokenService();
