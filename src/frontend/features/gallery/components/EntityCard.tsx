/**
 * Entity Card Component
 * Single Responsibility: Render individual entity card
 */

import React from 'react';
import type { LoreEntity } from '@core/types';

interface EntityCardProps {
  entity: LoreEntity;
  isSelected: boolean;
  onSelect: (entity: LoreEntity) => void;
  icon?: string;
}

export const EntityCard: React.FC<EntityCardProps> = ({
  entity,
  isSelected,
  onSelect,
  icon = 'category',
}) => {
  const handleClick = () => onSelect(entity);
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onSelect(entity);
    }
  };

  return (
    <div
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      className={`group relative p-3 rounded-lg cursor-pointer transition-all duration-200
        border hover:shadow-[0_0_15px_rgba(37,244,106,0.15)] ${
          isSelected
            ? 'bg-primary/10 border-primary shadow-[0_0_20px_rgba(37,244,106,0.2)]'
            : 'bg-surface-dark/50 border-primary/20 hover:border-primary/40'
        }`}
      role="button"
      tabIndex={0}
      aria-selected={isSelected}
      aria-label={`Seleccionar ${entity.name}`}
    >
      <div className="absolute top-0 left-0 w-2 h-2 border-t border-l border-primary/30 rounded-tl" />
      <div className="absolute top-0 right-0 w-2 h-2 border-t border-r border-primary/30 rounded-tr" />
      <div className="absolute bottom-0 left-0 w-2 h-2 border-b border-l border-primary/30 rounded-bl" />
      <div className="absolute bottom-0 right-0 w-2 h-2 border-b border-r border-primary/30 rounded-br" />

      <div className="flex items-start gap-3">
        {entity.imageUrl ? (
          <img
            src={entity.imageUrl}
            alt={entity.name}
            className="w-12 h-12 rounded object-cover border border-primary/20"
          />
        ) : (
          <div className="w-12 h-12 rounded bg-primary/10 flex items-center justify-center border border-primary/20">
            <span className="material-icons text-primary/50">{icon}</span>
          </div>
        )}

        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-medium text-primary truncate group-hover:text-glow">
            {entity.name}
          </h3>
          {entity.description && (
            <p className="text-[10px] text-primary/50 truncate mt-0.5">
              {entity.description}
            </p>
          )}
        </div>
      </div>

      {isSelected && (
        <div className="absolute inset-0 pointer-events-none border-2 border-primary rounded-lg animate-pulse" />
      )}
    </div>
  );
};
