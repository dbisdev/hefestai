/**
 * Authentication Context
 * Single Responsibility: Global authentication state management
 * Provides auth state and methods to the entire application
 */

import React, { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react';
import { authService } from '@core/services/api';
import { tokenService } from '@core/services/storage/token.service';
import type { User, LoginCredentials, RegisterCredentials, AuthState } from '@core/types';

interface AuthContextValue extends AuthState {
  login: (credentials: LoginCredentials) => Promise<void>;
  register: (credentials: RegisterCredentials) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
  isMaster: boolean;
  isPlayer: boolean;
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  // Initialize with cached user for faster initial render
  const [user, setUser] = useState<User | null>(() => tokenService.getUser());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Check authentication status on mount
  // Only makes API call if token exists and appears valid
  useEffect(() => {
    const checkAuth = async () => {
      try {
        const currentUser = await authService.getCurrentUser();
        setUser(currentUser);
      } catch {
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };
    checkAuth();
  }, []);

  const login = useCallback(async (credentials: LoginCredentials) => {
    setIsLoading(true);
    setError(null);
    try {
      const loggedInUser = await authService.login(credentials);
      setUser(loggedInUser);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error de autenticación';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const register = useCallback(async (credentials: RegisterCredentials) => {
    setIsLoading(true);
    setError(null);
    try {
      const newUser = await authService.register(credentials);
      setUser(newUser);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error de registro';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    setIsLoading(true);
    try {
      await authService.logout();
    } finally {
      setUser(null);
      setError(null);
      setIsLoading(false);
    }
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Role-based flags
  const isMaster = user?.role === 'MASTER';
  const isPlayer = user?.role === 'PLAYER';
  const isAdmin = user?.role === 'ADMIN';

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: !!user,
    isLoading,
    error,
    login,
    register,
    logout,
    clearError,
    isMaster,
    isPlayer,
    isAdmin,
  }), [user, isLoading, error, login, register, logout, clearError, isMaster, isPlayer, isAdmin]);

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

/**
 * Hook to access auth context
 * @throws Error if used outside AuthProvider
 */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

/**
 * Hook to get current user (convenience wrapper)
 */
export function useCurrentUser(): User | null {
  const { user } = useAuth();
  return user;
}
