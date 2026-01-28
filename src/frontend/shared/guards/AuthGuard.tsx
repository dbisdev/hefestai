/**
 * Authentication Guard Component
 * OWASP A01: Broken Access Control - Client-side access control
 * Note: Server-side validation is always authoritative
 */

import React from 'react';
import { useAuth } from '../../../core/context/AuthContext';
import { LoadingSpinner } from '../ui/LoadingSpinner';

interface AuthGuardProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

/**
 * Guard that only renders children if user is authenticated
 */
export const AuthGuard: React.FC<AuthGuardProps> = ({ children, fallback }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingSpinner fullScreen message="Verificando credenciales..." />;
  }

  if (!isAuthenticated) {
    return fallback ? <>{fallback}</> : null;
  }

  return <>{children}</>;
};

/**
 * Guard that only renders children if user is NOT authenticated
 */
export const GuestGuard: React.FC<AuthGuardProps> = ({ children, fallback }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingSpinner fullScreen message="Verificando credenciales..." />;
  }

  if (isAuthenticated) {
    return fallback ? <>{fallback}</> : null;
  }

  return <>{children}</>;
};
