/**
 * EditableStatsPanel Component
 * Renders dynamic stats from AI-generated entities with inline editing support.
 * Handles nested objects (like SKILLS), strings, and numbers.
 * Supports label mapping from template field definitions.
 */

import React from 'react';
import type { DynamicStats } from '@core/types';
import type { FieldDefinition } from '@core/types/template.types';
import { EditableField, EditableFieldVariant } from './EditableField';

/**
 * Map of field identifier (name) to display name.
 */
export type LabelMap = Record<string, string>;

export interface EditableStatsPanelProps {
  /** Stats object with dynamic keys and values */
  stats: DynamicStats | null | undefined;
  /** Callback when any stat changes */
  onStatsChange?: (stats: DynamicStats) => void;
  /** Color theme: 'primary' | 'danger' | 'warning' */
  variant?: 'primary' | 'danger' | 'warning';
  /** Maximum columns for the grid layout */
  maxColumns?: 3 | 4 | 5 | 6;
  /** Show progress bar for numeric values */
  showProgressBar?: boolean;
  /** Maximum value for progress bar calculation (default: 100) */
  maxProgressValue?: number;
  /** Optional label map to convert field identifiers to display names */
  labelMap?: LabelMap;
  /** Optional field definitions from template (alternative to labelMap) */
  fieldDefinitions?: FieldDefinition[];
  /** Whether the stats are editable */
  disabled?: boolean;
}

const variantColors: Record<'primary' | 'danger' | 'warning', EditableFieldVariant> = {
  primary: 'primary',
  danger: 'danger',
  warning: 'warning'
};

/**
 * Checks if a value is a nested object (like SKILLS)
 */
const isNestedObject = (value: unknown): value is Record<string, unknown> => {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
};

/**
 * Builds a label map from field definitions.
 * Maps field.name (identifier) -> field.displayName
 */
export const buildLabelMapFromFields = (fields: FieldDefinition[]): LabelMap => {
  const map: LabelMap = {};
  fields.forEach(field => {
    map[field.name] = field.displayName;
    map[field.name.toLowerCase()] = field.displayName;
    map[field.name.toUpperCase()] = field.displayName;
  });
  return map;
};

/**
 * Gets the display label for a field key.
 */
const getDisplayLabel = (key: string, labelMap?: LabelMap): string => {
  if (labelMap?.[key]) return labelMap[key];
  
  if (labelMap) {
    const lowerKey = key.toLowerCase();
    const upperKey = key.toUpperCase();
    if (labelMap[lowerKey]) return labelMap[lowerKey];
    if (labelMap[upperKey]) return labelMap[upperKey];
  }
  
  return key
    .replace(/_/g, ' ')
    .toLowerCase()
    .replace(/\b\w/g, c => c.toUpperCase());
};

/**
 * Formats a stat value for display
 */
const formatStatValue = (value: unknown): string => {
  if (value === null || value === undefined) return '--';
  if (typeof value === 'number') return value.toString();
  if (typeof value === 'string') return value;
  if (Array.isArray(value)) return value.join(', ');
  if (isNestedObject(value)) return '...';
  return String(value);
};

/**
 * Gets the color classes based on variant
 */
const getVariantColors = (variant: EditableStatsPanelProps['variant']) => {
  switch (variant) {
    case 'danger':
      return {
        text: 'text-danger',
        textMuted: 'text-danger/40',
        border: 'border-danger/20',
        progress: 'bg-danger/20',
      };
    case 'warning':
      return {
        text: 'text-yellow-500',
        textMuted: 'text-yellow-500/40',
        border: 'border-yellow-500/20',
        progress: 'bg-yellow-500/20',
      };
    default:
      return {
        text: 'text-primary',
        textMuted: 'text-primary/40',
        border: 'border-primary/20',
        progress: 'bg-primary/20',
      };
  }
};

/**
 * Gets grid column class based on maxColumns
 */
const getGridClass = (maxColumns: EditableStatsPanelProps['maxColumns']) => {
  switch (maxColumns) {
    case 4: return 'grid-cols-4';
    case 5: return 'grid-cols-5';
    case 6: return 'grid-cols-6';
    default: return 'grid-cols-3';
  }
};

/**
 * StatCard - Individual stat display with editing
 */
const StatCard: React.FC<{
  fieldKey: string;
  label: string;
  value: unknown;
  colors: ReturnType<typeof getVariantColors>;
  showProgressBar: boolean;
  maxProgressValue: number;
  variant: EditableStatsPanelProps['variant'];
  onChange: (key: string, value: string | number) => void;
  disabled?: boolean;
}> = ({ fieldKey, label, value, colors, showProgressBar, maxProgressValue, variant, onChange, disabled }) => {
  const isNumeric = typeof value === 'number';
  const displayValue = formatStatValue(value);
  const progressWidth = isNumeric && showProgressBar 
    ? Math.min((value / maxProgressValue) * 100, 100) 
    : 0;

  const handleChange = (newValue: string | number) => {
    onChange(fieldKey, newValue);
  };

  return (
    <div className={`bg-surface-dark border ${colors.border} p-2 text-center relative overflow-hidden`}>
      <p className={`text-[9px] ${colors.textMuted} uppercase mb-1 truncate`} title={`${label} (${fieldKey})`}>
        {label}
      </p>
      <EditableField
        value={isNumeric ? value : displayValue}
        type={isNumeric ? 'number' : 'text'}
        variant={variantColors[variant || 'primary']}
        onChange={handleChange}
        disabled={disabled}
        className="text-lg font-bold"
      />
      {showProgressBar && isNumeric && (
        <div 
          className={`absolute bottom-0 left-0 h-0.5 ${colors.progress}`} 
          style={{ width: `${progressWidth}%` }}
        />
      )}
    </div>
  );
};

/**
 * NestedStatsSection - Renders nested objects like SKILLS with editing
 */
const NestedStatsSection: React.FC<{
  fieldKey: string;
  label: string;
  data: Record<string, unknown>;
  colors: ReturnType<typeof getVariantColors>;
  labelMap?: LabelMap;
  variant: EditableStatsPanelProps['variant'];
  onChange: (parentKey: string, childKey: string, value: string | number) => void;
  disabled?: boolean;
}> = ({ fieldKey, label, data, colors, labelMap, variant, onChange, disabled }) => {
  const entries = Object.entries(data);
  
  return (
    <div className={`bg-surface-dark/50 border ${colors.border} p-3 mt-2`}>
      <p className={`text-[8px] ${colors.textMuted} uppercase tracking-widest mb-2 flex items-center gap-1`} title={fieldKey}>
        <span className="material-icons text-xs">folder_open</span>
        {label}
      </p>
      <div className="flex flex-wrap gap-2">
        {entries.map(([key, val]) => {
          const nestedLabel = getDisplayLabel(key, labelMap);
          return (
            <span 
              key={key} 
              className={`px-2 py-1 bg-black/40 border ${colors.border} text-[9px] ${colors.text}`}
              title={key}
            >
              <span className="opacity-60">{nestedLabel}:</span> 
              <EditableField
                value={formatStatValue(val)}
                type={typeof val === 'number' ? 'number' : 'text'}
                variant={variantColors[variant || 'primary']}
                onChange={(newVal) => onChange(fieldKey, key, newVal)}
                disabled={disabled}
                className="inline-block ml-1 min-w-[60px]"
              />
            </span>
          );
        })}
      </div>
    </div>
  );
};

/**
 * TextStatSection - Renders string stats with editing
 */
const TextStatSection: React.FC<{
  fieldKey: string;
  label: string;
  value: string;
  colors: ReturnType<typeof getVariantColors>;
  variant: EditableStatsPanelProps['variant'];
  onChange: (key: string, value: string) => void;
  disabled?: boolean;
}> = ({ fieldKey, label, value, colors, variant, onChange, disabled }) => (
  <div className={`bg-surface-dark/50 border ${colors.border} p-3 mt-2`}>
    <p className={`text-[8px] ${colors.textMuted} uppercase tracking-widest mb-1`} title={fieldKey}>
      {label}
    </p>
    <EditableField
      value={value}
      type="textarea"
      rows={3}
      variant={variantColors[variant || 'primary']}
      onChange={(newVal) => onChange(fieldKey, String(newVal))}
      disabled={disabled}
    />
  </div>
);

/**
 * EditableStatsPanel - Main component
 */
export const EditableStatsPanel: React.FC<EditableStatsPanelProps> = ({
  stats,
  onStatsChange,
  variant = 'primary',
  maxColumns = 3,
  showProgressBar = true,
  maxProgressValue = 10,
  labelMap: externalLabelMap,
  fieldDefinitions,
  disabled = false,
}) => {
  if (!stats || Object.keys(stats).length === 0) {
    return (
      <div className="text-center py-4 text-primary/30 text-[10px] uppercase tracking-widest">
        Sin estadisticas
      </div>
    );
  }

  const labelMap = externalLabelMap ?? (fieldDefinitions ? buildLabelMapFromFields(fieldDefinitions) : undefined);
  const colors = getVariantColors(variant);
  const gridClass = getGridClass(maxColumns);

  const handleStatChange = (key: string, value: string | number) => {
    if (onStatsChange) {
      onStatsChange({ ...stats, [key]: value });
    }
  };

  const handleNestedStatChange = (parentKey: string, childKey: string, value: string | number) => {
    if (onStatsChange) {
      const parent = stats[parentKey];
      if (isNestedObject(parent)) {
        onStatsChange({
          ...stats,
          [parentKey]: { ...parent, [childKey]: value }
        });
      }
    }
  };

  const handleTextStatChange = (key: string, value: string) => {
    handleStatChange(key, value);
  };

  const numericStats: [string, string, number][] = [];
  const stringStats: [string, string, string][] = [];
  const nestedStats: [string, string, Record<string, unknown>][] = [];

  Object.entries(stats).forEach(([key, value]) => {
    const displayLabel = getDisplayLabel(key, labelMap);
    if (typeof value === 'number') {
      numericStats.push([key, displayLabel, value]);
    } else if (typeof value === 'string') {
      stringStats.push([key, displayLabel, value]);
    } else if (isNestedObject(value)) {
      nestedStats.push([key, displayLabel, value]);
    }
  });

  return (
    <div className="space-y-2">
      {numericStats.length > 0 && (
        <div className={`grid ${gridClass} gap-2`}>
          {numericStats.map(([key, label, value]) => (
            <StatCard
              key={key}
              fieldKey={key}
              label={label}
              value={value}
              colors={colors}
              showProgressBar={showProgressBar}
              maxProgressValue={maxProgressValue}
              variant={variant}
              onChange={handleStatChange}
              disabled={disabled}
            />
          ))}
        </div>
      )}

      {stringStats.map(([key, label, value]) => (
        <TextStatSection
          key={key}
          fieldKey={key}
          label={label}
          value={value}
          colors={colors}
          variant={variant}
          onChange={handleTextStatChange}
          disabled={disabled}
        />
      ))}

      {nestedStats.map(([key, label, value]) => (
        <NestedStatsSection
          key={key}
          fieldKey={key}
          label={label}
          data={value}
          colors={colors}
          labelMap={labelMap}
          variant={variant}
          onChange={handleNestedStatChange}
          disabled={disabled}
        />
      ))}
    </div>
  );
};

export default EditableStatsPanel;
