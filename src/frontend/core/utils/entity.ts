/**
 * Entity Utility Functions
 * Shared utilities for entity display and manipulation
 * Used by EntityEditModal, EntityViewModal, and other components
 */

import type { FieldDefinition } from '@core/types';

/**
 * Map of field identifier (name) to display name.
 */
export type LabelMap = Record<string, string>;

/**
 * Builds a label map from field definitions.
 * Maps field.name (identifier) -> field.displayName
 * Also maps case variations for flexibility.
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
 * Falls back to formatting the raw key if not found in map.
 * @param key - The field identifier (e.g., "ATTRIBUTES_STRENGTH")
 * @param labelMap - Optional map of identifiers to display names
 * @returns Human-readable label
 */
export const getDisplayLabel = (key: string, labelMap?: LabelMap): string => {
  if (labelMap?.[key]) {
    return labelMap[key];
  }

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
 * Checks if a value is a nested object (like SKILLS)
 * Used to determine how to render attribute values
 */
export const isNestedObject = (value: unknown): value is Record<string, unknown> => {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
};

/**
 * Categorizes entity attributes by type for rendering
 * Returns separate arrays for numeric, string, and nested attributes
 */
export const categorizeAttributes = (attributes: Record<string, unknown>): {
  numeric: [string, number][];
  string: [string, string][];
  nested: [string, Record<string, unknown>][];
} => {
  const numeric: [string, number][] = [];
  const string: [string, string][] = [];
  const nested: [string, Record<string, unknown>][] = [];

  Object.entries(attributes).forEach(([key, value]) => {
    if (typeof value === 'number') {
      numeric.push([key, value]);
    } else if (typeof value === 'string') {
      string.push([key, value]);
    } else if (isNestedObject(value)) {
      nested.push([key, value]);
    }
  });

  return { numeric, string, nested };
};

/**
 * Checks if entity has any attributes
 */
export const hasEntityAttributes = (
  attributes: Record<string, unknown> | undefined | null
): boolean => {
  if (!attributes || Object.keys(attributes).length === 0) {
    return false;
  }
  
  const { numeric, string, nested } = categorizeAttributes(attributes);
  return numeric.length > 0 || string.length > 0 || nested.length > 0;
};
