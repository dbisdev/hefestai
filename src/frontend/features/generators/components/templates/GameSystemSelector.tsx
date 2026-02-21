/**
 * Game System Selector Component
 * Single Responsibility: Render game system dropdown selector
 */

import React from 'react';
import type { GameSystem } from '@core/types';

interface GameSystemSelectorProps {
  systems: GameSystem[];
  selectedId: string | null;
  onSelect: (systemId: string) => void;
  isLoading?: boolean;
}

export const GameSystemSelector: React.FC<GameSystemSelectorProps> = ({
  systems,
  selectedId,
  onSelect,
  isLoading = false,
}) => {
  if (isLoading) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 bg-primary/5 border border-primary/20 rounded">
        <span className="material-icons text-sm text-primary animate-pulse">sync</span>
        <span className="text-xs text-primary/50 uppercase">Cargando...</span>
      </div>
    );
  }

  if (systems.length === 0) {
    return (
      <div className="px-3 py-2 text-xs text-primary/50 uppercase bg-primary/5 border border-primary/20 rounded">
        Sin sistemas disponibles
      </div>
    );
  }

  return (
    <select
      value={selectedId || ''}
      onChange={(e) => onSelect(e.target.value)}
      className="bg-black/40 border border-primary/30 rounded px-3 py-2 text-sm text-primary
        focus:outline-none focus:border-primary transition-colors min-w-[200px]"
      aria-label="Seleccionar sistema de juego"
    >
      {systems.map((system) => (
        <option key={system.id} value={system.id}>
          {system.name} ({system.code})
        </option>
      ))}
    </select>
  );
};
