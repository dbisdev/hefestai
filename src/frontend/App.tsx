/**
 * Main Application Component
 * Uses AuthProvider and CampaignProvider for global state
 * Uses react-router-dom for navigation with proper browser history support
 */

import React from 'react';
import { AuthProvider, useAuth, CampaignProvider } from '@core/context';
import { AnimatedRoutes } from './AnimatedRoutes';

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

/**
 * Main App Content - requires AuthProvider and CampaignProvider
 */
const AppContent: React.FC = () => {
  const { user, isLoading } = useAuth();

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

  return (
    <div className="h-screen w-screen crt-flicker overflow-hidden bg-background-dark">
      <AnimatedRoutes 
        isAuthenticated={!!user}
        userRole={user?.role}
      />
    </div>
  );
};

export default App;
