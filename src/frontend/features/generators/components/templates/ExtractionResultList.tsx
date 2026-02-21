/**
 * Extraction Result List Component
 * Single Responsibility: Render list of extracted templates with status badges
 */

import React from 'react';
import { TemplateStatus, TemplateStatusLabels } from '@core/types';
import type { EntityTemplateSummary, ExtractedTemplateInfo } from '@core/types';

interface ExtractionResultListProps {
  extractionResult: ExtractedTemplateInfo[];
  templates: EntityTemplateSummary[];
  selectedId: string | null;
  onSelect: (templateId: string) => void;
  onViewComparison: (info: ExtractedTemplateInfo) => void;
  onConfirm?: (templateId: string) => void;
  confirmingId?: string | null;
  isNewlyExtracted: (templateId: string) => boolean;
  isLoading?: boolean;
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

export const ExtractionResultList: React.FC<ExtractionResultListProps> = ({
  extractionResult,
  templates,
  selectedId,
  onSelect,
  onViewComparison,
  onConfirm,
  confirmingId,
  isNewlyExtracted,
  isLoading = false,
}) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full text-primary/40">
        <span className="animate-pulse text-xs">CARGANDO...</span>
      </div>
    );
  }

  if (extractionResult.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-orange-400/40">
        <span className="material-icons text-3xl mb-2">auto_awesome</span>
        <p className="text-[10px] uppercase text-center">Sin extracción activa</p>
        <p className="text-[9px] mt-1 text-center">Ejecuta "Extraer de Manuales"</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {extractionResult.map((info) => {
        const hasValidId = info.templateId && info.templateId !== '00000000-0000-0000-0000-000000000000';
        const templateInDb = hasValidId ? templates.find(t => t.id === info.templateId) : null;
        const canConfirm = templateInDb && (templateInDb.status === TemplateStatus.Draft || templateInDb.status === TemplateStatus.PendingReview);
        const isConfirmedStatus = templateInDb?.status === TemplateStatus.Confirmed;
        
        const hasExtractedFields = info.extractedFields && info.extractedFields.length > 0;
        const isSkippedWithFields = hasExtractedFields && isConfirmedStatus;
        
        const handleClick = () => {
          if (isSkippedWithFields) {
            onViewComparison(info);
          } else if (hasValidId) {
            onSelect(info.templateId as string);
          }
        };

        return (
          <div
            key={info.templateId || info.entityTypeName}
            onClick={handleClick}
            className={`border p-2 cursor-pointer transition-all ${
              selectedId === info.templateId
                ? 'border-cyan-500 bg-cyan-500/10'
                : isSkippedWithFields
                ? 'border-purple-500/40 bg-purple-500/5 hover:border-purple-500/60'
                : info.extractionNotes && !hasExtractedFields
                ? 'border-red-500/40 bg-red-500/5 hover:border-red-500/60'
                : isNewlyExtracted(info.templateId || '')
                ? 'border-green-500/40 bg-green-500/5 hover:border-green-500/60'
                : 'border-yellow-500/40 bg-yellow-500/5 hover:border-yellow-500/60'
            }`}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1 flex-wrap">
                  <span className="font-mono text-cyan-400 text-[10px] truncate">
                    {info.entityTypeName}
                  </span>
                  {isSkippedWithFields ? (
                    <span className="text-[8px] px-1 py-0.5 border border-purple-500/60 text-purple-400 bg-purple-500/20">
                      COMPARAR
                    </span>
                  ) : info.extractionNotes ? (
                    <span className="text-[8px] px-1 py-0.5 border border-red-500/60 text-red-400 bg-red-500/20">
                      ERROR
                    </span>
                  ) : isNewlyExtracted(info.templateId || '') ? (
                    <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                      NUEVA
                    </span>
                  ) : (
                    <span className="text-[8px] px-1 py-0.5 border border-yellow-500/60 text-yellow-400 bg-yellow-500/20">
                      ACTUALIZADA
                    </span>
                  )}
                  {isConfirmedStatus && !isSkippedWithFields && (
                    <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                      CONFIRMADA
                    </span>
                  )}
                </div>
                <p className="text-xs text-primary mt-1 truncate">{info.displayName}</p>
                <p className="text-[9px] text-primary/40">
                  {isSkippedWithFields 
                    ? `${info.extractedFields?.length} campos extraídos` 
                    : `${info.fieldCount} campos`}
                </p>
                {info.extractionNotes && (
                  <p 
                    className={`text-[9px] mt-1 truncate ${isSkippedWithFields ? 'text-purple-400/80' : 'text-red-400/80'}`} 
                    title={info.extractionNotes}
                  >
                    {info.extractionNotes}
                  </p>
                )}
              </div>
              
              {isSkippedWithFields && (
                <span 
                  className="material-icons text-sm text-purple-400/60 ml-1"
                  title="Ver campos extraídos"
                >
                  compare_arrows
                </span>
              )}
              
              {canConfirm && onConfirm && (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onConfirm(info.templateId as string);
                  }}
                  disabled={confirmingId === info.templateId}
                  className="material-icons text-sm text-orange-400/60 hover:text-green-500 transition-colors ml-1 disabled:opacity-50 cursor-pointer"
                  title="Confirmar plantilla"
                >
                  check_circle_outline
                </button>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
};

export default ExtractionResultList;
