/**
 * Planet Card Component
 * Displays detailed information about a planet in a solar system
 * Extracted from SolarSystemGeneratorPage for reusability
 */

import React from 'react';
import type { PlanetData } from '@core/types';

interface PlanetCardProps {
  planet: PlanetData;
  index: number;
  onHover?: (index: number | null) => void;
  isHighlighted?: boolean;
}

const getPlanetTypeColor = (type: string): string => {
  if (type.includes('Gas')) return 'border-orange-500/50 bg-orange-500/5';
  if (type.includes('Ice')) return 'border-cyan-500/50 bg-cyan-500/5';
  if (type.includes('Terrestrial')) return 'border-primary/50 bg-primary/5';
  return 'border-primary/30 bg-primary/5';
};

const getPlanetTypeIcon = (type: string): string => {
  if (type.includes('Gas')) return 'blur_on';
  if (type.includes('Ice')) return 'ac_unit';
  return 'public';
};

const getTemperatureColor = (temp: string): string => {
  const lower = (temp || '').toLowerCase();
  if (lower.includes('hot') || lower.includes('caliente')) return 'text-red-400';
  if (lower.includes('cold') || lower.includes('frozen') || lower.includes('frio')) return 'text-cyan-400';
  return 'text-yellow-400';
};

const getPlanetIconColor = (type: string): string => {
  if (type.includes('Gas')) return 'text-orange-400';
  if (type.includes('Ice')) return 'text-cyan-400';
  return 'text-primary';
};

export const PlanetCard: React.FC<PlanetCardProps> = ({ 
  planet, 
  index, 
  onHover, 
  isHighlighted 
}) => {
  const planetType = planet.type || 'Unknown';

  return (
    <div
      className={`border p-3 backdrop-blur-sm transition-all cursor-pointer ${getPlanetTypeColor(planetType)} ${isHighlighted
        ? 'scale-[1.02] ring-2 ring-primary shadow-[0_0_20px_rgba(37,244,106,0.3)]'
        : 'hover:scale-[1.02]'
      }`}
      onMouseEnter={() => onHover?.(index)}
      onMouseLeave={() => onHover?.(null)}
    >
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center gap-2">
          <span className={`material-icons text-lg ${getPlanetIconColor(planetType)}`}>
            {getPlanetTypeIcon(planetType)}
          </span>
          <div>
            <h4 className="text-sm font-bold text-white">
              {planet.name || `Planeta ${index + 1}`}
            </h4>
            <p className="text-xs text-primary/60 uppercase">{planetType}</p>
          </div>
        </div>
        <span className="text-xs bg-black/50 px-2 py-0.5 border border-primary/20 text-primary/70">
          ORB-{(planet.orbital_position ?? index + 1).toString().padStart(2, '0')}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-2 text-xs mb-2">
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Tamaño</span>
          <span className="text-white font-mono">
            {planet.size != null ? planet.size.toLocaleString() : '—'} km
          </span>
        </div>
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Gravedad</span>
          <span className="text-white font-mono">{planet.gravity ?? '—'} g</span>
        </div>
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Temperatura</span>
          <span className={`font-mono ${getTemperatureColor(planet.temperature)}`}>
            {planet.temperature || '—'}
          </span>
        </div>
        <div className="bg-black/30 p-1.5 border-l-2 border-primary/30">
          <span className="text-primary/50 uppercase block">Atmósfera</span>
          <span className="text-white/80 text-[8px] leading-tight block">
            {planet.atmosphere || '—'}
          </span>
        </div>
      </div>

      {planet.features && (
        <div className="text-xs mb-2">
          <span className="text-primary/50 uppercase">Características: </span>
          <span className="text-white/70">{planet.features}</span>
        </div>
      )}

      {planet.resources && (
        <div className="text-xs bg-black/40 p-1.5 border border-primary/10">
          <span className="text-yellow-500/70 uppercase flex items-center gap-1">
            <span className="material-icons text-xs">inventory_2</span>
            Recursos:
          </span>
          <span className="text-white/80">{planet.resources}</span>
        </div>
      )}
    </div>
  );
};

export default PlanetCard;
