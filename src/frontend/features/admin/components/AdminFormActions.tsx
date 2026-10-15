/**
 * AdminFormActions Component
 * DRY: Replaces the identical Cancel / Save button pair that appeared at the
 * bottom of the edit forms in AdminUsersPage and AdminCampaignsPage.
 *
 * Renders a two-button row: a secondary "CANCELAR" and a primary "GUARDAR CAMBIOS".
 */

import React from 'react';
import { Button } from '@shared/components/ui';

interface AdminFormActionsProps {
  /** Called when the cancel button is clicked */
  onCancel: () => void;
  /** Called when the save button is clicked */
  onSave: () => void;
  /** Disables the save button and shows a loading label */
  isSaving?: boolean;
  /** Label for the save button in its idle state (default: "GUARDAR CAMBIOS") */
  saveLabel?: string;
  /** Label for the save button while saving (default: "GUARDANDO...") */
  savingLabel?: string;
}

/**
 * Two-button action row for admin edit forms.
 * Both buttons expand to fill equal width via `flex-1`.
 */
export const AdminFormActions: React.FC<AdminFormActionsProps> = ({
  onCancel,
  onSave,
  isSaving = false,
  saveLabel = 'GUARDAR CAMBIOS',
  savingLabel = 'GUARDANDO...',
}) => (
  <div className="mt-6 flex gap-2">
    <Button onClick={onCancel} variant="secondary" className="flex-1">
      CANCELAR
    </Button>
    <Button onClick={onSave} disabled={isSaving} className="flex-1">
      {isSaving ? savingLabel : saveLabel}
    </Button>
  </div>
);
