/**
 * Grid Background Component
 * Reusable retro grid background effect
 */

import React from 'react';

interface GridBackgroundProps {
  opacity?: number;
  size?: number;
  color?: string;
}

export const GridBackground: React.FC<GridBackgroundProps> = ({
  opacity = 0.1,
  size = 40,
  color = '#25f46a',
}) => (
  <div 
    className="absolute inset-0 pointer-events-none"
    style={{ 
      opacity,
      backgroundImage: `linear-gradient(${color} 1px, transparent 1px), linear-gradient(90deg, ${color} 1px, transparent 1px)`, 
      backgroundSize: `${size}px ${size}px` 
    }} 
  />
);
