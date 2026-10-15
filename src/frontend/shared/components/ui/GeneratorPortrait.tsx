/**
 * GeneratorPortrait Component
 * DRY: Encapsulates the entity portrait panel that appears identically
 * in CharacterGeneratorPage, NpcGeneratorPage, EnemyGeneratorPage, and VehicleGeneratorPage.
 *
 * Renders:
 *  - A responsive image (grayscale, with blur/scale transition while generating)
 *  - A scan-line loading overlay while isGenerating is true
 *  - An editable name field that slides up from the bottom once data is available
 *  - A "Sin Datos" placeholder when idle with no data
 */

import React from 'react';
import { EditableField } from './EditableField';
import type { EditableFieldVariant } from './EditableField';

export interface GeneratorPortraitProps {
  /** Image source URL (placeholder or generated) */
  src: string;
  /** Alt text for the image */
  alt: string;
  /** Current entity name (empty string when no data yet) */
  name: string;
  /** Whether generation is currently running */
  isGenerating: boolean;
  /** Whether editable data exists (controls name overlay visibility) */
  hasData: boolean;
  /** Called when the user edits the name field */
  onNameChange: (value: string | number) => void;
  /** Loading message shown during generation (default: "GENERANDO...") */
  generatingLabel?: string;
  /** Color variant for the name field and border accents (default: "primary") */
  variant?: EditableFieldVariant;
  /** Additional CSS classes on the root container */
  className?: string;
}

/**
 * Reusable portrait panel for entity generator pages.
 * Handles the image display, generation overlay, and editable name overlay.
 */
export const GeneratorPortrait: React.FC<GeneratorPortraitProps> = ({
  src,
  alt,
  name,
  isGenerating,
  hasData,
  onNameChange,
  generatingLabel = 'GENERANDO...',
  variant = 'primary',
  className = '',
}) => {
  const borderColor =
    variant === 'danger' ? 'border-danger/30' : 'border-primary/30';
  const overlayBarColor =
    variant === 'danger' ? 'bg-danger/20' : 'bg-primary/20';
  const overlayBarFillColor =
    variant === 'danger' ? 'bg-danger' : 'bg-primary';
  const overlayTextColor =
    variant === 'danger' ? 'text-danger' : 'text-primary';
  const nameBorderColor =
    variant === 'danger' ? 'border-danger/40' : 'border-primary/40';
  const placeholderColor =
    variant === 'danger' ? 'text-danger/20' : 'text-primary/20';

  return (
    <div
      className={`relative w-full h-64 md:h-[500px] ${borderColor} border bg-black p-1 flex flex-col clip-tech-br group ${className}`}
    >
      {/* Image layer */}
      <div className="relative flex-1 bg-black overflow-hidden flex items-center justify-center">
        <img
          className={`w-full h-full object-cover object-[center_25%] transition-all duration-1000 ${
            isGenerating ? 'opacity-10 scale-110 blur-sm' : 'opacity-80 scale-100'
          } grayscale brightness-90`}
          src={src}
          alt={alt}
        />

        {/* Generation overlay */}
        {isGenerating && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 z-20">
            <div className={`w-1/2 h-1 ${overlayBarColor} relative overflow-hidden mb-2`}>
              <div className={`absolute inset-0 ${overlayBarFillColor} animate-[scan_2s_linear_infinite]`} />
            </div>
            <span className={`${overlayTextColor} text-[10px] animate-pulse`}>
              {generatingLabel}
            </span>
          </div>
        )}

        <div className="absolute inset-0 pointer-events-none border border-primary/5 opacity-30" />
      </div>

      {/* Name overlay — slides up once data is available */}
      <div
        className={`absolute bottom-0 left-0 right-0 z-10 bg-black/80 p-3 border-t ${nameBorderColor} backdrop-blur-sm transition-transform duration-500 ${
          hasData ? 'translate-y-0' : 'translate-y-full'
        }`}
      >
        <EditableField
          value={name}
          label="Nombre"
          variant={variant}
          onChange={onNameChange}
          disabled={!hasData}
          className="font-bold"
        />
      </div>

      {/* Idle placeholder */}
      {!hasData && !isGenerating && (
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
          <span className={`${placeholderColor} text-[10px] tracking-[0.5em] uppercase font-bold`}>
            Sin Datos
          </span>
        </div>
      )}
    </div>
  );
};
