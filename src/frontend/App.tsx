/**
 * Main Application Component
 * Uses AuthProvider for global authentication state
 * Implements screen-based navigation with smooth transitions
 */

import React, { useState, useEffect, useCallback } from 'react';
import { AuthProvider, useAuth } from '@core/context/AuthContext';
import { LoginPage, SignupPage } from '@features/auth';
import { GalleryPage } from '@features/gallery';
import { CharacterGeneratorPage, SolarSystemGeneratorPage, VehicleGeneratorPage } from '@features/generators';
import { AccessDenied, ErrorScreen } from '@shared/components/feedback';
import { Screen } from '@core/types';

/**
 * Main App Content - requires AuthProvider
 */
const AppContent: React.FC = () => {
  const { user, isLoading, logout, login, register } = useAuth();
  const [displayScreen, setDisplayScreen] = useState<Screen>(Screen.LOGIN);
  const [transitionStage, setTransitionStage] = useState<'idle' | 'out' | 'in'>('idle');

  // Redirect to gallery if already authenticated
  useEffect(() => {
    if (user && displayScreen === Screen.LOGIN) {
      setDisplayScreen(Screen.GALLERY);
    }
  }, [user, displayScreen]);

  // Redirect to login if user logs out while on a protected screen
  useEffect(() => {
    const protectedScreens = [Screen.GALLERY, Screen.CHAR_GEN, Screen.SOLAR_GEN, Screen.VEHI_GEN];
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

  const handleLoginSuccess = useCallback(() => {
    navigate(Screen.GALLERY);
  }, [navigate]);

  const handleSignupSuccess = useCallback(() => {
    navigate(Screen.GALLERY);
  }, [navigate]);

  const handleNavigate = useCallback((screen: Screen) => {
    const isMaster = user?.role === 'MASTER';
    
    // Check if player is trying to access generator screens
    if (!isMaster && [Screen.CHAR_GEN, Screen.SOLAR_GEN, Screen.VEHI_GEN].includes(screen)) {
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
 * Root App Component with AuthProvider
 */
const App: React.FC = () => {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
};

export default App;
