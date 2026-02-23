/**
 * Solar System Visualization Component
 * Renders an animated orbital view of a solar system with planets
 * Extracted from SolarSystemGeneratorPage for reusability in modals
 */

import React, { useState } from 'react';
import type { PlanetData } from '@core/types';
import { PlanetCard } from './PlanetCard';

interface SolarSystemVisualizationProps {
  spectralClass?: string;
  planets?: PlanetData[];
  size?: 'sm' | 'md' | 'lg';
  showPlanetList?: boolean;
  backgroundImage?: string;
}

const SIZE_MAP = {
  sm: 'w-32 h-32',
  md: 'w-48 h-48',
  lg: 'w-48 h-48 md:w-64 md:h-64'
} as const;

const STAR_SIZE_MAP = {
  sm: 'w-4 h-4',
  md: 'w-6 h-6',
  lg: 'w-8 h-8 md:w-10 md:h-10'
} as const;

const getStarStyle = (spectralClass?: string): string => {
  switch (spectralClass) {
    case 'M': return 'bg-red-500 shadow-[0_0_30px_red]';
    case 'O': return 'bg-blue-400 shadow-[0_0_40px_cyan]';
    default: return 'bg-yellow-200 shadow-[0_0_40px_yellow]';
  }
};

const getPlanetColor = (type?: string): string => {
  if (type?.includes('Gas')) return 'bg-orange-400';
  if (type?.includes('Ice')) return 'bg-cyan-400';
  return 'bg-primary';
};

export const SolarSystemVisualization: React.FC<SolarSystemVisualizationProps> = ({
  spectralClass,
  planets = [],
  size = 'md',
  showPlanetList = false,
  backgroundImage
}) => {
  const [hoveredPlanetIndex, setHoveredPlanetIndex] = useState<number | null>(null);

  return (
    <div className="space-y-6">
      <div className="relative border border-primary/30 bg-surface-dark/30 overflow-hidden">
        {/* Background image */}
        {backgroundImage && (
          <>
            <div className="absolute inset-0 z-0 pointer-events-none">
              <img 
                src={backgroundImage} 
                alt="" 
                className="w-full h-full object-cover grayscale opacity-40" 
              />
              <div className="absolute inset-0 bg-gradient-to-b from-black/60 via-black/40 to-black/90"></div>
            </div>
          </>
        )}
        
        {/* Grid pattern overlay */}
        <div 
          className="absolute inset-0 opacity-10 pointer-events-none z-0" 
          style={{ 
            backgroundImage: 'linear-gradient(rgba(37, 244, 106, 0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(37, 244, 106, 0.1) 1px, transparent 1px)', 
            backgroundSize: '40px 40px' 
          }} 
        />

        {/* Orbital visualization */}
        <div className="relative z-10 flex justify-center p-6">
          <div className={`relative ${SIZE_MAP[size]} flex items-center justify-center`}>
            <div className={`absolute ${STAR_SIZE_MAP[size]} rounded-full animate-pulse transition-all duration-1000 z-10 ${getStarStyle(spectralClass)}`} />

            {planets.map((planet, i) => {
              const isHovered = hoveredPlanetIndex === i;

              return (
                <div
                  key={i}
                  className={`absolute rounded-full transition-all duration-300 ${isHovered
                    ? 'border-2 border-primary shadow-[0_0_15px_rgba(37,244,106,0.5)]'
                    : 'border border-primary/30'
                  }`}
                  style={{
                    width: `${50 + i * 25}px`,
                    height: `${50 + i * 25}px`,
                    animation: isHovered ? 'none' : `spin ${10 + i * 5}s linear infinite`
                  }}
                >
                  <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 group cursor-pointer">
                    <div className={`rounded-full transition-all duration-300 ${getPlanetColor(planet.type)} ${isHovered
                      ? 'w-4 h-4 shadow-[0_0_20px_currentColor,0_0_40px_currentColor] ring-2 ring-white/50'
                      : 'w-2 h-2 shadow-[0_0_5px_#25f46a]'
                    }`} />
                    <span className={`absolute left-5 top-0 text-[8px] text-primary whitespace-nowrap bg-black/90 px-2 py-0.5 border border-primary/30 font-bold z-20 transition-opacity ${isHovered ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'
                    }`}>
                      {planet.name || `PLN-${(i + 1).toString().padStart(2, '0')}`}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {showPlanetList && planets.length > 0 && (
        <div className="grid gap-3 md:grid-cols-2">
          {planets.map((planet, idx) => (
            <PlanetCard
              key={idx}
              planet={planet}
              index={idx}
              onHover={setHoveredPlanetIndex}
              isHighlighted={hoveredPlanetIndex === idx}
            />
          ))}
        </div>
      )}

      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
};

export default SolarSystemVisualization;
