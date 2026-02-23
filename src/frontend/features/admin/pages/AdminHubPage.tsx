/**
 * Admin Hub Page
 * Landing page for Admin users after login.
 * Displays 5 big panels for quick navigation to admin sections.
 * Red cyberpunk terminal aesthetics matching AdminLayout style.
 */

import React, { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { AdminLayout } from '@shared/components/layout';

interface HubPanel {
  path: string;
  title: string;
  subtitle: string;
  icon: string;
  color: string;
  description: string;
}

const ADMIN_HUB_PANELS: HubPanel[] = [
  {
    path: '/admin/users',
    title: 'Usuarios',
    subtitle: 'Gestión de cuentas y permisos',
    icon: 'admin_panel_settings',
    color: 'border-red-500/20',
    description: 'Administra usuarios del sistema. Controla roles, accesos y permisos de todos los operativos.'
  },
  {
    path: '/game-systems',
    title: 'Sistemas',
    subtitle: 'Configuración de reglas y mecánicas',
    icon: 'sports_esports',
    color: 'border-red-500/20',
    description: 'Gestiona los sistemas de juego disponibles. Configura manuales RAG y plantillas de entidades.'
  },
  {
    path: '/admin/system',
    title: 'RAG',
    subtitle: 'Operaciones del sistema RAG',
    icon: 'settings_suggest',
    color: 'border-red-500/20',
    description: 'Operaciones avanzadas del sistema. Gestión de documentos y embeddings para IA.'
  },
  {
    path: '/admin/campaigns',
    title: 'Campañas',
    subtitle: 'Gestión global de campañas',
    icon: 'shield',
    color: 'border-red-500/20',
    description: 'Supervisa todas las campañas del sistema. Control total sobre crónicas y sesiones.'
  },
  {
    path: '/templates',
    title: 'Plantillas',
    subtitle: 'Plantillas de entidades',
    icon: 'description',
    color: 'border-red-500/20',
    description: 'Gestiona plantillas de entidades para todos los sistemas de juego configurados.'
  }
];

export const AdminHubPage: React.FC = () => {
  const navigate = useNavigate();

  const handleNavigate = useCallback((path: string) => {
    navigate(path);
  }, [navigate]);

  return (
    <AdminLayout>
      <div className="h-full overflow-y-auto custom-scrollbar px-4 md:px-10 py-6 md:py-0">
        <div className="min-h-full flex flex-col items-center justify-center">
          <div className="mb-12 text-center relative w-full">
            <div className="absolute -top-6 left-1/2 -translate-x-1/2 w-32 h-0.5 bg-red-500/5" />
            <h2 className="text-4xl md:text-5xl font-display font-black text-red-500/90 text-glow uppercase tracking-[0.2em] mb-4">
              Bienvenido, Administrador
            </h2>
            <p className="text-red-500/40 font-mono text-sm max-w-2xl mx-auto uppercase tracking-widest">
              Panel de control del sistema. Acceso total a todas las funciones administrativas.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-6 w-full max-w-7xl mb-12">
            {ADMIN_HUB_PANELS.map((panel, index) => (
              <button
                key={panel.path}
                onClick={() => handleNavigate(panel.path)}
                className={`group relative bg-surface-dark border ${panel.color} p-6 text-left transition-all hover:scale-[1.02] hover:border-red-500/60 hover:shadow-[0_0_20px_rgba(239,68,68,0.2)] clip-tech-br flex flex-col md:min-h-[280px] min-h-[220px] overflow-hidden cursor-pointer`}
                style={{ animationDelay: `${index * 50}ms` }}
              >
                <div className="mb-4">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="w-12 h-12 shrink-0 border border-red-500/20 flex items-center justify-center bg-red-500/5 group-hover:bg-red-500/20 transition-all">
                      <span className="material-icons text-red-500/50 group-hover:text-red-500 text-2xl transition-all">
                        {panel.icon}
                      </span>
                    </div>
                    <h3 className="text-xl font-display font-bold text-red-500/80 group-hover:text-red-500 group-hover:text-glow uppercase tracking-widest leading-none transition-all">
                      {panel.title}
                    </h3>
                  </div>
                  <p className="text-xs text-red-500/50 font-mono uppercase tracking-wider italic group-hover:text-red-500/50 transition-colors">
                    {panel.subtitle}
                  </p>
                </div>

                <p className="text-sm text-red-500 lg:text-red-500/50 leading-relaxed font-mono mt-2 mb-6 flex-1 group-hover:text-red-500/90 transition-colors">
                  {panel.description}
                </p>

                <div className="absolute top-3 right-3 opacity-0 group-hover:opacity-100 transition-opacity">
                  <div className="flex gap-1">
                    <div className="w-1 h-3 bg-red-500/40 animate-pulse"></div>
                    <div className="w-1 h-3 bg-red-500/40 animate-pulse" style={{ animationDelay: '0.2s' }}></div>
                  </div>
                </div>

                <div className="mt-auto pt-4 border-t border-red-500/10 flex items-center justify-between group-hover:border-red-500/30 transition-colors">
                  <span className="text-xs text-red-500/50 font-bold uppercase tracking-widest group-hover:text-red-500/60 transition-colors">
                    Acceso Disponible
                  </span>
                  <span className="material-icons text-red-500/40 group-hover:text-red-500 group-hover:translate-x-1 transition-all">
                    arrow_forward
                  </span>
                </div>

                <div className="absolute inset-0 pointer-events-none opacity-0 group-hover:opacity-10 bg-[linear-gradient(rgba(18,16,16,0)_50%,rgba(0,0,0,0.25)_50%),linear-gradient(90deg,rgba(255,0,0,0.06),rgba(0,255,0,0.02),rgba(0,0,255,0.06))] bg-[length:100%_4px,3px_100%] transition-opacity"></div>
              </button>
            ))}
          </div>

          <div className="mt-4 flex flex-wrap justify-center items-center gap-6 md:gap-10 opacity-10 text-[10px] font-mono uppercase tracking-[0.5em] text-red-500 pb-8">
            <div className="flex items-center gap-2">
              <span className="w-1 h-1 bg-red-500/40 rounded-full" />
              TRANSCEPTOR_ACTIVO
            </div>
            <div className="hidden sm:block">ENLACE_ADMIN_ESTABLE</div>
            <div className="hidden sm:block">SEGURIDAD_NIVEL_OMEGA</div>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};
