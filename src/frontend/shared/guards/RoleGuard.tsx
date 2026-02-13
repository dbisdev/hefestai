/**
 * Role-Based Access Guard Component
 * OWASP A01: Broken Access Control - Client-side role verification
 * Note: Server-side validation is always authoritative
 */

import React from 'react';
import { useAuth } from '@core/context/AuthContext';
import type { UserRole } from '@core/types';

interface RoleGuardProps {
  children: React.ReactNode;
  allowedRoles: UserRole[];
  fallback?: React.ReactNode;
}

/**
 * Guard that only renders children if user has one of the allowed roles
 */
export const RoleGuard: React.FC<RoleGuardProps> = ({ 
  children, 
  allowedRoles, 
  fallback 
}) => {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated || !user) {
    return fallback ? <>{fallback}</> : null;
  }

  if (!allowedRoles.includes(user.role)) {
    return fallback ? <>{fallback}</> : null;
  }

  return <>{children}</>;
};

/**
 * Guard specifically for Master role
 */
export const MasterGuard: React.FC<Omit<RoleGuardProps, 'allowedRoles'>> = ({ 
  children, 
  fallback 
}) => {
  return (
    <RoleGuard allowedRoles={['MASTER', 'ADMIN']} fallback={fallback}>
      {children}
    </RoleGuard>
  );
};

/**
 * Guard specifically for Admin role
 */
export const AdminGuard: React.FC<Omit<RoleGuardProps, 'allowedRoles'>> = ({ 
  children, 
  fallback 
}) => {
  return (
    <RoleGuard allowedRoles={['ADMIN']} fallback={fallback}>
      {children}
    </RoleGuard>
  );
};
