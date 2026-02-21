/**
 * Game System List Component
 * Single Responsibility: Render list of game systems
 */

import React from 'react';
import { EmptyState } from '@shared/components/ui';
import type { GameSystem } from '@core/types';
import { GameSystemCard } from './GameSystemCard';

interface GameSystemListProps {
  systems: GameSystem[];
  selectedId: string | null;
  currentUserId?: string;
  userRole?: string;
  onSelect: (system: GameSystem) => void;
  onEdit: (system: GameSystem) => void;
  isLoading?: boolean;
}

export const GameSystemList: React.FC<GameSystemListProps> = ({
  systems,
  selectedId,
  currentUserId,
  userRole,
  onSelect,
  onEdit,
  isLoading = false,
}) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <span className="material-icons text-primary text-4xl animate-pulse">sync</span>
      </div>
    );
  }

  if (systems.length === 0) {
    return (
      <EmptyState
        icon="sports_esports"
        title="No hay sistemas"
        description="Crea un nuevo sistema de juego para empezar"
      />
    );
  }

  return (
    <div
      className="grid gap-3"
      role="listbox"
      aria-label="Lista de sistemas de juego"
    >
      {systems.map((system) => {
        const isOwned = userRole === 'ADMIN' || system.ownerId === currentUserId;

        return (
          <GameSystemCard
            key={system.id}
            system={system}
            isSelected={selectedId === system.id}
            isOwned={isOwned}
            onSelect={() => onSelect(system)}
            onEdit={() => onEdit(system)}
          />
        );
      })}
    </div>
  );
};
