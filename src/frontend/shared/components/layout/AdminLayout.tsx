/**
 * Admin Layout Component
 * Dedicated layout for admin pages with terminal styling.
 * Navigation is handled through the Admin Hub page.
 */

import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import DiceRoller from '@components/DiceRoller';
import { useAuth } from '@core/context';

interface AdminLayoutProps {
  children: React.ReactNode;
}

/**
 * AdminLayout Component
 * Provides a dedicated layout for admin pages with:
 * - Terminal header styling (red theme)
 * - Hub navigation button
 * - Back button for browser history
 */
export const AdminLayout: React.FC<AdminLayoutProps> = ({ children }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAdmin, logout } = useAuth();
  const [showDice, setShowDice] = useState(false);

  const handleBack = () => {
    navigate(-1);
  };

  const isHubPage = location.pathname === '/hub';

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
        <div className="flex items-center gap-2 md:gap-4">
          <div className="hidden md:flex flex-col text-right text-xs text-red-500/60">
            <span>ACCESO: ROOT</span>
            <span>PROTOCOLO: ADMIN</span>
          </div>

          {/* Hide dice roller for admin users */}
          {!isAdmin && (
            <button
              onClick={() => setShowDice(true)}
              className="cursor-pointer flex items-center gap-2 border border-red-500/40 px-2 md:px-3 py-1 text-xs uppercase hover:bg-red-500/20 transition-all text-red-500 font-bold"
            >
              <span className="material-icons text-sm">casino</span>
              <span className="hidden sm:inline">DADOS</span>
            </button>
          )}

          {/* Hub button - visible on all admin pages except hub itself */}
          {!isHubPage && (
            <button
              onClick={() => navigate('/hub')}
              className="flex items-center gap-2 border border-cyan-500/30 px-2 md:px-3 py-1 text-xs md:text-sm uppercase hover:border-cyan-500 hover:bg-cyan-500/40 transition-all font-bold text-cyan-500 cursor-pointer"
            >
              <span className="material-icons text-sm">home</span>
              <span className="hidden sm:inline">HUB</span>
            </button>
          )}

          {/* Back button - for browser history navigation, hidden on hub */}
          {!isHubPage && (
            <button
              onClick={handleBack}
              className="flex items-center gap-2 border border-primary/40 px-2 md:px-3 py-1 text-xs md:text-sm uppercase hover:bg-primary/20 transition-all text-primary font-bold cursor-pointer"
            >
              <span className="material-icons text-sm">arrow_back</span>
              <span className="hidden sm:inline">VOLVER</span>
            </button>
          )}

          {/* Logout Button - always visible */}
          <button
            onClick={async () => {
              await logout();
              window.location.replace('/');
            }}
            className="flex items-center gap-2 border border-red-500/60 px-2 md:px-3 py-1 text-xs md:text-sm uppercase hover:bg-red-500 hover:text-black transition-colors text-red-500 font-bold cursor-pointer"
          >
            <span className="material-icons text-sm">logout</span>
            <span className="hidden sm:inline">LOGOUT</span>
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <div className="flex-1 overflow-hidden">
        <main className="h-full">{children}</main>
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
