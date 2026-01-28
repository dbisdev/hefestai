/**
 * Loading Spinner Component
 * Reusable loading indicator with terminal styling
 */

import React from 'react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  message?: string;
  fullScreen?: boolean;
}

const sizeClasses = {
  sm: 'text-2xl',
  md: 'text-4xl',
  lg: 'text-6xl',
};

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  message,
  fullScreen = false,
}) => {
  const content = (
    <div className="flex flex-col items-center justify-center text-primary animate-pulse font-mono uppercase tracking-[0.3em]">
      <span className={`material-icons ${sizeClasses[size]} mb-4`}>settings_input_antenna</span>
      {message && <span className="text-sm">{message}</span>}
    </div>
  );

  if (fullScreen) {
    return (
      <div className="fixed inset-0 bg-background-dark flex items-center justify-center z-50">
        {content}
      </div>
    );
  }

  return (
    <div className="flex items-center justify-center h-64">
      {content}
    </div>
  );
};

/**
 * Inline loading indicator
 */
export const InlineLoader: React.FC<{ className?: string }> = ({ className = '' }) => (
  <span className={`material-icons animate-spin text-sm ${className}`}>sync</span>
);

/**
 * Progress bar loading indicator
 */
interface ProgressLoaderProps {
  message?: string;
}

export const ProgressLoader: React.FC<ProgressLoaderProps> = ({ message }) => (
  <div className="flex flex-col items-center justify-center">
    <div className="w-1/2 h-1 bg-primary/20 relative overflow-hidden mb-2">
      <div className="absolute inset-0 bg-primary animate-[scan_2s_linear_infinite]" />
    </div>
    {message && (
      <span className="text-primary text-[10px] animate-pulse uppercase">{message}</span>
    )}
  </div>
);
