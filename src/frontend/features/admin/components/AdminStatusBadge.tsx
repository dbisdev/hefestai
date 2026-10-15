/**
 * AdminStatusBadge Component
 * DRY: Replaces the isActive badge that appeared 4+ times across
 * AdminUsersPage and AdminCampaignsPage with identical Tailwind classes.
 *
 * Renders a small pill badge indicating active/inactive status.
 */

import React from 'react';

interface AdminStatusBadgeProps {
  /** Whether the entity is active */
  isActive: boolean;
  /** Label shown when active (e.g. "ACTIVO", "ACTIVA") */
  activeLabel?: string;
  /** Label shown when inactive (e.g. "INACTIVO", "INACTIVA") */
  inactiveLabel?: string;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Renders a colour-coded active/inactive status badge.
 * Green when active, grey when inactive.
 */
export const AdminStatusBadge: React.FC<AdminStatusBadgeProps> = ({
  isActive,
  activeLabel = 'ACTIVO',
  inactiveLabel = 'INACTIVO',
  className = '',
}) => (
  <span
    className={`text-xs px-2 py-0.5 border ${
      isActive
        ? 'border-green-500/40 text-green-400 bg-green-500/10'
        : 'border-gray-500/40 text-gray-400 bg-gray-500/10'
    } ${className}`}
  >
    {isActive ? activeLabel : inactiveLabel}
  </span>
);

/**
 * Smaller variant used in list rows (font-size [10px]).
 */
export const AdminStatusBadgeSm: React.FC<AdminStatusBadgeProps> = ({
  isActive,
  activeLabel = 'ACTIVO',
  inactiveLabel = 'INACTIVO',
  className = '',
}) => (
  <span
    className={`text-[10px] px-1.5 py-0.5 border ${
      isActive
        ? 'border-green-500/40 text-green-400 bg-green-500/10'
        : 'border-gray-500/40 text-gray-400 bg-gray-500/10'
    } ${className}`}
  >
    {isActive ? activeLabel : inactiveLabel}
  </span>
);
