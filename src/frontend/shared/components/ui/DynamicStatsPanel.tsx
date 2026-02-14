/**
 * DynamicStatsPanel Component
 * Renders dynamic stats from AI-generated entities.
 * Handles nested objects (like SKILLS), strings, and numbers.
 * Supports label mapping from template field definitions.
 */

import React from 'react';
import type { DynamicStats } from '@core/types';
import type { FieldDefinition } from '@core/types/template.types';

/**
 * Map of field identifier (name) to display name.
 * Used to show human-readable labels instead of raw keys.
 */
export type LabelMap = Record<string, string>;

export interface DynamicStatsPanelProps {
  /** Stats object with dynamic keys and values */
  stats: DynamicStats | null | undefined;
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
}

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
    // Also map case variations for flexibility
    map[field.name.toLowerCase()] = field.displayName;
    map[field.name.toUpperCase()] = field.displayName;
  });
  return map;
};

/**
 * Gets the display label for a field key.
 * Falls back to formatting the raw key if not found in map.
 * @param key - The field identifier (e.g., "ATTRIBUTES_STRENGTH")
 * @param labelMap - Optional map of identifiers to display names
 * @returns Human-readable label
 */
const getDisplayLabel = (key: string, labelMap?: LabelMap): string => {
  // First check exact match in label map
  if (labelMap?.[key]) {
    return labelMap[key];
  }
  
  // Check case-insensitive match
  if (labelMap) {
    const lowerKey = key.toLowerCase();
    const upperKey = key.toUpperCase();
    if (labelMap[lowerKey]) return labelMap[lowerKey];
    if (labelMap[upperKey]) return labelMap[upperKey];
  }
  
  // Fallback: Format the raw key to be more readable
  // "ATTRIBUTES_STRENGTH" -> "Attributes Strength"
  // "gear_items" -> "Gear Items"
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
  if (isNestedObject(value)) return '...'; // Will be expanded separately
  return String(value);
};

/**
 * Gets the color classes based on variant
 */
const getVariantColors = (variant: DynamicStatsPanelProps['variant']) => {
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
const getGridClass = (maxColumns: DynamicStatsPanelProps['maxColumns']) => {
  switch (maxColumns) {
    case 4: return 'grid-cols-4';
    case 5: return 'grid-cols-5';
    case 6: return 'grid-cols-6';
    default: return 'grid-cols-3';
  }
};

/**
 * StatCard - Individual stat display
 */
const StatCard: React.FC<{
  fieldKey: string;
  label: string;
  value: unknown;
  colors: ReturnType<typeof getVariantColors>;
  showProgressBar: boolean;
  maxProgressValue: number;
}> = ({ fieldKey, label, value, colors, showProgressBar, maxProgressValue }) => {
  const displayValue = formatStatValue(value);
  const isNumeric = typeof value === 'number';
  const progressWidth = isNumeric && showProgressBar 
    ? Math.min((value / maxProgressValue) * 100, 100) 
    : 0;

  return (
    <div className={`bg-surface-dark border ${colors.border} p-2 text-center relative overflow-hidden`}>
      <p className={`text-[9px] ${colors.textMuted} uppercase mb-1 truncate`} title={`${label} (${fieldKey})`}>
        {label}
      </p>
      <p className={`text-lg font-bold ${colors.text} font-mono truncate`} title={displayValue}>
        {displayValue}
      </p>
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
 * NestedStatsSection - Renders nested objects like SKILLS
 */
const NestedStatsSection: React.FC<{
  fieldKey: string;
  label: string;
  data: Record<string, unknown>;
  colors: ReturnType<typeof getVariantColors>;
  labelMap?: LabelMap;
}> = ({ fieldKey, label, data, colors, labelMap }) => {
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
              <span className="opacity-60">{nestedLabel}:</span> {formatStatValue(val)}
            </span>
          );
        })}
      </div>
    </div>
  );
};

/**
 * TextStatSection - Renders string stats (like TALENT, GEAR)
 */
const TextStatSection: React.FC<{
  fieldKey: string;
  label: string;
  value: string;
  colors: ReturnType<typeof getVariantColors>;
}> = ({ fieldKey, label, value, colors }) => (
  <div className={`bg-surface-dark/50 border ${colors.border} p-3 mt-2`}>
    <p className={`text-[8px] ${colors.textMuted} uppercase tracking-widest mb-1`} title={fieldKey}>
      {label}
    </p>
    <p className={`text-[10px] ${colors.text}`}>{value}</p>
  </div>
);

/**
 * DynamicStatsPanel - Main component
 * Renders stats dynamically based on their type:
 * - Numbers: Grid cards with optional progress bar
 * - Strings: Text sections below the grid
 * - Objects: Nested sections with key-value pairs
 */
export const DynamicStatsPanel: React.FC<DynamicStatsPanelProps> = ({
  stats,
  variant = 'primary',
  maxColumns = 3,
  showProgressBar = true,
  maxProgressValue = 10,
  labelMap: externalLabelMap,
  fieldDefinitions,
}) => {
  if (!stats || Object.keys(stats).length === 0) {
    return (
      <div className="text-center py-4 text-primary/30 text-[10px] uppercase tracking-widest">
        Sin estadisticas
      </div>
    );
  }

  // Build label map: prefer external labelMap, fallback to building from fieldDefinitions
  const labelMap = externalLabelMap ?? (fieldDefinitions ? buildLabelMapFromFields(fieldDefinitions) : undefined);

  const colors = getVariantColors(variant);
  const gridClass = getGridClass(maxColumns);

  // Separate stats by type for proper rendering
  const numericStats: [string, string, number][] = [];  // [key, displayLabel, value]
  const stringStats: [string, string, string][] = [];   // [key, displayLabel, value]
  const nestedStats: [string, string, Record<string, unknown>][] = [];  // [key, displayLabel, value]

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
      {/* Numeric stats grid */}
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
            />
          ))}
        </div>
      )}

      {/* String stats (TALENT, GEAR, etc.) */}
      {stringStats.map(([key, label, value]) => (
        <TextStatSection
          key={key}
          fieldKey={key}
          label={label}
          value={value}
          colors={colors}
        />
      ))}

      {/* Nested stats (SKILLS, etc.) */}
      {nestedStats.map(([key, label, value]) => (
        <NestedStatsSection
          key={key}
          fieldKey={key}
          label={label}
          data={value}
          colors={colors}
          labelMap={labelMap}
        />
      ))}
    </div>
  );
};

export default DynamicStatsPanel;
