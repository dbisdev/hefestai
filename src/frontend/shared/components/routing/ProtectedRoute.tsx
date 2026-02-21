/**
 * Protected Route Component
 * Handles authentication and role-based access control
 * Single Responsibility: Route protection logic
 */

import React from 'react';
import { Navigate } from 'react-router-dom';
import type { RouteConfig, UserRole } from '@core/config/routes';
import { getDefaultRoute } from '@core/config/routes';

interface ProtectedRouteProps {
  route: RouteConfig;
  isAuthenticated: boolean;
  userRole?: UserRole;
  onBack: () => void;
}

/**
 * Checks if user has any of the required roles
 */
const hasRequiredRole = (userRole: UserRole | undefined, requiredRoles: UserRole[]): boolean => {
  if (!userRole) return false;
  return requiredRoles.includes(userRole);
};

/**
 * Protected Route wrapper
 * Redirects based on authentication and role requirements
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  route,
  isAuthenticated,
  userRole,
  onBack,
}) => {
  const { component: Component, requiresAuth, requiredRoles, publicOnly, path } = route;

  // Public-only routes (login, signup) - redirect authenticated users
  if (publicOnly && isAuthenticated) {
    return <Navigate to={getDefaultRoute(isAuthenticated, userRole)} replace />;
  }

  // Routes requiring authentication
  if (requiresAuth && !isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  // Routes requiring specific roles
  if (requiredRoles && requiredRoles.length > 0) {
    if (!hasRequiredRole(userRole, requiredRoles)) {
      return <Navigate to="/access-denied" replace />;
    }
  }

  // Pass onBack prop to components that need it (generators, access-denied, error)
  const needsOnBack = 
    path.includes('-gen') || 
    path.includes('/campaigns/') || 
    path === '/access-denied' || 
    path === '/error' ||
    path === '/campaigns/new';

  return <Component {...(needsOnBack ? { onBack } : {})} />;
};

export default ProtectedRoute;
