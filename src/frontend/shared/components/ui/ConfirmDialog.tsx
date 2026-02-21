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
        className="bg-surface-dark border border-primary/30 rounded-lg max-w-md w-full p-6 relative"
        tabIndex={-1}
      >
        <div className="absolute top-0 left-0 w-4 h-4 border-t-2 border-l-2 border-primary rounded-tl-lg" />
        <div className="absolute top-0 right-0 w-4 h-4 border-t-2 border-r-2 border-primary rounded-tr-lg" />
        <div className="absolute bottom-0 left-0 w-4 h-4 border-b-2 border-l-2 border-primary rounded-bl-lg" />
        <div className="absolute bottom-0 right-0 w-4 h-4 border-b-2 border-r-2 border-primary rounded-br-lg" />

        <div className="flex items-start gap-4">
          <span className={`material-icons text-3xl ${variant === 'danger' ? 'text-danger' : 'text-primary'}`}>
            {styles.icon}
          </span>
          <div className="flex-1">
            <h2 
              id="confirm-dialog-title"
              className="text-primary text-lg font-display uppercase tracking-wider mb-2"
            >
              {title}
            </h2>
            <p 
              id="confirm-dialog-message"
              className="text-primary/70 text-sm mb-6"
            >
              {message}
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-3">
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
