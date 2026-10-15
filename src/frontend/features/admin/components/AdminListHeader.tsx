/**
 * AdminListHeader Component
 * DRY: Replaces the identical styled list-header-with-refresh-button block
 * that appeared in AdminUsersPage and AdminCampaignsPage.
 *
 * Renders a coloured header bar with a title, icon, and a refresh button.
 */

import React from 'react';

interface AdminListHeaderProps {
  /** Material icon name (e.g. "people", "public") */
  icon: string;
  /** Title text (e.g. "Lista de Usuarios") */
  title: string;
  /** Called when the refresh button is clicked */
  onRefresh: () => void;
  /** Disables the refresh button while loading */
  isLoading?: boolean;
}

/**
 * Styled list-panel header with a leading icon, title, and a circular refresh button.
 */
export const AdminListHeader: React.FC<AdminListHeaderProps> = ({
  icon,
  title,
  onRefresh,
  isLoading = false,
}) => (
  <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
    <span className="flex items-center gap-2">
      <span className="material-icons text-sm">{icon}</span>
      {title}
    </span>
    <button
      onClick={onRefresh}
      disabled={isLoading}
      className="material-icons text-sm text-primary/60 hover:text-primary transition-colors disabled:opacity-50"
      title="Recargar"
    >
      refresh
    </button>
  </div>
);
