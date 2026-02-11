/**
 * Main Application Component
 * Uses AuthProvider and CampaignProvider for global state
 * Implements screen-based navigation with smooth transitions
 */

import React, { useState, useEffect, useCallback } from 'react';
import { AuthProvider, useAuth, CampaignProvider } from '@core/context';
import { LoginPage, SignupPage } from '@features/auth';
import { GalleryPage, MasterHubPage } from '@features/gallery';
import { 
  CharacterGeneratorPage, 
  SolarSystemGeneratorPage, 
  VehicleGeneratorPage,
  NpcGeneratorPage,
  EnemyGeneratorPage,
  MissionGeneratorPage,
  EncounterGeneratorPage,
  CampaignGeneratorPage,
  CampaignSettingsPage,
  GameSystemsPage,
  TemplatesPage
} from '@features/generators';
import { AdminUsersPage, AdminCampaignsPage, AdminSystemPage } from '@features/admin';
import { AccessDenied, ErrorScreen } from '@shared/components/feedback';
import { Screen } from '@core/types';

/**
 * Main App Content - requires AuthProvider and CampaignProvider
 */
const AppContent: React.FC = () => {
  const { user, isLoading, logout } = useAuth();
  const [displayScreen, setDisplayScreen] = useState<Screen>(Screen.LOGIN);
  const [transitionStage, setTransitionStage] = useState<'idle' | 'out' | 'in'>('idle');

  // Redirect to appropriate screen if already authenticated
  // Admin users go to admin panel, Master users go to hub, Players go to gallery
  useEffect(() => {
    if (user && displayScreen === Screen.LOGIN) {
      if (user.role === 'ADMIN') {
        setDisplayScreen(Screen.ADMIN_USERS);
      } else if (user.role === 'MASTER') {
        setDisplayScreen(Screen.MASTER_HUB);
      } else {
        setDisplayScreen(Screen.GALLERY);
      }
    }
  }, [user, displayScreen]);

  // Redirect to login if user logs out while on a protected screen
  useEffect(() => {
    const protectedScreens = [
      Screen.MASTER_HUB,
      Screen.GALLERY, 
      Screen.CHAR_GEN, 
      Screen.SOLAR_GEN, 
      Screen.VEHI_GEN,
      Screen.NPC_GEN,
      Screen.ENEMY_GEN,
      Screen.MISSION_GEN,
      Screen.ENCOUNTER_GEN,
      Screen.CAMPAIGN_GEN,
      Screen.CAMPAIGN_SETTINGS,
      Screen.GAME_SYSTEMS,
      Screen.TEMPLATES,
      Screen.ADMIN_USERS,
      Screen.ADMIN_CAMPAIGNS,
      Screen.ADMIN_SYSTEM
    ];
    if (!user && !isLoading && protectedScreens.includes(displayScreen)) {
      setDisplayScreen(Screen.LOGIN);
      setTransitionStage('idle');
    }
  }, [user, isLoading, displayScreen]);

  const navigate = useCallback((newScreen: Screen) => {
    if (newScreen === displayScreen) return;
    setTransitionStage('out');
    setTimeout(() => {
      setDisplayScreen(newScreen);
      setTransitionStage('in');
      setTimeout(() => setTransitionStage('idle'), 500);
    }, 400);
  }, [displayScreen]);

  const handleLogout = useCallback(async () => {
    // Navigate away first to prevent GalleryPage from making API calls
    // while logout is clearing the auth state
    setDisplayScreen(Screen.LOGIN);
    setTransitionStage('idle');
    await logout();
  }, [logout]);

  // Handle successful login - navigation is handled by useEffect based on user role
  // This is intentionally a no-op; the useEffect watching 'user' handles redirection
  const handleLoginSuccess = useCallback(() => {
    // Navigation handled by useEffect when user state updates
    // Admin users → ADMIN_USERS, others → GALLERY
  }, []);

  // Handle successful signup - navigation is handled by useEffect based on user role
  const handleSignupSuccess = useCallback(() => {
    // Navigation handled by useEffect when user state updates
  }, []);

  const handleNavigate = useCallback((screen: Screen) => {
    const isMaster = user?.role === 'MASTER';
    
    // List of generator screens that require Master role
    // Note: CAMPAIGN_GEN is accessible to all authenticated users
    const masterOnlyScreens = [
      Screen.CHAR_GEN, 
      Screen.SOLAR_GEN, 
      Screen.VEHI_GEN,
      Screen.NPC_GEN,
      Screen.ENEMY_GEN,
      Screen.MISSION_GEN,
      Screen.ENCOUNTER_GEN
    ];
    
    // Check if player is trying to access master-only screens
    if (!isMaster && masterOnlyScreens.includes(screen)) {
      navigate(Screen.ACCESS_DENIED);
    } else {
      navigate(screen);
    }
  }, [user, navigate]);

  // Show loading state while checking auth
  if (isLoading && !user) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background-dark">
        <div className="text-primary text-xs uppercase tracking-widest animate-pulse">
          INITIALIZING_SYSTEM...
        </div>
      </div>
    );
  }

  const isMaster = user?.role === 'MASTER';

  const renderScreen = () => {
    switch (displayScreen) {
      case Screen.LOGIN:
        return (
          <LoginPage 
            onLoginSuccess={handleLoginSuccess} 
            onGoSignup={() => navigate(Screen.SIGNUP)} 
          />
        );
      
      case Screen.SIGNUP:
        return (
          <SignupPage 
            onSignupSuccess={handleSignupSuccess} 
            onBack={() => navigate(Screen.LOGIN)} 
          />
        );
      
      // Master hub - landing page for Master users
      case Screen.MASTER_HUB:
        return (
          <MasterHubPage 
            onNavigate={handleNavigate}
            onLogout={handleLogout}
          />
        );
      
      case Screen.GALLERY:
        return (
          <GalleryPage 
            user={user}
            onNavigate={handleNavigate} 
            onLogout={handleLogout} 
          />
        );
      
      case Screen.CHAR_GEN:
        return isMaster 
          ? <CharacterGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      case Screen.SOLAR_GEN:
        return isMaster 
          ? <SolarSystemGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      case Screen.VEHI_GEN:
        return isMaster 
          ? <VehicleGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      case Screen.NPC_GEN:
        return isMaster 
          ? <NpcGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      case Screen.ENEMY_GEN:
        return isMaster 
          ? <EnemyGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      case Screen.MISSION_GEN:
        return isMaster 
          ? <MissionGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      case Screen.ENCOUNTER_GEN:
        return isMaster 
          ? <EncounterGeneratorPage onBack={() => navigate(Screen.GALLERY)} /> 
          : <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      // Campaign creation is available to all authenticated users
      case Screen.CAMPAIGN_GEN:
        return <CampaignGeneratorPage onBack={() => navigate(Screen.GALLERY)} />;
      
      // Campaign settings (Master only - access control in component)
      case Screen.CAMPAIGN_SETTINGS:
        return <CampaignSettingsPage onBack={() => navigate(Screen.GALLERY)} />;
      
      // Game systems management (Master or Admin only - access control in component)
      case Screen.GAME_SYSTEMS:
        return (
          <GameSystemsPage 
            onNavigate={handleNavigate} 
            onBack={() => navigate(Screen.GALLERY)}
            onLogout={handleLogout}
          />
        );
      
      // Templates management (Admin only - access control in component)
      case Screen.TEMPLATES:
        return (
          <TemplatesPage 
            onNavigate={handleNavigate} 
            onBack={() => navigate(Screen.GALLERY)}
            onLogout={handleLogout}
          />
        );
      
      // Admin users management (Admin only - access control in component)
      case Screen.ADMIN_USERS:
        return (
          <AdminUsersPage 
            onNavigate={handleNavigate} 
            onBack={() => navigate(Screen.GALLERY)}
            onLogout={handleLogout}
          />
        );
      
      // Admin campaigns management (Admin only - access control in component)
      case Screen.ADMIN_CAMPAIGNS:
        return (
          <AdminCampaignsPage 
            onNavigate={handleNavigate} 
            onBack={() => navigate(Screen.GALLERY)}
            onLogout={handleLogout}
          />
        );
      
      // Admin system operations (Admin only - access control in component)
      case Screen.ADMIN_SYSTEM:
        return (
          <AdminSystemPage 
            onNavigate={handleNavigate} 
            onBack={() => navigate(Screen.GALLERY)}
            onLogout={handleLogout}
          />
        );
      
      case Screen.ERROR:
        return <ErrorScreen onReboot={() => navigate(Screen.LOGIN)} />;
      
      case Screen.ACCESS_DENIED:
        return <AccessDenied onBack={() => navigate(Screen.GALLERY)} />;
      
      default:
        return (
          <LoginPage 
            onLoginSuccess={handleLoginSuccess} 
            onGoSignup={() => navigate(Screen.SIGNUP)} 
          />
        );
    }
  };

  return (
    <div className="h-screen w-screen crt-flicker overflow-hidden bg-background-dark">
      <div className={`h-full w-full transition-all duration-300 ${
        transitionStage === 'out' ? 'section-transition-out' : 
        transitionStage === 'in' ? 'section-transition-in' : ''
      }`}>
        {renderScreen()}
      </div>
    </div>
  );
};

/**
 * Root App Component with AuthProvider and CampaignProvider
 */
const App: React.FC = () => {
  return (
    <AuthProvider>
      <CampaignProvider>
        <AppContent />
      </CampaignProvider>
    </AuthProvider>
  );
};

export default App;
