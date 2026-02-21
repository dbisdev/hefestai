/**
 * Empty State Component
 * Single Responsibility: Display empty state with optional action
 * Reusable across all pages that need empty state UI
 */

import React from 'react';
import { Button } from './Button';

interface EmptyStateAction {
  label: string;
  onClick: () => void;
  icon?: string;
}

interface EmptyStateProps {
  icon: string;
  title: string;
  description?: string;
  action?: EmptyStateAction;
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
  className = '',
}) => {
  return (
    <div 
      className={`flex flex-col items-center justify-center py-12 px-4 ${className}`}
      role="status"
      aria-label={title}
    >
      <span className="material-icons text-6xl text-primary/30 mb-4">{icon}</span>
      <h3 className="text-primary text-lg font-display uppercase tracking-wider mb-2">
        {title}
      </h3>
      {description && (
        <p className="text-primary/60 text-sm text-center max-w-md mb-4">
          {description}
        </p>
      )}
      {action && (
        <Button
          variant="primary"
          size="sm"
          onClick={action.onClick}
          icon={action.icon}
        >
          {action.label}
        </Button>
      )}
    </div>
  );
};
