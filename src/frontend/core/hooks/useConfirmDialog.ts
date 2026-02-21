/**
 * useConfirmDialog Hook
 * Single Responsibility: Manage confirm dialog state
 * Replaces window.confirm() with promise-based dialog
 */

import { useState, useCallback } from 'react';

interface ConfirmConfig {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'warning' | 'info';
}

interface ConfirmDialogState extends ConfirmConfig {
  isOpen: boolean;
  resolvePromise: ((value: boolean) => void) | null;
}

interface UseConfirmDialogReturn {
  isOpen: boolean;
  config: Omit<ConfirmDialogState, 'isOpen' | 'resolvePromise'> | null;
  confirm: (config: ConfirmConfig) => Promise<boolean>;
  handleConfirm: () => void;
  handleCancel: () => void;
}

const initialState: ConfirmDialogState = {
  isOpen: false,
  title: '',
  message: '',
  confirmLabel: 'Confirmar',
  cancelLabel: 'Cancelar',
  variant: 'info',
  resolvePromise: null,
};

export function useConfirmDialog(): UseConfirmDialogReturn {
  const [state, setState] = useState<ConfirmDialogState>(initialState);

  const confirm = useCallback((config: ConfirmConfig): Promise<boolean> => {
    return new Promise((resolve) => {
      setState({
        ...config,
        confirmLabel: config.confirmLabel ?? 'Confirmar',
        cancelLabel: config.cancelLabel ?? 'Cancelar',
        variant: config.variant ?? 'info',
        isOpen: true,
        resolvePromise: resolve,
      });
    });
  }, []);

  const handleConfirm = useCallback(() => {
    state.resolvePromise?.(true);
    setState(initialState);
  }, [state.resolvePromise]);

  const handleCancel = useCallback(() => {
    state.resolvePromise?.(false);
    setState(initialState);
  }, [state.resolvePromise]);

  const { isOpen, resolvePromise, ...config } = state;

  return {
    isOpen,
    config: isOpen ? config : null,
    confirm,
    handleConfirm,
    handleCancel,
  };
}
