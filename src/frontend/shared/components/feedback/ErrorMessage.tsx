/**
 * Error Message Component
 * Inline error display with terminal styling
 */

import React from 'react';

interface ErrorMessageProps {
  message: string;
  className?: string;
}

export const ErrorMessage: React.FC<ErrorMessageProps> = ({ message, className = '' }) => {
  if (!message) return null;
  
  return (
    <p className={`text-danger text-[10px] uppercase font-bold text-center animate-pulse ${className}`}>
      {message}
    </p>
  );
};
