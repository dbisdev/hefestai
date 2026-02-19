/**
 * useTerminalLog Hook Unit Tests
 * Tests for terminal-style logging functionality
 */
import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useTerminalLog } from './useTerminalLog';

describe('useTerminalLog', () => {
  it('initializes with empty logs by default', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    expect(result.current.logs).toEqual([]);
  });

  it('initializes with provided initial logs', () => {
    const initialLogs = ['log 1', 'log 2'];
    const { result } = renderHook(() => useTerminalLog({ initialLogs }));
    
    expect(result.current.logs).toEqual(['log 1', 'log 2']);
  });

  it('adds a log message with prefix', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    act(() => {
      result.current.addLog('Test message');
    });
    
    expect(result.current.logs).toEqual(['> Test message']);
  });

  it('adds multiple log messages in order', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    act(() => {
      result.current.addLog('First');
      result.current.addLog('Second');
      result.current.addLog('Third');
    });
    
    expect(result.current.logs).toEqual([
      '> First',
      '> Second',
      '> Third',
    ]);
  });

  it('limits logs to maxLogs', () => {
    const { result } = renderHook(() => useTerminalLog({ maxLogs: 3 }));
    
    act(() => {
      result.current.addLog('1');
      result.current.addLog('2');
      result.current.addLog('3');
      result.current.addLog('4');
      result.current.addLog('5');
    });
    
    expect(result.current.logs).toHaveLength(3);
    expect(result.current.logs).toEqual([
      '> 3',
      '> 4',
      '> 5',
    ]);
  });

  it('uses default maxLogs of 12', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    act(() => {
      for (let i = 0; i < 15; i++) {
        result.current.addLog(`log ${i}`);
      }
    });
    
    expect(result.current.logs).toHaveLength(12);
  });

  it('clears all logs', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    act(() => {
      result.current.addLog('1');
      result.current.addLog('2');
    });
    
    expect(result.current.logs).toHaveLength(2);
    
    act(() => {
      result.current.clearLogs();
    });
    
    expect(result.current.logs).toEqual([]);
  });

  it('sets logs directly', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    act(() => {
      result.current.setLogs(['custom 1', 'custom 2']);
    });
    
    expect(result.current.logs).toEqual(['custom 1', 'custom 2']);
  });

  it('trims logs when setting directly if exceeding maxLogs', () => {
    const { result } = renderHook(() => useTerminalLog({ maxLogs: 2 }));
    
    act(() => {
      result.current.setLogs(['1', '2', '3', '4']);
    });
    
    expect(result.current.logs).toEqual(['3', '4']);
  });

  it('does not add prefix when setting logs directly', () => {
    const { result } = renderHook(() => useTerminalLog());
    
    act(() => {
      result.current.setLogs(['no prefix']);
    });
    
    expect(result.current.logs).toEqual(['no prefix']);
  });
});
