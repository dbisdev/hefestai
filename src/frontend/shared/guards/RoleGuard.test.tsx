/**
 * RoleGuard Component Unit Tests
 * Tests for role-based access control
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { RoleGuard, MasterGuard, AdminGuard } from './RoleGuard';
import { useAuth } from '@core/context/AuthContext';
import type { User } from '@core/types';

vi.mock('@core/context/AuthContext', () => ({
  useAuth: vi.fn(),
}));

describe('RoleGuard', () => {
  const mockPlayerUser: User = {
    id: '1',
    username: 'player',
    email: 'player@test.com',
    role: 'PLAYER',
  };

  const mockMasterUser: User = {
    id: '2',
    username: 'master',
    email: 'master@test.com',
    role: 'MASTER',
  };

  const mockAdminUser: User = {
    id: '3',
    username: 'admin',
    email: 'admin@test.com',
    role: 'ADMIN',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('RoleGuard', () => {
    it('renders children when user has allowed role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockMasterUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: true,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <RoleGuard allowedRoles={['MASTER', 'ADMIN']}>
          <div>Master Content</div>
        </RoleGuard>
      );
      
      expect(screen.getByText('Master Content')).toBeInTheDocument();
    });

    it('renders nothing when user does not have allowed role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockPlayerUser,
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
        <RoleGuard allowedRoles={['MASTER', 'ADMIN']}>
          <div>Master Content</div>
        </RoleGuard>
      );
      
      expect(screen.queryByText('Master Content')).not.toBeInTheDocument();
    });

    it('renders fallback when user does not have allowed role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockPlayerUser,
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
        <RoleGuard allowedRoles={['MASTER']} fallback={<div>Access Denied</div>}>
          <div>Master Content</div>
        </RoleGuard>
      );
      
      expect(screen.getByText('Access Denied')).toBeInTheDocument();
      expect(screen.queryByText('Master Content')).not.toBeInTheDocument();
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
        <RoleGuard allowedRoles={['PLAYER']}>
          <div>Player Content</div>
        </RoleGuard>
      );
      
      expect(screen.queryByText('Player Content')).not.toBeInTheDocument();
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
        <RoleGuard allowedRoles={['PLAYER']} fallback={<div>Please Login</div>}>
          <div>Player Content</div>
        </RoleGuard>
      );
      
      expect(screen.getByText('Please Login')).toBeInTheDocument();
    });

    it('allows ADMIN role for MASTER content', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockAdminUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: true,
      });
      
      render(
        <RoleGuard allowedRoles={['MASTER']}>
          <div>Master Content</div>
        </RoleGuard>
      );
      
      expect(screen.queryByText('Master Content')).not.toBeInTheDocument();
    });

    it('allows multiple roles', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockPlayerUser,
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
        <RoleGuard allowedRoles={['PLAYER', 'MASTER', 'ADMIN']}>
          <div>Content</div>
        </RoleGuard>
      );
      
      expect(screen.getByText('Content')).toBeInTheDocument();
    });
  });

  describe('MasterGuard', () => {
    it('renders children for MASTER role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockMasterUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: true,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <MasterGuard>
          <div>Master Content</div>
        </MasterGuard>
      );
      
      expect(screen.getByText('Master Content')).toBeInTheDocument();
    });

    it('renders children for ADMIN role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockAdminUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: true,
      });
      
      render(
        <MasterGuard>
          <div>Master Content</div>
        </MasterGuard>
      );
      
      expect(screen.getByText('Master Content')).toBeInTheDocument();
    });

    it('does not render for PLAYER role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockPlayerUser,
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
        <MasterGuard>
          <div>Master Content</div>
        </MasterGuard>
      );
      
      expect(screen.queryByText('Master Content')).not.toBeInTheDocument();
    });

    it('renders fallback for non-master', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockPlayerUser,
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
        <MasterGuard fallback={<div>Master Only</div>}>
          <div>Master Content</div>
        </MasterGuard>
      );
      
      expect(screen.getByText('Master Only')).toBeInTheDocument();
    });
  });

  describe('AdminGuard', () => {
    it('renders children for ADMIN role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockAdminUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: false,
        isAdmin: true,
      });
      
      render(
        <AdminGuard>
          <div>Admin Content</div>
        </AdminGuard>
      );
      
      expect(screen.getByText('Admin Content')).toBeInTheDocument();
    });

    it('does not render for MASTER role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockMasterUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: true,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <AdminGuard>
          <div>Admin Content</div>
        </AdminGuard>
      );
      
      expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
    });

    it('does not render for PLAYER role', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockPlayerUser,
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
        <AdminGuard>
          <div>Admin Content</div>
        </AdminGuard>
      );
      
      expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
    });

    it('renders fallback for non-admin', () => {
      vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: mockMasterUser,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: true,
        isPlayer: false,
        isAdmin: false,
      });
      
      render(
        <AdminGuard fallback={<div>Admin Only</div>}>
          <div>Admin Content</div>
        </AdminGuard>
      );
      
      expect(screen.getByText('Admin Only')).toBeInTheDocument();
    });
  });
});
