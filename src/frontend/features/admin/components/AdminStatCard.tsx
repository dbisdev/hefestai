/**
 * AdminStatCard Component
 * DRY: Replaces the repeated "bg-black/40 border border-primary/20 p-3"
 * stat grid cell that appeared in both AdminUsersPage and AdminCampaignsPage
 * detail panels.
 *
 * Renders a labelled numeric stat in the admin cyberpunk style.
 */

import React from 'react';

interface AdminStatCardProps {
  /** Short uppercase label (e.g. "Miembros") */
  label: string;
  /** Numeric or string value to display prominently */
  value: React.ReactNode;
}

/**
 * Single stat cell: small uppercase label + large bold value.
 * Designed to be placed inside a `grid grid-cols-2 gap-3` container.
 */
export const AdminStatCard: React.FC<AdminStatCardProps> = ({ label, value }) => (
  <div className="bg-black/40 border border-primary/20 p-3">
    <p className="text-[10px] text-primary/40 uppercase">{label}</p>
    <p className="text-2xl text-primary font-bold">{value}</p>
  </div>
);
