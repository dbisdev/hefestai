/**
 * Gallery Category Constants
 * Single Responsibility: Define category configuration for gallery
 * DRY: Centralized category definitions used across gallery components
 */

import type { EntityCategory } from '@core/types';

export interface CategoryInfo {
  id: EntityCategory;
  label: string;
  icon: string;
}

export const ENTITY_CATEGORIES: CategoryInfo[] = [
  { id: 'character', label: 'PERSONAJES', icon: 'face' },
  { id: 'npc', label: 'ACTORES', icon: 'groups' },
  { id: 'enemy', label: 'ENEMIGOS', icon: 'dangerous' },
  { id: 'vehicle', label: 'VEHÍCULOS', icon: 'rocket_launch' },
  { id: 'mission', label: 'MISIONES', icon: 'assignment' },
  { id: 'encounter', label: 'ENCUENTROS', icon: 'pest_control' },
];

export const CATEGORIES_WITHOUT_TEMPLATE: EntityCategory[] = ['mission', 'encounter', 'solar_system'];

export const ENTITY_CATEGORIES_LABS: CategoryInfo[] = [
  { id: 'solar_system', label: 'SISTEMAS SOLARES', icon: 'public' },
];

export const CATEGORY_TO_ROUTE: Record<EntityCategory, string> = {
  'solar_system': '/gallery/solar-gen',
  'character': '/gallery/char-gen',
  'npc': '/gallery/npc-gen',
  'enemy': '/gallery/enemy-gen',
  'vehicle': '/gallery/vehi-gen',
  'mission': '/gallery/mission-gen',
  'encounter': '/gallery/encounter-gen',
};

export const TEMPLATE_TYPE_TO_ROUTE: Record<string, string> = {
  'character': '/gallery/char-gen',
  'npc': '/gallery/npc-gen',
  'enemy': '/gallery/enemy-gen',
  'vehicle': '/gallery/vehi-gen',
  'mission': '/gallery/mission-gen',
  'encounter': '/gallery/encounter-gen',
  'solar_system': '/gallery/solar-gen',
  'solarsystem': '/gallery/solar-gen',
  'solar-system': '/gallery/solar-gen',
};

export const ENTITY_TYPE_ICONS: Record<string, string> = {
  'player_character': 'face',
  'character': 'face',
  'npc': 'groups',
  'non_player_character': 'groups',
  'actor': 'groups',
  'enemy': 'dangerous',
  'adversary': 'dangerous',
  'vehicle': 'rocket_launch',
  'starship': 'rocket',
  'spacecraft': 'flight',
  'solar_system': 'public',
  'star_system': 'public',
  'planet': 'language',
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
