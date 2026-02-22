/**
 * Template Field Editor Component
 * Single Responsibility: Edit template field definitions
 * Restored: inline editing, full field modal, conditional fields
 */

import React, { useState, useCallback } from 'react';
import { Button } from '@shared/components/ui';
import type { FieldDefinition } from '@core/types';
import { FieldType, FieldTypeLabels } from '@core/types';

interface TemplateFieldEditorProps {
  fields: FieldDefinition[];
  onSave: (fields: FieldDefinition[]) => Promise<boolean>;
  onCancel: () => void;
  isLoading: boolean;
}

const fieldTypeOptions = Object.entries(FieldTypeLabels).map(([value, label]) => ({
  value: Number(value) as FieldType,
  label,
}));

const createEmptyField = (order: number): FieldDefinition => ({
  name: '',
  displayName: '',
  fieldType: FieldType.Text,
  isRequired: false,
  order,
  description: '',
  defaultValue: undefined,
  minValue: undefined,
  maxValue: undefined,
  options: undefined,
});

export const TemplateFieldEditor: React.FC<TemplateFieldEditorProps> = ({
  fields: initialFields,
  onSave,
  onCancel,
  isLoading,
}) => {
  const [editedFields, setEditedFields] = useState<FieldDefinition[]>([...initialFields]);
  const [editingField, setEditingField] = useState<FieldDefinition | null>(null);
  const [isAddingNew, setIsAddingNew] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const handleOmitField = useCallback((fieldName: string) => {
    setEditedFields(prev => prev.filter(f => f.name !== fieldName));
  }, []);

  const handleEditField = useCallback((field: FieldDefinition) => {
    setEditingField({ ...field });
    setIsAddingNew(false);
  }, []);

  const handleAddNewField = useCallback(() => {
    const maxOrder = editedFields.length > 0 
      ? Math.max(...editedFields.map(f => f.order)) 
      : 0;
    setEditingField(createEmptyField(maxOrder + 1));
    setIsAddingNew(true);
  }, [editedFields]);

  const handleCancelFieldEdit = useCallback(() => {
    setEditingField(null);
    setIsAddingNew(false);
  }, []);

  const handleSaveFieldEdit = useCallback(() => {
    if (!editingField) return;

    if (isAddingNew) {
      if (!editingField.name.trim()) {
        return;
      }

      const sanitizedName = editingField.name
        .toLowerCase()
        .replace(/\s+/g, '_')
        .replace(/[^a-z0-9_]/g, '');

      if (!sanitizedName) return;

      if (editedFields.some(f => f.name === sanitizedName)) {
        return;
      }

      const newField = { ...editingField, name: sanitizedName };
      setEditedFields(prev => [...prev, newField]);
    } else {
      setEditedFields(prev => prev.map(f => 
        f.name === editingField.name ? editingField : f
      ));
    }

    setEditingField(null);
    setIsAddingNew(false);
  }, [editingField, isAddingNew, editedFields]);

  const handleSaveAll = useCallback(async () => {
    setIsSaving(true);
    try {
      await onSave(editedFields);
    } finally {
      setIsSaving(false);
    }
  }, [editedFields, onSave]);

  const updateEditingField = useCallback((updates: Partial<FieldDefinition>) => {
    setEditingField(prev => prev ? { ...prev, ...updates } : null);
  }, []);

  return (
    <>
      {/* Field List in Edit Mode */}
      <div className="space-y-3">
        {editedFields.length === 0 ? (
          <div className="text-center text-primary/40 py-8">
            <span className="material-icons text-4xl mb-2">warning</span>
            <p className="text-xs uppercase">Sin campos definidos</p>
            <p className="text-[10px] mt-1">La plantilla necesita campos para funcionar</p>
            <button
              type="button"
              onClick={handleAddNewField}
              className="mt-4 text-xs px-3 py-2 border border-green-500/40 text-green-400 hover:bg-green-500/20 transition-colors flex items-center gap-2 mx-auto cursor-pointer"
            >
              <span className="material-icons text-sm">add</span>
              AÑADIR PRIMER CAMPO
            </button>
          </div>
        ) : (
          <>
            {editedFields
              .sort((a, b) => a.order - b.order)
              .map((field, index) => (
                <div 
                  key={field.name}
                  className="border border-cyan-500/30 bg-cyan-500/5 p-3"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-primary/40">#{index + 1}</span>
                        <span className="font-mono text-cyan-400 text-sm">{field.name}</span>
                        {field.isRequired && (
                          <span className="text-danger text-xs">*</span>
                        )}
                      </div>
                      <p className="text-sm text-primary mt-1">{field.displayName}</p>
                      {field.description && (
                        <p className="text-xs text-primary/50 mt-1">{field.description}</p>
                      )}
                    </div>
                    
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] px-2 py-1 bg-primary/10 border border-primary/20 text-primary/60">
                        {FieldTypeLabels[field.fieldType]}
                      </span>
                      
                      <div className="flex items-center gap-1 ml-2">
                        <button
                          type="button"
                          onClick={() => handleEditField(field)}
                          className="material-icons text-sm text-cyan-400/60 hover:text-cyan-400 transition-colors p-1 cursor-pointer"
                          title="Editar campo"
                        >
                          edit
                        </button>
                        <button
                          type="button"
                          onClick={() => handleOmitField(field.name)}
                          className="material-icons text-sm text-red-400/60 hover:text-red-400 transition-colors p-1 cursor-pointer"
                          title="Omitir campo"
                        >
                          delete
                        </button>
                      </div>
                    </div>
                  </div>
                  
                  <div className="mt-2 flex flex-wrap gap-2 text-[10px]">
                    {field.defaultValue && (
                      <span className="text-primary/40">
                        Default: <span className="text-primary/60">{field.defaultValue}</span>
                      </span>
                    )}
                    {field.minValue !== undefined && field.minValue !== null && (
                      <span className="text-primary/40">
                        Min: <span className="text-primary/60">{field.minValue}</span>
                      </span>
                    )}
                    {field.maxValue !== undefined && field.maxValue !== null && (
                      <span className="text-primary/40">
                        Max: <span className="text-primary/60">{field.maxValue}</span>
                      </span>
                    )}
                    {field.options && field.options.length > 0 && (
                      <span className="text-primary/40">
                        Opciones: <span className="text-primary/60">{field.options.join(', ')}</span>
                      </span>
                    )}
                  </div>
                </div>
              ))}
            
            {/* Add field button */}
            <button
              type="button"
              onClick={handleAddNewField}
              disabled={isLoading || isSaving}
              className="w-full text-xs px-3 py-2 border border-green-500/40 text-green-400 hover:bg-green-500/20 transition-colors flex items-center justify-center gap-2 disabled:opacity-50 cursor-pointer"
            >
              <span className="material-icons text-sm">add</span>
              AÑADIR CAMPO
            </button>
          </>
        )}
      </div>

      {/* Header Actions - shown during edit */}
      <div className="flex items-center justify-end gap-2 mt-4 pt-4 border-t border-primary/20">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading || isSaving}
          className="text-[10px] px-2 py-1 border border-red-500/40 text-red-400 hover:bg-red-500/20 transition-colors disabled:opacity-50 cursor-pointer"
        >
          CANCELAR
        </button>
        <button
          type="button"
          onClick={handleSaveAll}
          disabled={isLoading || isSaving}
          className="text-[10px] px-2 py-1 border border-cyan-500/40 text-cyan-400 hover:bg-cyan-500/20 transition-colors disabled:opacity-50 cursor-pointer"
        >
          {isSaving || isLoading ? 'GUARDANDO...' : 'GUARDAR'}
        </button>
      </div>

      {/* Field Edit Modal */}
      {editingField && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-lg max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col">
            <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
              <h3 className="text-xs uppercase tracking-widest flex items-center gap-2">
                <span className="material-icons text-sm">edit</span>
                {isAddingNew ? 'NUEVO_CAMPO' : 'EDITAR_CAMPO'}
              </h3>
              <button
                onClick={handleCancelFieldEdit}
                className="material-icons text-sm hover:rotate-90 transition-transform"
                aria-label="Cerrar"
              >
                close
              </button>
            </div>
            
            <div className="p-6 space-y-4 font-mono overflow-y-auto flex-1 custom-scrollbar">
              {/* Field Name (identifier) */}
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-1">Identificador (name)</label>
                <input
                  type="text"
                  value={editingField.name}
                  onChange={(e) => updateEditingField({ name: e.target.value })}
                  placeholder="ej: gear_items, health_points"
                  className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm font-mono focus:border-cyan-500 focus:outline-none"
                  disabled={!isAddingNew}
                />
                <p className="text-[10px] text-primary/40 mt-1">Nombre interno usado en el JSON (snake_case recomendado)</p>
              </div>
              
              {/* Display Name */}
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-1">Nombre Visible</label>
                <input
                  type="text"
                  value={editingField.displayName}
                  onChange={(e) => updateEditingField({ displayName: e.target.value })}
                  className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                />
              </div>
              
              {/* Description */}
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-1">Descripción</label>
                <textarea
                  value={editingField.description || ''}
                  onChange={(e) => updateEditingField({ description: e.target.value })}
                  rows={2}
                  className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none resize-none"
                />
              </div>
              
              {/* Field Type */}
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-1">Tipo</label>
                <select
                  value={editingField.fieldType}
                  onChange={(e) => updateEditingField({ fieldType: Number(e.target.value) as FieldType })}
                  className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                >
                  {fieldTypeOptions.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              
              {/* Is Required */}
              <label className="flex items-center gap-2 text-sm text-primary/60 cursor-pointer">
                <input
                  type="checkbox"
                  checked={editingField.isRequired}
                  onChange={(e) => updateEditingField({ isRequired: e.target.checked })}
                  className="accent-cyan-500"
                />
                Campo requerido
              </label>
              
              {/* Default Value */}
              <div>
                <label className="block text-xs text-primary/60 uppercase mb-1">Valor por defecto</label>
                <input
                  type="text"
                  value={editingField.defaultValue || ''}
                  onChange={(e) => updateEditingField({ defaultValue: e.target.value || undefined })}
                  className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                />
              </div>
              
              {/* Min/Max for Number type */}
              {editingField.fieldType === FieldType.Number && (
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs text-primary/60 uppercase mb-1">Mínimo</label>
                    <input
                      type="number"
                      value={editingField.minValue ?? ''}
                      onChange={(e) => updateEditingField({ 
                        minValue: e.target.value ? Number(e.target.value) : undefined 
                      })}
                      className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-primary/60 uppercase mb-1">Máximo</label>
                    <input
                      type="number"
                      value={editingField.maxValue ?? ''}
                      onChange={(e) => updateEditingField({ 
                        maxValue: e.target.value ? Number(e.target.value) : undefined 
                      })}
                      className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none"
                    />
                  </div>
                </div>
              )}
              
              {/* Options for Select/MultiSelect */}
              {(editingField.fieldType === FieldType.Select || editingField.fieldType === FieldType.MultiSelect) && (
                <div>
                  <label className="block text-xs text-primary/60 uppercase mb-1">
                    Opciones (una por línea)
                  </label>
                  <textarea
                    value={(editingField.options || []).join('\n')}
                    onChange={(e) => updateEditingField({ 
                      options: e.target.value.split('\n').filter(o => o.trim()) 
                    })}
                    rows={4}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none resize-none font-mono"
                    placeholder="Opción 1&#10;Opción 2&#10;Opción 3"
                  />
                </div>
              )}
            </div>
            
            {/* Modal Actions */}
            <div className="p-4 border-t border-primary/20 flex justify-end gap-2 flex-shrink-0">
              <Button variant="secondary" size="sm" onClick={handleCancelFieldEdit}>
                CANCELAR
              </Button>
              <Button variant="primary" size="sm" onClick={handleSaveFieldEdit}>
                APLICAR
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default TemplateFieldEditor;
