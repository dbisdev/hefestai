/**
 * Category Sidebar Component
 * Single Responsibility: Render category navigation
 */

import React, { KeyboardEvent } from 'react';
import type { EntityCategory } from '@core/types';
import type { CategoryInfo } from '../constants/categories';

interface CategorySidebarProps {
  categories: CategoryInfo[];
  labCategories: CategoryInfo[];
  activeCategory: EntityCategory;
  onSelect: (category: EntityCategory) => void;
  entityCounts?: Record<EntityCategory, number>;
  showLabs?: boolean;
}

export const CategorySidebar: React.FC<CategorySidebarProps> = ({
  categories,
  labCategories,
  activeCategory,
  onSelect,
  entityCounts,
  showLabs = false,
}) => {
  const handleKeyDown = (e: KeyboardEvent<HTMLButtonElement>, index: number, items: CategoryInfo[]) => {
    let newIndex = index;

    switch (e.key) {
      case 'ArrowUp':
        e.preventDefault();
        newIndex = index === 0 ? items.length - 1 : index - 1;
        break;
      case 'ArrowDown':
        e.preventDefault();
        newIndex = index === items.length - 1 ? 0 : index + 1;
        break;
      case 'Home':
        e.preventDefault();
        newIndex = 0;
        break;
      case 'End':
        e.preventDefault();
        newIndex = items.length - 1;
        break;
      default:
        return;
    }

    const buttons = e.currentTarget.parentElement?.querySelectorAll('button');
    buttons?.[newIndex]?.focus();
    onSelect(items[newIndex].id);
  };

  const renderCategory = (cat: CategoryInfo, index: number, items: CategoryInfo[]) => {
    const count = entityCounts?.[cat.id] ?? 0;
    const isActive = activeCategory === cat.id;

    return (
      <button
        key={cat.id}
        onClick={() => onSelect(cat.id)}
        onKeyDown={(e) => handleKeyDown(e, index, items)}
        className={`w-full flex items-center gap-2 px-3 py-2 text-left text-xs uppercase tracking-wider
          transition-all duration-200 border-l-2 ${
            isActive
              ? 'bg-primary/10 border-primary text-primary'
              : 'border-transparent text-primary/50 hover:bg-primary/5 hover:text-primary/70'
          }`}
        role="tab"
        aria-selected={isActive}
        aria-label={`${cat.label}${count > 0 ? `, ${count} entidades` : ''}`}
        tabIndex={isActive ? 0 : -1}
      >
        <span className="material-icons text-sm">{cat.icon}</span>
        <span className="flex-1">{cat.label}</span>
        {count > 0 && (
          <span className="text-[10px] text-primary/40">{count}</span>
        )}
      </button>
    );
  };

  return (
    <nav
      className="flex flex-col"
      role="tablist"
      aria-label="Categorías de entidades"
    >
      <div className="text-[10px] text-primary/30 uppercase tracking-widest px-3 py-2 border-b border-primary/10">
        Entidades
      </div>
      {categories.map((cat, index) => renderCategory(cat, index, categories))}
      
      {showLabs && labCategories.length > 0 && (
        <>
          <div className="text-[10px] text-primary/30 uppercase tracking-widest px-3 py-2 mt-2 border-t border-primary/10">
            Labs
          </div>
          {labCategories.map((cat, index) => renderCategory(cat, index, labCategories))}
        </>
      )}
    </nav>
  );
};
