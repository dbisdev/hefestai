/**
 * Terminal Log Hook
 * Provides terminal-style logging functionality for pages
 */

import { useState, useCallback } from 'react';

interface UseTerminalLogOptions {
  maxLogs?: number;
  initialLogs?: string[];
}

interface UseTerminalLogReturn {
  logs: string[];
  addLog: (message: string) => void;
  clearLogs: () => void;
  setLogs: (logs: string[]) => void;
}

export const useTerminalLog = (options: UseTerminalLogOptions = {}): UseTerminalLogReturn => {
  const { maxLogs = 12, initialLogs = [] } = options;
  
  const [logs, setLogsState] = useState<string[]>(initialLogs);

  const addLog = useCallback((message: string) => {
    setLogsState(prev => {
      const newLogs = [...prev, `> ${message}`];
      return newLogs.slice(-maxLogs);
    });
  }, [maxLogs]);

  const clearLogs = useCallback(() => {
    setLogsState([]);
  }, []);

  const setLogs = useCallback((newLogs: string[]) => {
    setLogsState(newLogs.slice(-maxLogs));
  }, [maxLogs]);

  return {
    logs,
    addLog,
    clearLogs,
    setLogs
  };
};

export default useTerminalLog;
