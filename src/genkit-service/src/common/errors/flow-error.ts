import type { Request, Response, NextFunction } from 'express';
import { logger } from '../../logger.js';

const NODE_ENV = process.env.NODE_ENV || 'development';

export function handleFlowError(error: unknown, res: Response, operation: string): void {
  if (error instanceof Error && error.name === 'ZodError') {
    logger.warn({ operation, error: (error as any).errors }, 'Validation error');
    res.status(400).json({
      error: 'Validation error',
      details: (error as any).errors,
    });
    return;
  }

  logger.error({ operation, error }, 'Flow execution error');
  res.status(500).json({
    error: 'Internal server error',
    message: NODE_ENV === 'development' && error instanceof Error ? error.message : 'An error occurred',
  });
}

export function errorHandler(err: Error, _req: Request, res: Response, _next: NextFunction): void {
  logger.error({ error: err }, 'Unhandled error');
  res.status(500).json({
    error: 'Internal server error',
    message: NODE_ENV === 'development' ? err.message : 'An unexpected error occurred',
  });
}

export function notFoundHandler(_req: Request, res: Response): void {
  res.status(404).json({ error: 'Not found' });
}
