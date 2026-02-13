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
    description: 'Ajusta los parámetros del motor de juego. Define tablas de probabilidad y sistemas de combate.'
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
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6 w-full max-w-7xl mb-12">
            {HUB_PANELS.map((panel) => (
              <button
                key={panel.id}
                onClick={() => onNavigate(panel.id)}
                className={`group relative bg-surface-dark/40 border ${panel.color} p-6 text-left transition-all duration-700 hover:scale-[1.01] hover:bg-surface-dark/80 hover:shadow-[0_0_20px_rgba(37,244,106,0.03)] clip-tech-br flex flex-col md:min-h-[280px] min-h-[220px] overflow-hidden`}
              >
                {/* Panel Header - Icon and Title inline */}
                <div className="mb-4">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="w-12 h-12 shrink-0 border border-primary/10 flex items-center justify-center bg-primary/5 group-hover:bg-primary/10 transition-colors duration-700">
                      <span className="material-icons text-primary/40 group-hover:text-primary/80 text-2xl transition-all">
                        {panel.icon}
                      </span>
                    </div>
                    <h3 className="text-xl font-display font-bold text-primary/70 group-hover:text-primary group-hover:text-glow uppercase tracking-widest leading-none transition-colors duration-500">
                      {panel.title}
                    </h3>
                  </div>
                  <p className="text-[10px] text-primary/20 font-mono uppercase tracking-wider italic">
                    {panel.subtitle}
                  </p>
                </div>

                {/* Panel Description */}
                <p className="text-xs text-primary/40 leading-relaxed font-mono mt-2 mb-6 flex-1 group-hover:text-primary/60 transition-colors duration-700">
                  {panel.description}
                </p>

                {/* Panel Footer */}
                <div className="mt-auto pt-4 border-t border-primary/5 flex items-center justify-between">
                  <span className="text-[10px] text-primary/20 font-bold uppercase tracking-widest group-hover:text-primary/40 transition-colors">
                    Entrada Disponible
                  </span>
                  <span className="material-icons text-primary/30 group-hover:text-primary/60 group-hover:translate-x-0.5 transition-all duration-500">
                    arrow_forward
                  </span>
                </div>
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
