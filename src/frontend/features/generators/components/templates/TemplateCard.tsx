/**
 * Template Card Component
 * Single Responsibility: Render individual template card
 * Restored original design with status-based styling
 */

import React from 'react';
import type { EntityTemplateSummary } from '@core/types';
import { TemplateStatus, TemplateStatusLabels } from '@core/types';

interface TemplateCardProps {
  template: EntityTemplateSummary;
  isSelected: boolean;
  onSelect: () => void;
  onConfirm?: () => void;
  isConfirming?: boolean;
  canConfirm?: boolean;
  isNewlyExtracted?: boolean;
}

const getStatusColor = (status: TemplateStatus): string => {
  switch (status) {
    case TemplateStatus.Confirmed:
      return 'border-green-500/40 text-green-400 bg-green-500/10';
    case TemplateStatus.PendingReview:
      return 'border-yellow-500/40 text-yellow-400 bg-yellow-500/10';
    case TemplateStatus.Rejected:
      return 'border-red-500/40 text-red-400 bg-red-500/10';
    default:
      return 'border-primary/40 text-primary/60 bg-primary/10';
  }
};

export const TemplateCard: React.FC<TemplateCardProps> = ({
  template,
  isSelected,
  onSelect,
  onConfirm,
  isConfirming = false,
  canConfirm = false,
  isNewlyExtracted = false,
}) => {
  const canConfirmThis = canConfirm && (
    template.status === TemplateStatus.Draft || 
    template.status === TemplateStatus.PendingReview
  );

  const getCardStyle = () => {
    if (isSelected) {
      return 'border-cyan-500 bg-cyan-500/10';
    }
    if (template.status === TemplateStatus.Confirmed) {
      return 'border-green-500/30 bg-green-500/5 hover:border-green-500/50';
    }
    return 'border-yellow-500/30 bg-yellow-500/5 hover:border-yellow-500/50';
  };

  return (
    <div
      onClick={onSelect}
      className={`border p-2 cursor-pointer transition-all ${getCardStyle()}`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-1 flex-wrap">
            <span className="font-mono text-cyan-400 text-[10px] truncate">
              {template.entityTypeName}
            </span>
            <span className={`text-[8px] px-1 py-0.5 border ${getStatusColor(template.status)}`}>
              {TemplateStatusLabels[template.status]}
            </span>
            {isNewlyExtracted && (
              <span className="text-[8px] px-1 py-0.5 border border-orange-500/60 text-orange-400 bg-orange-500/20 animate-pulse">
                NUEVA
              </span>
            )}
          </div>
          <p className="text-xs text-primary mt-1 truncate">{template.displayName}</p>
          <p className="text-[9px] text-primary/40">
            {template.fieldCount} campos
          </p>
        </div>
        
        {canConfirmThis && onConfirm && (
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onConfirm();
            }}
            disabled={isConfirming}
            className="material-icons text-sm text-yellow-500/60 hover:text-green-500 transition-colors ml-1 disabled:opacity-50 cursor-pointer"
            title="Confirmar plantilla"
          >
            check_circle_outline
          </button>
        )}
      </div>
    </div>
  );
};

export default TemplateCard;
