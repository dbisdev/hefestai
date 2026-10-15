/**
 * GallerySidebar Component
 * Left navigation sidebar for the Gallery page.
 *
 * Responsibilities:
 * - Category navigation (non-admin users)
 * - Labs / experimental categories section
 * - Admin panel shortcuts (admin users only)
 * - Status footer showing transition state and active campaign id
 *
 * Note: The "Generators" section is currently commented-out in the original
 * implementation and is kept absent here intentionally.
 *
 * Accessibility:
 * - Category nav uses role="tablist"
 * - CategoryButton handles individual tab roles
 */

import React, { RefObject } from 'react';
import { useNavigate } from 'react-router-dom';
import { CategoryButton } from './CategoryButton';
import {
  ENTITY_CATEGORIES,
  type CategoryInfo,
} from '@features/gallery/constants/categories';
import type { EntityCategory } from '@core/types';

export interface GallerySidebarProps {
  /** Ref for the category nav element (keyboard navigation) */
  categoryNavRef: RefObject<HTMLElement | null>;
  /** Currently active category */
  activeCategory: EntityCategory;
  /** Whether category transitions are in progress */
  transitionStatus: 'idle' | 'in' | 'out';
  /** Whether a campaign is active */
  activeCampaignId: string | null | undefined;
  /** Labs categories filtered by game system */
  filteredLabsCategories: CategoryInfo[];
  /** Whether a category is available (has confirmed templates or needs none) */
  isCategoryAvailable: (categoryId: EntityCategory) => boolean;
  /** Called when a category button is clicked */
  onCategoryChange: (category: EntityCategory) => void;
  /** Called when a category button receives a keyboard event */
  onCategoryKeyDown: (e: React.KeyboardEvent, index: number) => void;
  /** Current user role — admin users see a different panel */
  userRole?: string;
  /** Active campaign short id for display in footer */
  activeCampaignShortId?: string;
}

/** Admin nav button — consistent appearance across all admin links */
interface AdminNavButtonProps {
  icon: string;
  label: string;
  path: string;
}

const AdminNavButton: React.FC<AdminNavButtonProps> = ({ icon, label, path }) => {
  const navigate = useNavigate();
  return (
    <button
      onClick={() => navigate(path)}
      className="group flex items-center gap-3 p-3 border border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark transition-all"
    >
      <span className="material-icons text-xl text-red-500/60 group-hover:text-red-400">{icon}</span>
      <span className="hidden md:inline text-xs font-bold tracking-widest text-red-500/70 group-hover:text-red-400">
        {label}
      </span>
    </button>
  );
};

/**
 * Gallery sidebar with category selection and admin panel.
 */
export const GallerySidebar: React.FC<GallerySidebarProps> = ({
  categoryNavRef,
  activeCategory,
  transitionStatus,
  activeCampaignId,
  filteredLabsCategories,
  isCategoryAvailable,
  onCategoryChange,
  onCategoryKeyDown,
  userRole,
  activeCampaignShortId,
}) => {
  const isAdmin = userRole === 'ADMIN';

  return (
    <aside className="w-16 md:w-64 flex flex-col gap-4 shrink-0">
      {/* Entity Categories — hidden for admins */}
      {!isAdmin && (
        <nav
          ref={categoryNavRef}
          role="tablist"
          aria-label="Categorías de entidades"
          className="flex flex-col gap-2"
        >
          <div className="p-1 border border-primary/50 text-[10px] text-primary text-center uppercase mb-0 bg-primary/5 font-bold tracking-[0.2em]">
            <span className="hidden md:inline">:: GENERADORES ::</span>
            <span className="md:hidden xs:inline">:: GEN.AI ::</span>
          </div>

          {ENTITY_CATEGORIES.map((cat, index) => (
            <CategoryButton
              key={cat.id}
              category={cat}
              isActive={activeCategory === cat.id}
              isAvailable={isCategoryAvailable(cat.id)}
              isDisabled={transitionStatus !== 'idle' || !activeCampaignId}
              variant="primary"
              onClick={() => onCategoryChange(cat.id)}
              onKeyDown={(e) => onCategoryKeyDown(e, index)}
              index={index}
            />
          ))}

          {/* Labs / experimental section */}
          {filteredLabsCategories.length > 0 && (
            <>
              <div className="p-1 border border-cyan-500/50 text-[10px] text-cyan-500 text-center uppercase mt-2 mb-0 bg-cyan-500/5 font-bold tracking-[0.2em]">
                <span className="hidden md:inline">:: Experimental ::</span>
                <span className="md:hidden xs:inline">:: LABS.AI ::</span>
              </div>
              {filteredLabsCategories.map((cat, index) => (
                <CategoryButton
                  key={cat.id}
                  category={cat}
                  isActive={activeCategory === cat.id}
                  isAvailable={true}
                  isDisabled={transitionStatus !== 'idle' || !activeCampaignId}
                  variant="cyan"
                  onClick={() => onCategoryChange(cat.id)}
                  onKeyDown={(e) => onCategoryKeyDown(e, index)}
                  index={index}
                />
              ))}
            </>
          )}
        </nav>
      )}

      {/* Admin Panel — admin users only */}
      {isAdmin && (
        <div>
          <div className="p-1 border border-red-500/50 text-[10px] text-red-400 text-center uppercase mb-2 bg-red-500/5 font-bold tracking-[0.2em]">
            :: ADMIN_PANEL ::
          </div>
          <div className="flex flex-col gap-2">
            <AdminNavButton icon="admin_panel_settings" label="USUARIOS" path="/admin/users" />
            <AdminNavButton icon="shield" label="CAMPAÑAS" path="/admin/campaigns" />
            <AdminNavButton icon="description" label="PLANTILLAS" path="/templates" />
            <AdminNavButton icon="sports_esports" label="SISTEMAS" path="/game-systems" />
          </div>
        </div>
      )}

      {/* Status footer */}
      <div className="mt-auto hidden md:block p-3 border border-primary/10 bg-black/20 text-[8px] text-primary/40 leading-tight uppercase tracking-widest">
        <p className="mb-1">ESTADO: {transitionStatus === 'idle' ? 'ESTABLE' : 'TRANSFIRIENDO...'}</p>
        <p>CAMPAÑA: {activeCampaignShortId || 'N/A'}</p>
      </div>
    </aside>
  );
};
