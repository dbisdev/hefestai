/**
 * AdminEmptyState Component
 * DRY: Replaces the identical empty-state placeholder that appeared in
 * AdminUsersPage and AdminCampaignsPage right-column "no selection" branches.
 *
 * Renders a centred icon + two-line message inside a bordered container.
 */

import React from 'react';

interface AdminEmptyStateProps {
  /** Material icon name (e.g. "person_search", "public") */
  icon: string;
  /** Primary message line (e.g. "Selecciona un usuario") */
  message: string;
  /** Secondary message line (e.g. "para ver sus detalles") */
  subMessage?: string;
}

/**
 * Centred empty-state placeholder inside the right-column detail panel.
 * Fills its flex parent with `flex-1`.
 */
export const AdminEmptyState: React.FC<AdminEmptyStateProps> = ({
  icon,
  message,
  subMessage = 'para ver sus detalles',
}) => (
  <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col items-center justify-center text-primary/40">
    <span className="material-icons text-6xl mb-4">{icon}</span>
    <p className="text-sm uppercase">{message}</p>
    {subMessage && <p className="text-xs mt-1">{subMessage}</p>}
  </div>
);
