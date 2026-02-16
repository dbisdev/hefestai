/**
 * Master Hub Page
 * Landing page for Master users after login.
 * Displays 3 big panels for quick navigation to main sections.
 * Cyberpunk terminal aesthetics with clean, minimal animations.
 */

import React from 'react';
import { TerminalLayout } from '@shared/components/layout';
import { Screen } from '@core/types';

/**
 * Props for the MasterHubPage component
 */
interface MasterHubPageProps {
  /** Handler for navigating to other screens */
  onNavigate: (screen: Screen) => void;
  /** Handler for logging out */
  onLogout: () => void;
}

/**
 * Hub panel configuration for navigation cards
 */
interface HubPanel {
  /** Screen identifier for navigation */
  id: Screen;
  /** Display title */
  title: string;
  /** Secondary description */
  subtitle: string;
  /** Material icon name */
  icon: string;
  /** Border color class */
  color: string;
  /** Detailed description text */
  description: string;
}

/**
 * Available panels for Master users
 * Each panel represents a main section of the application
 */
const HUB_PANELS: HubPanel[] = [
  {
    id: Screen.CAMPAIGN_LIST,
    title: 'Campañas',
    subtitle: 'Gestión de crónicas y sesiones activas',
    icon: 'auto_stories',
    color: 'border-primary/20',
    description: 'Crea y administra tus hilos narrativos. Controla el progreso de los operativos en tiempo real.'
  },
  {
    id: Screen.INVITATIONS,
    title: 'Invitaciones',
    subtitle: 'Códigos de acceso y unión a campañas',
    icon: 'mail',
    color: 'border-primary/20',
    description: 'Únete a campañas usando códigos de acceso. Comparte tu código de Master con nuevos jugadores.'
  },
  {
    id: Screen.GAME_SYSTEMS,
    title: 'Sistemas de Juego',
    subtitle: 'Configuración de reglas y mecánicas',
    icon: 'settings_suggest',
    color: 'border-primary/20',
    description: 'Ajusta los parámetros del motor de juego. Extrae y define entidades para el generador de IA.'
  },
  {
    id: Screen.GALLERY,
    title: 'Galería de Entidades',
    subtitle: 'Archivo central de activos sintetizados',
    icon: 'grid_view',
    color: 'border-primary/20',
    description: 'Accede a tus personajes, vehículos y sistemas estelares generados por el núcleo de IA.'
  }
];

/**
 * MasterHubPage Component
 * Provides a hub interface for Master users to navigate to main sections.
 * Features:
 * - 3 large navigation panels with responsive layout
 * - Clean terminal aesthetic
 * - Scrollable content for mobile devices
 * - Minimal hover animations for performance
 */
export const MasterHubPage: React.FC<MasterHubPageProps> = ({ onNavigate, onLogout }) => {
  return (
    <TerminalLayout 
      title="NÚCLEO DE MANDO" 
      subtitle="Hub Central"
      icon="hub"
      onLogout={onLogout}
      onNavigate={onNavigate}
      hideBackToHub={true}
    >
      <div className="h-full overflow-y-auto custom-scrollbar px-4 md:px-10 py-6 md:py-0">
        <div className="min-h-full flex flex-col items-center justify-center">
          {/* Welcome Header */}
          <div className="mb-12 text-center relative w-full">
            {/* Decorative line - static, no pulse animation */}
            <div className="absolute -top-6 left-1/2 -translate-x-1/2 w-32 h-0.5 bg-primary/5" />
            <h2 className="text-4xl md:text-5xl font-display font-black text-primary/90 text-glow uppercase tracking-[0.2em] mb-4">
              Bienvenido, Maestro
            </h2>
            <p className="text-primary/40 font-mono text-sm max-w-2xl mx-auto uppercase tracking-widest">
              Selecciona un sector para inicializar los protocolos operativos. Todos los sistemas están nominales.
            </p>
          </div>

          {/* Navigation Panels Grid */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6 w-full max-w-7xl mb-12">
            {HUB_PANELS.map((panel, index) => (
              <button
                key={panel.id}
                onClick={() => onNavigate(panel.id)}
                className={`group relative bg-surface-dark border ${panel.color} p-6 text-left transition-all hover:scale-[1.02] hover:border-primary/60 hover:shadow-[0_0_20px_rgba(37,244,106,0.2)] clip-tech-br flex flex-col md:min-h-[280px] min-h-[220px] overflow-hidden cursor-pointer`}
                style={{ animationDelay: `${index * 50}ms` }}
              >
                {/* Panel Header - Icon and Title inline */}
                <div className="mb-4">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="w-12 h-12 shrink-0 border border-primary/20 flex items-center justify-center bg-primary/5 group-hover:bg-primary/20 transition-all">
                      <span className="material-icons text-primary/50 group-hover:text-primary text-2xl transition-all">
                        {panel.icon}
                      </span>
                    </div>
                    <h3 className="text-xl font-display font-bold text-primary/80 group-hover:text-primary group-hover:text-glow uppercase tracking-widest leading-none transition-all">
                      {panel.title}
                    </h3>
                  </div>
                  <p className="text-[10px] text-primary/30 font-mono uppercase tracking-wider italic group-hover:text-primary/50 transition-colors">
                    {panel.subtitle}
                  </p>
                </div>

                {/* Panel Description */}
                <p className="text-xs text-primary/40 leading-relaxed font-mono mt-2 mb-6 flex-1 group-hover:text-primary/70 transition-colors">
                  {panel.description}
                </p>

                {/* Decorative pulsing bars - visible on hover */}
                <div className="absolute top-3 right-3 opacity-0 group-hover:opacity-100 transition-opacity">
                  <div className="flex gap-1">
                    <div className="w-1 h-3 bg-primary/40 animate-pulse"></div>
                    <div className="w-1 h-3 bg-primary/40 animate-pulse" style={{ animationDelay: '0.2s' }}></div>
                  </div>
                </div>

                {/* Panel Footer */}
                <div className="mt-auto pt-4 border-t border-primary/10 flex items-center justify-between group-hover:border-primary/30 transition-colors">
                  <span className="text-[10px] text-primary/30 font-bold uppercase tracking-widest group-hover:text-primary/60 transition-colors">
                    Entrada Disponible
                  </span>
                  <span className="material-icons text-primary/40 group-hover:text-primary group-hover:translate-x-1 transition-all">
                    arrow_forward
                  </span>
                </div>

                {/* Scanline overlay effect on hover */}
                <div className="absolute inset-0 pointer-events-none opacity-0 group-hover:opacity-10 bg-[linear-gradient(rgba(18,16,16,0)_50%,rgba(0,0,0,0.25)_50%),linear-gradient(90deg,rgba(255,0,0,0.06),rgba(0,255,0,0.02),rgba(0,0,255,0.06))] bg-[length:100%_4px,3px_100%] transition-opacity"></div>
              </button>
            ))}
          </div>

          {/* Footer Status Indicators */}
          <div className="mt-4 flex flex-wrap justify-center items-center gap-6 md:gap-10 opacity-10 text-[10px] font-mono uppercase tracking-[0.5em] text-primary pb-8">
            <div className="flex items-center gap-2">
              <span className="w-1 h-1 bg-primary/40 rounded-full" />
              TRANSCEPTOR_ACTIVO
            </div>
            <div className="hidden sm:block">ENLACE_NEURAL_ESTABLE</div>
            <div className="hidden sm:block">SEGURIDAD_NIVEL_ALPHA</div>
          </div>
        </div>
      </div>
    </TerminalLayout>
  );
};
