/**
 * Confirm Dialog Component
 * Single Responsibility: Confirmation dialog for user actions
 * Replaces window.confirm() with accessible modal
 */

import React, { useEffect, useRef, useCallback } from 'react';
import { Button } from './Button';

type ConfirmVariant = 'danger' | 'warning' | 'info';

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: ConfirmVariant;
  onConfirm: () => void;
  onCancel: () => void;
  isLoading?: boolean;
}

const variantStyles: Record<ConfirmVariant, { icon: string; confirmVariant: 'primary' | 'danger' }> = {
  danger: { icon: 'warning', confirmVariant: 'danger' },
  warning: { icon: 'report_problem', confirmVariant: 'primary' },
  info: { icon: 'info', confirmVariant: 'primary' },
};

export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  variant = 'info',
  onConfirm,
  onCancel,
  isLoading = false,
}) => {
  const modalRef = useRef<HTMLDivElement>(null);
  const styles = variantStyles[variant];

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape' && !isLoading) {
      onCancel();
    }
  }, [onCancel, isLoading]);

  useEffect(() => {
    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
      modalRef.current?.focus();
      document.body.style.overflow = 'hidden';
    }
    
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
    };
  }, [isOpen, handleKeyDown]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-message"
    >
      <div
        ref={modalRef}
        className="w-full max-w-md max-h-[90vh] bg-surface-dark border border-primary shadow-2xl animate-glitch-in flex flex-col focus:outline-none"
        tabIndex={-1}
      >
        <div className="bg-primary text-black font-bold p-3 flex justify-between items-center flex-shrink-0">
          <h2
            id="confirm-dialog-title"
            className="text-xs uppercase tracking-widest flex items-center gap-2"
          >
            <span className={`material-icons text-sm ${variant === 'danger' ? 'text-red-800' : ''}`}>
              {styles.icon}
            </span>
            {title}
          </h2>
          {!isLoading && (
            <button
              onClick={onCancel}
              className="material-icons text-sm hover:rotate-90 transition-transform"
              aria-label="Cerrar"
            >
              close
            </button>
          )}
        </div>

        <div className="p-6 font-mono flex-1">
          <p
            id="confirm-dialog-message"
            className="text-primary/70 text-sm"
          >
            {message}
          </p>
        </div>

        <div className="p-4 border-t border-primary/20 flex justify-end gap-3 flex-shrink-0">
          <Button
            variant="ghost"
            size="sm"
            onClick={onCancel}
            disabled={isLoading}
          >
            {cancelLabel}
          </Button>
          <Button
            variant={styles.confirmVariant}
            size="sm"
            onClick={onConfirm}
            isLoading={isLoading}
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
};
