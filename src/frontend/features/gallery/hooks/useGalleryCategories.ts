/**
 * useGalleryCategories Hook
 * Single Responsibility: Manage category selection and transitions
 * KISS: Simple state management without entity logic
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import type { EntityCategory } from '@core/types';
import { 
  ENTITY_CATEGORIES, 
  TEMPLATE_ENTITY_TYPES,
  GENERATED_ENTITY_TYPES, 
  type CategoryInfo 
} from '../constants/categories';
import { TRANSITION_OUT_DURATION, TRANSITION_IN_DURATION } from '@core/config/constants';

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

  /** Refs to track pending timeouts so they can be cleared on unmount or rapid re-calls */
  const outerTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const innerTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  /** Clear pending timeouts on unmount to prevent state updates on unmounted component */
  useEffect(() => {
    return () => {
      if (outerTimeoutRef.current) clearTimeout(outerTimeoutRef.current);
      if (innerTimeoutRef.current) clearTimeout(innerTimeoutRef.current);
    };
  }, []);

  const setCategory = useCallback((newCategory: EntityCategory) => {
    if (newCategory === activeCategory || transitionStatus !== 'idle') return;

    // Clear any pending transitions before starting a new one
    if (outerTimeoutRef.current) clearTimeout(outerTimeoutRef.current);
    if (innerTimeoutRef.current) clearTimeout(innerTimeoutRef.current);

    setTransitionStatus('out');
    setActiveCategory(newCategory);

    outerTimeoutRef.current = setTimeout(() => {
      setDisplayCategory(newCategory);
      setTransitionStatus('in');

      innerTimeoutRef.current = setTimeout(() => {
        setTransitionStatus('idle');
      }, TRANSITION_IN_DURATION);
    }, TRANSITION_OUT_DURATION);
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
