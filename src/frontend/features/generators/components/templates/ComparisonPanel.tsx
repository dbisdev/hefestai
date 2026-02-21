/**
 * Comparison Panel Component
 * Single Responsibility: Display and manage comparison between extracted and existing fields
 */

import React from 'react';
import { Button } from '@shared/components/ui';
import { FieldTypeLabels } from '@core/types';
import type { FieldDefinition, EntityTemplate } from '@core/types';

interface ComparisonPanelProps {
  comparisonExtractedFields: FieldDefinition[];
  comparisonTemplateName: string;
  selectedTemplate: EntityTemplate;
  isSavingFields: boolean;
  onAddField: (field: FieldDefinition) => void;
  onAddAllNewFields: () => void;
  onClose: () => void;
}

export const ComparisonPanel: React.FC<ComparisonPanelProps> = ({
  comparisonExtractedFields,
  comparisonTemplateName,
  selectedTemplate,
  isSavingFields,
  onAddField,
  onAddAllNewFields,
  onClose,
}) => {
  const newFieldsCount = comparisonExtractedFields.filter(
    cf => !selectedTemplate.fields.some(tf => tf.name === cf.name)
  ).length;

  return (
    <div className="border border-purple-500/50 bg-black/60 flex flex-col max-h-[40vh]">
      <div className="bg-purple-500/20 p-3 text-xs text-purple-400 uppercase tracking-widest flex items-center justify-between">
        <span className="flex items-center gap-2">
          <span className="material-icons text-sm">compare_arrows</span>
          Campos Extraídos - Nueva Extracción ({comparisonExtractedFields.length})
        </span>
        <div className="flex items-center gap-2">
          {newFieldsCount > 0 && (
            <button
              type="button"
              onClick={onAddAllNewFields}
              disabled={isSavingFields}
              className="text-[10px] px-2 py-1 border border-green-500/40 text-green-400 hover:bg-green-500/20 transition-colors disabled:opacity-50 flex items-center gap-1 cursor-pointer"
              title="Añadir todos los campos nuevos"
            >
              <span className="material-icons text-xs">playlist_add</span>
              {isSavingFields ? 'AÑADIENDO...' : 'AÑADIR TODOS'}
            </button>
          )}
          <button
            type="button"
            onClick={onClose}
            className="material-icons text-sm text-purple-400 hover:text-purple-300 transition-colors cursor-pointer"
            title="Cerrar comparación"
          >
            close
          </button>
        </div>
      </div>
      
      <div className="p-3 text-xs text-purple-300/80 border-b border-purple-500/20 bg-purple-500/5">
        <p>
          La plantilla "<span className="text-purple-400">{comparisonTemplateName}</span>" ya está confirmada con {selectedTemplate.fields.length} campos.
        </p>
        <p className="mt-1">
          Nueva extracción detectó <span className="text-purple-400">{comparisonExtractedFields.length}</span> campos. 
          {newFieldsCount > 0 && (
            <span className="text-green-400 ml-1">
              ({newFieldsCount} nuevos)
            </span>
          )}
        </p>
      </div>
      
      <div className="flex-1 overflow-y-auto p-4">
        <div className="space-y-2">
          {comparisonExtractedFields
            .sort((a, b) => a.order - b.order)
            .map((field, index) => {
              const existingField = selectedTemplate.fields.find(f => f.name === field.name);
              const isNew = !existingField;
              const isDifferent = existingField && (
                existingField.displayName !== field.displayName ||
                existingField.fieldType !== field.fieldType ||
                existingField.description !== field.description
              );
              
              return (
                <div 
                  key={field.name}
                  className={`border p-2 ${
                    isNew 
                      ? 'border-green-500/40 bg-green-500/10' 
                      : isDifferent 
                      ? 'border-yellow-500/40 bg-yellow-500/10'
                      : 'border-purple-500/20 bg-purple-500/5'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="text-[10px] text-purple-400/40">#{index + 1}</span>
                        <span className="font-mono text-purple-400 text-xs">{field.name}</span>
                        {isNew && (
                          <span className="text-[8px] px-1 py-0.5 border border-green-500/60 text-green-400 bg-green-500/20">
                            NUEVO
                          </span>
                        )}
                        {isDifferent && (
                          <span className="text-[8px] px-1 py-0.5 border border-yellow-500/60 text-yellow-400 bg-yellow-500/20">
                            DIFERENTE
                          </span>
                        )}
                        {field.isRequired && (
                          <span className="text-danger text-[10px]">*</span>
                        )}
                      </div>
                      <p className="text-xs text-primary/80 mt-0.5">{field.displayName}</p>
                      {field.description && (
                        <p className="text-[10px] text-primary/40 mt-0.5 truncate" title={field.description}>
                          {field.description}
                        </p>
                      )}
                    </div>
                    
                    <div className="flex items-center gap-2">
                      <span className="text-[9px] px-1.5 py-0.5 bg-purple-500/10 border border-purple-500/20 text-purple-400/60">
                        {FieldTypeLabels[field.fieldType]}
                      </span>
                      
                      {isNew && (
                        <button
                          type="button"
                          onClick={() => onAddField(field)}
                          disabled={isSavingFields}
                          className="material-icons text-sm text-green-400/60 hover:text-green-400 transition-colors disabled:opacity-50 cursor-pointer"
                          title="Añadir este campo a la plantilla"
                        >
                          add_circle
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
        </div>
      </div>
    </div>
  );
};

export default ComparisonPanel;
