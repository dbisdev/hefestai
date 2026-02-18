/**
 * Admin Layout Component
 * Dedicated layout for admin pages with admin-specific sidebar navigation.
 * Hides the normal gallery sidebar and shows only admin panel options.
 */

import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import DiceRoller from '@components/DiceRoller';
import { useAuth } from '@core/context';

/** Navigation item definition for admin sidebar */
interface AdminNavItem {
  path: string;
  label: string;
  icon: string;
  description: string;
}

/** Admin navigation items configuration */
const ADMIN_NAV_ITEMS: AdminNavItem[] = [
  {
    path: '/admin/users',
    label: 'USUARIOS',
    icon: 'admin_panel_settings',
    description: 'Gestión de usuarios',
  },
  {
    path: '/game-systems',
    label: 'SISTEMAS',
    icon: 'sports_esports',
    description: 'Sistemas de juego',
  },
  {
    path: '/admin/system',
    label: 'RAG',
    icon: 'settings_suggest',
    description: 'Operaciones del sistema',
  },
  {
    path: '/admin/campaigns',
    label: 'CAMPAÑAS',
    icon: 'shield',
    description: 'Gestión de campañas',
  },
  {
    path: '/templates',
    label: 'PLANTILLAS',
    icon: 'description',
    description: 'Plantillas de entidades',
  },
];

interface AdminLayoutProps {
  children: React.ReactNode;
  /** Current active path for highlighting in navigation */
  activePath: string;
}

/**
 * AdminLayout Component
 * Provides a dedicated layout for admin pages with:
 * - Admin-specific sidebar navigation
 * - Terminal header styling
 * - Dice roller access
 * - Back to gallery button
 */
export const AdminLayout: React.FC<AdminLayoutProps> = ({
  children,
  activePath,
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAdmin, logout } = useAuth();
  const [showDice, setShowDice] = useState(false);

  const handleNavigate = (path: string) => {
    navigate(path);
  };

  const handleBack = () => {
    navigate(-1);
  };

  return (
    <div className="flex flex-col h-screen p-4 md:p-8 bg-background-dark font-mono relative">
      {/* Header */}
      <header className="flex justify-between items-center mb-6 pb-2 border-b-2 border-red-500/30">
        <div className="flex items-center gap-4">
          <div className="w-8 h-8 border border-red-500 flex items-center justify-center bg-red-500/10">
            <span className="material-icons text-red-500 text-sm">security</span>
          </div>
          <div>
            <h1 className="text-3xl font-display uppercase tracking-widest text-red-500 text-glow font-bold">
              ADMIN_PANEL
            </h1>
            <p className="text-xs text-red-500/60 uppercase tracking-wider">
              Sistema de administración
            </p>
          </div>
        </div>
        <div className="flex items-center gap-4 md:gap-6">
          <div className="hidden md:flex flex-col text-right text-xs text-red-500/60">
            <span>ACCESO: ROOT</span>
            <span>PROTOCOLO: ADMIN</span>
          </div>

          {/* Hide dice roller for admin users */}
          {!isAdmin && (
            <button
              onClick={() => setShowDice(true)}
              className="flex items-center gap-2 border border-red-500/40 px-3 py-1 text-xs uppercase hover:bg-red-500/20 transition-all text-red-500 font-bold"
            >
              <span className="material-icons text-sm">casino</span>
              <span className="hidden sm:inline">DADOS</span>
            </button>
          )}

          {/* Back button - always visible for browser history */}
          <button
            onClick={handleBack}
            className="flex items-center gap-2 border border-primary/40 px-3 py-1 text-xs uppercase hover:bg-primary/20 transition-all text-primary font-bold"
          >
            <span className="material-icons text-sm">arrow_back</span>
            <span className="hidden sm:inline">VOLVER</span>
          </button>

          {/* Logout Button - always visible */}
          <button
            onClick={async () => {
              await logout();
              window.location.replace('/');
            }}
            className="border border-red-500 px-4 py-1 text-xs uppercase hover:bg-red-500 hover:text-black transition-colors text-red-500 font-bold"
          >
            LOGOUT
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <div className="flex-1 flex gap-6 overflow-hidden">
        {/* Admin Sidebar */}
        <aside className="w-16 md:w-56 flex flex-col gap-4 shrink-0">
          <nav
            role="navigation"
            aria-label="Navegación de administración"
            className="flex flex-col gap-2"
          >
            <div className="p-1 border border-red-500/50 text-[10px] text-red-400 text-center uppercase mb-2 bg-red-500/5 font-bold tracking-[0.2em]">
              :: ADMIN_PANEL ::
            </div>
            {ADMIN_NAV_ITEMS.map((item) => (
              <button
                key={item.path}
                onClick={() => handleNavigate(item.path)}
                className={`group flex items-center gap-3 p-3 border transition-all relative overflow-hidden ${
                  activePath === item.path
                    ? 'border-l-4 border-l-red-500 border-y-red-500/30 border-r-red-500/30 bg-red-500/20 shadow-[inset_0_0_15px_rgba(239,68,68,0.1)]'
                    : 'border-red-500/30 hover:border-red-500 hover:bg-red-500/10 bg-surface-dark'
                }`}
              >
                {activePath === item.path && (
                  <div className="absolute inset-0 bg-red-500/5 animate-pulse pointer-events-none"></div>
                )}
                <span
                  className={`material-icons text-xl ${
                    activePath === item.path ? 'text-red-500' : 'text-red-500/60'
                  }`}
                >
                  {item.icon}
                </span>
                <div className="hidden md:flex flex-col items-start">
                  <span
                    className={`text-xs font-bold tracking-widest ${
                      activePath === item.path
                        ? 'text-red-500 text-glow'
                        : 'text-red-500/70'
                    }`}
                  >
                    {item.label}
                  </span>
                  <span className="text-[9px] text-red-500/40">{item.description}</span>
                </div>
                {activePath === item.path && (
                  <div className="absolute right-2 top-1/2 -translate-y-1/2 w-1 h-1 bg-red-500 rounded-full animate-ping"></div>
                )}
              </button>
            ))}
          </nav>

          {/* System Status */}
          <div className="mt-auto hidden md:block p-3 border border-red-500/10 bg-black/20 text-[8px] text-red-500/40 leading-tight uppercase tracking-widest">
            <p className="mb-1">MODO: ADMINISTRADOR</p>
            <p>PERMISOS: TOTAL</p>
          </div>
        </aside>

        {/* Main Content */}
        <main className="flex-1 overflow-hidden">{children}</main>
      </div>

      {/* Footer */}
      <footer className="mt-4 border-t border-red-500/30 pt-2 flex justify-between text-[10px] md:text-xs text-red-500/40 uppercase">
        <div className="flex gap-4">
          <span>CONEXION: SEGURA</span>
          <span className="hidden md:inline">NIVEL: ADMINISTRADOR</span>
        </div>
        <div className="flex gap-4 animate-pulse">
          <span>PROTOCOLO ADMIN ACTIVO</span>
          <span>[ |||||||||| ] OK</span>
        </div>
      </footer>

      {/* Dice Roller Modal */}
      {showDice && <DiceRoller onClose={() => setShowDice(false)} />}
    </div>
  );
};
