/**
 * AdminGuard Component
 * DRY: Replaces the identical role-guard block that was copy-pasted verbatim
 * in AdminUsersPage, AdminCampaignsPage, and AdminSystemPage.
 *
 * Renders a locked-access screen when the current user is not an Admin,
 * otherwise renders children.
 */

import React from 'react';
import { useNavigate } from 'react-router-dom';
import { AdminLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useAuth } from '@core/context';

interface AdminGuardProps {
  /** Content to render when the current user is an Admin */
  children: React.ReactNode;
}

/**
 * Wraps any admin page to enforce ADMIN role access.
 * Renders a "Acceso restringido" screen (inside AdminLayout) when the
 * user is not an Admin, so pages never need to replicate this check.
 */
export const AdminGuard: React.FC<AdminGuardProps> = ({ children }) => {
  const navigate = useNavigate();
  const { user: currentUser } = useAuth();

  if (currentUser?.role !== 'ADMIN') {
    return (
      <AdminLayout>
        <div className="flex flex-col items-center justify-center h-full text-danger/60">
          <span className="material-icons text-6xl mb-4">lock</span>
          <p className="text-sm uppercase tracking-widest">Acceso restringido a Administradores</p>
          <Button onClick={() => navigate(-1)} className="mt-4">VOLVER</Button>
        </div>
      </AdminLayout>
    );
  }

  return <>{children}</>;
};
