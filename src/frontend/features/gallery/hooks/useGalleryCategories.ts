/**
 * useGalleryCategories Hook
 * Single Responsibility: Manage category selection and transitions
 * KISS: Simple state management without entity logic
 */

import { useState, useCallback } from 'react';
import type { EntityCategory } from '@core/types';
import { 
  ENTITY_CATEGORIES, 
  TEMPLATE_ENTITY_TYPES,
  GENERATED_ENTITY_TYPES, 
  type CategoryInfo 
} from '../constants/categories';

type TransitionStatus = 'idle' | 'out' | 'in';

interface UseGalleryCategoriesReturn {
  activeCategory: EntityCategory;
  displayCategory: EntityCategory;
  transitionStatus: TransitionStatus;
  setCategory: (category: EntityCategory) => void;
  categories: CategoryInfo[];
  labCategories: CategoryInfo[];
  templateCategories: CategoryInfo[];
  getCategoryLabel: (category: EntityCategory) => string;
}

export function useGalleryCategories(
  initialCategory: EntityCategory = 'character'
): UseGalleryCategoriesReturn {
  const [activeCategory, setActiveCategory] = useState<EntityCategory>(initialCategory);
  const [displayCategory, setDisplayCategory] = useState<EntityCategory>(initialCategory);
  const [transitionStatus, setTransitionStatus] = useState<TransitionStatus>('idle');

  const setCategory = useCallback((newCategory: EntityCategory) => {
    if (newCategory === activeCategory || transitionStatus !== 'idle') return;

    setTransitionStatus('out');
    setActiveCategory(newCategory);

    setTimeout(() => {
      setDisplayCategory(newCategory);
      setTransitionStatus('in');

      setTimeout(() => {
        setTransitionStatus('idle');
      }, 500);
    }, 400);
  }, [activeCategory, transitionStatus]);

  const getCategoryLabel = useCallback((category: EntityCategory): string => {
    const found = ENTITY_CATEGORIES.find((c) => c.id === category);
    return found?.label || category;
  }, []);

  return {
    activeCategory,
    displayCategory,
    transitionStatus,
    setCategory,
    categories: ENTITY_CATEGORIES,
    labCategories: GENERATED_ENTITY_TYPES,
    templateCategories: TEMPLATE_ENTITY_TYPES,
    getCategoryLabel,
  };
}
