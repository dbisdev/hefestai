/**
 * InviteCodePopup Component
 * Displays the campaign join-code for Masters so they can share it with players.
 *
 * Accessibility:
 * - role="dialog" with aria-modal and aria-label
 * - tabIndex={-1} for programmatic focus
 */

import React, { forwardRef } from 'react';

export interface InviteCodePopupProps {
  /** Active campaign join code to display */
  joinCode: string;
  /** Called when the popup should close */
  onClose: () => void;
}

/**
 * Floating popup that shows the campaign join code.
 * Parent controls visibility via conditional rendering.
 */
export const InviteCodePopup = forwardRef<HTMLDivElement, InviteCodePopupProps>(
  ({ joinCode, onClose }, ref) => (
    <div
      ref={ref}
      role="dialog"
      aria-label="Código de invitación"
      aria-modal="true"
      tabIndex={-1}
      className="absolute top-0 right-0 z-50 bg-surface-dark border border-primary p-4 shadow-2xl animate-glitch-in font-mono"
    >
      <h4 className="text-primary text-[10px] uppercase font-bold mb-2">Enlace de Reclutamiento</h4>

      <div className="bg-black/60 p-2 border border-primary/20 text-xs text-white mb-4">
        <p className="text-[8px] text-primary/60 mb-1 tracking-widest">CÓDIGO_ACTIVO</p>
        <span className="font-bold text-primary text-lg tracking-[0.3em]">{joinCode}</span>
      </div>

      <p className="text-[9px] text-primary/40 leading-tight mb-4 max-w-[150px]">
        Comparte este código con nuevos operativos para asociarlos a tu campaña.
      </p>

      <button
        type="button"
        onClick={onClose}
        className="w-full bg-primary/20 border border-primary py-1 text-[10px] text-primary uppercase font-bold cursor-pointer"
      >
        CERRAR
      </button>
    </div>
  )
);

InviteCodePopup.displayName = 'InviteCodePopup';
