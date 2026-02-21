/**
 * Route Configuration
 * Declarative route definitions with authentication and role requirements
 * Follows SOLID principles - configuration is separate from routing logic
 */

import type { ComponentType } from 'react';

// Import page components
import Home from '@features/auth/pages/Home';
import LoginPage from '@features/auth/pages/LoginPage';
import SignupPage from '@features/auth/pages/SignupPage';
import { AccessDenied, ErrorScreen } from '@shared/components/feedback';
import GalleryPage from '@features/gallery/pages/GalleryPage';
import { MasterHubPage } from '@features/gallery/pages/MasterHubPage';
import { CampaignListPage } from '@features/campaigns/pages/CampaignListPage';
import { CampaignGeneratorPage } from '@features/generators/pages/CampaignGeneratorPage';
import { CampaignSettingsPage } from '@features/generators/pages/CampaignSettingsPage';
import { InvitationsPage } from '@features/invitations/pages/InvitationsPage';
import { CharacterGeneratorPage } from '@features/generators/pages/CharacterGeneratorPage';
import { SolarSystemGeneratorPage } from '@features/generators/pages/SolarSystemGeneratorPage';
import { VehicleGeneratorPage } from '@features/generators/pages/VehicleGeneratorPage';
import { NpcGeneratorPage } from '@features/generators/pages/NpcGeneratorPage';
import { EnemyGeneratorPage } from '@features/generators/pages/EnemyGeneratorPage';
import { MissionGeneratorPage } from '@features/generators/pages/MissionGeneratorPage';
import { EncounterGeneratorPage } from '@features/generators/pages/EncounterGeneratorPage';
import { GameSystemsPage } from '@features/generators/pages/GameSystemsPage';
import { TemplatesPage } from '@features/generators/pages/TemplatesPage';
import { AdminUsersPage } from '@features/admin/pages/AdminUsersPage';
import { AdminCampaignsPage } from '@features/admin/pages/AdminCampaignsPage';
import { AdminSystemPage } from '@features/admin/pages/AdminSystemPage';

export type UserRole = 'ADMIN' | 'MASTER' | 'PLAYER';

export interface RouteConfig {
  path: string;
  component: ComponentType<any>;
  requiresAuth?: boolean;
  requiredRoles?: UserRole[];
  publicOnly?: boolean;
  props?: Record<string, unknown>;
}

/**
 * All route configurations
 */
export const routeConfig: RouteConfig[] = [
  // Public routes
  { path: '/', component: Home, publicOnly: true },
  { path: '/login', component: LoginPage, publicOnly: true },
  { path: '/signup', component: SignupPage, publicOnly: true },
  
  // Special routes
  { path: '/access-denied', component: AccessDenied },
  { path: '/error', component: ErrorScreen },
  
  // Authenticated routes
  { path: '/gallery', component: GalleryPage, requiresAuth: true },
  { path: '/campaigns', component: CampaignListPage, requiresAuth: true },
  { path: '/campaigns/new', component: CampaignGeneratorPage, requiresAuth: true },
  { path: '/campaigns/:campaignId', component: CampaignSettingsPage, requiresAuth: true },
  { path: '/invitations', component: InvitationsPage, requiresAuth: true },
  { path: '/campaigns/:campaignId/invitations', component: InvitationsPage, requiresAuth: true },
  
  // Master routes (MASTER or ADMIN)
  { path: '/hub', component: MasterHubPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/game-systems', component: GameSystemsPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/templates', component: TemplatesPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  
  // Generator routes (MASTER or ADMIN)
  { path: '/gallery/char-gen', component: CharacterGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/gallery/solar-gen', component: SolarSystemGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/gallery/vehi-gen', component: VehicleGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/gallery/npc-gen', component: NpcGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/gallery/enemy-gen', component: EnemyGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/gallery/mission-gen', component: MissionGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  { path: '/gallery/encounter-gen', component: EncounterGeneratorPage, requiresAuth: true, requiredRoles: ['MASTER', 'ADMIN'] },
  
  // Admin routes (ADMIN only)
  { path: '/admin/users', component: AdminUsersPage, requiresAuth: true, requiredRoles: ['ADMIN'] },
  { path: '/admin/campaigns', component: AdminCampaignsPage, requiresAuth: true, requiredRoles: ['ADMIN'] },
  { path: '/admin/system', component: AdminSystemPage, requiresAuth: true, requiredRoles: ['ADMIN'] },
];

/**
 * Get default route based on user role
 */
export const getDefaultRoute = (isAuthenticated: boolean, userRole?: UserRole): string => {
  if (!isAuthenticated) return '/';
  if (userRole === 'ADMIN') return '/admin/users';
  if (userRole === 'MASTER') return '/hub';
  return '/gallery';
};
