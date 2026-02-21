/**
 * Game System Card Component
 * Single Responsibility: Render individual game system card
 * Restored original design with edit button and visibility badge
 */

import React from 'react';
import type { GameSystem } from '@core/types';

interface GameSystemCardProps {
  system: GameSystem;
  isSelected: boolean;
  isOwned: boolean;
  onSelect: () => void;
  onEdit: () => void;
}

export const GameSystemCard: React.FC<GameSystemCardProps> = ({
  system,
  isSelected,
  isOwned,
  onSelect,
  onEdit,
}) => {
  const handleClick = () => onSelect();
  const handleEditClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onEdit();
  };

  return (
    <div
      onClick={handleClick}
      className={`border p-4 cursor-pointer transition-all ${
        isSelected
          ? 'border-cyan-500 bg-cyan-500/10'
          : 'border-primary/20 bg-black/40 hover:border-primary/40'
      }`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-mono text-cyan-400 text-sm">{system.code}</span>
            <span className={`text-xs px-2 py-0.5 border ${
              system.isActive
                ? 'border-green-500/40 text-green-400'
                : 'border-yellow-500/40 text-yellow-400'
            }`}>
              {system.isActive ? 'ACTIVO' : 'INACTIVO'}
            </span>
            <span className={`text-[8px] px-1 py-0.5 border ${
              isOwned
                ? 'border-yellow-500/40 text-yellow-400 bg-yellow-500/10'
                : 'border-cyan-500/40 text-cyan-400 bg-cyan-500/10'
            }`}>
              {isOwned ? 'PRIVADO' : 'PÚBLICO'}
            </span>
          </div>
          <h3 className="text-primary font-bold mt-1">{system.name}</h3>
          {system.publisher && (
            <p className="text-primary/40 text-xs mt-1">
              {system.publisher} {system.version && `v${system.version}`}
            </p>
          )}
        </div>
        
        {isOwned && (
          <button
            type="button"
            onClick={handleEditClick}
            className="p-1 text-primary/40 hover:text-primary transition-colors cursor-pointer"
            aria-label={`Editar ${system.name}`}
            title="Editar sistema"
          >
            <span className="material-icons text-sm">edit</span>
          </button>
        )}
      </div>
      
      {system.description && (
        <p className="text-primary/50 text-xs mt-2 line-clamp-2">
          {system.description}
        </p>
      )}
      
      {system.supportedEntityTypes && system.supportedEntityTypes.length > 0 && (
        <div className="flex flex-wrap gap-1 mt-2">
          {system.supportedEntityTypes.map((type) => (
            <span 
              key={type}
              className="text-xs px-2 py-0.5 bg-primary/10 text-primary/60 border border-primary/20"
            >
              {type}
            </span>
          ))}
        </div>
      )}
    </div>
  );
};

export default GameSystemCard;
