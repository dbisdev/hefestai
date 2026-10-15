/**
 * GalleryEntityCard Component
 * Displays a single entity in the gallery grid with full-image style,
 * grayscale hover effect, owner badge and scanline overlay.
 *
 * This is the gallery-specific card variant used by GalleryPage.
 * It differs from the compact list-style EntityCard used elsewhere.
 *
 * Accessibility:
 * - role="gridcell" for use inside role="grid"
 * - Keyboard accessible: Enter and Space trigger selection
 * - aria-selected reflects selection state
 */

import React from 'react';
import type { LoreEntity } from '@core/types';

const FALLBACK_IMAGE =
  'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop';

export interface GalleryEntityCardProps {
  /** The entity to display */
  entity: LoreEntity;
  /** Whether this card is currently selected */
  selected: boolean;
  /** Current authenticated user id (to show OWNER badge) */
  currentUserId?: string;
  /** Called when the card is activated (click or keyboard) */
  onClick: () => void;
  /** CSS animation delay in milliseconds for stagger effect */
  animationDelay: number;
}

/**
 * Gallery entity card with full-image style and scanline hover overlay.
 */
export const GalleryEntityCard: React.FC<GalleryEntityCardProps> = ({
  entity,
  selected,
  currentUserId,
  onClick,
  animationDelay,
}) => {
  const imageUrl = entity.imageUrl || FALLBACK_IMAGE;
  const isOwner = currentUserId === entity.ownerId;

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onClick();
    }
  };

  const visibilityLabel =
    entity.visibility === 0 ? 'BORRADOR' :
    entity.visibility === 1 ? 'PRIVADO' :
    entity.visibility === 2 ? 'CAMPAÑA' : 'PUBLICO';

  return (
    <div
      role="gridcell"
      tabIndex={0}
      aria-label={`${entity.name}, ${entity.entityType.replace('_', ' ')}`}
      aria-selected={selected}
      onClick={onClick}
      onKeyDown={handleKeyDown}
      className={`group relative bg-surface-dark border transition-all hover:shadow-[0_0_20px_rgba(37,244,106,0.2)] hover:scale-[1.02] cursor-pointer clip-tech-br overflow-hidden focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-surface-dark ${
        selected ? 'border-primary' : 'border-primary/40'
      }`}
      style={{ animationDelay: `${animationDelay}ms` }}
    >
      {/* Image area */}
      <div className="h-44 overflow-hidden border-b border-primary/50 relative">
        <img
          src={imageUrl}
          alt={entity.name}
          className="w-full h-full object-cover object-[center_25%] grayscale brightness-50 group-hover:grayscale-0 group-hover:brightness-100 transition-all duration-500"
        />
        <div className="absolute inset-0 bg-primary/5 mix-blend-overlay group-hover:bg-transparent transition-colors" />

        {/* Owner badge */}
        {isOwner && (
          <div className="absolute top-2 left-2">
            <span className="bg-primary/90 text-black text-[8px] font-bold px-2 py-0.5 uppercase tracking-wider">
              OWNER
            </span>
          </div>
        )}

        {/* Hover pulse indicator */}
        <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity">
          <div className="flex gap-1">
            <div className="w-1 h-3 bg-primary/40 animate-pulse" />
            <div className="w-1 h-3 bg-primary/40 animate-pulse" style={{ animationDelay: '0.2s' }} />
          </div>
        </div>
      </div>

      {/* Text area */}
      <div className="p-4 bg-gradient-to-b from-surface-dark to-black/80">
        <div className="flex justify-between items-start mb-2">
          <h3 className="text-xl text-primary font-display uppercase tracking-widest group-hover:text-glow truncate max-w-[80%]">
            {entity.name}
          </h3>
          <span className="text-[8px] text-primary/40 border border-primary/20 px-1">
            ID_{entity.id.slice(0, 4)}
          </span>
        </div>
        <div className="flex justify-between items-center text-[10px] text-primary/60 font-mono tracking-tighter uppercase">
          <span className="flex items-center gap-1">
            <span className="w-1 h-1 bg-primary rounded-full" />
            {entity.entityType.replace('_', ' ')}
          </span>
          <span className="opacity-50 italic">{visibilityLabel}</span>
        </div>
      </div>

      {/* Scanline overlay on hover */}
      <div className="absolute inset-0 pointer-events-none opacity-0 group-hover:opacity-10 bg-[linear-gradient(rgba(18,16,16,0)_50%,rgba(0,0,0,0.25)_50%),linear-gradient(90deg,rgba(255,0,0,0.06),rgba(0,255,0,0.02),rgba(0,0,255,0.06))] bg-[length:100%_4px,3px_100%]" />
    </div>
  );
};
