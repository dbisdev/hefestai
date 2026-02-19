/**
 * AuthGuard Component Unit Tests
 * Tests for authentication-based route protection
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AuthGuard, GuestGuard } from './AuthGuard';
import { useAuth } from '@core/context/AuthContext';

vi.mock('@core/context/AuthContext', () => ({
  useAuth: vi.fn(),
}));

vi.mock('@shared/components/ui/LoadingSpinner', () => ({
  LoadingSpinner: ({ message }: { message?: string }) => (
    <div data-testid="loading-spinner">{message || 'Loading...'}</div>
  ),
}));

describe('AuthGuard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('AuthGuard', () => {
    it('shows loading spinner while checking auth', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: false,
        isLoading: true,
        user: null,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <AuthGuard>
          <div>Protected Content</div>
        </AuthGuard>
      );
      
      expect(screen.getByTestId('loading-spinner')).toHaveTextContent('Verificando credenciales...');
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });

    it('renders children when authenticated', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: { id: '1', username: 'test', email: 'test@test.com', role: 'PLAYER' },
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: true,
        isAdmin: false,
      });
      
      render(
        <AuthGuard>
          <div>Protected Content</div>
        </AuthGuard>
      );
      
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });

    it('renders nothing when not authenticated', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: false,
        isLoading: false,
        user: null,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <AuthGuard>
          <div>Protected Content</div>
        </AuthGuard>
      );
      
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });

    it('renders fallback when not authenticated', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: false,
        isLoading: false,
        user: null,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <AuthGuard fallback={<div>Please Login</div>}>
          <div>Protected Content</div>
        </AuthGuard>
      );
      
      expect(screen.getByText('Please Login')).toBeInTheDocument();
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });
  });

  describe('GuestGuard', () => {
    it('shows loading spinner while checking auth', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: false,
        isLoading: true,
        user: null,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <GuestGuard>
          <div>Guest Content</div>
        </GuestGuard>
      );
      
      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
      expect(screen.queryByText('Guest Content')).not.toBeInTheDocument();
    });

    it('renders children when not authenticated', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: false,
        isLoading: false,
        user: null,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <GuestGuard>
          <div>Guest Content</div>
        </GuestGuard>
      );
      
      expect(screen.getByText('Guest Content')).toBeInTheDocument();
    });

    it('renders nothing when authenticated', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: { id: '1', username: 'test', email: 'test@test.com', role: 'PLAYER' },
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: true,
        isAdmin: false,
      });
      
      render(
        <GuestGuard>
          <div>Guest Content</div>
        </GuestGuard>
      );
      
      expect(screen.queryByText('Guest Content')).not.toBeInTheDocument();
    });

    it('renders fallback when authenticated', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: { id: '1', username: 'test', email: 'test@test.com', role: 'PLAYER' },
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: true,
        isAdmin: false,
      });
      
      render(
        <GuestGuard fallback={<div>Welcome Back</div>}>
          <div>Guest Content</div>
        </GuestGuard>
      );
      
      expect(screen.getByText('Welcome Back')).toBeInTheDocument();
      expect(screen.queryByText('Guest Content')).not.toBeInTheDocument();
    });
  });
});
