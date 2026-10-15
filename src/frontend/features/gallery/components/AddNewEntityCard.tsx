/**
 * AddNewEntityCard Component
 * Placeholder card shown at the end of the entity grid for Masters.
 * Triggers entity creation flow on activation.
 *
 * Accessibility:
 * - role="button" for semantic correctness
 * - Keyboard accessible: Enter and Space trigger onClick
 * - aria-label describes the action
 */

import React from 'react';

export interface AddNewEntityCardProps {
  /** Called when the card is activated */
  onClick: () => void;
}

/**
 * Dashed card that acts as the "create new entity" trigger inside the grid.
 */
export const AddNewEntityCard: React.FC<AddNewEntityCardProps> = ({ onClick }) => {
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onClick();
    }
  };

  return (
    <div
      role="button"
      tabIndex={0}
      aria-label="Crear nueva entidad"
      onClick={onClick}
      onKeyDown={handleKeyDown}
      className="group relative bg-surface-dark border border-primary/40 border-dashed hover:border-solid hover:border-primary transition-all cursor-pointer clip-tech-br flex flex-col items-center justify-center min-h-[220px] hover:bg-primary/5 shadow-inner focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-surface-dark"
    >
      <div className="relative">
        <span className="material-icons text-6xl text-primary opacity-30 group-hover:opacity-100 transition-all group-hover:scale-110 group-hover:rotate-90">
          add
        </span>
        <div className="absolute inset-0 border border-primary opacity-0 group-hover:opacity-20 animate-ping rounded-full scale-150" />
      </div>
      <div className="p-3 text-center">
        <h3 className="text-sm text-primary font-bold uppercase tracking-[0.4em] group-hover:text-glow">
          Nueva_Entrada
        </h3>
        <p className="text-[8px] text-primary/40 mt-1 uppercase">Slot de Memoria Libre</p>
      </div>
    </div>
  );
};
