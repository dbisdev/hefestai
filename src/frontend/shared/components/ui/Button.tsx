/**
 * Button Component
 * Reusable terminal-styled button with variants
 */

import React from 'react';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  isLoading?: boolean;
  icon?: string;
  fullWidth?: boolean;
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-primary text-black hover:bg-white shadow-[0_0_10px_#25f46a44]',
  secondary: 'bg-primary/10 border border-primary text-primary hover:bg-primary hover:text-black',
  danger: 'border border-danger/60 text-danger hover:bg-danger hover:text-white',
  ghost: 'text-primary/60 hover:text-primary',
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-xs sm:text-sm',
  md: 'h-10 px-4 text-sm md:text-md',
  lg: 'h-12 px-6 text-md lg:text-lg',
};

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'secondary',
  size = 'md',
  isLoading = false,
  icon,
  fullWidth = false,
  disabled,
  className = '',
  ...props
}) => {
  const baseClasses = 'cursor-pointer font-bold uppercase tracking-widest transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed';
  
  return (
    <button
      className={`${baseClasses} ${variantClasses[variant]} ${sizeClasses[size]} ${fullWidth ? 'w-full' : ''} ${className}`}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? (
        <span className="material-icons animate-spin text-sm">sync</span>
      ) : icon ? (
        <span className="material-icons text-sm">{icon}</span>
      ) : null}
      {children}
    </button>
  );
};
