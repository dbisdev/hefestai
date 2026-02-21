/**
 * useConfirmDialog Hook Tests
 */

import { describe, it, expect, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useConfirmDialog } from './useConfirmDialog';

describe('useConfirmDialog', () => {
  it('starts with closed state', () => {
    const { result } = renderHook(() => useConfirmDialog());
    
    expect(result.current.isOpen).toBe(false);
    expect(result.current.config).toBeNull();
  });

  it('opens dialog with confirm()', () => {
    const { result } = renderHook(() => useConfirmDialog());
    
    act(() => {
      result.current.confirm({
        title: 'Test Title',
        message: 'Test Message',
      });
    });
    
    expect(result.current.isOpen).toBe(true);
    expect(result.current.config?.title).toBe('Test Title');
    expect(result.current.config?.message).toBe('Test Message');
  });

  it('resolves true on handleConfirm', async () => {
    const { result } = renderHook(() => useConfirmDialog());
    
    let promiseResult: boolean | undefined;
    let confirmPromise: Promise<boolean>;
    
    act(() => {
      confirmPromise = result.current.confirm({
        title: 'Test',
        message: 'Test',
      });
      confirmPromise.then((value) => {
        promiseResult = value;
      });
    });
    
    act(() => {
      result.current.handleConfirm();
    });
    
    await act(async () => {
      await confirmPromise!;
    });
    
    expect(result.current.isOpen).toBe(false);
    expect(promiseResult).toBe(true);
  });

  it('resolves false on handleCancel', async () => {
    const { result } = renderHook(() => useConfirmDialog());
    
    let promiseResult: boolean | undefined;
    let confirmPromise: Promise<boolean>;
    
    act(() => {
      confirmPromise = result.current.confirm({
        title: 'Test',
        message: 'Test',
      });
      confirmPromise.then((value) => {
        promiseResult = value;
      });
    });
    
    act(() => {
      result.current.handleCancel();
    });
    
    await act(async () => {
      await confirmPromise!;
    });
    
    expect(result.current.isOpen).toBe(false);
    expect(promiseResult).toBe(false);
  });

  it('uses default labels when not provided', () => {
    const { result } = renderHook(() => useConfirmDialog());
    
    act(() => {
      result.current.confirm({
        title: 'Test',
        message: 'Test',
      });
    });
    
    expect(result.current.config?.confirmLabel).toBe('Confirmar');
    expect(result.current.config?.cancelLabel).toBe('Cancelar');
    expect(result.current.config?.variant).toBe('info');
  });

  it('uses custom config values', () => {
    const { result } = renderHook(() => useConfirmDialog());
    
    act(() => {
      result.current.confirm({
        title: 'Test',
        message: 'Test',
        confirmLabel: 'Delete',
        cancelLabel: 'Abort',
        variant: 'danger',
      });
    });
    
    expect(result.current.config?.confirmLabel).toBe('Delete');
    expect(result.current.config?.cancelLabel).toBe('Abort');
    expect(result.current.config?.variant).toBe('danger');
  });
});
