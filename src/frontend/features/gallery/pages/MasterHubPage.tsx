/**
 * Master Hub Page
 * Landing page for Master users after login.
 * Displays 3 big panels for quick navigation to main sections.
 * Cyberpunk terminal aesthetics.
 */

import React from 'react';
import { GridBackground } from '@shared/components/layout';
import { useAuth } from '@core/context';
import { Screen } from '@core/types';

interface MasterHubPageProps {
  /** Handler for navigating to other screens */
  onNavigate: (screen: Screen) => void;
  /** Handler for logging out */
  onLogout: () => void;
}

/** Hub panel configuration */
interface HubPanel {
  id: Screen;
  title: string;
  subtitle: string;
  icon: string;
  description: string;
  color: string;
  glowColor: string;
}

/** Available panels for Master users */
const HUB_PANELS: HubPanel[] = [
  {
    id: Screen.GALLERY,
    title: 'GALERÍA',
    subtitle: 'Entity Repository',
    icon: 'collections',
    description: 'Accede a todas tus entidades generadas: personajes, NPCs, vehículos, sistemas solares y más.',
    color: 'primary',
    glowColor: 'rgba(37, 244, 106, 0.3)',
  },
  {
    id: Screen.CAMPAIGN_GEN,
    title: 'CAMPAÑAS',
    subtitle: 'Campaign Manager',
    icon: 'shield',
    description: 'Crea y administra campañas, invita jugadores y organiza tus partidas.',
    color: 'purple-400',
    glowColor: 'rgba(192, 132, 252, 0.3)',
  },
  {
    id: Screen.GAME_SYSTEMS,
    title: 'SISTEMAS',
    subtitle: 'Game Systems',
    icon: 'sports_esports',
    description: 'Gestiona los sistemas de juego disponibles para tus campañas y entidades.',
    color: 'cyan-400',
    glowColor: 'rgba(34, 211, 238, 0.3)',
  },
];

/**
 * MasterHubPage Component
 * Provides a hub interface for Master users to navigate to main sections.
 */
export const MasterHubPage: React.FC<MasterHubPageProps> = ({ onNavigate, onLogout }) => {
  const { user } = useAuth();

  return (
    <div className="flex flex-col min-h-screen bg-background-dark font-mono relative overflow-hidden">
      <GridBackground opacity={0.08} size={50} />
      
      {/* Header */}
      <header className="relative z-10 flex justify-between items-center p-6 md:p-8 border-b border-primary/20">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 border-2 border-primary flex items-center justify-center bg-primary/10">
            <span className="material-icons text-primary">hub</span>
          </div>
          <div>
            <h1 className="text-2xl md:text-3xl font-display uppercase tracking-widest text-primary text-glow font-bold">
              MASTER_HUB
            </h1>
            <p className="text-xs text-primary/60 uppercase tracking-wider">
              Centro de control // {user?.email}
            </p>
          </div>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="hidden md:flex flex-col text-right text-xs text-primary/60">
            <span>ROL: MASTER</span>
            <span>STATUS: ONLINE</span>
          </div>
          <button
            onClick={onLogout}
            className="border border-red-500 px-4 py-2 text-xs uppercase hover:bg-red-500 hover:text-black transition-colors text-red-500 font-bold"
          >
            LOGOUT
          </button>
        </div>
      </header>

      {/* Main Content - Hub Panels */}
      <main className="relative z-10 flex-1 flex items-center justify-center p-6 md:p-12">
        <div className="w-full max-w-6xl">
          {/* Welcome Message */}
          <div className="text-center mb-8 md:mb-12">
            <p className="text-primary/60 text-sm uppercase tracking-[0.3em] mb-2">
              Bienvenido, {user?.username || 'Master'}
            </p>
            <h2 className="text-xl md:text-2xl text-primary/80 font-display uppercase tracking-widest">
              Selecciona un módulo para comenzar
            </h2>
          </div>

          {/* Panels Grid */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 md:gap-8">
            {HUB_PANELS.map((panel) => (
              <button
                key={panel.id}
                onClick={() => onNavigate(panel.id)}
                className={`group relative flex flex-col items-center p-8 md:p-10 border-2 border-${panel.color}/30 bg-black/60 
                  hover:border-${panel.color} hover:bg-${panel.color}/10 transition-all duration-300
                  hover:shadow-[0_0_30px_${panel.glowColor}] active:scale-[0.98]`}
                style={{
                  boxShadow: `inset 0 0 20px rgba(0,0,0,0.5)`,
                }}
              >
                {/* Decorative corners */}
                <div className={`absolute top-0 left-0 w-4 h-4 border-t-2 border-l-2 border-${panel.color}/50`} />
                <div className={`absolute top-0 right-0 w-4 h-4 border-t-2 border-r-2 border-${panel.color}/50`} />
                <div className={`absolute bottom-0 left-0 w-4 h-4 border-b-2 border-l-2 border-${panel.color}/50`} />
                <div className={`absolute bottom-0 right-0 w-4 h-4 border-b-2 border-r-2 border-${panel.color}/50`} />

                {/* Icon */}
                <div className={`w-20 h-20 md:w-24 md:h-24 border-2 border-${panel.color}/40 flex items-center justify-center 
                  bg-${panel.color}/5 mb-6 group-hover:border-${panel.color} group-hover:bg-${panel.color}/20 transition-all`}>
                  <span className={`material-icons text-4xl md:text-5xl text-${panel.color}/70 group-hover:text-${panel.color} transition-colors`}>
                    {panel.icon}
                  </span>
                </div>

                {/* Title */}
                <h3 className={`text-xl md:text-2xl font-display uppercase tracking-widest text-${panel.color}/80 
                  group-hover:text-${panel.color} group-hover:text-glow transition-all mb-1`}>
                  {panel.title}
                </h3>

                {/* Subtitle */}
                <p className={`text-[10px] text-${panel.color}/40 uppercase tracking-[0.2em] mb-4`}>
                  {panel.subtitle}
                </p>

                {/* Description */}
                <p className={`text-xs text-${panel.color}/50 text-center leading-relaxed group-hover:text-${panel.color}/70 transition-colors`}>
                  {panel.description}
                </p>

                {/* Bottom indicator */}
                <div className={`mt-6 flex items-center gap-2 text-${panel.color}/40 group-hover:text-${panel.color} transition-colors`}>
                  <span className="text-[10px] uppercase tracking-widest">Acceder</span>
                  <span className="material-icons text-sm group-hover:translate-x-1 transition-transform">
                    arrow_forward
                  </span>
                </div>

                {/* Hover glow effect */}
                <div className={`absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none`}
                  style={{
                    background: `radial-gradient(ellipse at center, ${panel.glowColor} 0%, transparent 70%)`,
                  }}
                />
              </button>
            ))}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="relative z-10 p-4 border-t border-primary/20 flex justify-between text-[10px] md:text-xs text-primary/40 uppercase">
        <div className="flex gap-4">
          <span>HefestAI v3.0</span>
          <span className="hidden md:inline">// Master Control Panel</span>
        </div>
        <div className="flex gap-4 animate-pulse">
          <span>Sistema operativo</span>
          <span>[ |||||||||| ] OK</span>
        </div>
      </footer>
    </div>
  );
};
