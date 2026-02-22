/**
 * Input Component
 * Reusable terminal-styled input field
 */

import React from 'react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: string;
}

export const Input: React.FC<InputProps> = ({
  label,
  error,
  icon,
  className = '',
  id,
  ...props
}) => {
  const inputId = id || props.name;

  return (
    <div className="w-full">
      {label && (
        <label 
          htmlFor={inputId}
          className="block text-primary/80 text-sm uppercase mb-1 tracking-wider"
        >
          {icon && <span className="material-icons text-xs mr-1 align-middle">{icon}</span>}
          {label}
        </label>
      )}
      <input
        id={inputId}
        className={`w-full bg-black/40 border border-primary/30 rounded p-2 text-primary 
          focus:outline-none focus:border-primary transition-colors text-md md:text-sm
          placeholder:text-primary/30 disabled:opacity-50 disabled:cursor-not-allowed
          ${error ? 'border-danger/50 focus:border-danger' : ''}
          ${className}`}
        {...props}
      />
      {error && (
        <p className="text-danger text-[10px] uppercase font-bold mt-1 animate-pulse">
          {error}
        </p>
      )}
    </div>
  );
};
