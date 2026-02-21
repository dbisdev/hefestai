/**
 * Animated Routes Component
 * Declarative routing with transition effects for smooth page changes
 * Simplified using route configuration
 */

import React, { useEffect, useState } from 'react';
import { useLocation, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import { ProtectedRoute } from '@shared/components/routing';
import { routeConfig, getDefaultRoute } from '@core/config/routes';
import type { UserRole } from '@core/config/routes';

interface AnimatedRoutesProps {
  isAuthenticated: boolean;
  userRole?: UserRole;
}

export const AnimatedRoutes: React.FC<AnimatedRoutesProps> = ({ isAuthenticated, userRole }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [transitionStage, setTransitionStage] = useState<'idle' | 'out' | 'in'>('idle');
  const [displayLocation, setDisplayLocation] = useState(location);

  const handleGoBack = () => navigate('/gallery');

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

  return (
    <div className={`h-full w-full transition-all duration-300 overflow-y-auto sm:overflow-visible ${
      transitionStage === 'out' ? 'section-transition-out' :
      transitionStage === 'in' ? 'section-transition-in' : ''
    }`}>
      <Routes location={displayLocation}>
        {routeConfig.map((route) => (
          <Route
            key={route.path}
            path={route.path}
            element={
              <ProtectedRoute
                route={route}
                isAuthenticated={isAuthenticated}
                userRole={userRole}
                onBack={handleGoBack}
              />
            }
          />
        ))}
        <Route path="/*" element={<Navigate to={getDefaultRoute(isAuthenticated, userRole)} replace />} />
      </Routes>
    </div>
  );
};

export default AnimatedRoutes;
