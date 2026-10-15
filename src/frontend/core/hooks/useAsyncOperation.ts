/**
 * useAsyncOperation
 *
 * A lightweight hook that encapsulates the repeating loading/error/finally
 * scaffold found across all async hooks:
 *
 *   setLoading(true)
 *   setError(null)
 *   try { ... }
 *   catch (err) { setError(extractErrorMessage(err)) }
 *   finally { setLoading(false) }
 *
 * Usage:
 *   const { isLoading, error, run } = useAsyncOperation();
 *   const result = await run(() => someService.call(params), 'Fallback error message');
 *
 * The caller keeps full control of what happens on success — there is
 * intentionally no success callback here (that belongs in the calling hook).
 */

import { useState, useCallback } from 'react';
import { extractErrorMessage } from '@core/utils/error';

export interface UseAsyncOperationReturn {
  isLoading: boolean;
  error: string | null;
  clearError: () => void;
  /**
   * Executes `fn` wrapped in the standard loading/error/finally scaffold.
   * Returns the result on success, `null` on error.
   */
  run: <T>(fn: () => Promise<T>, errorFallback?: string) => Promise<T | null>;
}

/**
 * Provides a `run` helper that wraps any async function in the standard
 * loading → try/catch → finally scaffold, keeping the calling hook DRY.
 */
export function useAsyncOperation(): UseAsyncOperationReturn {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const clearError = useCallback(() => setError(null), []);

  const run = useCallback(async <T>(
    fn: () => Promise<T>,
    errorFallback = 'An error occurred',
  ): Promise<T | null> => {
    setIsLoading(true);
    setError(null);

    try {
      return await fn();
    } catch (err) {
      setError(extractErrorMessage(err, errorFallback));
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { isLoading, error, clearError, run };
}
