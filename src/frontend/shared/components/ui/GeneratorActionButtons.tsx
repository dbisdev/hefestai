/**
 * GeneratorActionButtons Component
 * DRY: Encapsulates the generate + save button pair that appears identically
 * in every entity generator page.
 */

import React from 'react';
import { Button } from './Button';

export interface GeneratorActionButtonsProps {
  /** Called when the user clicks the generate button */
  onGenerate: () => void;
  /** Called when the user clicks the save button */
  onSave: () => void;
  /** Whether generation is currently running */
  isGenerating: boolean;
  /** Whether save is currently running */
  isSaving: boolean;
  /** Whether editable data exists (enables save button) */
  hasData: boolean;
  /** Whether an active campaign is selected (enables save button) */
  hasCampaign: boolean;
  /** Label for the generate button (default: "GENERAR") */
  generateLabel?: string;
  /** Material icon for the generate button (default: "blur_on") */
  generateIcon?: string;
}

/**
 * Renders the two-column Generate / Save button row used by all generator pages.
 */
export const GeneratorActionButtons: React.FC<GeneratorActionButtonsProps> = ({
  onGenerate,
  onSave,
  isGenerating,
  isSaving,
  hasData,
  hasCampaign,
  generateLabel = 'GENERAR',
  generateIcon = 'blur_on',
}) => (
  <div className="mt-auto pt-6 border-t border-primary/30 grid grid-cols-2 gap-4">
    <Button
      onClick={onGenerate}
      disabled={isGenerating}
      variant="secondary"
      size="lg"
      isLoading={isGenerating}
      icon={generateIcon}
    >
      {generateLabel}
    </Button>
    <Button
      onClick={onSave}
      disabled={!hasData || isSaving || isGenerating || !hasCampaign}
      variant="primary"
      size="lg"
      isLoading={isSaving}
      icon="save"
    >
      GUARDAR
    </Button>
  </div>
);
