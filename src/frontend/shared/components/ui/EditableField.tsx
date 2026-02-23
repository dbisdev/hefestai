/**
 * EditableField Component
 * Inline editable text/number field with cyberpunk aesthetics
 * Click to edit, blur or Enter to save
 */

import React, { useState, useRef, useEffect } from 'react';

export type EditableFieldVariant = 'primary' | 'danger' | 'warning';

export interface EditableFieldProps {
  /** Current value */
  value: string | number;
  /** Label to display */
  label?: string;
  /** Field type */
  type?: 'text' | 'number' | 'textarea';
  /** Number of rows for textarea */
  rows?: number;
  /** Color variant */
  variant?: EditableFieldVariant;
  /** Whether the field is disabled */
  disabled?: boolean;
  /** Callback when value changes */
  onChange: (value: string | number) => void;
  /** Custom className */
  className?: string;
}

const variantColors: Record<EditableFieldVariant, { text: string; border: string; bg: string; placeholder: string }> = {
  primary: {
    text: 'text-primary',
    border: 'border-primary/30 focus:border-primary',
    bg: 'bg-black/40',
    placeholder: 'placeholder:text-primary/20'
  },
  danger: {
    text: 'text-danger',
    border: 'border-danger/30 focus:border-danger',
    bg: 'bg-black/40',
    placeholder: 'placeholder:text-danger/20'
  },
  warning: {
    text: 'text-yellow-500',
    border: 'border-yellow-500/30 focus:border-yellow-500',
    bg: 'bg-black/40',
    placeholder: 'placeholder:text-yellow-500/20'
  }
};

export const EditableField: React.FC<EditableFieldProps> = ({
  value,
  label,
  type = 'text',
  rows = 2,
  variant = 'primary',
  disabled = false,
  onChange,
  className = ''
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState<string | number>(value);
  const inputRef = useRef<HTMLInputElement | HTMLTextAreaElement>(null);

  const colors = variantColors[variant];

  useEffect(() => {
    setEditValue(value);
  }, [value]);

  useEffect(() => {
    if (isEditing && inputRef.current) {
      inputRef.current.focus();
      inputRef.current.select?.();
    }
  }, [isEditing]);

  const handleStartEdit = () => {
    if (!disabled) {
      setIsEditing(true);
      setEditValue(value);
    }
  };

  const handleSave = () => {
    setIsEditing(false);
    if (editValue !== value) {
      onChange(editValue);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && type !== 'textarea') {
      e.preventDefault();
      handleSave();
    }
    if (e.key === 'Escape') {
      setEditValue(value);
      setIsEditing(false);
    }
  };

  const handleBlur = () => {
    handleSave();
  };

  if (isEditing) {
    if (type === 'textarea') {
      return (
        <textarea
          ref={inputRef as React.RefObject<HTMLTextAreaElement>}
          value={editValue}
          onChange={(e) => setEditValue(e.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={handleBlur}
          disabled={disabled}
          rows={rows}
          style={{ minHeight: '400px' }}
          className={`text-sm w-full ${colors.bg} border ${colors.border} ${colors.text} p-2 focus:outline-none resize-none font-mono ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
        />
      );
    }

    return (
      <input
        ref={inputRef as React.RefObject<HTMLInputElement>}
        type={type}
        value={editValue}
        onChange={(e) => setEditValue(type === 'number' ? Number(e.target.value) : e.target.value)}
        onKeyDown={handleKeyDown}
        onBlur={handleBlur}
        disabled={disabled}
        className={`w-full ${colors.bg} border ${colors.border} ${colors.text} p-2 text-sm focus:outline-none font-mono ${disabled ? 'opacity-50 cursor-not-allowed' : ''} ${className}`}
      />
    );
  }

  return (
    <div
      onClick={disabled ? undefined : handleStartEdit}
      className={`
        ${colors.bg} border ${colors.border} p-2 
        ${disabled ? 'cursor-default' : 'cursor-text hover:border-opacity-100'} 
        transition-all group
        ${disabled ? 'opacity-50' : ''}
        ${className}
      `}
    >
      {label && (
        <p className={`text-sm ${colors.text}/40 uppercase mb-1`}>
          {label}
        </p>
      )}
      <p className={`${colors.text} text-sm font-mono whitespace-pre-wrap break-words ${!value && !disabled ? colors.placeholder : ''} ${className}`}>
        {value || (disabled ? '' : (type === 'textarea' ? 'Click para editar...' : '---'))}
      </p>
    </div>
  );
};

export default EditableField;
