/**
 * Token Service Unit Tests
 * Tests for secure token storage and retrieval
 */
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { tokenService } from './token.service';

describe('TokenService', () => {
  const mockAccessToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjoxNzEwMDAwMDAwfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
  const mockRefreshToken = 'refresh-token-123';
  const mockUser = {
    id: 'user-123',
    username: 'testuser',
    email: 'test@example.com',
    role: 'PLAYER' as const,
  };

  beforeEach(() => {
    localStorage.clear();
    vi.spyOn(console, 'warn').mockImplementation(() => {});
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getAccessToken', () => {
    it('returns null when no token is stored', () => {
      expect(tokenService.getAccessToken()).toBeNull();
    });

    it('returns the stored access token', () => {
      localStorage.setItem('omega_access_token', mockAccessToken);
      expect(tokenService.getAccessToken()).toBe(mockAccessToken);
    });
  });

  describe('getRefreshToken', () => {
    it('returns null when no refresh token is stored', () => {
      expect(tokenService.getRefreshToken()).toBeNull();
    });

    it('returns the stored refresh token', () => {
      localStorage.setItem('omega_refresh_token', mockRefreshToken);
      expect(tokenService.getRefreshToken()).toBe(mockRefreshToken);
    });
  });

  describe('setTokens', () => {
    it('stores both tokens in localStorage', () => {
      tokenService.setTokens(mockAccessToken, mockRefreshToken);
      
      expect(localStorage.getItem('omega_access_token')).toBe(mockAccessToken);
      expect(localStorage.getItem('omega_refresh_token')).toBe(mockRefreshToken);
    });

    it('overwrites existing tokens', () => {
      localStorage.setItem('omega_access_token', 'old-token');
      localStorage.setItem('omega_refresh_token', 'old-refresh');
      
      tokenService.setTokens(mockAccessToken, mockRefreshToken);
      
      expect(localStorage.getItem('omega_access_token')).toBe(mockAccessToken);
      expect(localStorage.getItem('omega_refresh_token')).toBe(mockRefreshToken);
    });
  });

  describe('clearTokens', () => {
    it('removes all tokens from localStorage', () => {
      localStorage.setItem('omega_access_token', mockAccessToken);
      localStorage.setItem('omega_refresh_token', mockRefreshToken);
      localStorage.setItem('omega_user', JSON.stringify(mockUser));
      
      tokenService.clearTokens();
      
      expect(localStorage.getItem('omega_access_token')).toBeNull();
      expect(localStorage.getItem('omega_refresh_token')).toBeNull();
      expect(localStorage.getItem('omega_user')).toBeNull();
    });

    it('does not throw when localStorage is empty', () => {
      expect(() => tokenService.clearTokens()).not.toThrow();
    });
  });

  describe('hasToken', () => {
    it('returns false when no token is stored', () => {
      expect(tokenService.hasToken()).toBe(false);
    });

    it('returns true when token is stored', () => {
      localStorage.setItem('omega_access_token', mockAccessToken);
      expect(tokenService.hasToken()).toBe(true);
    });
  });

  describe('setUser', () => {
    it('stores user data in localStorage', () => {
      tokenService.setUser(mockUser);
      
      const stored = localStorage.getItem('omega_user');
      expect(stored).not.toBeNull();
      expect(JSON.parse(stored!)).toEqual(mockUser);
    });
  });

  describe('getUser', () => {
    it('returns null when no user is stored', () => {
      expect(tokenService.getUser()).toBeNull();
    });

    it('returns the stored user data', () => {
      localStorage.setItem('omega_user', JSON.stringify(mockUser));
      expect(tokenService.getUser()).toEqual(mockUser);
    });

    it('returns null for invalid JSON', () => {
      localStorage.setItem('omega_user', 'invalid-json');
      expect(tokenService.getUser()).toBeNull();
    });
  });

  describe('getTokenExpiration', () => {
    it('returns null for invalid token format', () => {
      expect(tokenService.getTokenExpiration('invalid')).toBeNull();
    });

    it('returns null for token without exp claim', () => {
      const tokenWithoutExp = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U';
      expect(tokenService.getTokenExpiration(tokenWithoutExp)).toBeNull();
    });

    it('returns expiration date for valid token', () => {
      const exp = 1710000000;
      const token = `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3MTAwMDAwMDB9.signature`;
      const result = tokenService.getTokenExpiration(token);
      
      expect(result).toBeInstanceOf(Date);
      expect(result?.getTime()).toBe(exp * 1000);
    });
  });

  describe('isTokenExpired', () => {
    it('returns true when no token is stored', () => {
      expect(tokenService.isTokenExpired()).toBe(true);
    });

    it('returns true for token without expiration', () => {
      const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U';
      localStorage.setItem('omega_access_token', token);
      
      expect(tokenService.isTokenExpired()).toBe(true);
    });

    it('returns true for expired token', () => {
      const pastExp = Math.floor(Date.now() / 1000) - 3600;
      const payload = btoa(JSON.stringify({ exp: pastExp }));
      const expiredToken = `header.${payload}.signature`;
      localStorage.setItem('omega_access_token', expiredToken);
      
      expect(tokenService.isTokenExpired()).toBe(true);
    });

    it('returns false for valid non-expired token', () => {
      const futureExp = Math.floor(Date.now() / 1000) + 3600;
      const payload = btoa(JSON.stringify({ exp: futureExp }));
      const validToken = `header.${payload}.signature`;
      localStorage.setItem('omega_access_token', validToken);
      
      expect(tokenService.isTokenExpired()).toBe(false);
    });

    it('returns true for token expiring within 30 seconds', () => {
      const nearFutureExp = Math.floor(Date.now() / 1000) + 15;
      const payload = btoa(JSON.stringify({ exp: nearFutureExp }));
      const nearExpiringToken = `header.${payload}.signature`;
      localStorage.setItem('omega_access_token', nearExpiringToken);
      
      expect(tokenService.isTokenExpired()).toBe(true);
    });
  });
});
