/**
 * Hub Page - Router Component
 * Single Responsibility: Route to appropriate hub based on user role
 * ADMIN -> AdminHubPage (red theme)
 * MASTER -> MasterHubPage (green theme)
 */

import React from 'react';
import { useAuth } from '@core/context';
import { MasterHubPage } from './MasterHubPage';
import { AdminHubPage } from '@features/admin/pages/AdminHubPage';

export const HubPage: React.FC = () => {
  const { user } = useAuth();
  
  if (user?.role === 'ADMIN') {
    return <AdminHubPage />;
  }
  
  return <MasterHubPage />;
};
