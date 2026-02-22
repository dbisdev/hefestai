/**
 * Gallery Category Constants
 * Single Responsibility: Define category configuration for gallery
 * DRY: Centralized category definitions used across gallery components
 */

import type { EntityCategory, TemplateEntityType, GeneratedEntityType } from '@core/types';

export interface CategoryInfo {
  id: EntityCategory;
  label: string;
  icon: string;
}

export interface SelectOption {
  value: string;
  label: string;
}

/**
 * Entity types with templates (extracted from manuals).
 * These appear in the template management UI and main gallery menu.
 */
export const TEMPLATE_ENTITY_TYPES: CategoryInfo[] = [
  { id: 'character', label: 'PERSONAJES', icon: 'face' },
  { id: 'actor', label: 'ACTORES', icon: 'groups' },
  { id: 'monster', label: 'MONSTRUOS', icon: 'dangerous' },
  { id: 'vehicle', label: 'VEHÍCULOS', icon: 'rocket_launch' },
];

/**
 * Standard generated entity types (no templates, fixed schema).
 * These appear in the main gallery menu, not in Labs.
 */
export const STANDARD_GENERATED_TYPES: CategoryInfo[] = [
  { id: 'mission', label: 'MISIONES', icon: 'assignment' },
  { id: 'encounter', label: 'ENCUENTROS', icon: 'pest_control' },
];

/**
 * Labs/Experimental entity types.
 * Only appears in the Labs section of the gallery.
 */
export const LABS_ENTITY_TYPES: CategoryInfo[] = [
  { id: 'solar_system', label: 'SISTEMAS', icon: 'public' },
];

/**
 * All entity categories for main gallery sidebar.
 * Includes template-based types + standard generated types.
 */
export const ENTITY_CATEGORIES: CategoryInfo[] = [
  ...TEMPLATE_ENTITY_TYPES,
  ...STANDARD_GENERATED_TYPES,
];

/**
 * Categories for Labs section (experimental features).
 */
export const GENERATED_ENTITY_TYPES: CategoryInfo[] = LABS_ENTITY_TYPES;

/**
 * Categories that don't use templates (fixed schema in code).
 */
export const CATEGORIES_WITHOUT_TEMPLATE: GeneratedEntityType[] = ['mission', 'encounter', 'solar_system'];

/**
 * Select options for canonical entity types (used in template editor).
 */
export const CANONICAL_ENTITY_TYPE_OPTIONS: SelectOption[] = [
  { value: 'character', label: 'Personaje' },
  { value: 'actor', label: 'Actor/NPC' },
  { value: 'vehicle', label: 'Vehículo' },
  { value: 'monster', label: 'Monstruo' },
];

/**
 * Display names for canonical entity types (Spanish).
 */
export const CANONICAL_ENTITY_TYPE_LABELS: Record<TemplateEntityType, string> = {
  character: 'Personaje',
  actor: 'Actor/NPC',
  vehicle: 'Vehículo',
  monster: 'Monstruo',
};

export const CATEGORY_TO_ROUTE: Record<EntityCategory, string> = {
  'solar_system': '/gallery/solar-gen',
  'character': '/gallery/char-gen',
  'actor': '/gallery/npc-gen',
  'monster': '/gallery/enemy-gen',
  'vehicle': '/gallery/vehi-gen',
  'mission': '/gallery/mission-gen',
  'encounter': '/gallery/encounter-gen',
};

export const TEMPLATE_TYPE_TO_ROUTE: Record<string, string> = {
  'character': '/gallery/char-gen',
  'actor': '/gallery/npc-gen',
  'npc': '/gallery/npc-gen',
  'monster': '/gallery/enemy-gen',
  'enemy': '/gallery/enemy-gen',
  'vehicle': '/gallery/vehi-gen',
  'mission': '/gallery/mission-gen',
  'encounter': '/gallery/encounter-gen',
  'solar_system': '/gallery/solar-gen',
  'solarsystem': '/gallery/solar-gen',
  'solar-system': '/gallery/solar-gen',
};

export const ENTITY_TYPE_ICONS: Record<string, string> = {
  'character': 'face',
  'player_character': 'face',
  'actor': 'groups',
  'npc': 'groups',
  'non_player_character': 'groups',
  'monster': 'dangerous',
  'enemy': 'dangerous',
  'adversary': 'dangerous',
  'vehicle': 'rocket_launch',
  'starship': 'rocket',
  'spacecraft': 'flight',
  'planet': 'language',
  'solar_system': 'public',
  'star_system': 'public',
  'mission': 'assignment',
  'quest': 'explore',
  'encounter': 'pest_control',
  'combat': 'sports_martial_arts',
};

export function getRouteForTemplate(entityTypeName: string): string | null {
  return TEMPLATE_TYPE_TO_ROUTE[entityTypeName.toLowerCase()] || null;
}

export function getIconForEntityType(entityTypeName: string, iconHint?: string): string {
  if (iconHint) return iconHint;
  return ENTITY_TYPE_ICONS[entityTypeName.toLowerCase()] || 'category';
}
