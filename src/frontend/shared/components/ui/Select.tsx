/**
 * Select Component
 * Reusable terminal-styled select dropdown
 */

import React from 'react';

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'children'> {
  label?: string;
  options: SelectOption[];
  placeholder?: string;
  error?: string;
  icon?: string;
}

export const Select: React.FC<SelectProps> = ({
  label,
  options,
  placeholder,
  error,
  icon,
  className = '',
  id,
  ...props
}) => {
  const selectId = id || props.name;

  return (
    <div className="w-full">
      {label && (
        <label 
          htmlFor={selectId}
          className="text-primary text-[10px] uppercase tracking-widest mb-2 flex items-center gap-2"
        >
          {icon && <span className="material-icons text-sm">{icon}</span>}
          {label}
        </label>
      )}
      <select
        id={selectId}
        className={`w-full bg-surface-dark border border-primary/30 text-white h-10 px-4 
          focus:ring-primary focus:border-primary text-sm uppercase
          disabled:opacity-50 disabled:cursor-not-allowed
          ${error ? 'border-danger/50 focus:border-danger' : ''}
          ${className}`}
        {...props}
      >
        {placeholder && (
          <option value="">{placeholder}</option>
        )}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && (
        <p className="text-danger text-[10px] uppercase font-bold mt-1 animate-pulse">
          {error}
        </p>
      )}
    </div>
  );
};
