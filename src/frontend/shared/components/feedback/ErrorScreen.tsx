/**
 * Error Screen Component
 * System failure / critical error display
 */

import React from 'react';
import { Button } from '../ui/Button';

interface ErrorScreenProps {
  onReboot: () => void;
  title?: string;
  message?: string;
}

export const ErrorScreen: React.FC<ErrorScreenProps> = ({ 
  onReboot,
  title = 'System Failure // 404_VOID',
  message = 'CRITICAL ERROR DETECTED. NEURAL LINK SEVERED. ATTEMPTING TO RESTORE CONNECTION TO HOST...'
}) => {
  return (
    <div className="h-full w-full bg-background-dark flex items-center justify-center p-6 relative font-sans">
      <div className="absolute inset-0 opacity-10 pointer-events-none grid grid-cols-[repeat(20,1fr)] grid-rows-[repeat(20,1fr)]">
        <div className="col-span-full border-b border-primary/40 row-start-10" />
        <div className="col-start-10 row-span-full border-r border-primary/40" />
      </div>

      <div className="relative w-full max-w-2xl bg-surface-dark border border-primary/60 rounded-lg shadow-[0_0_30px_rgba(37,244,106,0.1)] flex flex-col overflow-hidden z-20">
        <div className="h-10 bg-primary/10 border-b border-primary/40 flex items-center justify-between px-4">
          <div className="flex items-center gap-2">
            <span className="material-icons text-primary text-sm">warning</span>
            <span className="text-xs font-bold tracking-wider text-primary font-mono uppercase">SYSTEM_ALERT_0x84</span>
          </div>
          <div className="flex gap-2">
            <div className="size-2 rounded-full bg-primary/30" />
            <div className="size-2 rounded-full bg-primary/30" />
          </div>
        </div>

        <div className="p-10 flex flex-col items-center text-center">
          <div className="mb-8 relative p-1 border-2 border-primary/30 bg-black/40">
            <img 
              alt="Error Icon" 
              className="w-32 h-32 object-contain brightness-90 grayscale contrast-125" 
              src="https://lh3.googleusercontent.com/aida-public/AB6AXuAL7bysBkiypb6VpGUiT_iGN5Khw70xGdfDcHX0BIs4LteuVTTMkSKehymaPmcHGpGTSik0ZZtlD-uuQsiZ_Tzuo-dG3d-BAPRAYSXL1Pxq8DYQ0-iuDzamtfaedSa2Ts_ybJ6SgO3vV3xeFh9n-tA_6m5yGaNy9zSuHGWR581E9L1Op9uWzmeRKFmQzdZeLAa-0iu4lKPinUwqxEUm6gW-mERqdqWe2k3-A8RVNjHC6HZDllnliil7JZ7TwQCHgIEJe-YYhcT2jBg" 
              style={{ imageRendering: 'pixelated' }} 
            />
          </div>

          <h1 className="text-primary tracking-widest text-3xl md:text-5xl font-bold mb-4 text-glow uppercase font-display">
            {title}
          </h1>

          <div className="max-w-md mx-auto mb-8 border-y border-primary/20 border-dashed py-4">
            <p className="text-primary/90 text-sm md:text-base font-mono leading-relaxed">
              {message}
            </p>
          </div>

          <div className="w-full bg-black/60 rounded border border-primary/20 p-4 mb-8 text-left font-mono text-xs text-primary/70 h-32 overflow-y-auto shadow-inner">
            <p>&gt; initializing diagnostics...</p>
            <p>&gt; checking memory_blocks [0x00 - 0xFF]... <span className="text-red-400">CORRUPTED</span></p>
            <p>&gt; attempting core_dump...</p>
            <p>&gt; loading fallback_protocol.exe</p>
            <p>&gt; error: resource not found</p>
            <p className="animate-pulse">&gt; _waiting for user input_</p>
          </div>

          <div className="flex flex-col sm:flex-row gap-4 w-full justify-center">
            <Button variant="primary" icon="restart_alt" onClick={onReboot}>
              Reboot System
            </Button>
            <Button variant="secondary">
              Debug Mode
            </Button>
          </div>
        </div>

        <div className="h-8 bg-black/60 border-t border-primary/30 flex items-center px-4 justify-between text-[10px] font-mono text-primary/50 uppercase">
          <span>PID: 882910</span>
          <span>T: 00:00:14:02</span>
        </div>
      </div>
    </div>
  );
};
