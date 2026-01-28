/**
 * Card Component
 * Reusable terminal-styled card container
 */

import React from 'react';

interface CardProps {
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
  selected?: boolean;
  hoverable?: boolean;
}

export const Card: React.FC<CardProps> = ({
  children,
  className = '',
  onClick,
  selected = false,
  hoverable = false,
}) => {
  const baseClasses = 'bg-surface-dark/90 backdrop-blur-md border rounded-lg relative overflow-hidden';
  const interactiveClasses = hoverable || onClick 
    ? 'cursor-pointer hover:shadow-[0_0_20px_rgba(37,244,106,0.2)] hover:scale-[1.02] transition-all' 
    : '';
  const selectedClasses = selected ? 'border-primary' : 'border-primary/30';

  return (
    <div 
      className={`${baseClasses} ${interactiveClasses} ${selectedClasses} ${className}`}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
    >
      {/* Corner decorations */}
      <div className="absolute top-0 left-0 w-4 h-4 border-t-2 border-l-2 border-primary rounded-tl-lg pointer-events-none" />
      <div className="absolute top-0 right-0 w-4 h-4 border-t-2 border-r-2 border-primary rounded-tr-lg pointer-events-none" />
      <div className="absolute bottom-0 left-0 w-4 h-4 border-b-2 border-l-2 border-primary rounded-bl-lg pointer-events-none" />
      <div className="absolute bottom-0 right-0 w-4 h-4 border-b-2 border-r-2 border-primary rounded-br-lg pointer-events-none" />
      
      {children}
    </div>
  );
};

/**
 * Card Header Component
 */
interface CardHeaderProps {
  title: string;
  subtitle?: string;
  centered?: boolean;
}

export const CardHeader: React.FC<CardHeaderProps> = ({ title, subtitle, centered = false }) => (
  <div className={`mb-8 ${centered ? 'text-center' : ''}`}>
    <h1 className="text-primary text-4xl font-display font-black text-glow uppercase mb-2">
      {title}
    </h1>
    {subtitle && (
      <p className="text-primary/60 text-xs tracking-widest uppercase">{subtitle}</p>
    )}
  </div>
);
