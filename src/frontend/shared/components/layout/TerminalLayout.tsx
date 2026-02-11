/**
 * Terminal Layout Component
 * Main layout wrapper with header, footer, and terminal styling
 */

import React, { useState } from 'react';
import DiceRoller from '../../../components/DiceRoller';
import RuleQuery from '../../../components/RuleQuery';

interface TerminalLayoutProps {
  children: React.ReactNode;
  title: string;
  subtitle: string;
  onLogout?: () => void;
  actions?: React.ReactNode;
  /** Optional game system ID for RAG rule queries */
  gameSystemId?: string;
  /** Optional game system name for display */
  gameSystemName?: string;
}

export const TerminalLayout: React.FC<TerminalLayoutProps> = ({ 
  children, 
  title, 
  subtitle, 
  onLogout, 
  actions,
  gameSystemId,
  gameSystemName
}) => {
  const [showDice, setShowDice] = useState(false);
  const [showRuleQuery, setShowRuleQuery] = useState(false);

  return (
    <div className="flex flex-col h-screen p-4 md:p-8 bg-background-dark font-mono relative">
      <header className="flex justify-between items-center mb-6 pb-2 border-b-2 border-primary/30">
        <div className="flex items-center gap-4">
          <div className="w-8 h-8 border border-primary flex items-center justify-center bg-primary/10">
            <span className="material-icons text-primary text-sm">terminal</span>
          </div>
          <div>
            <h1 className="text-3xl font-display uppercase tracking-widest text-primary text-glow font-bold">
              {title}
            </h1>
            <p className="text-xs text-primary/60 uppercase tracking-wider">
              {subtitle}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-4 md:gap-6">
          <div className="hidden md:flex flex-col text-right text-xs text-primary/60">
            <span>MEM: 64TB [OK]</span>
            <span>NET: ENCRYPTED</span>
          </div>
          
          <button 
            onClick={() => setShowDice(true)}
            className="flex items-center gap-2 border border-primary/40 px-3 py-1 text-xs uppercase hover:bg-primary/20 transition-all text-primary font-bold"
          >
            <span className="material-icons text-sm">casino</span>
            <span className="hidden sm:inline">DADOS</span>
          </button>

          <button 
            onClick={() => setShowRuleQuery(true)}
            className="flex items-center gap-2 border border-primary/40 px-3 py-1 text-xs uppercase hover:bg-primary/20 transition-all text-primary font-bold"
            aria-label="Consultar reglas"
          >
            <span className="material-icons text-sm">menu_book</span>
            <span className="hidden sm:inline">REGLAS</span>
          </button>

          {actions}
          
          {onLogout && (
            <button 
              onClick={onLogout}
              className="border border-primary px-4 py-1 text-xs uppercase hover:bg-primary hover:text-black transition-colors text-primary font-bold"
            >
              LOGOUT
            </button>
          )}
        </div>
      </header>

      <div className="flex-1 overflow-hidden">
        {children}
      </div>

      <footer className="mt-4 border-t border-primary/30 pt-2 flex justify-between text-[10px] md:text-xs text-primary/40 uppercase">
        <div className="flex gap-4">
          <span>CONEXION: SEGURA</span>
          <span className="hidden md:inline">ENCRIPTACION: AES-4096</span>
        </div>
        <div className="flex gap-4 animate-pulse">
          <span>TRANSFERENCIA DE DATOS...</span>
          <span>[ |||||||||| ] 100%</span>
        </div>
      </footer>

      {showDice && <DiceRoller onClose={() => setShowDice(false)} />}
      {showRuleQuery && (
        <RuleQuery 
          onClose={() => setShowRuleQuery(false)} 
          gameSystemId={gameSystemId}
          gameSystemName={gameSystemName}
        />
      )}
    </div>
  );
};
