/**
 * Category Button Component
 * Single Responsibility: Render a single category navigation button
 * Configurable for availability (template-based) and visual variants
 */

import React, { KeyboardEvent } from 'react';
import type { CategoryInfo } from '../constants/categories';

export interface CategoryButtonProps {
  category: CategoryInfo;
  isActive: boolean;
  isAvailable?: boolean;
  isDisabled?: boolean;
  variant?: 'primary' | 'cyan';
  onClick: () => void;
  onKeyDown?: (e: KeyboardEvent<HTMLButtonElement>) => void;
  index: number;
}

const variantStyles = {
  primary: {
    active: 'border-l-4 border-l-primary border-y-primary/30 border-r-primary/30 bg-primary/20 shadow-[inset_0_0_15px_rgba(37,244,106,0.1)]',
    available: 'border-primary/30 hover:border-primary hover:bg-primary/5 bg-surface-dark',
    unavailable: 'border-primary/15 bg-surface-dark/50 text-primary/40 cursor-not-allowed opacity-60',
    iconActive: 'text-primary',
    iconAvailable: 'text-primary/60',
    iconUnavailable: 'text-primary/30',
    labelActive: 'text-primary text-glow',
    labelAvailable: 'text-primary/70',
    labelUnavailable: 'text-primary/40',
    pulseBg: 'bg-primary/5',
    pulseDot: 'bg-primary',
    headerBg: 'bg-primary/5',
    headerBorder: 'border-primary/50',
    headerText: 'text-primary',
  },
  cyan: {
    active: 'border-l-4 border-l-cyan-500 border-y-cyan-500/30 border-r-cyan-500/30 bg-cyan-500/20 shadow-[inset_0_0_15px_rgba(37,244,106,0.1)]',
    available: 'border-cyan-500/30 hover:border-cyan-500 hover:bg-cyan-500/5 bg-surface-dark',
    unavailable: 'border-cyan-500/15 bg-surface-dark/50 text-cyan-500/40 cursor-not-allowed opacity-60',
    iconActive: 'text-cyan-500',
    iconAvailable: 'text-cyan-500/60',
    iconUnavailable: 'text-cyan-500/30',
    labelActive: 'text-cyan-500 text-glow',
    labelAvailable: 'text-cyan-500/70',
    labelUnavailable: 'text-cyan-500/40',
    pulseBg: 'bg-cyan-500/5',
    pulseDot: 'bg-cyan-500',
    headerBg: 'bg-cyan-500/5',
    headerBorder: 'border-cyan-500/50',
    headerText: 'text-cyan-500',
  },
};

export const CategoryButton: React.FC<CategoryButtonProps> = ({
  category,
  isActive,
  isAvailable = true,
  isDisabled = false,
  variant = 'primary',
  onClick,
  onKeyDown,
  index,
}) => {
  const styles = variantStyles[variant];

  const getButtonClass = () => {
    if (isActive) return styles.active;
    if (!isAvailable) return styles.unavailable;
    return styles.available;
  };

  const getIconClass = () => {
    if (isActive) return styles.iconActive;
    if (!isAvailable) return styles.iconUnavailable;
    return styles.iconAvailable;
  };

  const getLabelClass = () => {
    if (isActive) return styles.labelActive;
    if (!isAvailable) return styles.labelUnavailable;
    return styles.labelAvailable;
  };

  return (
    <button
      key={category.id}
      role="tab"
      aria-selected={isActive}
      aria-controls="entity-grid"
      tabIndex={isActive ? 0 : -1}
      onClick={onClick}
      onKeyDown={onKeyDown}
      disabled={isDisabled}
      className={`cursor-pointer group flex items-center gap-3 p-3 border transition-all clip-tech-tl relative overflow-hidden ${getButtonClass()} ${isDisabled ? 'opacity-50' : ''}`}
    >
      {isActive && (
        <div className={`absolute inset-0 ${styles.pulseBg} animate-pulse pointer-events-none`}></div>
      )}
      <span className={`material-icons text-xl ${getIconClass()}`}>
        {category.icon}
      </span>
      <span className={`hidden md:inline text-xs font-bold tracking-widest ${getLabelClass()}`}>
        {category.label}
      </span>
      {!isAvailable && (
        <span className="absolute right-1 top-1/2 -translate-y-1/2 text-[6px] text-yellow-500 border border-yellow-500/50 px-0.5 uppercase bg-yellow-500/10">
          NO DISP
        </span>
      )}
      {isActive && (
        <div className={`absolute right-2 top-1/2 -translate-y-1/2 w-1 h-1 ${styles.pulseDot} rounded-full animate-ping`}></div>
      )}
    </button>
  );
};

export default CategoryButton;
