/**
 * Access Denied Component
 * OWASP compliant error display for unauthorized access
 */

import React from 'react';
import { Button } from '../ui/Button';
import { GridBackground } from '../layout/GridBackground';

interface AccessDeniedProps {
  onBack: () => void;
}

export const AccessDenied: React.FC<AccessDeniedProps> = ({ onBack }) => {
  return (
    <div className="h-full w-full bg-background-dark flex items-center justify-center p-6 relative">
      <GridBackground opacity={0.2} color="rgba(37, 244, 106, 0.05)" />
      
      <div className="relative w-full max-w-2xl bg-surface-dark border border-danger/40 rounded-lg shadow-[0_0_50px_rgba(239,68,68,0.1)] flex flex-col overflow-hidden">
        <div className="bg-danger/10 border-b border-danger/30 px-4 py-2 flex justify-between items-center font-mono">
          <div className="flex items-center gap-2 text-danger">
            <span className="material-icons text-sm">lock</span>
            <span className="text-xs font-bold tracking-widest uppercase">Incidente de Seguridad #9940</span>
          </div>
          <div className="flex gap-1.5">
            <div className="w-2 h-2 rounded-full bg-danger/50"></div>
            <div className="w-2 h-2 rounded-full bg-danger"></div>
          </div>
        </div>

        <div className="p-10 flex flex-col items-center text-center">
          <div className="mb-8 relative">
            <div className="absolute inset-0 bg-danger/20 blur-2xl rounded-full animate-pulse"></div>
            <span className="material-icons text-9xl text-danger relative z-10 drop-shadow-[0_0_15px_rgba(239,68,68,0.5)]">lock_person</span>
          </div>

          <h1 className="text-4xl md:text-6xl font-display font-black text-danger mb-4 tracking-tighter text-glow-danger uppercase">
            Acceso Denegado
          </h1>

          <div className="max-w-lg mb-8 border-l-2 border-danger/40 pl-6 text-left">
            <p className="text-primary/90 text-lg font-light leading-relaxed">
              <span className="font-bold text-danger">ERROR 403:</span> Sus credenciales biometricas no coinciden con los registros del personal autorizado para el <span className="text-white">Sector 7</span>. Protocolo de contencion iniciado.
            </p>
          </div>

          <div className="w-full bg-black/60 rounded border border-danger/10 p-4 mb-8 font-mono text-xs text-left text-danger/70 shadow-inner h-24 overflow-y-auto">
            <p>&gt; AUTH_FAILURE: PERMISSION_LEVEL_TOO_LOW</p>
            <p className="animate-pulse">&gt; ALERT: SECURITY_ADMIN_NOTIFIED</p>
            <p>&gt; TRACE_IP: 192.168.0.X DETECTED</p>
          </div>

          <div className="flex flex-col sm:flex-row gap-4 w-full justify-center font-mono">
            <Button variant="danger" icon="refresh" onClick={onBack}>
              REINTENTAR
            </Button>
            <Button variant="primary" icon="home" onClick={onBack}>
              VOLVER AL INICIO
            </Button>
          </div>
        </div>

        <div className="mt-6 text-center pb-4">
          <p className="text-danger/40 text-[10px] font-mono uppercase tracking-[0.2em]">
            SYSTEM_ID: XF-99-ALPHA | ENCRYPTION: AES-4096 | STATUS: LOCKED
          </p>
        </div>
      </div>
    </div>
  );
};
