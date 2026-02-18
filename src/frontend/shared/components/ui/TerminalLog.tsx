/**
 * Terminal Log Component
 * Reusable terminal-style log display for cyberpunk UI
 * Displays scrollable log messages with automatic color coding
 */

import React, { useRef, useEffect } from 'react';

interface TerminalLogProps {
  logs: string[];
  maxLogs?: number;
  className?: string;
  highlightLast?: boolean;
}

const getLogColor = (log: string): string => {
  if (log.includes('ERROR') || log.includes('FAILED') || log.includes('CRITICAL')) {
    return 'text-red-400';
  }
  if (log.includes('SUCCESS') || log.includes('COMPLETE')) {
    return 'text-green-400';
  }
  if (log.includes('WARNING') || log.includes('WARN')) {
    return 'text-yellow-400';
  }
  return 'text-primary/70';
};

export const TerminalLog: React.FC<TerminalLogProps> = ({ 
  logs, 
  maxLogs = 12,
  className = '',
  highlightLast = true
}) => {
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [logs]);

  const displayLogs = logs.slice(-maxLogs);

  return (
    <div 
      ref={scrollRef}
      className={`bg-black/60 border border-primary/20 p-3 font-mono text-[10px] leading-relaxed overflow-y-auto custom-scrollbar ${className}`}
      role="log"
      aria-label="Terminal logs"
    >
      {displayLogs.map((log, index) => {
        const isLast = highlightLast && index === displayLogs.length - 1;
        const colorClass = getLogColor(log);
        
        return (
          <div 
            key={index} 
            className={`whitespace-pre-wrap break-all ${colorClass} ${isLast ? 'font-bold' : ''}`}
          >
            {log}
          </div>
        );
      })}
    </div>
  );
};

export default TerminalLog;
