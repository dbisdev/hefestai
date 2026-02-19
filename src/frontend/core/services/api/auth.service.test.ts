/**
 * Auth Service Unit Tests
 * Tests for authentication operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { authService } from './auth.service';
import { httpClient } from './client';
import { tokenService } from '@core/services/storage/token.service';
import type { User } from '@core/types';

vi.mock('./client', () => ({
  httpClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

vi.mock('@core/services/storage/token.service', () => ({
  tokenService: {
    setTokens: vi.fn(),
    setUser: vi.fn(),
    getUser: vi.fn(),
    clearTokens: vi.fn(),
    hasToken: vi.fn(),
    isTokenExpired: vi.fn(),
    getAccessToken: vi.fn(),
  },
}));

describe('Auth Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('login', () => {
    const mockAuthResponse = {
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      userId: 'user-123',
      email: 'test@example.com',
      displayName: 'Test User',
      role: 'player',
      masterId: null,
      invitationCode: null,
    };

    it('logs in successfully', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockAuthResponse);
      
      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/auth/login', 
        { email: 'test@example.com', password: 'password123' },
        { skipAuth: true }
      );
      expect(tokenService.setTokens).toHaveBeenCalledWith('access-token', 'refresh-token');
      expect(tokenService.setUser).toHaveBeenCalled();
      expect(result.email).toBe('test@example.com');
      expect(result.role).toBe('PLAYER');
    });

    it('maps role to uppercase', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        ...mockAuthResponse,
        role: 'master',
      });
      
      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
      
      expect(result.role).toBe('MASTER');
    });

    it('maps userId correctly', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockAuthResponse);
      
      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
      
      expect(result.id).toBe('user-123');
    });

    it('uses displayName as username', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockAuthResponse);
      
      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
      
      expect(result.username).toBe('Test User');
    });

    it('falls back to email as username if no displayName', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce({
        ...mockAuthResponse,
        displayName: null,
      });
      
      const result = await authService.login({
        email: 'test@example.com',
        password: 'password123',
      });
      
      expect(result.username).toBe('test@example.com');
    });
  });

  describe('register', () => {
    const mockRegisterResponse = {
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      userId: 'user-123',
      email: 'new@example.com',
      displayName: 'New User',
      role: 'player',
      masterId: null,
      invitationCode: null,
    };

    it('registers a new user successfully', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockRegisterResponse);
      
      const result = await authService.register({
        email: 'new@example.com',
        password: 'Password123',
        displayName: 'New User',
        role: 'PLAYER',
      });
      
      expect(httpClient.post).toHaveBeenCalledWith('/auth/register', 
        expect.objectContaining({
          email: 'new@example.com',
          password: 'Password123',
          displayName: 'New User',
          role: 'Player',
        }),
        { skipAuth: true }
      );
      expect(tokenService.setTokens).toHaveBeenCalled();
      expect(tokenService.setUser).toHaveBeenCalled();
      expect(result.email).toBe('new@example.com');
    });

    it('formats role correctly (PLAYER -> Player)', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockRegisterResponse);
      
      await authService.register({
        email: 'new@example.com',
        password: 'Password123',
        displayName: 'New User',
        role: 'PLAYER',
      });
      
      const callArgs = vi.mocked(httpClient.post).mock.calls[0][1];
      expect(callArgs).toHaveProperty('role', 'Player');
    });

    it('includes invite code when provided', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(mockRegisterResponse);
      
      await authService.register({
        email: 'new@example.com',
        password: 'Password123',
        displayName: 'New User',
        role: 'MASTER',
        inviteCode: 'MASTER-123',
      });
      
      const callArgs = vi.mocked(httpClient.post).mock.calls[0][1];
      expect(callArgs).toHaveProperty('inviteCode', 'MASTER-123');
    });
  });

  describe('logout', () => {
    it('logs out successfully', async () => {
      vi.mocked(httpClient.post).mockResolvedValueOnce(undefined);
      
      await authService.logout();
      
      expect(httpClient.post).toHaveBeenCalledWith('/auth/logout');
      expect(tokenService.clearTokens).toHaveBeenCalled();
    });

    it('clears tokens even when API call fails', async () => {
      vi.mocked(httpClient.post).mockRejectedValueOnce(new Error('Network error'));
      
      await authService.logout();
      
      expect(tokenService.clearTokens).toHaveBeenCalled();
    });
  });

  describe('getCurrentUser', () => {
    const mockCurrentUser = {
      id: 'user-123',
      email: 'test@example.com',
      displayName: 'Test User',
      role: 'player',
      masterId: null,
      invitationCode: 'ABC123',
    };

    it('returns null when no token exists', async () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(false);
      
      const result = await authService.getCurrentUser();
      
      expect(result).toBeNull();
      expect(httpClient.get).not.toHaveBeenCalled();
    });

    it('returns null and clears tokens when token is expired', async () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(true);
      vi.mocked(tokenService.isTokenExpired).mockReturnValue(true);
      
      const result = await authService.getCurrentUser();
      
      expect(result).toBeNull();
      expect(tokenService.clearTokens).toHaveBeenCalled();
      expect(httpClient.get).not.toHaveBeenCalled();
    });

    it('fetches current user when token is valid', async () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(true);
      vi.mocked(tokenService.isTokenExpired).mockReturnValue(false);
      vi.mocked(httpClient.get).mockResolvedValueOnce(mockCurrentUser);
      
      const result = await authService.getCurrentUser();
      
      expect(httpClient.get).toHaveBeenCalledWith('/auth/me');
      expect(result).not.toBeNull();
      expect(result?.email).toBe('test@example.com');
      expect(tokenService.setUser).toHaveBeenCalled();
    });

    it('returns null and clears tokens on API error', async () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(true);
      vi.mocked(tokenService.isTokenExpired).mockReturnValue(false);
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('Unauthorized'));
      
      const result = await authService.getCurrentUser();
      
      expect(result).toBeNull();
      expect(tokenService.clearTokens).toHaveBeenCalled();
    });

    it('maps masterId correctly', async () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(true);
      vi.mocked(tokenService.isTokenExpired).mockReturnValue(false);
      vi.mocked(httpClient.get).mockResolvedValueOnce({
        ...mockCurrentUser,
        masterId: 'master-123',
      });
      
      const result = await authService.getCurrentUser();
      
      expect(result?.masterId).toBe('master-123');
    });
  });

  describe('isAuthenticated', () => {
    it('returns true when token exists', () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(true);
      
      expect(authService.isAuthenticated()).toBe(true);
    });

    it('returns false when no token exists', () => {
      vi.mocked(tokenService.hasToken).mockReturnValue(false);
      
      expect(authService.isAuthenticated()).toBe(false);
    });
  });

  describe('getCachedUser', () => {
    it('returns cached user', () => {
      const mockUser: User = {
        id: 'user-123',
        username: 'testuser',
        email: 'test@example.com',
        role: 'PLAYER',
      };
      vi.mocked(tokenService.getUser).mockReturnValue(mockUser);
      
      const result = authService.getCachedUser();
      
      expect(result).toEqual(mockUser);
    });

    it('returns null when no cached user', () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      
      expect(authService.getCachedUser()).toBeNull();
    });
  });
});
