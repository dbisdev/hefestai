/**
 * Template List Component
 * Single Responsibility: Render list of templates
 */

import React from 'react';
import { EmptyState } from '@shared/components/ui';
import type { EntityTemplateSummary } from '@core/types';
import { TemplateCard } from './TemplateCard';

interface TemplateListProps {
  templates: EntityTemplateSummary[];
  selectedId: string | null;
  onSelect: (templateId: string) => void;
  onConfirm?: (templateId: string) => void;
  confirmingId?: string | null;
  canConfirm?: boolean;
  isLoading?: boolean;
  isNewlyExtracted?: (templateId: string) => boolean;
}

export const TemplateList: React.FC<TemplateListProps> = ({
  templates,
  selectedId,
  onSelect,
  onConfirm,
  confirmingId,
  canConfirm = false,
  isLoading = false,
  isNewlyExtracted,
}) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <span className="material-icons text-primary text-4xl animate-pulse">sync</span>
      </div>
    );
  }

  if (templates.length === 0) {
    return (
      <EmptyState
        icon="description"
        title="No hay plantillas"
        description="Extrae plantillas del manual del sistema de juego"
      />
    );
  }

  return (
    <div
      className="space-y-2"
      role="listbox"
      aria-label="Lista de plantillas"
    >
      {templates.map((template) => (
        <TemplateCard
          key={template.id}
          template={template}
          isSelected={selectedId === template.id}
          onSelect={() => onSelect(template.id)}
          onConfirm={onConfirm ? () => onConfirm(template.id) : undefined}
          isConfirming={confirmingId === template.id}
          canConfirm={canConfirm}
          isNewlyExtracted={isNewlyExtracted?.(template.id)}
        />
      ))}
    </div>
  );
};
