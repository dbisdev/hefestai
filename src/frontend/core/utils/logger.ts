/**
 * Application logger utility.
 *
 * Wraps the browser console to provide environment-aware logging:
 * - In **production** (`import.meta.env.PROD`): suppresses `log` and `debug` calls
 *   to avoid leaking internal state; `warn` and `error` are always emitted.
 * - In **development**: all levels are forwarded to the native console.
 *
 * Usage:
 * ```ts
 * import { logger } from '@/core/utils';
 * logger.error('[MyComponent] Something went wrong:', err);
 * logger.warn('Blocked dangerous URL:', url);
 * logger.log('[Debug] Value:', value); // suppressed in production
 * ```
 */

const isProd = import.meta.env.PROD;

/* eslint-disable no-console */
export const logger = {
  /**
   * Logs a debug/informational message.
   * Suppressed in production builds.
   */
  log: (...args: unknown[]): void => {
    if (!isProd) console.log(...args);
  },

  /**
   * Logs a debug message (alias for log).
   * Suppressed in production builds.
   */
  debug: (...args: unknown[]): void => {
    if (!isProd) console.debug(...args);
  },

  /**
   * Logs a warning. Always emitted (useful for security/degradation notices).
   */
  warn: (...args: unknown[]): void => {
    console.warn(...args);
  },

  /**
   * Logs an error. Always emitted.
   */
  error: (...args: unknown[]): void => {
    console.error(...args);
  },
} as const;
