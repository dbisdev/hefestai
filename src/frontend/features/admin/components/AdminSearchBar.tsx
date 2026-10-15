/**
 * AdminSearchBar Component
 * DRY: Replaces the structurally identical search-input + inactive-toggle bar
 * that appeared in AdminUsersPage and AdminCampaignsPage.
 *
 * Renders a search input with a leading icon and an "include inactive" checkbox.
 */

import React from 'react';

interface AdminSearchBarProps {
  /** Current search term value */
  searchTerm: string;
  /** Called when the search input changes */
  onSearch: (value: string) => void;
  /** Whether to include inactive records */
  includeInactive: boolean;
  /** Called when the inactive checkbox toggles */
  onToggleInactive: (checked: boolean) => void;
  /** Placeholder text for the search input */
  placeholder?: string;
  /** Label for the inactive checkbox (e.g. "Inactivos", "Inactivas") */
  inactiveLabel?: string;
}

/**
 * Search bar with an "include inactive" toggle checkbox.
 * Layout: [search input | inactive checkbox]
 */
export const AdminSearchBar: React.FC<AdminSearchBarProps> = ({
  searchTerm,
  onSearch,
  includeInactive,
  onToggleInactive,
  placeholder = 'Buscar...',
  inactiveLabel = 'Inactivos',
}) => (
  <div className="flex gap-2">
    <div className="relative flex-1">
      <span className="absolute left-3 top-1/2 -translate-y-1/2 material-icons text-primary/40 text-sm">
        search
      </span>
      <input
        type="text"
        value={searchTerm}
        onChange={(e) => onSearch(e.target.value)}
        placeholder={placeholder}
        className="w-full bg-black/40 border border-primary/30 pl-10 pr-3 py-2 text-sm text-primary placeholder-primary/30 focus:border-primary focus:outline-none"
      />
    </div>
    <label className="flex items-center gap-2 text-xs text-primary/60 whitespace-nowrap cursor-pointer">
      <input
        type="checkbox"
        checked={includeInactive}
        onChange={(e) => onToggleInactive(e.target.checked)}
        className="accent-primary"
      />
      {inactiveLabel}
    </label>
  </div>
);
