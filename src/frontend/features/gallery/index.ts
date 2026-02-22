/**
 * Gallery Feature Module
 * Barrel exports for gallery-related components
 */

// Pages
export { GalleryPage } from './pages/GalleryPage';
export { MasterHubPage } from './pages/MasterHubPage';

// Hooks
export { useGalleryEntities, useEntityActions, useGalleryCategories } from './hooks';

// Components
export { CategorySidebar, EntityCard, EntityGrid, CampaignSelector, TemplateLinks } from './components';

// Constants
export { 
  ENTITY_CATEGORIES, 
  TEMPLATE_ENTITY_TYPES,
  STANDARD_GENERATED_TYPES,
  LABS_ENTITY_TYPES,
  GENERATED_ENTITY_TYPES,
  CANONICAL_ENTITY_TYPE_OPTIONS,
  CATEGORY_TO_ROUTE,
  getRouteForTemplate,
  getIconForEntityType,
} from './constants/categories';
export type { CategoryInfo, SelectOption } from './constants/categories';
