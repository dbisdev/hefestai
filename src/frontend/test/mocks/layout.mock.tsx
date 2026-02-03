/**
 * Mock for TerminalLayout Component
 * Simple wrapper that renders children without complex layout
 */
import React, { ReactNode } from 'react';

interface TerminalLayoutProps {
  children: ReactNode;
  title: string;
  subtitle: string;
  onLogout?: () => void;
  actions?: ReactNode;
}

export const TerminalLayout: React.FC<TerminalLayoutProps> = ({ 
  children, 
  title, 
  subtitle, 
  onLogout, 
  actions 
}) => {
  return (
    <div data-testid="terminal-layout">
      <header>
        <h1>{title}</h1>
        <p>{subtitle}</p>
        {actions && <div data-testid="header-actions">{actions}</div>}
        {onLogout && (
          <button onClick={onLogout} data-testid="logout-button">
            LOGOUT
          </button>
        )}
      </header>
      <main>{children}</main>
    </div>
  );
};

export const GridBackground: React.FC = () => null;
