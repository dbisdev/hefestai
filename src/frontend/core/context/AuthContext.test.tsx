/**
 * AuthContext Unit Tests
 * Tests for global authentication state management
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { ReactNode } from 'react';
import { AuthProvider, useAuth, useCurrentUser } from './AuthContext';
import { authService } from '@core/services/api';
import { tokenService } from '@core/services/storage/token.service';
import type { User } from '@core/types';

vi.mock('@core/services/api', () => ({
  authService: {
    getCurrentUser: vi.fn(),
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  },
}));

vi.mock('@core/services/storage/token.service', () => ({
  tokenService: {
    getUser: vi.fn(),
  },
}));

const wrapper = ({ children }: { children: ReactNode }) => (
  <AuthProvider>{children}</AuthProvider>
);

describe('AuthContext', () => {
  const mockUser: User = {
    id: 'user-123',
    username: 'testuser',
    email: 'test@example.com',
    role: 'PLAYER',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('AuthProvider', () => {
    it('initializes with cached user', () => {
      vi.mocked(tokenService.getUser).mockReturnValue(mockUser);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      expect(result.current.user).toEqual(mockUser);
    });

    it('sets loading state during initial auth check', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve(null), 100))
      );
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      expect(result.current.isLoading).toBe(true);
      
      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });
    });

    it('fetches current user on mount', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => {
        expect(authService.getCurrentUser).toHaveBeenCalled();
        expect(result.current.user).toEqual(mockUser);
      });
    });

    it('clears user when auth check fails', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(mockUser);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.user).toBeNull();
      });
    });

    it('handles auth check error', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(mockUser);
      vi.mocked(authService.getCurrentUser).mockRejectedValue(new Error('Auth error'));
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.user).toBeNull();
      });
    });
  });

  describe('login', () => {
    it('logs in successfully', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      vi.mocked(authService.login).mockResolvedValue(mockUser);
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      await act(async () => {
        await result.current.login({
          email: 'test@example.com',
          password: 'password',
        });
      });
      
      expect(authService.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password',
      });
      expect(result.current.user).toEqual(mockUser);
      expect(result.current.isAuthenticated).toBe(true);
    });

    it('sets error on login failure', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      vi.mocked(authService.login).mockRejectedValue(new Error('Invalid credentials'));
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      await act(async () => {
        try {
          await result.current.login({
            email: 'test@example.com',
            password: 'wrong',
          });
        } catch (e) {
          // Expected
        }
      });
      
      expect(result.current.error).toBe('Invalid credentials');
      expect(result.current.user).toBeNull();
    });

    it('sets loading state during login', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      vi.mocked(authService.login).mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve(mockUser), 100))
      );
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      act(() => {
        result.current.login({
          email: 'test@example.com',
          password: 'password',
        });
      });
      
      expect(result.current.isLoading).toBe(true);
      
      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });
    });
  });

  describe('register', () => {
    it('registers successfully', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      vi.mocked(authService.register).mockResolvedValue(mockUser);
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      await act(async () => {
        await result.current.register({
          email: 'new@example.com',
          password: 'Password123',
          displayName: 'New User',
          role: 'PLAYER',
        });
      });
      
      expect(authService.register).toHaveBeenCalled();
      expect(result.current.user).toEqual(mockUser);
    });

    it('sets error on register failure', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      vi.mocked(authService.register).mockRejectedValue(new Error('Email exists'));
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      await act(async () => {
        try {
          await result.current.register({
            email: 'existing@example.com',
            password: 'Password123',
            displayName: 'User',
            role: 'PLAYER',
          });
        } catch (e) {
          // Expected
        }
      });
      
      expect(result.current.error).toBe('Email exists');
    });
  });

  describe('logout', () => {
    it('logs out successfully', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(mockUser);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);
      vi.mocked(authService.logout).mockResolvedValue(undefined);
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.user).toEqual(mockUser));
      
      await act(async () => {
        await result.current.logout();
      });
      
      expect(authService.logout).toHaveBeenCalled();
      expect(result.current.user).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
    });
  });

  describe('clearError', () => {
    it('clears error state', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(null);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(null);
      vi.mocked(authService.login).mockRejectedValue(new Error('Error'));
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => expect(result.current.isLoading).toBe(false));
      
      await act(async () => {
        try {
          await result.current.login({ email: 'test', password: 'test' });
        } catch (e) {}
      });
      
      expect(result.current.error).not.toBeNull();
      
      act(() => {
        result.current.clearError();
      });
      
      expect(result.current.error).toBeNull();
    });
  });

  describe('role flags', () => {
    it('sets isPlayer true for PLAYER role', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue({ ...mockUser, role: 'PLAYER' });
      vi.mocked(authService.getCurrentUser).mockResolvedValue({ ...mockUser, role: 'PLAYER' });
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.isPlayer).toBe(true);
        expect(result.current.isMaster).toBe(false);
        expect(result.current.isAdmin).toBe(false);
      });
    });

    it('sets isMaster true for MASTER role', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue({ ...mockUser, role: 'MASTER' });
      vi.mocked(authService.getCurrentUser).mockResolvedValue({ ...mockUser, role: 'MASTER' });
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.isPlayer).toBe(false);
        expect(result.current.isMaster).toBe(true);
        expect(result.current.isAdmin).toBe(false);
      });
    });

    it('sets isAdmin true for ADMIN role', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue({ ...mockUser, role: 'ADMIN' });
      vi.mocked(authService.getCurrentUser).mockResolvedValue({ ...mockUser, role: 'ADMIN' });
      
      const { result } = renderHook(() => useAuth(), { wrapper });
      
      await waitFor(() => {
        expect(result.current.isPlayer).toBe(false);
        expect(result.current.isMaster).toBe(false);
        expect(result.current.isAdmin).toBe(true);
      });
    });
  });

  describe('useCurrentUser', () => {
    it('returns current user', async () => {
      vi.mocked(tokenService.getUser).mockReturnValue(mockUser);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);
      
      const { result } = renderHook(() => useCurrentUser(), { wrapper });
      
      await waitFor(() => {
        expect(result.current).toEqual(mockUser);
      });
    });
  });

  describe('useAuth error handling', () => {
    it('throws when used outside provider', () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      
      expect(() => {
        renderHook(() => useAuth());
      }).toThrow('useAuth must be used within an AuthProvider');
      
      consoleError.mockRestore();
    });
  });
});
