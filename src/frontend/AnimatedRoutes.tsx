/**
 * Animated Routes Component
 * Wraps Routes with transition effects for smooth page changes
 */

import { useLocation, Routes, Route, Navigate } from 'react-router-dom';
import { useEffect, useState } from 'react';

// Import all page components directly
import Home from '@features/auth/pages/Home';
import LoginPage from '@features/auth/pages/LoginPage';
import SignupPage from '@features/auth/pages/SignupPage';
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
import { AccessDenied, ErrorScreen } from '@shared/components/feedback';

interface AnimatedRoutesProps {
  isAuthenticated: boolean;
  userRole?: string;
}

export const AnimatedRoutes: React.FC<AnimatedRoutesProps> = ({ isAuthenticated, userRole }) => {
  const location = useLocation();
  const [transitionStage, setTransitionStage] = useState<'idle' | 'out' | 'in'>('idle');
  const [displayLocation, setDisplayLocation] = useState(location);

  useEffect(() => {
    if (location.pathname !== displayLocation.pathname) {
      setTransitionStage('out');
      const timer = setTimeout(() => {
        setDisplayLocation(location);
        setTransitionStage('in');
        setTimeout(() => setTransitionStage('idle'), 500);
      }, 400);
      return () => clearTimeout(timer);
    }
  }, [location, displayLocation.pathname]);

  // Helper to check if user has required role
  const hasRole = (requiredRoles?: string[]) => {
    if (!requiredRoles || requiredRoles.length === 0) return true;
    if (!userRole) return false;
    return requiredRoles.includes(userRole);
  };

  // Helper to check if route requires auth
  const requiresAuth = (routeAuth?: boolean) => {
    return routeAuth === true;
  };

  // Redirect to appropriate page based on auth state
  const getDefaultRoute = () => {
    if (!isAuthenticated) return '/login';
    if (userRole === 'ADMIN') return '/admin/users';
    if (userRole === 'MASTER') return '/hub';
    return '/gallery';
  };

  return (
    <div className={`h-full w-full transition-all duration-300 ${
      transitionStage === 'out' ? 'section-transition-out' : 
      transitionStage === 'in' ? 'section-transition-in' : ''
    }`}>
      <Routes location={displayLocation}>
          {/* Public routes */}
          <Route path="/" element={
            isAuthenticated ? <Navigate to={getDefaultRoute()} replace /> : <Home />
          } />
          <Route path="/login" element={
            isAuthenticated ? <Navigate to={getDefaultRoute()} replace /> : <LoginPage />
          } />
          <Route path="/signup" element={
            isAuthenticated ? <Navigate to={getDefaultRoute()} replace /> : <SignupPage />
          } />
          <Route path="/access-denied" element={<AccessDenied />} />
          <Route path="/error" element={<ErrorScreen />} />
          
          {/* Gallery - authenticated users */}
          <Route path="/gallery" element={
            requiresAuth(true) && !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : <GalleryPage />
          } />
          
          {/* Master Hub - MASTER or ADMIN only */}
          <Route path="/hub" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <MasterHubPage />
          } />
          
          {/* Campaign routes */}
          <Route path="/campaigns" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : <CampaignListPage />
          } />
          <Route path="/campaigns/new" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : <CampaignGeneratorPage />
          } />
          <Route path="/campaigns/:campaignId" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : <CampaignSettingsPage />
          } />
          {/* Invitations - uses campaign ID from context or param */}
          <Route path="/invitations" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : <InvitationsPage />
          } />
          <Route path="/campaigns/:campaignId/invitations" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : <InvitationsPage />
          } />
          
          {/* Generator routes - MASTER or ADMIN only */}
          <Route path="/gallery/char-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <CharacterGeneratorPage />
          } />
          <Route path="/gallery/solar-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <SolarSystemGeneratorPage />
          } />
          <Route path="/gallery/vehi-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <VehicleGeneratorPage />
          } />
          <Route path="/gallery/npc-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <NpcGeneratorPage />
          } />
          <Route path="/gallery/enemy-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <EnemyGeneratorPage />
          } />
          <Route path="/gallery/mission-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <MissionGeneratorPage />
          } />
          <Route path="/gallery/encounter-gen" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <EncounterGeneratorPage />
          } />
          
          {/* Game Systems - MASTER or ADMIN only */}
          <Route path="/game-systems" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['MASTER', 'ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <GameSystemsPage />
          } />
          
          {/* Templates - ADMIN only */}
          <Route path="/templates" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <TemplatesPage />
          } />
          
          {/* Admin routes - ADMIN only */}
          <Route path="/admin/users" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <AdminUsersPage />
          } />
          <Route path="/admin/campaigns" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <AdminCampaignsPage />
          } />
          <Route path="/admin/system" element={
            !isAuthenticated 
              ? <Navigate to="/login" state={{ from: location }} replace />
              : !hasRole(['ADMIN'])
                ? <Navigate to="/access-denied" replace />
                : <AdminSystemPage />
          } />
          
          {/* Catch all */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    </div>
  );
};

export default AnimatedRoutes;
