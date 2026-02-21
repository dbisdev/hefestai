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
  ENTITY_CATEGORIES_LABS, 
  CATEGORY_TO_ROUTE,
  getRouteForTemplate,
  getIconForEntityType,
} from './constants/categories';
export type { CategoryInfo } from './constants/categories';
