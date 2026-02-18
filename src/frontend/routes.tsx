/**
 * Application Routes Configuration
 * Uses react-router-dom for navigation with proper browser history support
 */

import { lazy, Suspense } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import type { UserRole } from '@core/types';

// Lazy load all page components
const Home = lazy(() => import('@features/auth/pages/Home'));
const LoginPage = lazy(() => import('@features/auth/pages/LoginPage'));
const SignupPage = lazy(() => import('@features/auth/pages/SignupPage'));
const GalleryPage = lazy(() => import('@features/gallery/pages/GalleryPage'));
const MasterHubPage = lazy(() => import('@features/gallery/pages/MasterHubPage'));
const CampaignListPage = lazy(() => import('@features/campaigns/pages/CampaignListPage'));
const CampaignGeneratorPage = lazy(() => import('@features/generators/pages/CampaignGeneratorPage'));
const CampaignSettingsPage = lazy(() => import('@features/generators/pages/CampaignSettingsPage'));
const InvitationsPage = lazy(() => import('@features/invitations/pages/InvitationsPage'));
const CharacterGeneratorPage = lazy(() => import('@features/generators/pages/CharacterGeneratorPage'));
const SolarSystemGeneratorPage = lazy(() => import('@features/generators/pages/SolarSystemGeneratorPage'));
const VehicleGeneratorPage = lazy(() => import('@features/generators/pages/VehicleGeneratorPage'));
const NpcGeneratorPage = lazy(() => import('@features/generators/pages/NpcGeneratorPage'));
const EnemyGeneratorPage = lazy(() => import('@features/generators/pages/EnemyGeneratorPage'));
const MissionGeneratorPage = lazy(() => import('@features/generators/pages/MissionGeneratorPage'));
const EncounterGeneratorPage = lazy(() => import('@features/generators/pages/EncounterGeneratorPage'));
const GameSystemsPage = lazy(() => import('@features/generators/pages/GameSystemsPage'));
const TemplatesPage = lazy(() => import('@features/generators/pages/TemplatesPage'));
const AdminUsersPage = lazy(() => import('@features/admin/pages/AdminUsersPage'));
const AdminCampaignsPage = lazy(() => import('@features/admin/pages/AdminCampaignsPage'));
const AdminSystemPage = lazy(() => import('@features/admin/pages/AdminSystemPage'));
const AccessDenied = lazy(() => import('@shared/components/feedback/AccessDenied'));
const ErrorScreen = lazy(() => import('@shared/components/feedback/ErrorScreen'));

// Loading component
const LoadingScreen = () => (
  <div className="h-screen w-screen flex items-center justify-center bg-background-dark">
    <div className="text-primary text-xs uppercase tracking-widest animate-pulse">
      INITIALIZING_SYSTEM...
    </div>
  </div>
);

/**
 * Protected Route wrapper
 * Checks authentication and optional role requirements
 */
interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: UserRole[];
  allowUnauthenticated?: boolean;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRole,
  allowUnauthenticated = false 
}) => {
  const location = useLocation();
  
  // We use a custom auth check via useAuth context in the App component
  // This component is a placeholder that gets wrapped with auth context
  // The actual auth logic is handled in App.tsx
  
  return <>{children}</>;
};

/**
 * Route requiring specific roles
 */
interface RoleProtectedRouteProps {
  children: React.ReactNode;
  userRole?: UserRole;
  requiredRoles: UserRole[];
  fallbackPath?: string;
}

export const RoleProtectedRoute: React.FC<RoleProtectedRouteProps> = ({
  children,
  userRole,
  requiredRoles,
  fallbackPath = '/access-denied'
}) => {
  const location = useLocation();
  
  if (!userRole) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  
  if (!requiredRoles.includes(userRole)) {
    return <Navigate to={fallbackPath} replace />;
  }
  
  return <>{children}</>;
};

/**
 * Redirect authenticated users away from public pages (login, signup, home)
 */
interface PublicRouteRedirectProps {
  children: React.ReactNode;
  isAuthenticated: boolean;
  targetPath: string;
}

export const PublicRouteRedirect: React.FC<PublicRouteRedirectProps> = ({
  children,
  isAuthenticated,
  targetPath
}) => {
  if (isAuthenticated) {
    return <Navigate to={targetPath} replace />;
  }
  return <>{children}</>;
};

/**
 * App Routes Configuration
 */
export const routes = [
  // Public routes
  {
    path: '/',
    name: 'home',
    element: <Home />,
    isPublic: true,
  },
  {
    path: '/login',
    name: 'login',
    element: <LoginPage />,
    isPublic: true,
  },
  {
    path: '/signup',
    name: 'signup',
    element: <SignupPage />,
    isPublic: true,
  },
  {
    path: '/access-denied',
    name: 'access-denied',
    element: <AccessDenied />,
    isPublic: true,
  },
  {
    path: '/error',
    name: 'error',
    element: <ErrorScreen />,
    isPublic: true,
  },
  
  // Protected routes - requires authentication
  {
    path: '/gallery',
    name: 'gallery',
    element: <GalleryPage />,
    requiredAuth: true,
  },
  {
    path: '/hub',
    name: 'master-hub',
    element: <MasterHubPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  
  // Campaign routes
  {
    path: '/campaigns',
    name: 'campaigns',
    element: <CampaignListPage />,
    requiredAuth: true,
  },
  {
    path: '/campaigns/new',
    name: 'campaign-new',
    element: <CampaignGeneratorPage />,
    requiredAuth: true,
  },
  {
    path: '/campaigns/:campaignId',
    name: 'campaign-settings',
    element: <CampaignSettingsPage />,
    requiredAuth: true,
  },
  {
    path: '/campaigns/:campaignId/invitations',
    name: 'campaign-invitations',
    element: <InvitationsPage />,
    requiredAuth: true,
  },
  
  // Generator routes (Master only)
  {
    path: '/gallery/char-gen',
    name: 'char-gen',
    element: <CharacterGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/gallery/solar-gen',
    name: 'solar-gen',
    element: <SolarSystemGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/gallery/vehi-gen',
    name: 'vehi-gen',
    element: <VehicleGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/gallery/npc-gen',
    name: 'npc-gen',
    element: <NpcGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/gallery/enemy-gen',
    name: 'enemy-gen',
    element: <EnemyGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/gallery/mission-gen',
    name: 'mission-gen',
    element: <MissionGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/gallery/encounter-gen',
    name: 'encounter-gen',
    element: <EncounterGeneratorPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  
  // Game Systems and Templates
  {
    path: '/game-systems',
    name: 'game-systems',
    element: <GameSystemsPage />,
    requiredAuth: true,
    requiredRoles: ['MASTER', 'ADMIN'],
  },
  {
    path: '/templates',
    name: 'templates',
    element: <TemplatesPage />,
    requiredAuth: true,
    requiredRoles: ['ADMIN'],
  },
  
  // Admin routes
  {
    path: '/admin/users',
    name: 'admin-users',
    element: <AdminUsersPage />,
    requiredAuth: true,
    requiredRoles: ['ADMIN'],
  },
  {
    path: '/admin/campaigns',
    name: 'admin-campaigns',
    element: <AdminCampaignsPage />,
    requiredAuth: true,
    requiredRoles: ['ADMIN'],
  },
  {
    path: '/admin/system',
    name: 'admin-system',
    element: <AdminSystemPage />,
    requiredAuth: true,
    requiredRoles: ['ADMIN'],
  },
  
  // Catch all - redirect to home
  {
    path: '*',
    name: 'catch-all',
    element: <Navigate to="/" replace />,
    isPublic: true,
  },
];

/**
 * Get route path with params
 */
export const getRoutePath = (routeName: string, params?: Record<string, string>): string => {
  const route = routes.find(r => r.name === routeName);
  if (!route) return '/';
  
  let path = route.path;
  if (params) {
    Object.entries(params).forEach(([key, value]) => {
      path = path.replace(`:${key}`, value);
    });
  }
  return path;
};

/**
 * Export route names for type safety
 */
export const routeNames = {
  HOME: 'home',
  LOGIN: 'login',
  SIGNUP: 'signup',
  GALLERY: 'gallery',
  MASTER_HUB: 'master-hub',
  CAMPAIGNS: 'campaigns',
  CAMPAIGN_NEW: 'campaign-new',
  CAMPAIGN_SETTINGS: 'campaign-settings',
  CAMPAIGN_INVITATIONS: 'campaign-invitations',
  CHAR_GEN: 'char-gen',
  SOLAR_GEN: 'solar-gen',
  VEHI_GEN: 'vehi-gen',
  NPC_GEN: 'npc-gen',
  ENEMY_GEN: 'enemy-gen',
  MISSION_GEN: 'mission-gen',
  ENCOUNTER_GEN: 'encounter-gen',
  GAME_SYSTEMS: 'game-systems',
  TEMPLATES: 'templates',
  ADMIN_USERS: 'admin-users',
  ADMIN_CAMPAIGNS: 'admin-campaigns',
  ADMIN_SYSTEM: 'admin-system',
  ACCESS_DENIED: 'access-denied',
  ERROR: 'error',
} as const;
